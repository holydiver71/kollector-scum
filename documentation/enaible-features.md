# Enaible — Feature Overview

> Enaible is a **unified CLI for AI-assisted engineering workflows**. It provides deterministic analyzers paired with AI skills that run inside tools like GitHub Copilot CLI, Cursor, Claude Code, Gemini, and more.

---

## 🔍 Analyzers

Deterministic tools that produce normalised findings — used as evidence backing for AI skills.

### Security

| Analyzer | Description |
|---|---|
| `security:semgrep` | Static application security testing (SAST) |
| `security:detect_secrets` | Hardcoded secrets and high-entropy string detection |
| `security:osv` | Dependency vulnerability scanning via OSV database |

### Code Quality

| Analyzer | Description |
|---|---|
| `quality:lizard` | Cyclomatic complexity analysis |
| `quality:jscpd` | Copy-paste / code duplication detection |
| `quality:coverage` | Language-agnostic test coverage analysis |
| `quality:patterns` | Composite pattern classifier (anti-patterns, code smells) |
| `quality:react-doctor` | React-specific diagnostics |
| `quality:aggregate` | Aggregates and organises results from multiple analyzers |

### Performance

| Analyzer | Description |
|---|---|
| `performance:semgrep` | Universal performance heuristics via Semgrep rules |
| `performance:dotnet` | C# / .NET build analyzer for performance issues |
| `performance:frontend` | Frontend performance issues and optimisation opportunities |
| `performance:ruff` | Python performance lints via Ruff |
| `performance:golangci-lint` | Go performance findings via golangci-lint |
| `performance:clippy` | Rust performance lints via Clippy |
| `performance:sqlglot` | SQL file analysis for common performance issues |
| `performance:baseline` | Language-agnostic performance baseline |

### Architecture

| Analyzer | Description |
|---|---|
| `architecture:coupling` | Code coupling patterns and dependency relationships |
| `architecture:dependency` | Project dependencies and potential issues |
| `architecture:patterns` | Design pattern and architectural decision evaluation |
| `architecture:scalability` | Scalability bottlenecks and architectural constraints |

### Root Cause

| Analyzer | Description |
|---|---|
| `root_cause:error_patterns` | Known error patterns and failure modes |
| `root_cause:recent_changes` | Git history analysis to identify potential root causes |
| `root_cause:trace_execution` | Execution pattern analysis and debugging pointers |

---

## 🧠 Skills

Structured multi-step AI workflows invokable directly from your AI assistant (e.g. `run analyze-security`). Each skill uses deterministic analyzer output as evidence before producing recommendations.

### Analysis Skills

| Skill | Description |
|---|---|
| `analyze-security` | OWASP-aligned security scan — semgrep, detect-secrets, OSV + gap analysis |
| `analyze-architecture` | Architecture assessment — coupling, dependencies, patterns, scalability |
| `analyze-code-quality` | Code quality review — complexity, duplication, coverage, patterns |
| `analyze-performance` | Performance bottleneck identification across language-specific analyzers |
| `analyze-root-cause` | Evidence-backed root cause investigation for defects or incidents |
| `analyze-code-integrity` | Review active code contracts for integrity drift |
| `analyze-agentic-readiness` | Score agentic readiness and identify maintenance gaps |

### Codification Skills

| Skill | Description |
|---|---|
| `codify-git-history` | Mine git history for recurring patterns; propose prompt or skill candidates |
| `codify-pr-reviews` | Convert recurring PR review comments into deterministic tooling rules |
| `codify-session-history` | Turn assistant session history into enforcement proposals |

### Knowledge Base Skills

| Skill | Description |
|---|---|
| `kb-repository-setup` | Scaffold a project KB with docs, AGENTS.md, skills, and a code-to-KB map |
| `kb-code-drift` | Detect drift between repository code and KB documentation |
| `kb-prd-creator` | Create or update Markdown PRDs from KB setup artifacts |
| `kb-lean-canvas` | Create or update a Lean Canvas and paired HTML view |
| `kb-spec-impact-analysis` | Trace spec changes to impacted code surfaces and classify delivery gaps |
| `kb-external-kb-consolidation` | Consolidate external project signals against repository KB content |

### Documentation Skills

| Skill | Description |
|---|---|
| `docs-scraper` | Fetch documentation from URLs and save clean markdown files |
| `docs-site-setup` | Scaffold an Astro/Starlight docs site pre-configured with a KB sidebar |

### Research Skills

| Skill | Description |
|---|---|
| `deep-topic-research` | Deterministic, auditable deep-topic research with source validation |

---

## 🛠 Other CLI Commands

| Command | Description |
|---|---|
| `enaible stack analyze` | Detect languages, frameworks, and package managers |
| `enaible inventory build` | Build project structure maps for AI context |
| `enaible prompts` | Manage and render reusable AI prompt templates |
| `enaible ci` | CI utilities — format converters and renderers |
| `enaible setup` | Dev environment setup utilities |
| `enaible doctor` | Run basic environment diagnostics |
| `enaible context_capture` | Capture session context for supported AI assistant platforms |

---

## 🤝 Supported AI Assistants

Skills are compatible with: **GitHub Copilot CLI**, **Cursor**, **Claude Code**, **Codex**, **Gemini**, **Cursor**, **OpenCode**, **Antigravity**, **Pi**
