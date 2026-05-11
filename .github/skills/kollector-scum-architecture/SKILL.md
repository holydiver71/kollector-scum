---
name: kollector-scum-architecture
version: 0.2.0
description: Architecture and layering rules for backend and services
tags: [copilot-skill, architecture]
owner: @holydiver71
stability: stable
scope: layering, service-repository patterns, transactions
entry_point: .github/skills/kollector-scum-architecture/SKILL.md
last_updated: 2026-04-26
intent: [layering-rules, transaction-boundaries, design-guidance]
---

# Architecture and Layer Rules

Primary backend flow

Controllers -> Services -> Repositories -> EF Core DbContext -> PostgreSQL

- Do not skip layers: controllers should not access repositories or DbContext directly.
- Services own business logic; repositories own persistence.
- Use IUnitOfWork for multi-write transactions and lookup upserts.

Base controller pattern

- Standard CRUD controllers inherit from BaseApiController which provides common logging, pagination validation and centralized exception-to-response mapping.

Generic CRUD pattern

- Lookup-style entities use GenericCrudService<TEntity, TDto> which auto-scopes by acting user and provides GetOrCreateByNameAsync.
- Register lookup CRUD services in ServiceCollectionExtensions.

MusicRelease exception

- MusicRelease uses query/command split (IMusicReleaseQueryService / IMusicReleaseCommandService) — treat it as a richer aggregate.
