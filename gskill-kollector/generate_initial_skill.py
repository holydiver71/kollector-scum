"""Generate initial skill files for the kollector-scum repository.

This script performs a focused static analysis pass over the repository,
packages the most important source material into a prompt, asks a model via the
GitHub Models API (Copilot Pro) to draft repository-specific agent guidance, and writes two outputs:

* ``.claude/skills/kollector-scum/SKILL.md``
* ``.github/copilot-instructions.md``

The generated markdown body is shared between both files.  The Claude skill
adds YAML frontmatter while the Copilot instructions file adds only the
required header comment and title.
"""

from __future__ import annotations

import os
import re
import textwrap
from pathlib import Path
from typing import Iterable

import typer
from openai import OpenAI  # noqa: F401
from _llm_client import make_client


app = typer.Typer(
    add_completion=False,
    help="Generate initial Claude/Copilot skill files from repository analysis.",
)


CLAUDE_SKILL_PATH = Path(".claude/skills/kollector-scum/SKILL.md")
COPILOT_INSTRUCTIONS_PATH = Path(".github/copilot-instructions.md")

DEFAULT_MODEL = "gpt-4o"

# These are the files explicitly called out in the Phase 6 instructions plus a
# few nearby files that materially affect the agent guidance an AI should learn.
KEY_SOURCE_FILES = [
    "CLAUDE.md",
    "backend/KollectorScum.Api/Services/GenericCrudService.cs",
    "backend/KollectorScum.Api/Models/IUserOwnedEntity.cs",
    "backend/KollectorScum.Api/Models/INamedUserOwnedEntity.cs",
    "backend/KollectorScum.Api/Interfaces/IUserContext.cs",
    "backend/KollectorScum.Api/Services/UserContext.cs",
    "backend/KollectorScum.Api/Data/KollectorScumDbContext.cs",
    "backend/KollectorScum.Api/Extensions/ServiceCollectionExtensions.cs",
    "backend/KollectorScum.Api/Models/MusicRelease.cs",
    "backend/KollectorScum.Api/Services/MusicReleaseCommandService.cs",
    "backend/KollectorScum.Api/Services/MusicReleaseQueryService.cs",
    "backend/KollectorScum.Api/Controllers/MusicReleasesController.cs",
    "backend/KollectorScum.Api/Models/Artist.cs",
    "backend/KollectorScum.Api/Services/ArtistService.cs",
    "backend/KollectorScum.Api/Controllers/ArtistsController.cs",
    "backend/KollectorScum.Api/Validators/CreateMusicReleaseDtoValidator.cs",
    "backend/KollectorScum.Api/Services/EntityResolverService.cs",
    "backend/KollectorScum.Api/Repositories/Repository.cs",
    "backend/KollectorScum.Api/Repositories/UnitOfWork.cs",
    "backend/KollectorScum.Tests/Services/MusicReleaseCommandServiceTests.cs",
    "backend/KollectorScum.Tests/Integration/CollectionStatisticsIntegrationTests.cs",
    "backend/KollectorScum.Tests/Integration/TestAuthHandler.cs",
    "frontend/app/lib/api.ts",
    "frontend/app/lib/auth.ts",
]


SYSTEM_PROMPT = """You are an expert repository analyst.

Your job is to write repository-specific AI agent instructions that teach an
LLM how to contribute safely and effectively to a codebase.

Return markdown BODY ONLY.
Do not include YAML frontmatter.
Do not wrap the answer in code fences.
Start with '## Repo Identity'.

Requirements:
- Be concrete and codebase-specific.
- Emphasize architecture, layering, multi-tenancy, tests, auth, and common pitfalls.
- Include actionable checklists.
- Mention important file paths.
- Prefer precise rules over generic advice.
- Call out exceptions where the codebase uses a special pattern.
- The result should be detailed enough to onboard an AI coding agent.
"""


def read_text(path: Path) -> str:
    """Read a UTF-8 text file from disk."""

    return path.read_text(encoding="utf-8")


def normalize_markdown_body(markdown: str) -> str:
    """Trim wrappers the model might add and normalize surrounding whitespace."""

    cleaned = markdown.strip()
    cleaned = re.sub(r"^```(?:markdown)?\s*", "", cleaned, flags=re.IGNORECASE)
    cleaned = re.sub(r"\s*```$", "", cleaned)
    return cleaned.strip() + "\n"


def truncate_for_prompt(text: str, max_chars: int) -> str:
    """Keep prompts bounded while preserving the most useful context."""

    if len(text) <= max_chars:
        return text
    suffix = "\n\n[...truncated for prompt length...]\n"
    return text[: max_chars - len(suffix)] + suffix


def iter_existing_paths(repo_root: Path, relative_paths: Iterable[str]) -> list[Path]:
    """Resolve and validate the source files used for analysis."""

    existing: list[Path] = []
    missing: list[str] = []

    for relative_path in relative_paths:
        path = repo_root / relative_path
        if path.exists():
            existing.append(path)
        else:
            missing.append(relative_path)

    if missing:
        formatted = "\n".join(f"- {item}" for item in missing)
        raise typer.BadParameter(
            f"Required analysis files are missing:\n{formatted}",
            param_hint="repo_root",
        )

    return existing


