---
name: kollector-scum-pitfalls
version: 0.2.0
description: Common pitfalls, anti-patterns and a short do/don't checklist
tags: [copilot-skill, pitfalls]
owner: @holydiver71
stability: stable
scope: security, multitenancy, layering anti-patterns
entry_point: .github/skills/kollector-scum-pitfalls/SKILL.md
last_updated: 2026-04-26
intent: [avoid-anti-patterns, security-checks, tenant-safety]
---

# Common Pitfalls & Do / Don't

Security & Auth pitfalls

- Forgetting [Authorize] on controllers.
- Using wrong user ID source instead of GetActingUserId().

Multi-tenancy pitfalls

- Missing IUserOwnedEntity, global unique index on Name, querying without UserId filter.

Layering pitfalls

- Controllers reaching into repositories or DbContext.

Do / Don't summary

Do: use IUserOwnedEntity, (UserId, Name) uniqueness, GetActingUserId(), GenericCrudService, dedicated services for rich aggregates.
Don't: create global lookup uniqueness, bypass layers, forget [Authorize].
