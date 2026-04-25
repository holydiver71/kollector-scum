---
name: kollector-scum
version: "0.1.0"
description: "AI agent skill for contributing to the kollector-scum music catalog SaaS"
tags: [dotnet, csharp, nextjs, typescript, postgresql, multitenancy]
---

# Kollector Scum — Agent Skill

## Repo Identity

- Kollector Scum is a full-stack music collection catalog SaaS.
- Users catalog vinyl, CD, and related music releases.
- The system is invitation-only.
- The system is fully multi-tenant.
- Correct tenant scoping is the single most important correctness rule.

### Stack

- Backend: .NET 8 Web API in C#.
- Frontend: Next.js 15 App Router with TypeScript.
- Database: PostgreSQL via Supabase in real environments.
- Object storage: Cloudflare R2.
- Public image serving: Cloudflare Worker.
- Backend tests: xUnit + Moq.
- Frontend tests: Jest + React Testing Library.

### Purpose

- Store user-owned music release data.
- Manage per-user lookup data such as artists and genres.
- Import releases from Discogs.
- Serve cover art from object storage.
- Provide collection statistics and search.

### Deployment shape

- API talks to PostgreSQL.
- Frontend talks to the API through `frontend/app/lib/api.ts`.
- Images are uploaded through a storage service abstraction.
- Images are then served publicly through the worker path.
- Discogs imports run asynchronously via a background queue.

### What an agent should optimize for

- Preserve tenant isolation first.
- Preserve layering second.
- Keep DTO, validation, and Swagger docs aligned.
- Prefer existing generic patterns over inventing new ones.
- Follow the codebase’s service/repository abstractions even when EF Core could do it directly.

## Architecture and Layer Rules

### Primary backend flow

- Standard backend flow is:
- `Controllers -> Services -> Repositories -> EF Core DbContext -> PostgreSQL`
- Do not skip layers.
- Controllers should not reach into repositories directly.
- Services own business logic.
- Repositories own persistence mechanics.
- `IUnitOfWork` owns transaction boundaries when multiple writes are involved.

### Base controller pattern

- Standard CRUD controllers inherit from `BaseApiController`.
- `BaseApiController` provides:
- common API metadata,
- logging helpers,
- pagination validation,
- centralized exception-to-response mapping.
- Example standard CRUD controller:
- `backend/KollectorScum.Api/Controllers/ArtistsController.cs`

### Generic CRUD pattern

- Lookup-style entities use `GenericCrudService<TEntity, TDto>`.
- `GenericCrudService` is the default service base for:
- `Artist`
- `Genre`
- `Label`
- `Country`
- `Format`
- `Packaging`
- `Store`
- The registrations live in:
- `backend/KollectorScum.Api/Extensions/ServiceCollectionExtensions.cs`

### What GenericCrudService gives you

- Automatic tenant scoping for `IUserOwnedEntity` queries.
- `GetAllAsync` adds a `UserId` filter automatically.
- `GetByIdAsync` verifies ownership after loading.
- `CreateAsync` stamps `UserId` automatically.
- `UpdateAsync` rejects cross-tenant updates.
- `DeleteAsync` rejects cross-tenant deletes.
- `GetOrCreateByNameAsync` looks up by `(UserId, Name)`.
- Lookup/list results can be cached for 5 minutes.

### What subclasses must supply

- `MapToDto`
- `MapToEntity`
- `UpdateEntity`
- `GetEntityId`
- optional search filter
- optional default ordering
- optional DTO validation

### Repository layer

- Generic repository abstraction is `IRepository<T>`.
- Implementation is `Repository<T>`.
- It exposes composable querying, paging, includes, add/update/delete, and existence checks.
- Repository methods are usually `AsNoTracking` for reads.
- `Repository<T>.Update` has explicit tracked-entity conflict handling.
- Use the repo API instead of inline `DbContext` access in normal service work.

### Unit of Work

- `IUnitOfWork` coordinates:
- repository access,
- transactions,
- save boundaries,
- lookup-table upserts,
- sequence resets,
- change tracker clearing.
- Use it whenever a feature does more than one related write.