def build_user_prompt(repo_root: Path, source_paths: list[Path], max_chars_per_file: int) -> str:
    """Build the model prompt from the selected repository files."""

    sections: list[str] = []
    for path in source_paths:
        relative_path = path.relative_to(repo_root).as_posix()
        text = truncate_for_prompt(read_text(path), max_chars=max_chars_per_file)
        sections.append(f"## FILE: {relative_path}\n\n{text}")

    joined_sources = "\n\n".join(sections)

    return textwrap.dedent(
        f"""
        Generate initial AI coding-agent instructions for the kollector-scum repository.

        Output requirements:
        - Markdown body only.
        - Start with '## Repo Identity'.
        - Include these sections at minimum:
          1. Repo Identity
          2. Architecture and layer rules
          3. Multi-tenancy (highest priority)
          4. Adding a new entity (step-by-step checklist)
          5. Testing
          6. Common pitfalls
          7. Key files reference
        - Mention that MusicRelease is a special-case CQRS flow while lookup entities use GenericCrudService.
        - Explain that user-owned entities implement IUserOwnedEntity and lookups use (UserId, Name) uniqueness.
        - Mention auth and impersonation via IUserContext.
        - Mention the frontend API helper and JWT header behavior.
        - Mention the Discogs background job pattern.
        - Mention unit tests, integration tests, and frontend tests.
        - Be specific about file paths and conventions.

        Repository root: {repo_root}

        Source material follows.

        {joined_sources}
        """
    ).strip()


def generate_body(
    *,
    repo_root: Path,
    model: str,
    source_paths: list[Path],
    max_chars_per_file: int,
) -> str:
    """Call the GitHub Models API and return the generated markdown body."""

    client = make_client()
    user_prompt = build_user_prompt(
        repo_root=repo_root,
        source_paths=source_paths,
        max_chars_per_file=max_chars_per_file,
    )

    response = client.chat.completions.create(
        model=model,
        temperature=0.2,
        messages=[
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": user_prompt},
        ],
    )

    return normalize_markdown_body(response.choices[0].message.content or "")


def render_claude_skill(body: str) -> str:
    """Wrap the shared markdown body in Claude skill frontmatter."""

    frontmatter = textwrap.dedent(
        """\
        ---
        name: kollector-scum
        version: "0.1.0"
        description: "AI agent skill for contributing to the kollector-scum music catalog SaaS"
        tags: [dotnet, csharp, nextjs, typescript, postgresql, multitenancy]
        ---

        # Kollector Scum — Agent Skill
        """
    )
    return frontmatter + "\n" + body


def render_copilot_instructions(body: str) -> str:
    """Wrap the shared markdown body in the Copilot instructions header."""

    header = textwrap.dedent(
        """\
        <!-- This file is auto-generated by gskill-kollector. Do not edit manually. -->
        # Kollector Scum — Copilot Coding Agent Instructions
        """
    )
    return header + "\n" + body


def write_outputs(repo_root: Path, body: str) -> tuple[Path, Path]:
    """Write both skill files to their repository locations."""

    claude_path = repo_root / CLAUDE_SKILL_PATH
    copilot_path = repo_root / COPILOT_INSTRUCTIONS_PATH

    claude_path.parent.mkdir(parents=True, exist_ok=True)
    copilot_path.parent.mkdir(parents=True, exist_ok=True)

    claude_path.write_text(render_claude_skill(body), encoding="utf-8")
    copilot_path.write_text(render_copilot_instructions(body), encoding="utf-8")

    return claude_path, copilot_path


@app.command()
def generate(
    repo_root: Path = typer.Option(
        Path(__file__).resolve().parent.parent,
        "--repo-root",
        help="Absolute or relative path to the kollector-scum repository root.",
        exists=True,
        file_okay=False,
        dir_okay=True,
        resolve_path=True,
    ),
    model: str = typer.Option(
        DEFAULT_MODEL,
        "--model",
        help="OpenAI model name to use for generation.",
    ),
    max_chars_per_file: int = typer.Option(
        18_000,
        "--max-chars-per-file",
        min=2_000,
        help="Maximum number of characters from each source file to send to the model.",
    ),
    dry_run: bool = typer.Option(
        False,
        "--dry-run",
        help="Print the generated markdown body instead of writing files.",
    ),
) -> None:
    """Generate repository-specific skill files from a curated source bundle."""

    repo_root = repo_root.resolve()
    source_paths = iter_existing_paths(repo_root, KEY_SOURCE_FILES)
    body = generate_body(
        repo_root=repo_root,
        model=model,
        source_paths=source_paths,
        max_chars_per_file=max_chars_per_file,
    )

    if dry_run:
        typer.echo(body)
        return

    claude_path, copilot_path = write_outputs(repo_root, body)
    typer.echo(f"Wrote {claude_path}")
    typer.echo(f"Wrote {copilot_path}")


if __name__ == "__main__":
    app()
