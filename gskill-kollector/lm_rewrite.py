"""
lm_rewrite.py — SWE-smith LM Rewrite strategy for kollector-scum.

Uses an LLM to rewrite entire methods with introduced bugs,
generating more complex SWE-bench tasks than simple line-level modifications.
"""

import json
import uuid
import difflib
import subprocess
from pathlib import Path
from datetime import datetime
import typer
from openai import OpenAI

REWRITE_SYSTEM_PROMPT = """You are a software engineer introducing a complex, realistic bug into a .NET 8 C# codebase.

The codebase is a multi-tenant music catalog SaaS called kollector-scum.
CRITICAL ARCHITECTURE RULE: Every user-owned entity has a UserId: Guid field.
GenericCrudService auto-scopes ALL queries by the current user's UserId.
Lookup tables (Artist, Genre, Label, Format, Country, Packaging, Store) are per-user with composite unique constraint (UserId, Name).

Your task is to REWRITE an entire method to introduce a subtle but realistic bug.

Steps:
1. Identify the BEST method to target (prefer methods that query user data, validate input, or map DTOs).
2. Rewrite the ENTIRE method body with a complex bug, e.g.:
   - Remove UserId filtering but keep all other logic intact (drop the user scoping check)
   - Swap two similar-looking variable names in DTO mapping
   - Change LINQ .Where() to incorrect predicate (e.g., remove a filter condition)
   - Return wrong status code or type in an error branch
   - Incorrectly combine filters using OR instead of AND
3. The rewrite must be syntactically valid C# that compiles.
4. The bug must cause at least one existing test to fail.
5. Make the bug span multiple lines (not just one single-line change).

Return a JSON object:
{
  "method_name": "full method signature including return type and parameters",
  "original_method": "original complete method text (entire method body and signature)",
  "rewritten_method": "rewritten complete method text (entire method body and signature with the bug)",
  "bug_description": "one paragraph describing the bug, what it breaks, and what tests would catch it"
}"""

app = typer.Typer()


def replace_method_in_source(source: str, original: str, rewritten: str) -> str:
    """Replace original method text with rewritten version in source code.
    
    Args:
        source: The full source file content
        original: The original method text to find and replace
        rewritten: The rewritten method text with the bug
        
    Returns:
        The modified source code with the method replaced
    """
    if original not in source:
        raise ValueError(f"Original method not found in source file:\n{original[:200]}...")
    
    return source.replace(original, rewritten)


def generate_unified_diff(original_path: str, original_content: str, modified_content: str) -> str:
    """Generate a unified diff between original and modified file content.
    
    Args:
        original_path: Path to the original file
        original_content: Original file content
        modified_content: Modified file content
        
    Returns:
        Unified diff as a string
    """
    original_lines = original_content.splitlines(keepends=True)
    modified_lines = modified_content.splitlines(keepends=True)
    
    diff = difflib.unified_diff(
        original_lines,
        modified_lines,
        fromfile=original_path,
        tofile=original_path,
        lineterm=""
    )
    
    return "\n".join(diff)


def get_test_filter_for_file(source_file_path: str) -> str:
    """Determine which test project to run based on the source file location.
    
    Args:
        source_file_path: Path to the source file
        
    Returns:
        Test filter string for dotnet test
    """
    path_str = str(source_file_path).lower()
    
    if "services" in path_str:
        return "KollectorScum.Tests.Services"
    elif "controllers" in path_str:
        return "KollectorScum.Tests.Controllers"
    else:
        return ""  # Use default (all tests)


