---
name: kollector-scum-testing
version: 0.2.0
description: Testing commands, patterns and expectations for backend and frontend
tags: [copilot-skill, testing]
owner: @holydiver71
stability: stable
scope: unit, integration, E2E guidance and commands
entry_point: .github/skills/kollector-scum-testing/SKILL.md
last_updated: 2026-04-26
intent: [run-tests, triage-failures, maintain-coverage]
---

# Testing

Backend

- dotnet test backend/KollectorScum.Tests
- Unit tests: xUnit + Moq (mock dependencies, no real DB)
- Integration tests: WebApplicationFactory<Program> with SQLite in-memory and TestAuthHandler for auth.
- Coverage target: 80%+

Frontend

- npm --prefix frontend run lint
- npm --prefix frontend test
- npm --prefix frontend run test:coverage
- E2E: Playwright (requires running backend)

Test failure triage

- Check authorization, tenant scoping, mapping, null handling, logic inversion, indices, transaction scope.
