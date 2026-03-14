# Backend Optimization and Security Plan

## Overview

The backend has grown organically with 30 services, 21 controllers, and a generic repository pattern. While the architecture is generally sound (CQRS for music releases, GenericCrudService for lookups), accumulated technical debt includes: bloated services violating SRP (DiscogsCollectionImportService at 700+ lines), critical security gaps, performance issues (sync-over-async, missing AsNoTracking, in-memory aggregation), inconsistent error handling, and dead code.

This plan systematically addresses these across 6 phases, ordered by severity.

### Scope

- **Included**: All 30 services, 21 controllers, 2 middleware, Program.cs, repository layer
- **Excluded**: Frontend changes, database schema changes (indexes already good), migration rewrites, worker service, DataSeeder project

### Approach

- Each phase produces a working, testable application
- Security first (OWASP risks), then performance, then refactoring
- New branch per phase, following project conventions
- All tests must pass after each phase; coverage must exceed 80%

---

## Plan Status

**Last Updated**: 2026-03-14

| Phase | Title | Status |
|-------|-------|--------|
| Phase 1 | Security Hardening | ✅ Complete |
| Phase 2 | Performance Optimization | ✅ Complete |
| Phase 3 | Service Layer Refactoring | ⏳ Pending |
| Phase 4 | Dead Code & Cleanup | ⏳ Pending |
| Phase 5 | Test Gap Coverage | ⏳ Pending |
| Phase 6 | Documentation & Summary | ⏳ Pending |

### Recent Progress (Since PR #77)

- [x] Phase 2.10 completed: added short-TTL in-memory caching for `ValidateUserMiddleware` user-existence checks to reduce per-request repository lookups on authenticated traffic.
- [x] Added middleware unit tests covering authenticated/unauthenticated flows and repository-call reduction through cache reuse.
- [x] Phase 2.8 completed: added explicit response compression registration with brotli/gzip providers for JSON API responses and enabled middleware in the request pipeline.
- [x] Added integration coverage to verify gzip-compressed JSON responses are returned when `Accept-Encoding: gzip` is requested.
- [x] Phase 2.7 completed for high-traffic paths: added cancellation-token aware overloads in `IMusicReleaseQueryService` and `IMusicReleaseCommandService`, plus repository and unit-of-work overloads (`IRepository<T>`, `IUnitOfWork`) for cancellable data operations.
- [x] Updated `MusicReleaseQueryService` and `MusicReleaseCommandService` to expose cancellation-aware APIs while preserving non-token overloads for backward compatibility.
- [x] Phase 2.6 completed: duplicate detection now uses targeted database candidate queries (user + normalized catalog/title + exclude id) instead of loading all user releases.
- [x] Added dedicated `MusicReleaseDuplicateService` tests to verify catalog matching, title+artist matching, exclude-id behavior, user scoping, and no-acting-user handling.
- [x] Phase 2.5 completed: moved structured collection statistics aggregation into database queries for totals, distinct labels, year distribution, format distribution, country distribution, and recent releases.
- [x] Reduced JSON-backed statistics overhead by projecting only `Artists`, `Genres`, and `PurchaseInfo` columns instead of materializing all release entities.
- [x] Reworked `CollectionStatisticsService` tests to use EF Core in-memory repositories so async `COUNT/GROUP BY` behavior is validated realistically.
- [x] Phase 2.4 completed: removed unnecessary `Task.Run()` wrapping around synchronous summary mapping in `MusicReleaseService.GetMusicReleasesAsync`.
- [x] Added regression coverage verifying mapper exceptions propagate directly without task-wrapping.
- [x] Phase 2.1 completed: added `.AsNoTracking()` to all generic repository read-only query paths (`GetAllAsync`, `GetAsync`, `GetFirstOrDefaultAsync`, `GetPagedAsync`) to reduce EF Core change-tracker overhead.
- [x] Added repository tests validating read-only methods do not track entities.
- Frontend test reliability maintenance was completed to keep delivery velocity high while backend phases continue:
  - [x] ESLint v9 flat-config migration completed (`frontend/eslint.config.mjs`) and lint run now clean (no errors/warnings).
  - [x] Playwright environment stabilized (browser install + deterministic auth/mock setup for visual spec).
  - [x] `header-and-filters.spec.ts` selectors/timeouts hardened with explicit readiness checks and stable locators.
  - [x] Visual snapshots refreshed to current UI baselines.
  - [x] Repeatability validated: `6/6` Playwright tests passing across repeated runs.