### MusicRelease is the important exception

- `MusicRelease` does not follow the plain lookup CRUD pattern.
- It uses separate query and command services:
- `IMusicReleaseQueryService`
- `IMusicReleaseCommandService`
- Controller:
- `backend/KollectorScum.Api/Controllers/MusicReleasesController.cs`
- Query service:
- `backend/KollectorScum.Api/Services/MusicReleaseQueryService.cs`
- Command service:
- `backend/KollectorScum.Api/Services/MusicReleaseCommandService.cs`
- This is effectively a CQRS-style split.

### When to use which service pattern

- Use `GenericCrudService` for simple per-user lookup entities.
- Use a dedicated command/query split when:
- the entity has richer write orchestration,
- related entities may be auto-created,
- duplicate detection exists,
- background integration behavior exists,
- or a richer DTO mapping flow is required.

### Frontend architecture rules

- The frontend uses Next.js App Router.
- Routes live under `frontend/app/`.
- Shared API access belongs in `frontend/app/lib/api.ts`.
- Shared auth helpers belong in `frontend/app/lib/auth.ts`.
- Do not create ad hoc fetch wrappers in random components.
- There is no Redux or Zustand.
- Auth state is propagated through events and React context/state.

## Multi-Tenancy (Highest Priority)

### Non-negotiable rule

- Every user-owned entity must be scoped to a user.
- If you break tenant scoping, you create a data leak.
- Treat tenant isolation as a correctness bug, not just a style issue.

### Core interface

- User-owned entities implement:
- `backend/KollectorScum.Api/Models/IUserOwnedEntity.cs`
- The interface is simple:
- `Guid UserId { get; set; }`
- Named lookup entities often implement:
- `backend/KollectorScum.Api/Models/INamedUserOwnedEntity.cs`
- That extends `IUserOwnedEntity` with:
- `int Id`
- `string Name`

### Entities that are explicitly per-user

- `MusicRelease`
- `Artist`
- `Genre`
- `Label`
- `Country`
- `Format`
- `Packaging`
- `Store`
- `Kollection`
- `List`
- `DiscogsImportJob`
- and other user-owned aggregates

### Database constraints you must preserve

- Lookup tables are not global shared dictionaries.
- They are per-user lookup tables.
- The uniqueness rule is:
- `(UserId, Name)`
- not:
- `Name`
- This is configured in `KollectorScumDbContext`.

### Concrete examples of composite unique indexes

- `Artists`: `(UserId, Name)`
- `Genres`: `(UserId, Name)`
- `Labels`: `(UserId, Name)`
- `Countries`: `(UserId, Name)`
- `Formats`: `(UserId, Name)`
- `Packagings`: `(UserId, Name)`
- `Stores`: `(UserId, Name)`
- `Kollections`: `(UserId, Name)`

### GenericCrudService tenant behavior

- `GetAllAsync`:
- injects `IUserContext`,
- gets `GetActingUserId()`,
- adds a `UserId == actingUserId` expression for `IUserOwnedEntity`.
- `GetByIdAsync`:
- loads the entity,
- verifies ownership,
- returns `null` for mismatched tenants.
- `CreateAsync`:
- sets `UserId` automatically before save.
- `UpdateAsync`:
- verifies the existing entity belongs to the acting user.
- `DeleteAsync`:
- verifies the existing entity belongs to the acting user.
- `GetOrCreateByNameAsync`:
- finds by both `UserId` and `Name`.

### IUserContext rules

- `IUserContext` lives at:
- `backend/KollectorScum.Api/Interfaces/IUserContext.cs`
- Implementation is:
- `backend/KollectorScum.Api/Services/UserContext.cs`
- It resolves the authenticated user from JWT claims.
- `ClaimTypes.NameIdentifier` is the user ID.
- `GetActingUserId()` supports admin impersonation.
- Impersonation sources:
- `X-Admin-Act-As` header
- `impersonation_userId` cookie
- If your service is user-scoped, use `GetActingUserId()`, not just `GetUserId()`.

