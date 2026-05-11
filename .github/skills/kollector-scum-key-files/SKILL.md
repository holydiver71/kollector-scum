---
name: kollector-scum-key-files
version: 0.2.0
description: Key files and references contributors should know
tags: [copilot-skill, references]
owner: @holydiver71
stability: stable
scope: important references and paths, multi-tenancy core
entry_point: .github/skills/kollector-scum-key-files/SKILL.md
last_updated: 2026-04-26
intent: [discover-files, reference-paths]
---

# Key Files Reference

Core repo guidance

- CLAUDE.md — high-level contributor guidance.

Multi-tenancy core

- backend/KollectorScum.Api/Models/IUserOwnedEntity.cs
- backend/KollectorScum.Api/Models/INamedUserOwnedEntity.cs
- backend/KollectorScum.Api/Interfaces/IUserContext.cs
- backend/KollectorScum.Api/Services/UserContext.cs

Data model & persistence

- backend/KollectorScum.Api/Data/KollectorScumDbContext.cs
- backend/KollectorScum.Api/Repositories/Repository.cs
- backend/KollectorScum.Api/Repositories/UnitOfWork.cs

Generic CRUD & MusicRelease refs

- backend/KollectorScum.Api/Services/GenericCrudService.cs
- backend/KollectorScum.Api/Services/MusicReleaseCommandService.cs
- backend/KollectorScum.Api/Services/MusicReleaseQueryService.cs
