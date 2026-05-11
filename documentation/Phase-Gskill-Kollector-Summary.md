# Phase Summary — gskill-kollector Pipeline

**Branch:** `feature/gskill-kollector`  
**Date:** July 2025  
**Status:** ✅ Complete (Phases 1–8)

---

## Overview

**gskill** is a recipe for automatically generating and evolving AI agent skill files using
two complementary frameworks:

- **SWE-smith** — generates realistic software-engineering bug tasks by mutating or
  rewriting source code, then validates them against the repository's real test suite.
- **GEPA (`optimize_anything`)** — uses an evolutionary loop to refine a skill document
  (SKILL.md / copilot-instructions.md) against a corpus of those tasks, maximising the
  probability that an AI coding agent solves them correctly.

This phase adapted the gskill recipe for **kollector-scum**, a .NET 8 C# + Next.js 15
full-stack SaaS application. The output is a pair of skill files — one for Claude Code,
one for GitHub Copilot coding agent — that capture the codebase's architecture,
conventions, and common pitfalls in a form that helps AI agents work effectively with it.

---

## What Was Built

| File | Lines | Purpose |
|---|---|---|
| `gskill-kollector/Dockerfile.swesmith` | — | Ubuntu 22.04 + .NET 8 + Node 20 Docker evaluation image |
| `gskill-kollector/lm_modify.py` | 190 | LM Modify bug generator (targeted AST-level mutations) |
| `gskill-kollector/lm_rewrite.py` | 269 | LM Rewrite bug generator (function-level rewrites) |
| `gskill-kollector/swesmith_config/eval.sh` | 104 | SWE-smith test harness (`dotnet test` wrapper) |
| `gskill-kollector/swesmith_config/run_validation.py` | 246 | Parallel task validator |
| `gskill-kollector/issue_generator.py` | 136 | GitHub issue text generator |
| `gskill-kollector/generate_initial_skill.py` | 314 | Initial SKILL.md generator (source introspection) |
| `gskill-kollector/evaluator.py` | 211 | GEPA-compatible evaluator |
| `gskill-kollector/pipeline.py` | 348 | GEPA `optimize_anything` loop with fallback |
| `gskill-kollector/tasks/sample_task.json` | — | Example task definition |
| `.claude/skills/kollector-scum/SKILL.md` | 902 | Claude Code skill file |
| `.github/copilot-instructions.md` | 896 | GitHub Copilot coding agent skill file |

---

## Pipeline Architecture

```
Source Code
    │
    ├─► lm_modify.py ──────────────────────────────────────────────┐
    │    (targeted AST-level mutations, C# method body changes)     │
    │                                                               │
    └─► lm_rewrite.py ─────────────────────────────────────────────┤
         (function-level LLM rewrites, more diverse bug types)      │
                                                                    ▼
                                                          tasks/*.json
                                                                    │
                                                    swesmith_config/eval.sh
                                                    (dotnet test, exit 0 = pass)
                                                                    │
                                                    run_validation.py
                                                    (parallel, filters valid tasks)
                                                                    │
                                                    issue_generator.py
                                                    (natural-language GitHub issues)
                                                                    │
                                            ┌───────────────────────┘
                                            │
                                generate_initial_skill.py
                                (introspects source, produces SKILL.md draft)
                                            │
                                            ▼
                                  Initial SKILL.md / copilot-instructions.md
                                            │
                                    GEPA optimize_anything loop
                                    ┌───────────────────────────┐
                                    │  evaluator.py             │
                                    │  (patch → eval.sh → score)│
                                    │         ↕ iterate         │
                                    │  pipeline.py              │
                                    │  (mutate skill → re-eval) │
                                    └───────────────────────────┘
                                            │
                                            ▼
                                  Evolved SKILL.md / copilot-instructions.md
```

---

## Skill Files Generated

### `.claude/skills/kollector-scum/SKILL.md` (902 lines)

YAML frontmatter + markdown body. Format consumed by Claude Code when working in the repo.

**Key sections:**
- Project overview and technology stack
- Repository layout (backend, frontend, worker, migrations)
- Multi-tenancy architecture (per-user lookup tables, `IUserOwnedEntity`)
- Authentication flow (Google OAuth → JWT, invitation system)
- Backend layering (Controllers → Services → Repositories → EF Core)
- GenericCrudService usage and scoping patterns
- Frontend conventions (App Router, AuthContext, Axios api.ts)
- Testing approach (Moq unit tests, WebApplicationFactory integration tests, Jest, Playwright)
- Common pitfalls and anti-patterns to avoid
- File storage (Cloudflare R2 staging/prod separation)
- Discogs integration (background job queue, polling)