### Important implication for admin work

- Admin features can act on behalf of another user.
- Anything that scopes data should use the acting user.
- If you accidentally use the raw user ID instead, impersonation can become inconsistent.

### Join table / association table rule

- Existing tenant safety is sometimes enforced through parent foreign-key chains.
- The CLAUDE guidance explicitly calls out that some join tables do not store `UserId`.
- Do not cargo-cult either choice.
- For every new join table, decide tenant isolation deliberately.
- If the row is only meaningful through a single user-owned parent, FK-chain scoping may be enough.
- If the row can be queried independently, filtered directly, or reused across parents, add the scoping key you need.
- Never leave tenancy ambiguous.

### Anti-patterns that cause tenant bugs

- Adding a new entity without `IUserOwnedEntity`.
- Using a global unique index on `Name`.
- Writing direct repository queries without a `UserId` filter.
- Adding a custom query service and forgetting the ownership check that GenericCrudService would have given you.
- Resolving related lookup names without scoping by acting user.
- Creating association tables whose rows can float across users.

### Safe mental model

- Ask: “What user owns this row?”
- Ask: “How is that ownership enforced in queries?”
- Ask: “How is uniqueness enforced per user?”
- Ask: “If admin impersonation is active, does this still work?”
- If you cannot answer all four, stop and fix the design.

## Authentication and Authorization

### Auth flow

- Google OAuth is the initial identity proof.
- The backend validates the Google token server-side.
- The backend issues its own JWT.
- The API uses JWT bearer auth.

### Important claims

- `NameIdentifier` holds the application user ID.
- Email is also present.
- Admin behavior is represented by `IsAdmin`.

### Invitation-only behavior

- Access is not open signup.
- Users must have a `UserInvitation`.
- The invitation must be valid and then marked used.

### Controller rule

- API endpoints should require `[Authorize]` unless there is a clear public exception.
- Standard CRUD controllers all use `[Authorize]`.
- `MusicReleasesController` uses `[Authorize]`.
- Forgetting `[Authorize]` is a security bug.

### Frontend auth behavior

- Auth token key is `auth_token`.
- It is stored in local storage by current frontend helpers.
- `frontend/app/lib/api.ts` injects:
- `Authorization: Bearer <token>`
- It also sends:
- `X-Admin-Act-As`
- when impersonation is active.
- On real `401` responses, the helper clears auth state and dispatches `authChanged`.

### Agent guidance

- Do not invent a second auth token storage mechanism.
- Reuse `fetchJson`.
- Reuse `frontend/app/lib/auth.ts`.
- If you add authenticated frontend calls, let the central helper attach headers.

## Data Modeling and Persistence Patterns

### Model conventions

- Models use C# classes under `backend/KollectorScum.Api/Models/`.
- They use XML docs extensively.
- They use data annotations such as:
- `[Required]`
- `[StringLength(...)]`
- `[ForeignKey(...)]`

### DbContext conventions

- EF configuration is centralized in:
- `backend/KollectorScum.Api/Data/KollectorScumDbContext.cs`
- New entities should usually get:
- `DbSet<T>`
- indexes
- unique constraints
- relationship configuration
- delete behavior rules

### Important music release persistence detail

- `MusicRelease` stores several complex values as JSON strings.
- Do not assume everything is normalized into join tables.
- Examples:
- `Artists`
- `Genres`
- `PurchaseInfo`
- `Images`
- `Links`
- `Media`

### Consequence for contributors

- If you add a field to a release DTO, verify whether it belongs:
- as a scalar column,
- as a related entity,
- or as serialized JSON.
- Keep DTO mapping, validation, and serialization in sync.

### Lookup upsert pattern

