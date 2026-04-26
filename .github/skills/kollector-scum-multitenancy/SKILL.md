---
name: kollector-scum-multitenancy
version: 0.2.0
description: Multi-tenancy rules and tenant-safety checks
tags: [copilot-skill, multitenancy]
owner: @holydiver71
stability: stable
scope: tenant-safety, IUserOwnedEntity rules, lookup uniqueness
entry_point: .github/skills/kollector-scum-multitenancy/SKILL.md
last_updated: 2026-04-26
intent: [tenant-isolation, enforcement-patterns]
---

# Multi-Tenancy (Highest Priority)

Non-negotiable

- Every user-owned entity must be scoped to a user. Tenant isolation is a correctness requirement.
- Named lookup uniqueness is (UserId, Name) — never global Name uniqueness.

Core interfaces

- IUserOwnedEntity: Guid UserId { get; set; }
- INamedUserOwnedEntity: Id + Name + UserId

Enforcement patterns

- GenericCrudService injects IUserContext and adds UserId == GetActingUserId() filters.
- CreateAsync stamps UserId automatically.
- Update/Delete verify ownership before applying changes.

IUserContext

- Lives at backend/KollectorScum.Api/Interfaces/IUserContext.cs
- GetActingUserId() supports admin impersonation (X-Admin-Act-As header / impersonation_userId cookie)

Anti-patterns

- Omitting UserId on new entities, using global unique indices, or querying without UserId filters.
