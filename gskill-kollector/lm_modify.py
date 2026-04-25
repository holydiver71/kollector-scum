"""
lm_modify.py — SWE-smith LM Modify strategy for kollector-scum.

Uses an LLM to introduce realistic bugs into C# source files,
generating unified diff patches for use as SWE-bench tasks.
"""
import json
import uuid
import difflib
import typer
from pathlib import Path
from openai import OpenAI


MODIFY_SYSTEM_PROMPT = """You are a software engineer introducing a subtle, realistic bug into a .NET 8 C# codebase.

The codebase is a multi-tenant music catalog SaaS called kollector-scum.
CRITICAL ARCHITECTURE RULE: Every user-owned entity has a UserId: Guid field.
GenericCrudService auto-scopes ALL queries by the current user's UserId.

Introduce ONE subtle bug from this priority list:
1. Remove or bypass UserId scoping (most critical — causes cross-tenant data leakage)
2. Incorrect DTO field mapping (swap two fields, or omit one)
3. Off-by-one in pagination (e.g., skip/take math error)
4. Missing null check on a nullable field
5. Wrong HTTP status code on error (200 instead of 404, etc.)
6. Incorrect FluentValidation rule (wrong max length or missing required)

Rules:
- The bug MUST compile (syntactically valid C#)
- The bug MUST cause at least one existing unit test to fail
- The bug should be subtle enough that a human reviewer might miss it
- Make EXACTLY ONE change (one logical bug)
- Return a JSON object: {"modified_content": "...", "bug_description": "one sentence"}"""


app = typer.Typer()


def get_test_command(source_file: str) -> str:
    """Determine the appropriate test command for a source file."""
    if "Services/" in source_file:
        return "dotnet test backend/KollectorScum.Tests --filter \"KollectorScum.Tests.Services\""
    elif "Controllers/" in source_file:
        return "dotnet test backend/KollectorScum.Tests --filter \"KollectorScum.Tests.Controllers\""
    else:
        return "dotnet test backend/KollectorScum.Tests"


def generate_unified_diff(original: str, modified: str) -> str:
    """Generate a unified diff between original and modified content."""
    original_lines = original.splitlines(keepends=True)
    modified_lines = modified.splitlines(keepends=True)
    diff = difflib.unified_diff(
        original_lines,
        modified_lines,
        fromfile="original",
        tofile="modified",
        lineterm=''
    )
    return ''.join(diff)


def call_llm_to_generate_bug(
    source_content: str,
    model: str,
) -> tuple[str, str]:
    """
    Call the LLM to generate a modified version with a bug.
    
    Returns:
        (modified_content, bug_description)
    """
    client = OpenAI()
    
    user_message = f"""Below is a C# source file from the kollector-scum multi-tenant SaaS codebase.
Introduce ONE realistic, subtle bug using the priority list in the system prompt.
The bug MUST compile and MUST cause at least one existing unit test to fail.

Source file:
```csharp
{source_content}
```

Return ONLY a valid JSON object with two fields:
- "modified_content": The complete modified file content (as a string)
- "bug_description": A one-sentence description of the bug introduced

Do not wrap the JSON in markdown code blocks."""
    
    response = client.chat.completions.create(
        model=model,
        messages=[
            {"role": "system", "content": MODIFY_SYSTEM_PROMPT},
            {"role": "user", "content": user_message}
        ],
        temperature=0.7,
        max_tokens=4000,
    )
    
    response_text = response.choices[0].message.content.strip()
    
    # Parse JSON response
    result = json.loads(response_text)
    return result["modified_content"], result["bug_description"]


@app.command()
def main(
    source_file: Path = typer.Argument(..., help="Path to the C# source file"),
    output_dir: Path = typer.Option(Path("tasks"), "--output-dir", help="Output directory for task files"),
    num_bugs: int = typer.Option(3, "--num-bugs", help="Number of bugs to generate"),
    model: str = typer.Option("gpt-4o", "--model", help="LLM model to use"),
):
    """
    Generate realistic bugs in a C# source file using an LLM.
    
    Produces unified diff patches and task metadata for SWE-smith bug corpus.
    """
    # Resolve paths
    source_file = source_file.resolve()
    output_dir = output_dir.resolve()
    
    if not source_file.exists():
        typer.echo(f"Error: Source file not found: {source_file}", err=True)
        raise typer.Exit(1)
    
    output_dir.mkdir(parents=True, exist_ok=True)
    
    # Read source file
    original_content = source_file.read_text(encoding="utf-8")
    
    # Extract repo name and file stem
    repo_name = source_file.parts[-5]  # e.g., "kollector-scum"
    file_stem = source_file.stem  # e.g., "ArtistService"
    
    # Get test command
    test_command = get_test_command(str(source_file))
    
    # Compute relative path
    relative_path = str(source_file.relative_to(source_file.parents[5]))
    
    tasks_created = []
    
    for i in range(num_bugs):
        typer.echo(f"Generating bug {i+1}/{num_bugs}...")
        
        try:
            # Call LLM to generate bug
            modified_content, bug_description = call_llm_to_generate_bug(
                original_content,
                model
            )
            
            # Generate unified diff
            patch = generate_unified_diff(original_content, modified_content)
            
            # Create task ID
            task_id = f"{repo_name}__{file_stem}__{uuid.uuid4().hex[:8]}"
            
            # Build task object
            task = {
                "task_id": task_id,
                "patch": patch,
                "bug_description": bug_description,
                "source_file": relative_path,
                "test_command": test_command,
            }
            
            # Save task file
            task_file = output_dir / f"{task_id}.json"
            task_file.write_text(json.dumps(task, indent=2), encoding="utf-8")
            
            tasks_created.append(task_id)
            typer.echo(f"  ✓ Created {task_id}")
            
        except json.JSONDecodeError as e:
            typer.echo(f"  ✗ Failed to parse LLM response: {e}", err=True)
        except Exception as e:
            typer.echo(f"  ✗ Error generating bug: {e}", err=True)
    
    # Print summary
    typer.echo("")
    typer.echo(f"Generated {len(tasks_created)} tasks:")
    for task_id in tasks_created:
        typer.echo(f"  - {task_id}")


if __name__ == "__main__":
    app()