- These frontend tasks are **out of scope** for this backend plan and therefore are not counted in backend phase completion totals below.

---

## Phase 1: Security Hardening ✅ Complete

**Goal**: Implement core security controls addressing OWASP Top 10 vulnerabilities.

### Phase 1.1 – Security Response Headers (OWASP A05 – Security Misconfiguration)

- [x] Create `SecurityHeadersMiddleware` with the following headers:
  - `X-Content-Type-Options: nosniff` – prevents MIME-type sniffing
  - `X-Frame-Options: DENY` – prevents clickjacking
  - `X-XSS-Protection: 1; mode=block` – legacy XSS filter for older browsers
  - `Content-Security-Policy` – restricts resource loading
  - `Referrer-Policy: strict-origin-when-cross-origin` – limits referrer information
  - `Permissions-Policy` – disables unused browser features
- [x] Register middleware in `Program.cs`
- [x] Unit tests for `SecurityHeadersMiddleware`

### Phase 1.2 – Rate Limiting (OWASP A04 – Insecure Design)

- [x] Add global rate limiting policy: 100 requests per minute per IP
- [x] Add strict rate limiting policy for authentication endpoints: 10 requests per minute per IP
- [x] Apply `[EnableRateLimiting]` attribute to auth endpoints (login, magic link, Google OAuth)
- [x] Return `429 Too Many Requests` with `Retry-After` header when limit exceeded
- [x] Register rate limiting in `Program.cs`

### Phase 1.3 – Fix Information Disclosure (OWASP A09 – Security Logging and Monitoring)

- [x] Update `ErrorHandlingMiddleware` to suppress internal exception details in non-development environments
- [x] Return generic error messages in staging/production while logging full details server-side
- [x] Unit tests for updated `ErrorHandlingMiddleware`

### Phase 1.4 – HTTPS Enforcement (OWASP A05 – Security Misconfiguration)

- [x] Enable HSTS (HTTP Strict Transport Security) in non-development environments
- [x] Enable HTTPS redirection in non-development environments
- [x] Register in `Program.cs`

### Phase 1.5 – Deferred Security Items (carried to Phase 3)

The following were identified during Phase 1 planning but deferred as lower priority:

- [ ] **Harden NaturalLanguageQuery SQL Execution** – `NaturalLanguageQueryService.cs` generates raw SQL from LLM output; consider read-only DB connection, row limits, query timeouts, and stronger `SqlValidationService` — **Files**: `Services/NaturalLanguageQueryService.cs`, `Services/SqlValidationService.cs`
- [ ] **Remove Admin Impersonation via Query Parameter** – `UserContext.cs` allows impersonation via `userId` query param; keep header/cookie only — **Files**: `Services/UserContext.cs`
- [ ] **Sanitize Sensitive Data in Logs** – Audit and redact user IDs, SQL queries, impersonation details in production logging — **Files**: `Middleware/ValidateUserMiddleware.cs`, `Services/UserContext.cs`, `Services/NaturalLanguageQueryService.cs`

---

## OWASP Top 10 Coverage Matrix

| OWASP Risk | Description | Mitigation |
|---|---|---|
| A01 – Broken Access Control | Unauthorised data access | Multi-tenant user scoping, JWT auth, `[Authorize]` attributes |
| A02 – Cryptographic Failures | Weak encryption | JWT HS256 with configurable key, HTTPS via HSTS |
| A03 – Injection | SQL injection, etc. | EF Core parameterised queries, `SqlValidationService` for NL queries |
| A04 – Insecure Design | DoS, brute force | Rate limiting (Phase 1.2) |
| A05 – Security Misconfiguration | Missing headers, verbose errors | Security headers (Phase 1.1), error sanitisation (Phase 1.3), HSTS (Phase 1.4) |
| A06 – Vulnerable Components | Outdated packages | NuGet package references kept up-to-date |
| A07 – Identification and Auth Failures | Weak auth | JWT validation, magic link expiry, user existence check on each request |
| A08 – Software and Data Integrity | Tampered payloads | FluentValidation on all inputs, JWT signature validation |
| A09 – Security Logging and Monitoring | Missing audit trail | Structured logging, suppressed verbose errors in production |
| A10 – Server-Side Request Forgery | Internal service calls | HTTP client factory, no user-controlled URLs in outbound calls |

---

## Phase 2: Performance Optimization (HIGH)