### `.github/copilot-instructions.md` (896 lines)

Markdown body only (no YAML frontmatter). Format consumed by GitHub Copilot coding agent
via the `.github/copilot-instructions.md` convention.

Content is substantively identical to SKILL.md but without the YAML header.

---

## Validation Results

| Check | Result |
|---|---|
| `py_compile` on all 7 Python files | ✅ All pass |
| `eval.sh` smoke test (1025 real backend tests) | ✅ 1025 passing |
| Docker image build | ⚠️ Not tested (Docker unavailable in this environment) |
| Tasks corpus | ⚠️ Sample only — real corpus requires `OPENAI_API_KEY` + Docker |

---

## How to Run

### 1. Install Python dependencies
```bash
pip install openai anthropic tiktoken rich
# or with uv:
uv pip install openai anthropic tiktoken rich
```

### 2. Build the Docker evaluation image
```bash
docker build -f gskill-kollector/Dockerfile.swesmith -t swesmith.kollector-scum .
```

### 3. Generate bug tasks
```bash
# Targeted mutations:
OPENAI_API_KEY=sk-... python gskill-kollector/lm_modify.py \
    --source-dir backend/KollectorScum.Api \
    --output-dir gskill-kollector/tasks/ \
    --count 20

# Function-level rewrites:
OPENAI_API_KEY=sk-... python gskill-kollector/lm_rewrite.py \
    --source-dir backend/KollectorScum.Api \
    --output-dir gskill-kollector/tasks/ \
    --count 20
```

### 4. Validate tasks
```bash
python gskill-kollector/swesmith_config/run_validation.py \
    --tasks-dir gskill-kollector/tasks/ \
    --docker-image swesmith.kollector-scum \
    --workers 4
```

### 5. Generate issue text
```bash
python gskill-kollector/issue_generator.py \
    --tasks-dir gskill-kollector/tasks/ \
    --output-dir gskill-kollector/tasks/issues/
```

### 6. Regenerate initial skill files
```bash
python gskill-kollector/generate_initial_skill.py
```

### 7. Run GEPA evolution loop
```bash
OPENAI_API_KEY=sk-... python gskill-kollector/pipeline.py \
    --max-evals 50 \
    --tasks-dir gskill-kollector/tasks/
```

---

## Model Assignments

| Phase | Component | Model |
|---|---|---|
| 3a | `lm_modify.py` bug generation | `gpt-4o` |
| 3b | `lm_rewrite.py` bug generation | `gpt-4o` |
| 5 | `issue_generator.py` issue text | `gpt-4o` |
| 6 | `generate_initial_skill.py` | `gpt-4o` |
| 7 | `evaluator.py` patch scoring | `gpt-4o-mini` |
| 7 | `pipeline.py` skill evolution | `gpt-4o` |
| 1–8 | Implementation (Copilot agent) | `claude-sonnet-4` |

---

## Limitations and Next Steps

### Current Limitations
- **Docker not available** in the development environment used for this phase; the Docker
  image and eval.sh harness have not been end-to-end tested locally.
- **Empty task corpus** — `tasks/` ships with only `sample_task.json`. Generating a real
  corpus requires an OpenAI API key and a running Docker daemon.
- **SWE-smith/gskill not installed** — `lm_modify.py` and `lm_rewrite.py` call out to
  OpenAI directly and do not depend on a local SWE-smith install, but the GEPA
  `optimize_anything` loop in `pipeline.py` was written to be self-contained.
- **No GEPA feedback yet** — the skill files were generated from source introspection
  only; they have not been refined by the evolutionary loop against real tasks.

### Recommended Next Steps
1. Run `lm_modify.py` and `lm_rewrite.py` against the real backend source to build a
   task corpus of ≥ 50 validated tasks.
2. Run `pipeline.py --max-evals 100` once a Docker environment is available to produce
   a GEPA-evolved skill file.
3. Periodically re-run the pipeline after significant architecture changes (e.g., new
   services, new auth flows) to keep skill files current.
4. Consider adding a CI job that runs `py_compile` on all gskill-kollector Python files
   to catch regressions.
