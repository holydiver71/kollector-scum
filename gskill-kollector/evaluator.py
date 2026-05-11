"""
evaluator.py — GEPA-compatible evaluator for kollector-scum gskill pipeline.

Scores a skill text by:
1. Loading all validated tasks from tasks/
2. For each task: prompting the coding agent (LLM) with skill + problem_statement
3. Applying the agent's proposed patch
4. Running eval.sh to check if tests pass
5. Returning fraction of tasks passed as score
"""

from __future__ import annotations

import concurrent.futures
import json
import os
import shutil
import subprocess
import tempfile
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from openai import OpenAI  # noqa: F401
from _llm_client import make_client


_config = {
    "tasks_dir": Path("tasks"),
    "repo_dir": Path(".."),
    "eval_script": Path("swesmith_config/eval.sh"),
    "model": "gpt-4o",
    "max_workers": 4,
}


@dataclass(slots=True)
class TaskResult:
    task_id: str
    passed: bool
    agent_patch: str
    error: str | None


def _task_problem_statement(task: dict[str, Any]) -> str:
    return (
        task.get("problem_statement")
        or task.get("issue_text")
        or task.get("bug_description")
        or "Fix the bug in this task and return a unified diff patch."
    )


def _task_test_cmd(task: dict[str, Any]) -> str:
    return task.get("test_cmd") or task.get("test_command") or ""


def run_agent_on_task(task: dict[str, Any], skill_text: str, model: str = "gpt-4o") -> str:
    """
    Prompt the coding model with current skill + task problem statement.
    Returns raw model output (expected to be a unified diff patch).
    """
    client = make_client()
    problem_statement = _task_problem_statement(task)
    system_prompt = (
        f"{skill_text.rstrip()}\n\n"
        "Return ONLY a unified diff patch. No explanation."
    )

    response = client.chat.completions.create(
        model=model,
        messages=[
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": problem_statement},
        ],
        temperature=0.2,
    )

    return (response.choices[0].message.content or "").strip()


def apply_and_test(repo_dir: Path, patch_text: str, test_cmd: str, eval_script: Path) -> bool:
    """
    Apply agent patch in an isolated repo copy and run eval.sh.
    Returns True when eval.sh exits 0 (tests pass after fix).
    """
    if not patch_text.strip():
        return False

    with tempfile.TemporaryDirectory(prefix="kollector_agent_eval_") as tmp_dir:
        tmp_root = Path(tmp_dir)
        tmp_repo = tmp_root / "repo"
        patch_file = tmp_root / "agent.patch"

        shutil.copytree(
            repo_dir,
            tmp_repo,
            symlinks=True,
            ignore=shutil.ignore_patterns(
                ".git",
                "node_modules",
                "bin",
                "obj",
                "__pycache__",
            ),
        )
        shutil.copytree(repo_dir / ".git", tmp_repo / ".git", symlinks=True)
        patch_file.write_text(patch_text, encoding="utf-8")

        env = os.environ.copy()
        env["REPO_DIR"] = str(tmp_repo)
        env["PATCH_FILE"] = str(patch_file)
        env["TEST_CMD"] = test_cmd

        proc = subprocess.run(
            ["bash", str(eval_script.resolve())],
            env=env,
            capture_output=True,
            text=True,
            timeout=120,
        )
        return proc.returncode == 0


def _evaluate_single_task(
    task_path: Path,
    skill_text: str,
    repo_dir: Path,
    eval_script: Path,
    model: str,
) -> TaskResult:
    task_id = task_path.stem
    try:
        task_data: dict[str, Any] = json.loads(task_path.read_text(encoding="utf-8"))
    except Exception as exc:  # noqa: BLE001
        return TaskResult(task_id=task_id, passed=False, agent_patch="", error=f"JSON parse error: {exc}")

    task_id = str(task_data.get("task_id") or task_data.get("id") or task_id)
    test_cmd = _task_test_cmd(task_data)

    try:
        agent_patch = run_agent_on_task(task_data, skill_text, model=model)
    except Exception as exc:  # noqa: BLE001
        return TaskResult(task_id=task_id, passed=False, agent_patch="", error=f"Agent call failed: {exc}")

    try:
        passed = apply_and_test(repo_dir, agent_patch, test_cmd, eval_script)
    except subprocess.TimeoutExpired:
        return TaskResult(task_id=task_id, passed=False, agent_patch=agent_patch, error="eval.sh timeout (120s)")
    except Exception as exc:  # noqa: BLE001
        return TaskResult(task_id=task_id, passed=False, agent_patch=agent_patch, error=f"eval failed: {exc}")

    return TaskResult(task_id=task_id, passed=passed, agent_patch=agent_patch, error=None if passed else "tests failed")


def evaluate_skill(
    skill_text: str,
    tasks_dir: Path,
    repo_dir: Path,
    eval_script: Path,
    model: str,
    max_workers: int = 4,
) -> tuple[float, list[TaskResult]]:
    """
    Score a skill text by running the agent on all tasks.
    Returns (score, results), where score is in [0.0, 1.0].
    """
    tasks_dir = tasks_dir.resolve()
    repo_dir = repo_dir.resolve()
    eval_script = eval_script.resolve()

    task_files = sorted(tasks_dir.glob("*.json"))
    if not task_files:
        return 0.0, []

    results: list[TaskResult] = []
    with concurrent.futures.ThreadPoolExecutor(max_workers=max_workers) as executor:
        futures = [
            executor.submit(
                _evaluate_single_task,
                task_path,
                skill_text,
                repo_dir,
                eval_script,
                model,
            )
            for task_path in task_files
        ]
        for future in concurrent.futures.as_completed(futures):
            results.append(future.result())

    total = len(results)
    passed = sum(1 for result in results if result.passed)
    score = passed / total if total > 0 else 0.0
    results.sort(key=lambda item: item.task_id)
    return score, results


def evaluate_skill_fn(skill_text: str) -> float:
    """
    Thin GEPA adapter: skill_text -> float score.
    Uses module-level evaluator config populated by pipeline.py.
    """
    score, _ = evaluate_skill(
        skill_text=skill_text,
        tasks_dir=Path(_config["tasks_dir"]),
        repo_dir=Path(_config["repo_dir"]),
        eval_script=Path(_config["eval_script"]),
        model=str(_config["model"]),
        max_workers=int(_config["max_workers"]),
    )
    return score
