# gskill-kollector

Adapted gskill pipeline for the kollector-scum repository.

This directory implements the gskill recipe (SWE-smith + GEPA `optimize_anything`)
for a .NET 8 C# + Next.js 15 codebase. See the parent repo's plan for full details.

## Pipeline Steps

1. Build Docker environment: `docker build -f Dockerfile.swesmith -t swesmith.kollector-scum ..`
2. Generate bugs: see Phase 3 in plan
3. Validate tasks: see Phase 4 in plan
4. Generate issue text: see Phase 5 in plan
5. Generate initial skill: `python generate_initial_skill.py`
6. Run GEPA optimization: `python pipeline.py --max-evals 50`

## Outputs

- `.claude/skills/kollector-scum/SKILL.md` — for Claude Code
- `.github/copilot-instructions.md` — for Copilot coding agent

## Prerequisites

- Python 3.13+ with `uv`
- Docker
- .NET 8 SDK + Node.js 20 (for image verification)
- `OPENAI_API_KEY`
- `ANTHROPIC_API_KEY` (optional)
