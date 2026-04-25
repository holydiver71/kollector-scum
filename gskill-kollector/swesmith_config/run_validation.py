"""
run_validation.py — Validates SWE-smith tasks by checking bug patches break tests.

For each task in tasks/, applies the patch and runs eval.sh.
Tasks where tests FAIL are marked VALID (bug is real and detectable).

Usage:
    python run_validation.py \\
        --tasks-dir tasks/ \\
        --repo-dir .. \\
        --eval-script swesmith_config/eval.sh \\
        --max-workers 4 \\
        --output validated_tasks.json
"""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import tempfile
import concurrent.futures
from pathlib import Path
from typing import Any

import typer

app = typer.Typer(add_completion=False, pretty_exceptions_enable=False)


# ---------------------------------------------------------------------------
# Validation logic for a single task
# ---------------------------------------------------------------------------

def _validate_task(
    task_path: Path,
    repo_dir: Path,
    eval_script: Path,
) -> dict[str, Any]:
    """
    Validate a single SWE-smith task.

    1. Load the task JSON.
    2. Clone the repo into a temp directory.
    3. Apply the bug patch.
    4. Run eval.sh.
    5. Return a result dict with VALID/INVALID status.

    A task is VALID when eval.sh exits non-zero (tests FAIL), confirming the
    bug is real and detectable by the test suite.
    """
    task_id = task_path.stem
    result: dict[str, Any] = {
        "task_id": task_id,
        "task_file": str(task_path),
        "status": "INVALID",
        "exit_code": None,
        "error": None,
    }

    # ------------------------------------------------------------------
    # Load task JSON
    # ------------------------------------------------------------------
    try:
        task_data: dict[str, Any] = json.loads(task_path.read_text())
    except Exception as exc:  # noqa: BLE001
        result["error"] = f"Failed to parse task JSON: {exc}"
        return result

    patch_text: str | None = task_data.get("patch") or task_data.get("bug_patch")
    test_cmd: str = task_data.get("test_cmd", "")

    if not patch_text:
        result["error"] = "No 'patch' or 'bug_patch' field found in task JSON."
        return result

    # ------------------------------------------------------------------
    # Copy the repo into a temp directory to avoid polluting the original
    # ------------------------------------------------------------------
    with tempfile.TemporaryDirectory(prefix=f"kollector_eval_{task_id}_") as tmp_dir:
        tmp_repo = Path(tmp_dir) / "repo"
        try:
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
            # Copy .git separately (needed for git apply)
            shutil.copytree(repo_dir / ".git", tmp_repo / ".git", symlinks=True)
        except Exception as exc:  # noqa: BLE001
            result["error"] = f"Failed to copy repo: {exc}"
            return result

        # Write patch to a temp file inside the tmp dir
        patch_file = Path(tmp_dir) / "bug.patch"
        patch_file.write_text(patch_text)

        # ------------------------------------------------------------------
        # Run eval.sh
        # ------------------------------------------------------------------
        env = os.environ.copy()
        env["REPO_DIR"] = str(tmp_repo)
        env["PATCH_FILE"] = str(patch_file)
        if test_cmd:
            env["TEST_CMD"] = test_cmd

        try:
            proc = subprocess.run(
                ["bash", str(eval_script.resolve())],
                env=env,
                capture_output=True,
                text=True,
                timeout=600,  # 10 minutes max per task
            )
            exit_code = proc.returncode
        except subprocess.TimeoutExpired:
            result["error"] = "eval.sh timed out after 600 seconds."
            return result
        except Exception as exc:  # noqa: BLE001
            result["error"] = f"Failed to run eval.sh: {exc}"
            return result

        result["exit_code"] = exit_code
        result["stdout"] = proc.stdout[-4000:] if proc.stdout else ""
        result["stderr"] = proc.stderr[-2000:] if proc.stderr else ""

        # A non-zero exit code means tests FAILED → bug confirmed → VALID
        if exit_code != 0:
            result["status"] = "VALID"
        else:
            result["status"] = "INVALID"

    return result


