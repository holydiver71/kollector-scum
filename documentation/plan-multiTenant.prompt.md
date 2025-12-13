## Plan: Multi-Tenant Migration (FK-Scoped + Admin)

We will convert the schema and services to per-user isolation by adding `UserId: Guid` to all parent and lookup entities, enforcing user-scoped queries via FK chains (join tables unchanged if they already FK to scoped parents/lookups), and applying policy-based admin override with explicit act-as semantics. All existing data will be assigned to `12337b39-c346-449c-b269-33b2e820d74f` (admin; Google `sub`: 112960998711458483443).

### Steps
1. Add `UserId` to models and map indices
- Update parent entities: [backend/KollectorScum.Api/Models/MusicRelease.cs](backend/KollectorScum.Api/Models/MusicRelease.cs), [backend/KollectorScum.Api/Models/List.cs](backend/KollectorScum.Api/Models/List.cs), [backend/KollectorScum.Api/Models/Kollection.cs](backend/KollectorScum.Api/Models/Kollection.cs).
- Update lookups: [backend/KollectorScum.Api/Models/Artist.cs](backend/KollectorScum.Api/Models/Artist.cs), [backend/KollectorScum.Api/Models/Genre.cs](backend/KollectorScum.Api/Models/Genre.cs), [backend/KollectorScum.Api/Models/Label.cs](backend/KollectorScum.Api/Models/Label.cs), [backend/KollectorScum.Api/Models/Country.cs](backend/KollectorScum.Api/Models/Country.cs), [backend/KollectorScum.Api/Models/Format.cs](backend/KollectorScum.Api/Models/Format.cs), [backend/KollectorScum.Api/Models/Packaging.cs](backend/KollectorScum.Api/Models/Packaging.cs), [backend/KollectorScum.Api/Models/Store.cs](backend/KollectorScum.Api/Models/Store.cs).
- Map in `DbContext`: [backend/KollectorScum.Api/Data/ApplicationDbContext.cs](backend/KollectorScum.Api/Data/ApplicationDbContext.cs) — add `HasIndex(e => e.UserId)`; add composite unique `(UserId, Name)` for name-based lookups; remove global unique `Name`.

2. Enforce FK-scoped isolation (keep join tables as-is)
- Confirm join tables (e.g., `MusicReleaseArtist`, `MusicReleaseGenre`, `MusicReleaseLabel`) point to scoped parents/lookups via FKs in [backend/KollectorScum.Api/Models](backend/KollectorScum.Api/Models).
- Do not add `UserId` to join tables if FKs exist; enforce isolation by joining through parents filtered by `UserId`.
- If any join lacks FKs (denormalized strings), either add FKs or add `UserId` with composite keys `(UserId, <fields>)`.

3. Update repositories/services to filter by `UserId`
- Services: [backend/KollectorScum.Api/Services/MusicReleaseQueryService.cs](backend/KollectorScum.Api/Services/MusicReleaseQueryService.cs), [backend/KollectorScum.Api/Services/MusicReleaseCommandService.cs](backend/KollectorScum.Api/Services/MusicReleaseCommandService.cs), [backend/KollectorScum.Api/Services/ListService.cs](backend/KollectorScum.Api/Services/ListService.cs), [backend/KollectorScum.Api/Services/KollectionService.cs](backend/KollectorScum.Api/Services/KollectionService.cs), lookup services.
- Interfaces: [backend/KollectorScum.Api/Interfaces](backend/KollectorScum.Api/Interfaces) — add `Guid userId` parameters to query/create methods; set `entity.UserId = userId` on create; add `GetOrCreateByName(userId, name)` for lookups.
- Aggregations: [backend/KollectorScum.Api/Services/CollectionStatisticsService.cs](backend/KollectorScum.Api/Services/CollectionStatisticsService.cs) — aggregate only `UserId`-scoped rows.

