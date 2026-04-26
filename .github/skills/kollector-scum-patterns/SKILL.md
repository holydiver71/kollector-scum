---
name: kollector-scum-patterns
version: 0.2.0
description: DTOs, validation, generic lookup patterns and entity guidelines
tags: [copilot-skill, dto, validation, patterns]
owner: @holydiver71
stability: stable
scope: DTOs, validators, lookup upsert patterns
entry_point: .github/skills/kollector-scum-patterns/SKILL.md
last_updated: 2026-04-26
intent: [dto-guidance, validation, lookup-patterns]
---

# Data Modeling, DTOs and Lookup Patterns

DTOs & Validation

- DTOs live under backend/KollectorScum.Api/DTOs/
- FluentValidation is registered globally; validators discovered from the assembly.
- CreateMusicReleaseDto rules: title required (max 300), at least one artist id or name, no mixed id+name on same field, bounded years, UPC/label number length limits, etc.

Lookup entity pattern

- Lookup entities are user-owned, name-based, simple, and managed by GenericCrudService.
- GetOrCreateByNameAsync finds (UserId, Name).
- Use KollectorScumDbContext upsert helpers (ON CONFLICT ("UserId","Name") DO NOTHING) when creating lookups concurrently.