- [x] **2.1 Add `.AsNoTracking()` to All Read-Only Queries**
  - `Repository.cs` methods `GetAllAsync()`, `GetAsync()`, `GetFirstOrDefaultAsync()`, `GetPagedAsync()` all track entities unnecessarily for reads
  - Add `.AsNoTracking()` to read-only repository methods or create `AsNoTracking` variants
  - **Files**: `Repositories/Repository.cs`, `KollectorScum.Tests/Repositories/RepositoryTests.cs`

- [x] **2.2 Fix N+1 Queries in MusicReleaseMapperService** *(completed in PR #76)*
  - ~~`MapToSummaryDto()` calls `GetArtistNameSync()` which does `.GetAwaiter().GetResult()` per artist — causes N blocking DB calls per release~~
  - Added `MapToSummaryDtosAsync()` batch method — collects all unique artist/genre IDs, issues 2 queries total instead of ~150
  - **Files**: `Services/MusicReleaseMapperService.cs`, `Interfaces/IMusicReleaseMapperService.cs`

- [x] **2.3 Remove Unnecessary `Task.Run()` Wrappers in MusicReleaseQueryService** *(completed in PR #76)*
  - ~~`MusicReleaseQueryService.cs` wrapped synchronous LINQ in `Task.Run()` unnecessarily~~
  - Replaced with `MapToSummaryDtosAsync()` batch call
  - **Files**: `Services/MusicReleaseQueryService.cs`

- [x] **2.4 Remove Unnecessary `Task.Run()` Wrappers in MusicReleaseService**
  - `MusicReleaseService.cs` still wraps synchronous mapping in `Task.Run()` — context switch overhead with no benefit
  - **Files**: `Services/MusicReleaseService.cs`, `KollectorScum.Tests/Services/MusicReleaseServiceTests.cs`

- [x] **2.5 Move CollectionStatistics Aggregation to Database**
  - `CollectionStatisticsService.cs` loads ALL user releases into memory then iterates with foreach loops for counting
  - Replace with database-side GROUP BY queries for artist counts, genre counts, format distribution, etc.
  - **Files**: `Services/CollectionStatisticsService.cs`, `Interfaces/IRepository.cs`, `Repositories/Repository.cs`, `KollectorScum.Tests/Services/CollectionStatisticsServiceTests.cs`

- [x] **2.6 Move Duplicate Detection to Database**
  - `MusicReleaseDuplicateService.cs` loads all user releases then filters in-memory
  - Replace with targeted database query using WHERE clause matching on title/artist/year
  - **Files**: `Services/MusicReleaseDuplicateService.cs`, `KollectorScum.Tests/Services/MusicReleaseDuplicateServiceTests.cs`

- [x] **2.7 Add CancellationToken Support**
  - Add `CancellationToken` parameter to service interfaces and implementations
  - Propagate to EF Core `ToListAsync(ct)`, `SaveChangesAsync(ct)`, `FirstOrDefaultAsync(ct)`
  - Start with high-traffic paths: MusicReleaseQueryService, MusicReleaseCommandService
  - **Files**: `Interfaces/IMusicReleaseQueryService.cs`, `Interfaces/IMusicReleaseCommandService.cs`, `Interfaces/IRepository.cs`, `Interfaces/IUnitOfWork.cs`, `Services/MusicReleaseQueryService.cs`, `Services/MusicReleaseCommandService.cs`, `Repositories/Repository.cs`, `Repositories/UnitOfWork.cs`

- [x] **2.8 Add Response Compression**
  - Add explicit `AddResponseCompression()` with gzip/brotli for JSON responses
  - **Files**: `Program.cs`, `KollectorScum.Tests/Integration/ResponseCompressionIntegrationTests.cs`

- [x] **2.9 Add In-Memory Caching for Lookup Endpoints** *(completed in PR #76)*
  - ~~Lookup data (artists, genres, labels, etc.) hit the DB on every request despite rarely changing~~
  - Added `ICacheService` interface and `MemoryCacheService` (IMemoryCache-backed) with group-based invalidation via `CancellationChangeToken`
  - `GenericCrudService` caches `GetAllAsync`/`GetByIdAsync` results with 5-min TTL, keyed by `{EntityType}:{userId}:...`
  - Write operations (`Create`/`Update`/`Delete`) invalidate the relevant user+entity cache group automatically
  - All 7 lookup services updated, registered as singleton in DI
  - **Files**: New `Interfaces/ICacheService.cs`, new `Services/MemoryCacheService.cs`, `Services/GenericCrudService.cs`, `Program.cs`, all lookup services

- [x] **2.10 Add Caching for ValidateUserMiddleware**
  - Currently queries DB on every authenticated request to check user still exists
  - Add short-TTL in-memory cache (e.g., 5 minutes) for user existence checks
  - **Files**: `Middleware/ValidateUserMiddleware.cs`, `KollectorScum.Tests/Middleware/ValidateUserMiddlewareTests.cs`, `Program.cs` (register IMemoryCache if not already)

**Verification:**
- Run full test suite
- Performance test: compare response times before/after on collection endpoints with 500+ releases
- Verify MapToSummaryDto no longer makes N+1 queries (check EF Core SQL logging)
- Verify statistics endpoint uses GROUP BY (check generated SQL)

---

## Phase 3: Service Layer Refactoring (MEDIUM)

- [ ] **3.1 Split DiscogsCollectionImportService (700+ lines)**
  - Extract into:
    - `DiscogsImportOrchestrator` — orchestrates pagination, rate limiting, overall flow
    - `DiscogsEntityMapper` — maps Discogs DTOs to app entities, handles entity resolution/creation
    - `DiscogsImageService` — handles image downloading and storage
  - **Remove `TestImportLimit = 50` hardcoded constant** — silently truncates imports in production
  - **Files**: `Services/DiscogsCollectionImportService.cs` → split into 3 new service files

- [ ] **3.2 Refactor EntityResolverService to Use Generics**
  - 6 nearly identical `ResolveOrCreate*Async` methods (~250 lines of duplication)
  - Extract generic `ResolveOrCreateAsync<TEntity>` method
  - **Files**: `Services/EntityResolverService.cs`

- [ ] **3.3 Extract Business Logic from AdminController (649 lines)**
  - Extract storage migration logic (lines 320-525) → `IStorageMigrationService`
  - Extract user impersonation logic → `IUserImpersonationService`
  - Extract email validation → use FluentValidation
  - Keep controller thin — delegate only
  - **Files**: `Controllers/AdminController.cs`, new service files

- [ ] **3.4 Extract Business Logic from AuthController (550+ lines)**
  - Extract user creation/update logic from Google auth flow → `IUserAuthenticationService`
  - Controller should only handle HTTP concerns (request/response mapping, status codes)
  - **Files**: `Controllers/AuthController.cs`, new `Services/UserAuthenticationService.cs`

- [ ] **3.5 Standardize Error Response Format**
  - Create unified `ApiErrorResponse` DTO with `message`, `errorCode`, optional `details` (dev only)
  - Update all controllers to use consistent format (currently mix of `new { message }`, raw strings, `ModelState`)
  - Update `ErrorHandlingMiddleware` to use this DTO
  - Update `BaseApiController.HandleError()` to use this DTO
  - **Files**: New `DTOs/ApiErrorResponse.cs`, `Middleware/ErrorHandlingMiddleware.cs`, `Controllers/BaseApiController.cs`, all controllers

- [ ] **3.6 Fix Overly Broad Exception Handling**
  - `MusicReleaseBatchProcessor.cs` — `catch (Exception)` with no logging
  - `MusicReleaseService.cs` — swallows errors, returns null
  - `MusicReleaseImportService.cs` — continues silently on failures
  - Add specific exception types, proper logging, and consider returning `Result<T>` with error details
  - **Files**: `Services/MusicReleaseBatchProcessor.cs`, `Services/MusicReleaseService.cs`, `Services/MusicReleaseImportService.cs`

**Verification:**
- All existing tests must pass (update mocks for new service interfaces)
- New services must have unit tests (>80% coverage)
- Verify Discogs import still works end-to-end after split
- Verify admin operations work after extraction

---

## Phase 4: Dead Code & Cleanup (LOW)

- [ ] **4.1 Remove Unused Repository Interfaces**
  - 8 repository interfaces (`IArtistRepository`, `IFormatRepository`, `IGenreRepository`, `ILabelRepository`, `IPackagingRepository`, `ICountryRepository`, `IMusicReleaseRepository`, `IStoreRepository`) defined but never implemented
  - UnitOfWork uses `Repository<T>` directly
  - Remove dead interfaces or implement them properly
  - **Files**: All files in `Interfaces/` that are unused

- [ ] **4.2 Remove or Auto-Generate DatabaseSchemaService**
  - Entire schema documentation hardcoded as strings — will drift from actual DB
  - Either auto-generate from DbContext metadata or remove if unused
  - **Files**: `Services/DatabaseSchemaService.cs`

- [ ] **4.3 Remove Backward Compatibility Service Registrations**
  - `Program.cs` has old import service registered alongside new one with "remove after testing" comment
  - Verify new service is stable and remove old registration
  - **Files**: `Program.cs`

- [ ] **4.4 Standardize StatusCode Constants**
  - Replace magic numbers (200, 500) with `StatusCodes.Status200OK` etc. across controllers
  - **Files**: All controllers

**Verification:**
- Full test suite passes
- No compilation errors after interface removal
- Grep for removed interface names to ensure no references remain

---

## Phase 5: Test Gap Coverage (MEDIUM)

- [ ] **5.1 Add Tests for Untested Controllers**
  - 7 controllers with zero tests: ImportController, ImagesController, KollectionsController, ListsController, NowPlayingController, QueryController, SeedController
  - **Files**: New test files in `KollectorScum.Tests/Controllers/`

- [ ] **5.2 Add Repository Integration Tests**
  - Only 1 repository test exists (UserProfileRepositoryTests)
  - Add tests for `Repository<T>` generic methods — especially `GetPagedAsync`, `GetAsync` with filters and includes
  - **Files**: New test files in `KollectorScum.Tests/Repositories/`

- [ ] **5.3 Add Tests for New Services from Phase 3**
  - All services extracted in Phase 3 need unit tests
  - SecurityHeadersMiddleware needs integration test
  - **Files**: New test files in `KollectorScum.Tests/Services/`, `KollectorScum.Tests/Middleware/`

**Verification:**
- Code coverage report exceeds 80%
- All new tests pass

---

## Phase 6: Documentation & Summary

- [ ] **6.1 Update API Documentation**
  - Ensure Swagger annotations are complete on all endpoints
  - Add `[ProducesResponseType]` attributes to any endpoints missing them

- [ ] **6.2 Create Phase Summary Document**
  - Document all changes made, decisions taken, and before/after metrics
  - Place in `documentation/` folder per project convention

---

## Further Considerations

These items are not in the current plan scope but should be considered for future work:

- **Structured logging** — Currently console-only. Serilog would enable production observability, log querying, and proper redaction. Recommend adding but could be its own task.
- **Distributed caching** — In-memory caching (added in Phase 2.9) is fine for single-instance, but if multi-instance deployment is planned, Redis should be considered.
- **`GetAllAsync()` deprecation** — The base `Repository.GetAllAsync()` materializes all records with no pagination. Recommend deprecating with `[Obsolete]` and migrating callers to `GetPagedAsync()`.

---

## Progress Summary

**Note**: Summary counts below track backend-plan tasks only.

| Phase | Status | Tasks | Completed |
|-------|--------|-------|-----------|
| Phase 1: Security Hardening | ✅ Complete | 4 | 4 |
| Phase 2: Performance Optimization | Complete | 10 | 10 |
| Phase 3: Service Layer Refactoring | Not started | 6 | 0 |
| Phase 4: Dead Code & Cleanup | Not started | 4 | 0 |
| Phase 5: Test Gap Coverage | Not started | 3 | 0 |
| Phase 6: Documentation & Summary | Not started | 2 | 0 |
| **Total** | | **31** | **14** |

### Completed Items

**PR #76:**
- ✅ 2.2 — Fix N+1 queries in MusicReleaseMapperService (batch `MapToSummaryDtosAsync`)
- ✅ 2.3 — Remove `Task.Run()` wrappers in MusicReleaseQueryService
- ✅ 2.9 — Add in-memory caching for lookup endpoints (`ICacheService`, `MemoryCacheService`, `GenericCrudService` caching)
- 38 new tests added (817 total, 100% passing)

**PR #77:**
- ✅ 1.1 — Security Response Headers middleware (`SecurityHeadersMiddleware`)
- ✅ 1.2 — Rate limiting (global + strict auth policies, `429 Too Many Requests`)
- ✅ 1.3 — Error information disclosure fix (`ErrorHandlingMiddleware` production sanitization)
- ✅ 1.4 — HTTPS enforcement via HSTS in non-development environments

**Post-PR #77 (out-of-scope frontend reliability work):**
- [x] Frontend linting pipeline modernized for ESLint v9 flat config; lint now passes cleanly.
- [x] `header-and-filters.spec.ts` Playwright selectors/timeouts stabilized.
- [x] Visual e2e snapshots updated and verified with repeated green runs (`6/6` passing).