# ---------------------------------------------------------------------------
# CLI entry point
# ---------------------------------------------------------------------------

@app.command()
def main(
    tasks_dir: Path = typer.Option(Path("tasks"), "--tasks-dir", help="Directory containing task JSON files."),
    repo_dir: Path = typer.Option(Path(".."), "--repo-dir", help="Root of the kollector-scum repository."),
    eval_script: Path = typer.Option(
        Path("swesmith_config/eval.sh"),
        "--eval-script",
        help="Path to eval.sh (relative to CWD or absolute).",
    ),
    max_workers: int = typer.Option(4, "--max-workers", help="Number of parallel validation workers."),
    output_file: Path = typer.Option(Path("validated_tasks.json"), "--output", help="Output JSON file."),
) -> None:
    """
    Validate SWE-smith tasks by applying bug patches and checking that tests fail.
    """
    # Resolve paths
    tasks_dir = tasks_dir.resolve()
    repo_dir = repo_dir.resolve()
    eval_script = eval_script.resolve()

    if not tasks_dir.is_dir():
        typer.echo(f"ERROR: tasks_dir not found: {tasks_dir}", err=True)
        raise typer.Exit(1)

    if not repo_dir.is_dir():
        typer.echo(f"ERROR: repo_dir not found: {repo_dir}", err=True)
        raise typer.Exit(1)

    if not eval_script.is_file():
        typer.echo(f"ERROR: eval_script not found: {eval_script}", err=True)
        raise typer.Exit(1)

    task_files = sorted(tasks_dir.glob("*.json"))
    if not task_files:
        typer.echo(f"No task JSON files found in {tasks_dir}.")
        raise typer.Exit(0)

    typer.echo(f"Found {len(task_files)} task(s) in {tasks_dir}")
    typer.echo(f"Repo     : {repo_dir}")
    typer.echo(f"Eval     : {eval_script}")
    typer.echo(f"Workers  : {max_workers}")
    typer.echo("")

    results: list[dict[str, Any]] = []

    with concurrent.futures.ProcessPoolExecutor(max_workers=max_workers) as executor:
        future_to_task = {
            executor.submit(_validate_task, tf, repo_dir, eval_script): tf
            for tf in task_files
        }
        for future in concurrent.futures.as_completed(future_to_task):
            task_path = future_to_task[future]
            try:
                result = future.result()
            except Exception as exc:  # noqa: BLE001
                result = {
                    "task_id": task_path.stem,
                    "task_file": str(task_path),
                    "status": "ERROR",
                    "exit_code": None,
                    "error": str(exc),
                }

            results.append(result)
            status_icon = "✓" if result["status"] == "VALID" else "✗"
            typer.echo(
                f"  [{status_icon}] {result['task_id']:40s}  "
                f"status={result['status']:8s}  exit={result.get('exit_code', 'N/A')}"
            )

    # ------------------------------------------------------------------
    # Summary
    # ------------------------------------------------------------------
    valid_count = sum(1 for r in results if r["status"] == "VALID")
    invalid_count = sum(1 for r in results if r["status"] == "INVALID")
    error_count = sum(1 for r in results if r["status"] == "ERROR")

    typer.echo("")
    typer.echo("=== VALIDATION SUMMARY ===")
    typer.echo(f"  Total   : {len(results)}")
    typer.echo(f"  VALID   : {valid_count}  (bug confirmed — tests fail)")
    typer.echo(f"  INVALID : {invalid_count}  (bug not detected — tests pass)")
    typer.echo(f"  ERROR   : {error_count}  (harness error)")
    typer.echo("==========================")

    # ------------------------------------------------------------------
    # Write output
    # ------------------------------------------------------------------
    output_file = output_file.resolve()
    output_file.write_text(json.dumps(results, indent=2))
    typer.echo(f"\nResults written to: {output_file}")

    # Exit non-zero if any task errored (not just invalid)
    if error_count > 0:
        raise typer.Exit(2)


if __name__ == "__main__":
    app()