@app.command()
def main(
    source_file: Path = typer.Argument(
        ...,
        help="Path to the C# source file to rewrite"
    ),
    target_method: str = typer.Option(
        None,
        "--target-method",
        help="Optional: specific method to rewrite. If not provided, LLM will select the best method."
    ),
    output_dir: Path = typer.Option(
        Path("tasks"),
        "--output-dir",
        help="Output directory for generated task JSON files"
    ),
    model: str = typer.Option(
        "gpt-4o",
        "--model",
        help="LLM model to use for bug generation"
    ),
):
    """Generate a complex bug by rewriting an entire C# method using an LLM.
    
    The LLM will analyze the source file and rewrite a method to introduce
    a realistic, multi-line bug that should cause test failures.
    """
    
    # Validate inputs
    if not source_file.exists():
        typer.echo(f"Error: Source file not found: {source_file}", err=True)
        raise typer.Exit(1)
    
    if not source_file.suffix.lower() == ".cs":
        typer.echo(f"Error: Expected .cs file, got {source_file.suffix}", err=True)
        raise typer.Exit(1)
    
    output_dir.mkdir(parents=True, exist_ok=True)
    
    # Read the source file
    original_content = source_file.read_text(encoding="utf-8")
    
    typer.echo(f"📖 Read source file: {source_file}")
    typer.echo(f"   Lines: {len(original_content.splitlines())}")
    
    # Prepare the LLM prompt
    user_prompt = f"""Here is the C# source file to analyze:

FILE: {source_file.name}
```csharp
{original_content}
```

{f'Target method: {target_method}' if target_method else 'Find the best method to rewrite with a complex bug.'}

Rewrite the method to introduce a subtle, realistic multi-line bug that will cause test failures.
Return ONLY valid JSON with no additional text."""

    typer.echo(f"🤖 Calling {model} to generate rewrite...")
    
    # Call the LLM
    client = OpenAI()
    response = client.chat.completions.create(
        model=model,
        messages=[
            {
                "role": "system",
                "content": REWRITE_SYSTEM_PROMPT
            },
            {
                "role": "user",
                "content": user_prompt
            }
        ],
        temperature=1.0,  # Higher temperature for more creative bug generation
        top_p=1.0
    )
    
    # Parse the response
    response_text = response.choices[0].message.content.strip()
    
    # Try to extract JSON if it's wrapped in markdown code blocks
    if response_text.startswith("```"):
        # Extract JSON from markdown code block
        json_start = response_text.find("{")
        json_end = response_text.rfind("}") + 1
        if json_start >= 0 and json_end > json_start:
            response_text = response_text[json_start:json_end]
    
    try:
        rewrite_result = json.loads(response_text)
    except json.JSONDecodeError as e:
        typer.echo(f"Error: Failed to parse LLM response as JSON: {e}", err=True)
        typer.echo(f"Response: {response_text[:500]}", err=True)
        raise typer.Exit(1)
    
    # Validate the response
    required_fields = {"method_name", "original_method", "rewritten_method", "bug_description"}
    if not required_fields.issubset(rewrite_result.keys()):
        typer.echo(f"Error: LLM response missing fields. Got: {rewrite_result.keys()}", err=True)
        raise typer.Exit(1)
    
    typer.echo(f"✅ LLM response received")
    typer.echo(f"   Method: {rewrite_result['method_name']}")
    typer.echo(f"   Bug: {rewrite_result['bug_description'][:100]}...")
    
    # Replace the method in the source
    try:
        modified_content = replace_method_in_source(
            original_content,
            rewrite_result["original_method"],
            rewrite_result["rewritten_method"]
        )
    except ValueError as e:
        typer.echo(f"Error: {e}", err=True)
        raise typer.Exit(1)
    
    typer.echo(f"🔄 Method replaced in source")
    
    # Generate unified diff
    diff = generate_unified_diff(str(source_file), original_content, modified_content)
    
    # Determine which tests to run
    test_filter = get_test_filter_for_file(source_file)
    
    # Create the task record
    task_id = str(uuid.uuid4())[:8]
    task_record = {
        "id": task_id,
        "timestamp": datetime.now().isoformat(),
        "strategy": "lm-rewrite",
        "source_file": str(source_file),
        "method_name": rewrite_result["method_name"],
        "bug_description": rewrite_result["bug_description"],
        "original_method": rewrite_result["original_method"],
        "rewritten_method": rewrite_result["rewritten_method"],
        "diff": diff,
        "model": model,
        "test_filter": test_filter,
        "original_content": original_content,
        "modified_content": modified_content
    }
    
    # Save the task JSON
    task_file = output_dir / f"task_lm_rewrite_{task_id}.json"
    task_file.write_text(json.dumps(task_record, indent=2), encoding="utf-8")
    
    typer.echo(f"💾 Task saved: {task_file}")
    typer.echo(f"\n📋 Task Summary:")
    typer.echo(f"   ID: {task_id}")
    typer.echo(f"   Strategy: lm-rewrite")
    typer.echo(f"   File: {source_file.name}")
    typer.echo(f"   Method: {rewrite_result['method_name']}")
    typer.echo(f"   Bug: {rewrite_result['bug_description'][:80]}...")
    typer.echo(f"   Test filter: {test_filter or 'all tests'}")
    typer.echo(f"\n✨ Rewrite complete!")


if __name__ == "__main__":
    app()
