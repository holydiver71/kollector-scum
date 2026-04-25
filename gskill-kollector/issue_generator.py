"""
issue_generator.py — Generates GitHub issue text for SWE-smith tasks.

Converts raw bug patches + descriptions into realistic problem_statement
text that AI coding agents receive as task prompts.
"""

import json
import typer
from pathlib import Path
from openai import OpenAI

ISSUE_SYSTEM_PROMPT = """You are a GitHub issue author describing a bug in a .NET 8 C# multi-tenant music catalog SaaS called kollector-scum.

You will be given:
- A unified diff showing a code change that introduced a bug
- A one-sentence bug description

Write a realistic GitHub issue body (problem_statement) that:
1. Describes the SYMPTOMS of the bug (what a user would observe), NOT the root cause
2. Includes a minimal reproduction scenario where applicable
3. Uses realistic language (not overly technical, as if filed by an engineer or QA)
4. Does NOT reveal the exact code change that caused it
5. Mentions relevant context (e.g., "when listing releases in the collection", "when searching artists")
6. Is 3-5 paragraphs long

Important for multi-tenancy bugs:
- If the bug is about data leakage between users, describe it as "unexpected data appearing in another user's collection" or similar
- Never say "the UserId filter was removed" — describe only what the user would see

Return plain markdown text (no JSON wrapping)."""

app = typer.Typer()


@app.command()
def main(
    tasks_dir: Path = typer.Argument(Path("tasks"), help="Directory of task JSON files"),
    output_dir: Path = typer.Option(Path("tasks"), "--output-dir", help="Where to write enriched task JSONs"),
    model: str = typer.Option("gpt-4o", "--model", help="LLM model to use"),
):
    """Generate GitHub issue text for bug tasks."""
    
    tasks_dir = tasks_dir.resolve()
    output_dir = output_dir.resolve()
    
    if not tasks_dir.exists():
        typer.echo(f"❌ tasks_dir does not exist: {tasks_dir}", err=True)
        raise typer.Exit(1)
    
    output_dir.mkdir(parents=True, exist_ok=True)
    
    # Find all task JSON files
    task_files = sorted(tasks_dir.glob("*.json"))
    
    if not task_files:
        typer.echo(f"⚠️  No .json files found in {tasks_dir}")
        raise typer.Exit(0)
    
    typer.echo(f"📋 Found {len(task_files)} task file(s)")
    typer.echo(f"🤖 Using model: {model}")
    typer.echo()
    
    client = OpenAI()
    processed = 0
    failed = 0
    
    for task_file in task_files:
        try:
            with open(task_file) as f:
                task = json.load(f)
            
            task_id = task.get("task_id", task_file.stem)
            patch = task.get("patch", "")
            bug_description = task.get("bug_description", "")
            
            if not patch or not bug_description:
                typer.echo(f"⏭️  Skipping {task_id}: missing patch or bug_description")
                continue
            
            # Skip if problem_statement already exists and is non-empty
            if task.get("problem_statement", "").strip():
                typer.echo(f"⏭️  Already generated: {task_id}")
                processed += 1
                continue
            
            typer.echo(f"🔄 Processing {task_id}...", nl=False)
            
            # Call LLM to generate issue text
            user_prompt = f"""Here is a bug patch and description:

**Patch:**
```diff
{patch}
```

**Bug Description:**
{bug_description}

Generate a realistic GitHub issue body describing the symptoms a user would observe."""
            
            response = client.chat.completions.create(
                model=model,
                messages=[
                    {"role": "system", "content": ISSUE_SYSTEM_PROMPT},
                    {"role": "user", "content": user_prompt},
                ],
                temperature=0.7,
            )
            
            problem_statement = response.choices[0].message.content.strip()
            
            # Enrich task with problem_statement
            task["problem_statement"] = problem_statement
            
            # Write enriched task JSON
            output_file = output_dir / task_file.name
            with open(output_file, "w") as f:
                json.dump(task, f, indent=2)
            
            typer.echo(f" ✅ ({len(problem_statement)} chars)")
            processed += 1
            
        except Exception as e:
            typer.echo(f" ❌ Error: {e}", err=True)
            failed += 1
    
    typer.echo()
    typer.echo(f"📊 Summary: {processed} processed, {failed} failed")
    
    if failed > 0:
        raise typer.Exit(1)


if __name__ == "__main__":
    app()
