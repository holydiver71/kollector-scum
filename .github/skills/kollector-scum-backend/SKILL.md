---
name: kollector-scum-backend
version: 0.2.0
description: Backend conventions, repositories, unit-of-work, DTOs and MusicRelease patterns
tags: [copilot-skill, backend, dotnet]
owner: @holydiver71
stability: stable
scope: repositories, unit-of-work, MusicRelease patterns
entry_point: .github/skills/kollector-scum-backend/SKILL.md
last_updated: 2026-04-26
intent: [backend-guidance, repo-patterns, musicrelease-rules]
---

# Backend Patterns

Repository layer

- Use IRepository<T> and Repository<T> for composable querying, paging and existence checks.
- Prefer repository API over direct DbContext access in services.

IUnitOfWork

- Use IUnitOfWork to coordinate repository access, transactions, lookup upserts and save boundaries.

Data modeling

- Models in backend/KollectorScum.Api/Models/ with XML docs and data annotations.
- DbContext: backend/KollectorScum.Api/Data/KollectorScumDbContext.cs — add DbSet<T>, indexes and unique constraints.

MusicRelease specifics

- MusicRelease stores some complex values as JSON strings (artists, genres, purchase info, images).
- Use IMusicReleaseCommandService for writes (resolves lookups, stamps acting UserId, serializes JSON blobs).
- Use IMusicReleaseQueryService for reads and ownership enforcement.

Controller rules

- Add [Authorize] to endpoints unless explicitly public. Forgetting it is a security bug.