- `KollectorScumDbContext` contains atomic upsert helpers for several lookup tables.
- They use SQL with:
- `ON CONFLICT ("UserId","Name") DO NOTHING`
- Examples:
- `UpsertFormatAsync`
- `UpsertLabelAsync`
- `UpsertCountryAsync`
- `UpsertArtistAsync`
- `UpsertGenreAsync`
- `UpsertPackagingAsync`
- When concurrency matters for lookup creation, prefer the existing upsert path over ad hoc insert-then-select logic.

## DTOs, Validation, and Serialization

### DTO location

- DTOs live under:
- `backend/KollectorScum.Api/DTOs/`

### Validation approach

- FluentValidation is registered globally.
- Validators are discovered from the assembly.
- Example validator:
- `backend/KollectorScum.Api/Validators/CreateMusicReleaseDtoValidator.cs`

### Validation patterns to copy

- Validate required fields explicitly.
- Validate mutually exclusive input combinations.
- Validate collection items.
- Validate ranges for dates and numeric fields.
- Validate URLs carefully.
- Use conditional rules with `When(...)` for optional fields.

### Concrete CreateMusicReleaseDto validation rules

- Title is required.
- Title max length is 300.
- At least one artist ID or artist name is required.
- IDs in lookup lists must be positive.
- Name lists cannot contain empty values.
- Release years are bounded.
- You cannot specify both lookup ID and lookup name for the same field.
- UPC and label number have length limits.
- Purchase info cannot specify both `StoreId` and `StoreName`.
- Image fields may be either a local filename or an HTTP(S) URL.

### Serialization conventions

- JSON is camelCase in API responses.
- Frontend and backend DTOs assume camelCase transport.
- Serialized JSON blobs are handled through `System.Text.Json`.

### Contributor checklist for new DTO fields

- Add the property to the DTO.
- Add or update FluentValidation rules.
- Update mapper/service code.
- Update tests.
- Update Swagger-facing docs/comments.
- Verify frontend types match the backend response shape.

## Standard Entity Patterns

### Lookup entity pattern

- A lookup entity is usually:
- user-owned,
- name-based,
- simple,
- and managed via `GenericCrudService`.
- Good examples:
- `Artist`
- `Genre`
- `Label`
- `Country`
- `Format`
- `Packaging`
- `Store`

### Lookup service pattern

- Inherit from `GenericCrudService<TEntity, TDto>`.
- Map only the small DTO surface.
- Override search filter.
- Override default ordering.
- Override `ValidateDto` for entity-specific checks.

### Lookup controller pattern

- Inherit from `BaseApiController`.
- Add `[Authorize]`.
- Use `IGenericCrudService<TEntity, TDto>`.
- Validate pagination parameters through the base class.
- Use base logging helpers.
- Convert exceptions through `HandleError`.

### Rich aggregate pattern

- `MusicRelease` is the reference aggregate with more orchestration.
- It uses:
- query service,
- command service,
- mapper service,
- validator service,
- duplicate detection,
- entity resolution,
- statistics support,
- and Discogs backfill/import flows.

## Adding a New Entity (Step-by-Step Checklist)

### Default checklist

1. Decide whether the new thing is:
   - a simple lookup entity,
   - a user-owned aggregate,
   - or a richer CQRS-style aggregate.
2. Decide tenant ownership first.
3. If it is user-owned, implement `IUserOwnedEntity`.
4. If it is a named per-user lookup, strongly prefer `INamedUserOwnedEntity`.
5. Add the model class under `backend/KollectorScum.Api/Models/`.
6. Add XML doc comments to the class and properties.
7. Add data annotations such as `[Required]` and `[StringLength]`.
8. Add the `DbSet<T>` to `KollectorScumDbContext`.
9. Add indexes and constraints in `OnModelCreating`.
10. If the entity is name-based per user, add a unique index on `(UserId, Name)`.
11. Configure relationships and delete behavior.
12. Create DTOs under `backend/KollectorScum.Api/DTOs/`.
13. Add FluentValidation validators if the input shape is non-trivial.
14. Add repository support if a custom repo is needed.
15. Otherwise rely on `IRepository<T>`.
16. Decide whether `GenericCrudService` is enough.
17. If yes, create a service inheriting from `GenericCrudService`.
18. Implement mapping methods and validation overrides.
19. Create a controller inheriting from `BaseApiController` for standard CRUD cases.
20. Add `[Authorize]` to the controller.
21. Register the service in `ServiceCollectionExtensions`.
22. Register any repository or interface additions in `ServiceCollectionExtensions`.
23. Add an EF Core migration.
24. Update Swagger/XML comments for any new endpoints.
25. Add unit tests with Moq.
26. Add integration tests if the endpoint/query behavior is important.
27. Add frontend API wrapper functions if the UI will call it.
28. Reuse `fetchJson` for frontend requests.
29. Update agent-facing documentation if the pattern is important enough to teach future contributors.

