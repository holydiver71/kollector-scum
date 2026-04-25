# gskill-kollector

Adapted [gskill](https://github.com/geekan/gskill) pipeline for the **kollector-scum** repository.

This directory implements the gskill recipe — SWE-smith task generation + GEPA
`optimize_anything` evolutionary refinement — for a **.NET 8 C# + Next.js 15** codebase.

---

## Prerequisites

| Requirement | Version |
|---|---|
| Python | 3.12+ |
| pip / uv | any recent |
| Docker | required for full SWE-smith evaluation |
| .NET SDK | 8 (inside Docker image) |
| Node.js | 20 (inside Docker image) |
| `OPENAI_API_KEY` | required for bug generation and GEPA |
| `ANTHROPIC_API_KEY` | optional (for Claude-based generation) |

---

## Pipeline Files

| File | Lines | Purpose |
|---|---|---|
| `Dockerfile.swesmith` | — | Ubuntu 22.04 + .NET 8 + Node 20 evaluation image |
| `lm_modify.py` | 190 | LM Modify bug generator (targeted AST-level changes) |
| `lm_rewrite.py` | 269 | LM Rewrite bug generator (function-level rewrites) |
| `swesmith_config/eval.sh` | 104 | SWE-smith test harness for `dotnet test` |
| `swesmith_config/run_validation.py` | 246 | Parallel task validator (runs eval.sh per task) |
| `issue_generator.py` | 136 | GitHub issue text generator from task JSON |
| `generate_initial_skill.py` | 314 | Generate initial SKILL.md from source introspection |
| `evaluator.py` | 211 | GEPA-compatible evaluator (runs pipeline, scores output) |
| `pipeline.py` | 348 | GEPA `optimize_anything` loop with fallback logic |
| `tasks/sample_task.json` | — | Example task definition |

---

## Output Skill Files

| File | Format | Purpose |
|---|---|---|
| `.claude/skills/kollector-scum/SKILL.md` | YAML frontmatter + markdown body | Claude Code skill |
| `.github/copilot-instructions.md` | Markdown body only | GitHub Copilot coding agent |

---

## Step-by-Step Usage

### 1. Install Python dependencies
```bash
pip install openai anthropic tiktoken rich
# or with uv:
uv pip install openai anthropic tiktoken rich
```

### 2. Build the Docker evaluation image
```bash
docker build -f Dockerfile.swesmith -t swesmith.kollector-scum ..
```

### 3. Generate bug tasks (choose one or both strategies)

**LM Modify** — targeted, AST-level mutations:
```bash
OPENAI_API_KEY=sk-... python lm_modify.py \
    --source-dir ../backend/KollectorScum.Api \
    --output-dir tasks/ \
    --count 20
```

**LM Rewrite** — function-level rewrites (more diverse bugs):
```bash
OPENAI_API_KEY=sk-... python lm_rewrite.py \
    --source-dir ../backend/KollectorScum.Api \
    --output-dir tasks/ \
    --count 20
```

### 4. Validate tasks
```bash
python swesmith_config/run_validation.py \
    --tasks-dir tasks/ \
    --docker-image swesmith.kollector-scum \
    --workers 4
```

### 5. Generate GitHub issue text for each task
```bash
python issue_generator.py \
    --tasks-dir tasks/ \
    --output-dir tasks/issues/
```

### 6. Generate (or regenerate) the initial SKILL.md
```bash
python generate_initial_skill.py
# Outputs: ../.claude/skills/kollector-scum/SKILL.md
#           ../.github/copilot-instructions.md
```

### 7. Run the GEPA evolution loop
```bash
OPENAI_API_KEY=sk-... python pipeline.py \
    --max-evals 50 \
    --tasks-dir tasks/ \
    --output-skill ../.claude/skills/kollector-scum/SKILL.md
```

---

## Notes

- The `tasks/` directory ships with only `sample_task.json`. Real tasks are generated
  by `lm_modify.py` / `lm_rewrite.py` and require an OpenAI API key.
- Docker is not required to run `generate_initial_skill.py` or `pipeline.py` in
  dry-run mode (no task evaluation).
- The GEPA loop uses `gpt-4o` by default; set `--model` to override.
