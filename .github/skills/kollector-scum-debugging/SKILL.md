---
name: kollector-scum-debugging
version: 0.2.0
description: Debugging and repair patterns for tests and tenant issues
tags: [copilot-skill, debugging]
owner: @holydiver71
stability: stable
scope: test-driven debugging, tenant-scope checks
entry_point: .github/skills/kollector-scum-debugging/SKILL.md
last_updated: 2026-04-26
intent: [triage-tests, trace-failures, fix-tenant-bugs]
---

# Debugging and Repair Patterns

Test-driven debugging workflow

1. Read the failing test and assertion.
2. Trace from assertion → method → service → repository.
3. Check tenant scoping (UserId filters).
4. Verify field mapping and null handling.
5. Fix root cause and re-run tests.

Common bug patterns

- Missing UserId assignment on entity creation — stamp UserId.
- Inverted ownership check — ensure comparisons are correct.
- Missing mapper fields — keep DTO ↔ Entity mapping in sync.
- Off-by-one pagination indexes — convert zero-based to one-based where needed.