### If the entity is a lookup table

- Use the lookup CRUD pattern.
- Register it in `AddLookupCrudServices`.
- Add `(UserId, Name)` uniqueness.
- Add alphabetical default ordering.
- Add simple name search.
- Consider cache friendliness.

### If the entity is a richer aggregate

- Consider separate command and query services.
- Decide whether create/update require a transaction.
- Decide whether related lookup resolution is needed.
- Decide whether duplicate detection is needed.
- Decide whether mapping should be delegated to a mapper service.

### If the entity touches imports or background work

- Decide whether a job entity is required.
- Decide how status will be persisted.
- Decide how the frontend will poll for completion.
- Decide what user-scoped indexes are needed for recovery/status queries.

## MusicRelease-Specific Patterns You Must Know

### Why MusicRelease matters

- Many real features revolve around `MusicRelease`.
- It is the best reference for non-trivial backend work.

### Write path

- `MusicReleaseCommandService`:
- starts a unit-of-work transaction,
- resolves or creates related lookup entities,
- validates,
- stamps the acting `UserId`,
- serializes JSON-backed fields,
- saves,
- commits,
- maps to a full DTO response.

### Read path

- `MusicReleaseQueryService`:
- delegates list retrieval to `IGetMusicReleasesQueryHandler`,
- enforces ownership on single-release fetch,
- maps to full DTOs,
- computes last-played information,
- may backfill tracklists from Discogs when missing.

### Duplicate handling

- Duplicate detection exists on create.
- Validation can return duplicate candidates.
- Controllers convert the duplicate case into `409 Conflict`.

### Related entity resolution

- `EntityResolverService` resolves lookup IDs or names.
- It can create lookup rows when only names are supplied.
- It scopes everything to the acting user.
- Reuse this pattern when adding more name-or-id resolution behavior.

### JSON storage caution

- Artist IDs and genre IDs are serialized JSON arrays on `MusicRelease`.
- Do not “fix” this casually in a small feature.
- If you touch this area, preserve compatibility with the rest of the mapper/query logic.

## Testing

### Backend test commands

```bash
dotnet test backend/KollectorScum.Tests
dotnet test backend/KollectorScum.Tests --filter "KollectorScum.Tests.Services.MusicReleaseCommandServiceTests"
dotnet test backend/KollectorScum.Tests --filter "KollectorScum.Tests.Services.MusicReleaseCommandServiceTests.MethodName"
```

### Frontend test commands

```bash
npm --prefix frontend run lint
npm --prefix frontend test
npm --prefix frontend run test:coverage
npm --prefix frontend run test:e2e
```

### Coverage target

- Backend target is 80%+ coverage.

### Backend unit test pattern

- Unit tests use xUnit.
- Dependencies are mocked with Moq.
- Services are tested in isolation.
- Do not hit a real database in unit tests.
- `MusicReleaseCommandServiceTests` is a good template.

### Backend integration test pattern

- Integration tests use `WebApplicationFactory<Program>`.
- The test host swaps the DbContext to SQLite in-memory.
- Authentication is replaced with `TestAuthHandler`.
- Tests seed data directly into the test database.
- `CollectionStatisticsIntegrationTests` is a good example.

### Important integration details

- Test auth uses a fixed `NameIdentifier` GUID.
- Seed data must line up with that user.
- Integration assertions should verify tenant isolation, not just happy-path values.

