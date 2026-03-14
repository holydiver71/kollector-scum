# Phase 3 – Backend Optimization and Security Summary

**Branch:** `copilot/implement-stage-3-of-plan`  
**Build status:** ✅ Build succeeded (0 errors, 2 pre-existing warnings)  
**Test status:** ✅ 856 tests passing  

---

## Tasks Completed

### 3.5 – Standardized Error Response Format

| Artifact | Change |
|---|---|
| `DTOs/ApiErrorResponse.cs` | **New** – Unified error DTO with `message`, `errorCode`, `details` (camelCase JSON) |
| `Middleware/ErrorHandlingMiddleware.cs` | Uses `ApiErrorResponse` instead of anonymous object |
| `Controllers/BaseApiController.cs` | All `HandleError()` branches return `ApiErrorResponse` |

All error responses now share a consistent JSON shape:
```json
{ "message": "...", "errorCode": "INTERNAL_ERROR", "details": null }
```

---

### 1.5 – Removed Admin Impersonation via Query Parameter

- **Removed** `userId` query-parameter impersonation from `UserContext.GetActingUserId()`.
- Header (`X-Admin-Act-As`) and cookie (`impersonation_userId`) channels are retained.
- **Removed URL path** from impersonation audit log entries to reduce information exposure.

---

### 1.5 – Hardened SqlValidationService SQL Execution

- `Validate()` rejects any SELECT query that lacks a `LIMIT` clause.
- `Sanitize()` enforces a maximum `LIMIT 100` by appending it when absent and clamping any higher value using a source-generated regex (`ClampLimitClause`).

---

### 3.6 – Fixed Overly Broad Exception Handling

Both `MusicReleaseBatchProcessor.cs` and `MusicReleaseImportService.cs` had outer `catch (Exception)` blocks that rolled back and rethrew without logging.  
These now log `LogError` with the exception context before rethrowing, ensuring transaction failures are always captured in structured logs.

---

### 3.2 – Refactored EntityResolverService with Generics

**New interface:** `Models/INamedUserOwnedEntity` (extends `IUserOwnedEntity` with `int Id` and `string Name`).

**Updated models:** Artist, Genre, Label, Country, Format, Packaging now implement `INamedUserOwnedEntity`.

`EntityResolverService` now has two private generic helpers instead of six repeated implementations:
- `ResolveOrCreateEntityAsync<TEntity>` – core look-up/create logic
- `ResolveOrCreateSingleEntityAsync<TEntity>` – null-guard wrapper for single entities

---

### 3.1 – Split DiscogsCollectionImportService

| Artifact | Purpose |
|---|---|
| `Interfaces/IDiscogsImageService.cs` | **New** – Contract for image download/upload |
| `Services/DiscogsImageService.cs` | **New** – Downloads Discogs images, uploads to R2 |
| `DiscogsCollectionImportService.cs` | **Modified** – Injects `IDiscogsImageService`; removed `HttpClient`, `IStorageService`, and duplicate filename logic |

---

### 3.3 – Extracted Logic from AdminController

| Artifact | Purpose |
|---|---|
| `Interfaces/IStorageMigrationService.cs` | **New** – Contract for local→R2 migration |
| `Services/StorageMigrationService.cs` | **New** – Migrates legacy flat-file cover art to cloud storage |
| `Interfaces/IUserImpersonationService.cs` | **New** – Contract for admin user impersonation |
| `Services/UserImpersonationService.cs` | **New** – Validates and executes impersonation |
| `Controllers/AdminController.cs` | **Modified** – Constructor reduced from 7 to 5 params; removed 200+ lines of inline logic |

---

### 3.4 – Extracted Logic from AuthController

| Artifact | Purpose |
|---|---|
| `Interfaces/IUserAuthenticationService.cs` | **New** – Contract for user find-or-create |
| `Services/UserAuthenticationService.cs` | **New** – Centralises invitation-gated user creation for both Google OAuth and magic-link flows |
| `Controllers/AuthController.cs` | **Modified** – `GoogleAuth`, `GoogleCallback`, and `VerifyMagicLink` now delegate to `IUserAuthenticationService` |

---

## Service Registrations (Program.cs)

```csharp
builder.Services.AddHttpClient<DiscogsImageService>();
builder.Services.AddScoped<IDiscogsImageService, DiscogsImageService>();
builder.Services.AddScoped<IDiscogsCollectionImportService, DiscogsCollectionImportService>();
builder.Services.AddScoped<IStorageMigrationService, StorageMigrationService>();
builder.Services.AddScoped<IUserImpersonationService, UserImpersonationService>();
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
```

---

## New Test Files

| File | Tests |
|---|---|
| `DiscogsImageServiceTests.cs` | SanitizeFilename, DownloadAndStore (success/failure/exception) |
| `StorageMigrationServiceTests.cs` | Empty DB, release not found, already-migrated skip |
| `UserImpersonationServiceTests.cs` | Self-impersonation, not found, admin target, valid target |
| `UserAuthenticationServiceTests.cs` | Google + email find-or-create flows, error paths |

---

## Security Summary

| Finding | Status |
|---|---|
| Admin impersonation via URL query parameter (IDOR risk) | ✅ Fixed – removed entirely |
| Sensitive path in impersonation audit logs | ✅ Fixed – path removed from log entries |
| Unbounded SQL LIMIT in NL query | ✅ Fixed – enforced via validation and sanitisation |
| Unlogged transaction rollbacks | ✅ Fixed – outer catch blocks now log before rethrow |
| CodeQL scan | ✅ 0 alerts |
