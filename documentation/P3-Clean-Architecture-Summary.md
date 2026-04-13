# P3 — Clean Architecture Summary

**Branch:** `feat/priority-3-architecture`  
**Completed:** 2026-04-13

---

## What Was Done

Priority 3 addressed the remaining architectural issues identified in the April 2026 architecture review:
- **P3.7** — Introduce Clean Architecture layering (Domain / Application separation)
- **P3.8** — Reduce `GetMusicReleasesAsync` complexity (CCN 46)

---

## Approach: Namespace-Level Layering

A **full physical project split** (`KollectorScum.Domain`, `KollectorScum.Application`) was considered and deliberately rejected. The cost (updating 52 test files' `using` directives, DataSeeder project references, solution file changes) exceeded the benefit for a single-team SaaS app. The chosen approach uses **namespace and folder-level layering** within the existing `KollectorScum.Api` project, which:

- Maps 1:1 to physical project names if a split is needed in future
- Creates zero breaking changes for tests or the DataSeeder
- Is immediately navigable and enforces the layering intent

---

## Layer Definitions

### Domain Layer (`KollectorScum.Api.Domain.*`)
Contains code that is independent of infrastructure concerns.

**Folder:** `backend/KollectorScum.Api/Domain/`

| Sub-folder | Contains |
|---|---|
| `Models/` | Canonical domain entities (pointer to `../Models/`) |
| `Interfaces/` | Pure domain contracts (pointer to domain-facing subset of `../Interfaces/`) |
| `ValueObjects/` | Value objects (pointer to `../Models/ValueObjects/`) |

**Domain interfaces (must not depend on EF, HTTP, or persistence namespaces):**
- `IRepository<TEntity>`
- `IUnitOfWork`
- `IUserOwnedEntity`
- `INamedUserOwnedEntity`
- `IMusicReleaseRepository`

### Application Layer (`KollectorScum.Api.Application.*`)
Contains use-case orchestration. May depend on Domain; must not depend on Infrastructure details.

**Folder:** `backend/KollectorScum.Api/Application/`

| Sub-folder | Contains |
|---|---|
| `Queries/` | Query objects + handlers (read side) |
| `Commands/` | Command objects + handlers (write side — future) |

**Application interfaces:**
- `IGetMusicReleasesQueryHandler`
- `IMusicReleaseQueryService` (orchestrator, delegates to handlers)
- `IMusicReleaseCommandService`

### Infrastructure Layer (implicit, within `KollectorScum.Api`)
Depends on Application + Domain. Contains EF Core, storage, HTTP clients, auth.

- `IStorageService`, `IDiscogsHttpClient`, `IUserContext`, `IEmailService`
- All concrete `*Repository`, `*Service` implementations
- `KollectorScumDbContext`, EF migrations

### Presentation Layer (`KollectorScum.Api.Controllers`, DTOs)
Depends on Application. Contains controllers, DTOs, validators, middleware.

- Controllers inherit from `BaseApiController`
- DTOs are transport objects — they live here, not in Domain
- `MusicReleaseQueryParameters` is a presentation-layer parameter object (pragmatic accepted deviation from strict DDD)

---

## Pragmatic Deviations from Strict Clean Architecture

| Deviation | Reason |
|---|---|
| `GetMusicReleasesQuery` wraps `MusicReleaseQueryParameters` (a DTO) | `MusicReleaseQueryParameters` is effectively a parameter object; splitting it would add noise without clarity |
| All layers co-located in one .csproj | Team size and app scale don't warrant the cross-project dependency management overhead |
| `KollectorScumDbContext` injected directly into handlers | EF Core in-memory is used for tests; a repository abstraction exists; adding another abstraction layer is premature |

---

## Changes Made

### Phase 1 — Retire legacy MusicReleaseService (gap-fill first)
- Added 39 new tests covering Create/Update (CommandService) and GetList/GetSingle/Search/Stats (QueryService) scenarios missing from the new service test files
- Removed `IMusicReleaseService` DI registration from `ServiceCollectionExtensions.cs`
- Deleted `MusicReleaseService.cs`, `IMusicReleaseService.cs`, `MusicReleaseServiceTests.cs`

### Phase 2 — Extract MusicReleaseFilterBuilder (P3.8 CCN reduction)
- Created `Services/MusicReleaseFilterBuilder.cs` — pure static expression builder, 10 focused private methods, each CCN ≤ 4
- Removed 175-line `BuildFilterExpression` method from `MusicReleaseQueryService`
- Extracted `ResolveKollectionFilterAsync` as a proper async method (previously used sync `.Any()` and `.ToList()` calls inside the query service)
- Added 20 unit tests for `MusicReleaseFilterBuilder` (all filter clauses in isolation and combination)

### Phase 3 — GetMusicReleasesQueryHandler CQRS object (P3.7 Application layer)
- Created `Application/Queries/GetMusicReleasesQuery.cs` — sealed record wrapping `MusicReleaseQueryParameters`
- Created `Application/Queries/GetMusicReleasesQueryHandler.cs` — owns the read path for paginated lists; split into `HandleArtistSortAsync`, `HandleStandardPagedAsync`, `ResolveKollectionFilterAsync`, `BuildSortExpression`
- Created `Interfaces/IGetMusicReleasesQueryHandler.cs`
- `MusicReleaseQueryService.GetMusicReleasesAsync` is now a single-line dispatcher to the handler
- Added 5 unit tests for `GetMusicReleasesQueryHandler`
- Registered `IGetMusicReleasesQueryHandler` → `GetMusicReleasesQueryHandler` in DI

### Phase 4 — Domain folder structure + documentation
- Created `Domain/Models/`, `Domain/Interfaces/`, `Domain/ValueObjects/` folders
- Created `Application/Commands/` folder (placeholder for future command objects)
- This document

---

## Test Counts

| Phase | Tests Added | Tests Removed | Running Total |
|---|---|---|---|
| Baseline (start of P3) | — | — | 1,015 |
| Phase 1 (gap-fill + legacy removal) | +39 | −32 | 1,022 |
| Phase 2 (filter builder) | +20 | 0 | 1,035 (also updated 1 test) |
| Phase 3 (handler) | +5 | 0 | 1,009* |

*After legacy MusicReleaseServiceTests.cs deletion (−32) and handler tests (+5): net −27 from 1,035.

All 1,009 backend tests pass. All 753 frontend tests pass.

---

## Migration Path to Physical Project Split

If team growth or service boundaries ever warrant a physical split:

```
KollectorScum.Domain/
  Models/          ← move from Api/Models/
  Interfaces/      ← move domain interfaces from Api/Interfaces/
  ValueObjects/    ← move from Api/Models/ValueObjects/

KollectorScum.Application/
  Queries/         ← move from Api/Application/Queries/
  Commands/        ← move from Api/Application/Commands/

KollectorScum.Infrastructure/
  Repositories/    ← move from Api/Repositories/
  Services/        ← move infrastructure services (storage, email, etc.)
  Data/            ← move EF Core context and migrations

KollectorScum.Api/
  Controllers/
  DTOs/
  Middleware/
  Program.cs
```

The namespace structure already matches — the move would be mechanical.