### Frontend unit test pattern

- Use Jest + React Testing Library.
- Keep API helpers mockable.
- Reuse the existing auth and fetch utilities rather than mocking arbitrary global state shapes.

### What to test for every new user-owned feature

- Happy path create/read/update/delete.
- Unauthenticated behavior.
- Cross-tenant access denial.
- Validation failures.
- Duplicate/uniqueness behavior where relevant.
- Frontend request/response typing where relevant.

## Discogs Integration Pattern

### High-level rule

- Discogs imports are asynchronous.
- Do not model them as immediate synchronous CRUD work.

### Existing architecture

- Queue:
- `IDiscogsImportJobQueue`
- Background service:
- `DiscogsImportBackgroundService`
- Job entity:
- `DiscogsImportJob`
- Status service:
- `IDiscogsImportJobService`

### Agent guidance

- If a new import step may take time, fit it into the job pattern.
- Persist user-scoped job status.
- Make the frontend poll for status instead of blocking.
- Preserve Discogs rate-limit awareness.

## Frontend Contribution Guidance

### API access

- Centralize API calls in `frontend/app/lib/api.ts`.
- Reuse `fetchJson`.
- Do not create component-local fetch wrappers unless there is a strong reason.

### Auth-aware behavior

- `fetchJson` handles bearer auth.
- `fetchJson` handles impersonation headers.
- `fetchJson` clears stale auth state on genuine `401`.

### Auth helper file

- Use `frontend/app/lib/auth.ts` for:
- token access,
- sign-in token exchange,
- magic link verification,
- sign-out,
- profile fetch/update.

### State guidance

- There is no global Redux-style store.
- Follow the existing lightweight pattern:
- central helper utilities,
- React context where needed,
- component/local state otherwise.

### Safe contribution rule

- If a frontend change needs backend data:
- add or update the backend DTO,
- add/update the backend endpoint,
- expose it through `fetchJson`,
- then update the component.
- Do not hardcode mismatched response shapes in the component layer.

## Common Pitfalls

### Security and auth pitfalls

- Forgetting `[Authorize]` on a new controller.
- Using the wrong user ID source instead of `GetActingUserId()`.
- Returning internal exception details from controllers.

### Multi-tenancy pitfalls

- Forgetting to implement `IUserOwnedEntity`.
- Forgetting `(UserId, Name)` uniqueness on per-user lookups.
- Querying a user-owned repository without a user filter in custom service code.
- Adding a new independently queried association table without explicit tenant scoping.

### Layering pitfalls

- Accessing repositories directly from controllers.
- Reaching into `DbContext` from controllers.
- Bypassing `IUnitOfWork` for multi-step writes.
- Duplicating logic that already lives in `GenericCrudService` or a domain service.

### DTO/validation pitfalls

- Adding DTO fields without validator updates.
- Adding model fields without mapper updates.
- Forgetting camelCase JSON expectations.
- Forgetting to keep frontend types aligned.

### Documentation pitfalls

- Not adding XML comments.
- Not keeping Swagger docs current.
- Not updating agent-facing conventions when a new pattern becomes standard.

### MusicRelease pitfalls

- Forgetting that artists/genres are stored as JSON arrays on `MusicRelease`.
- Forgetting ownership checks in custom MusicRelease queries.
- Ignoring duplicate detection paths on create.

## Key Files Reference

### Core repo guidance

- `CLAUDE.md`
- Primary high-level contributor guidance and command reference.

### Multi-tenancy core

- `backend/KollectorScum.Api/Models/IUserOwnedEntity.cs`
- Base tenant-ownership contract.

- `backend/KollectorScum.Api/Models/INamedUserOwnedEntity.cs`
- Named lookup entity contract.

- `backend/KollectorScum.Api/Interfaces/IUserContext.cs`
- Service abstraction for current/acting user.

- `backend/KollectorScum.Api/Services/UserContext.cs`
- JWT and impersonation resolution.

### Data model and persistence