4. Controllers: authorize and pass `userId`
- Add `[Authorize]` and extract `userId` from claims (`sub`) in [backend/KollectorScum.Api/Controllers/MusicReleasesController.cs](backend/KollectorScum.Api/Controllers/MusicReleasesController.cs), [backend/KollectorScum.Api/Controllers/ListsController.cs](backend/KollectorScum.Api/Controllers/ListsController.cs), [backend/KollectorScum.Api/Controllers/KollectionsController.cs](backend/KollectorScum.Api/Controllers/KollectionsController.cs).
- Token service: ensure `sub = ApplicationUser.Id` and include `googleSub` in [backend/KollectorScum.Api/Services/TokenService.cs](backend/KollectorScum.Api/Services/TokenService.cs); interfaces in [backend/KollectorScum.Api/Interfaces/ITokenService.cs](backend/KollectorScum.Api/Interfaces/ITokenService.cs).
- Auth config: validate JWT issuer/audience/key in [backend/KollectorScum.Api/Program.cs](backend/KollectorScum.Api/Program.cs) using [backend/KollectorScum.Api/appsettings.Development.json](backend/KollectorScum.Api/appsettings.Development.json).

5. Admin capability (view any user’s data)
- Model: add `bool IsAdmin` to [backend/KollectorScum.Api/Models/ApplicationUser.cs](backend/KollectorScum.Api/Models/ApplicationUser.cs); migration sets `IsAdmin=true` for `12337b39-c346-449c-b269-33b2e820d74f`.
- Claims/policies: include `role=admin` in `TokenService`; define `AdminOnly` in [backend/KollectorScum.Api/Program.cs](backend/KollectorScum.Api/Program.cs).
- User context: add `IUserContext` to read `UserId` and `IsAdmin` from claims; inject into services to centralize filtering.
- Act-as semantics: allow optional `?userId=<GUID>` or `X-Admin-Act-As` header on endpoints; require `AdminOnly`; log to `AdminAuditLog` table/service for cross-user reads; default deny writes unless policy `AdminWrites` is required.

6. EF migrations: add columns, indexes, assign ownership
- Create migration files in [backend/KollectorScum.Api/Migrations](backend/KollectorScum.Api/Migrations):
  - Add `UserId uuid not null` to tables: `MusicReleases`, `Lists`, `Kollections`, `Artists`, `Genres`, `Labels`, `Countries`, `Formats`, `Packagings`, `Stores`.
  - Create indexes: `CREATE INDEX ON <table>(UserId)`; drop global `UNIQUE(Name)` where present; add `UNIQUE(UserId, Name)` on lookups.
  - Data updates: in `Up()`, set `UserId = '12337b39-c346-449c-b269-33b2e820d74f'` across all affected tables via `Sql("UPDATE <table> SET UserId = '...'")`.
  - ApplicationUsers: add `IsAdmin boolean not null default false`; set admin: `UPDATE ApplicationUsers SET IsAdmin = true WHERE Id = '12337b39-c346-449c-b269-33b2e820d74f'`.

7. Frontend: add auth flow, guards, and 401 handling
- Routing: add login/landing; protect routes under [frontend/app](frontend/app); hide sidebar when unauthenticated.
- API client: attach bearer token; on `401`, clear session and redirect; support admin `?userId=` when present; show an “Viewing as” banner for impersonation.

### FK Keys and Constraints
- Parents: `MusicRelease(UserId)`, `List(UserId)`, `Kollection(UserId)` — FK chains from join tables must reference these.
- Lookups: `Artist(UserId, Name) UNIQUE`, `Genre(UserId, Name) UNIQUE`, `Label(UserId, Name) UNIQUE`, `Country(UserId, Name) UNIQUE`, `Format(UserId, Name) UNIQUE`, `Packaging(UserId, Name) UNIQUE`, `Store(UserId, Name) UNIQUE`.
- Join tables: keep FKs to scoped parents/lookups (e.g., `MusicReleaseGenre(MusicReleaseId FK, GenreId FK)`); no `UserId` needed when FKs exist; service-level checks ensure linked rows share the same `UserId`.

### Further Considerations
1. Write integrity: On create/update, validate linked entities share the same `UserId`; reject cross-user associations.
2. Performance: Add composite indexes aligned with hot paths (e.g., `(UserId, KollectionId)`) in `MusicRelease`.
3. Bootstrap cleanup: After confirming mapping and admin flag, remove temporary `POST api/auth/bootstrap` from [backend/KollectorScum.Api/Controllers/AuthController.cs](backend/KollectorScum.Api/Controllers/AuthController.cs) and `Features` from [backend/KollectorScum.Api/appsettings.Development.json](backend/KollectorScum.Api/appsettings.Development.json).