- `backend/KollectorScum.Api/Data/KollectorScumDbContext.cs`
- DbSets, indexes, unique constraints, relationships, lookup upserts.

- `backend/KollectorScum.Api/Repositories/Repository.cs`
- Generic repository implementation.

- `backend/KollectorScum.Api/Repositories/UnitOfWork.cs`
- Transaction coordinator and repo access point.

### Generic CRUD reference

- `backend/KollectorScum.Api/Services/GenericCrudService.cs`
- Canonical service base for per-user CRUD.

- `backend/KollectorScum.Api/Services/ArtistService.cs`
- Minimal lookup service example.

- `backend/KollectorScum.Api/Controllers/ArtistsController.cs`
- Standard `BaseApiController` CRUD controller example.

### MusicRelease reference

- `backend/KollectorScum.Api/Models/MusicRelease.cs`
- Core aggregate model.

- `backend/KollectorScum.Api/Services/MusicReleaseCommandService.cs`
- Write orchestration and transaction flow.

- `backend/KollectorScum.Api/Services/MusicReleaseQueryService.cs`
- Read orchestration and ownership checks.

- `backend/KollectorScum.Api/Controllers/MusicReleasesController.cs`
- CQRS-style release controller.

- `backend/KollectorScum.Api/Services/EntityResolverService.cs`
- Lookup resolution/auto-create pattern.

- `backend/KollectorScum.Api/Services/MusicReleaseValidator.cs`
- Domain validation and duplicate handling.

- `backend/KollectorScum.Api/Validators/CreateMusicReleaseDtoValidator.cs`
- FluentValidation example for a rich DTO.

### Service registration

- `backend/KollectorScum.Api/Extensions/ServiceCollectionExtensions.cs`
- DI registration map for repos, services, auth, caching, Discogs, and infrastructure.

### Testing references

- `backend/KollectorScum.Tests/Services/MusicReleaseCommandServiceTests.cs`
- Canonical backend unit-test style with Moq.

- `backend/KollectorScum.Tests/Integration/CollectionStatisticsIntegrationTests.cs`
- Integration test style with `WebApplicationFactory` + SQLite in-memory.

- `backend/KollectorScum.Tests/Integration/TestAuthHandler.cs`
- Fake auth for integration tests.

### Frontend references

- `frontend/app/lib/api.ts`
- Central fetch helper, auth header injection, impersonation header, uniform error handling.

- `frontend/app/lib/auth.ts`
- Token storage and auth workflow utilities.

## Practical Agent Rules

### Before editing backend code

- Identify the owning layer first.
- Check whether the entity is user-owned.
- Check whether a generic CRUD pattern already exists.
- Check whether there is already a validator or mapper you should extend.

### Before editing frontend code

- Find the existing helper in `app/lib/`.
- Confirm the backend response shape.
- Reuse centralized auth-aware fetch behavior.

### Before adding a database field

- Decide ownership and indexing.
- Decide whether the field belongs in a DTO.
- Decide whether it needs validation.
- Decide whether it needs frontend type support.
- Decide whether tests need new seeded data.

### Before merging

- Verify authorization.
- Verify tenant scoping.
- Verify unique constraints.
- Verify docs/comments.
- Verify tests.

## Short “Do / Don’t” Summary

### Do

- Do use `IUserOwnedEntity` for user-owned data.
- Do use `(UserId, Name)` uniqueness for per-user lookups.
- Do use `GetActingUserId()` in scoped service logic.
- Do reuse `GenericCrudService` for simple lookups.
- Do use dedicated services for richer aggregates.
- Do reuse `fetchJson` on the frontend.
- Do add tests for tenant isolation and auth.

### Don’t

- Don’t create global lookup uniqueness.
- Don’t bypass service or repository layers.
- Don’t let controllers own business logic.
- Don’t forget `[Authorize]`.
- Don’t forget XML comments and Swagger upkeep.
- Don’t assume MusicRelease fields are all normalized relational columns.
- Don’t ship a feature without thinking through multi-tenancy first.
