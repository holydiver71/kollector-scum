# Multi-Tenant Implementation Summary

## Overview
This document describes the implementation of multi-tenant functionality in the Kollector Scum application, providing complete data isolation between users.

## Architecture Changes

### 1. Database Schema Changes

#### User-Owned Entities
All main entities and lookup tables now include a `UserId` field (UUID) to associate data with specific users:

**Parent Entities:**
- `MusicRelease` - User's music release collection
- `List` - User-defined lists of music releases
- `Kollection` - User-defined collections filtered by genres

**Lookup Entities:**
- `Artist` - User-specific artist names
- `Genre` - User-specific genre categories
- `Label` - User-specific record labels
- `Country` - User-specific countries
- `Format` - User-specific formats (vinyl, CD, etc.)
- `Packaging` - User-specific packaging types
- `Store` - User-specific store names

#### Join Tables
Join tables (e.g., `MusicReleaseArtist`, `MusicReleaseGenre`, `KollectionGenre`) do **not** include `UserId`. Data isolation is enforced through FK chains - these tables reference parent entities that are already scoped by `UserId`.

#### Indexes and Constraints
- **Single UserId Index**: All user-owned entities have an index on `UserId` for efficient filtering
- **Composite Unique Constraints**: Lookup entities have `UNIQUE(UserId, Name)` constraint instead of global unique `Name`
- **Removed Global Unique Constraints**: The global unique constraint on `Name` was removed from lookups to allow different users to have the same lookup values

### 2. Application User Model

#### IsAdmin Flag
Added `IsAdmin` boolean field to `ApplicationUser` model:
```csharp
public bool IsAdmin { get; set; } = false;
```

This flag is used for:
- Including admin role in JWT claims
- Enabling admin users to view/manage data across users (future feature)
- Audit logging for cross-user access

### 3. Authentication & Authorization

#### IUserContext Interface
Created `IUserContext` interface to centralize user context access:
```csharp
public interface IUserContext
{
    Guid? GetUserId();           // Current authenticated user's ID
    bool IsAdmin();              // Check if user is admin
    Guid? GetActingUserId();     // Support admin impersonation
}
```

#### UserContext Implementation
Implemented in `UserContext` service:
- Reads `ClaimTypes.NameIdentifier` from HTTP context
- Checks for `IsAdmin` claim
- Supports admin impersonation via `X-Admin-Act-As` header or `?userId=` query parameter

#### JWT Token Service
Updated `TokenService` to include claims:
- `ClaimTypes.NameIdentifier` - User ID for compatibility
- `JwtRegisteredClaimNames.Sub` - User ID (standard claim)
- `JwtRegisteredClaimNames.Email` - User email
- `IsAdmin` - Boolean admin flag
- `ClaimTypes.Role` - "Admin" role for admin users
- `googleSub` - Google subject identifier

#### Controller Authorization
Added `[Authorize]` attribute to all CRUD controllers:
- ArtistsController
- CountriesController
- FormatsController
- GenresController
- KollectionsController
- LabelsController
- ListsController
- MusicReleasesController
- NowPlayingController
- PackagingsController
- StoresController

**Note:** `AuthController` remains accessible without authentication for login endpoints.

### 4. Service Layer Changes

#### IUserOwnedEntity Interface
Created marker interface for user-owned entities:
```csharp
public interface IUserOwnedEntity
{
    Guid UserId { get; set; }
}
```

All entities with `UserId` implement this interface.

#### GenericCrudService Updates
Updated `GenericCrudService<TEntity, TDto>` to automatically handle user isolation:

**GetAllAsync:**
- Injects `IUserContext` to get current user ID
- Automatically adds `UserId` filter for user-owned entities
- Throws `UnauthorizedAccessException` if user is not authenticated

**GetByIdAsync:**
- Validates that the entity belongs to the current user
- Returns `null` if user doesn't own the entity

**CreateAsync:**
- Automatically sets `UserId` from current user context
- Ensures new entities are created under correct user

**UpdateAsync:**
- Validates user owns the entity before allowing update
- Throws `UnauthorizedAccessException` for unauthorized access

**DeleteAsync:**
- Validates user owns the entity before allowing deletion
- Throws `UnauthorizedAccessException` for unauthorized access

**GetOrCreateByNameAsync:**
- New method for lookup entities
- Finds or creates a lookup by name within user's scope
- Ensures name uniqueness per user (not globally)

#### Service Implementations
All service implementations updated to inject `IUserContext`:
- ArtistService
- CountryService
- FormatService
- GenreService
- LabelService
- PackagingService
- StoreService

### 5. Database Migration

Created migration `AddMultiTenantSupport` (timestamp: 20251213105441):

**Schema Changes:**
- Added `UserId UUID NOT NULL` to all user-owned tables
- Added indexes on `UserId` columns
- Added composite unique constraints `(UserId, Name)` for lookups
- Dropped global unique constraints on `Name` for lookups
- Added `IsAdmin BOOLEAN NOT NULL DEFAULT false` to ApplicationUsers

**Data Migration:**
- Assigned all existing data to admin user: `12337b39-c346-449c-b269-33b2e820d74f`
- Set `IsAdmin = true` for the admin user (Google sub: 112960998711458483443)

**Migration Application:**
```bash
cd backend/KollectorScum.Api
dotnet ef database update
```

## Security Considerations

### Data Isolation
- **Enforced at Service Layer**: All CRUD operations automatically filter by `UserId`
- **Foreign Key Enforcement**: Join tables inherit isolation through parent entity relationships
- **No Cross-User Access**: Users cannot view, modify, or delete data owned by other users
- **Validation on Updates**: Update and delete operations verify ownership before proceeding

### Authentication Requirements
- All API endpoints (except auth endpoints) require valid JWT token
- Tokens include user ID and admin flag
- Token expiry enforced by ASP.NET Core JWT middleware

### Admin Capabilities
Framework in place for admin features:
- Admin flag in database and claims
- `GetActingUserId()` supports impersonation via header/query parameter
- Can be extended for admin UI with "View as User" functionality
- Audit logging recommended for admin cross-user access

## Testing Requirements

### Unit Tests
- Service layer methods with user filtering
- UserContext claim extraction
- Authorization attribute enforcement

### Integration Tests
- Data isolation between users
- Lookup uniqueness per user (same name allowed for different users)
- Cross-user access prevention
- Admin impersonation (if implemented)

### End-to-End Tests
- User registration and login
- Creating entities as different users
- Verifying data isolation in UI
- Duplicate lookup names between users

## Future Enhancements

### Short Term
1. **Admin Dashboard**: UI for viewing all users and their data
2. **Act-As Feature**: Allow admins to view/edit as another user
3. **Audit Logging**: Track admin cross-user access

### Long Term
1. **Shared Lookups**: Optional global lookup tables for common values
2. **User Groups/Teams**: Share data within groups
3. **Data Export/Import**: Per-user data export for backup
4. **Usage Analytics**: Per-user storage and API usage tracking

## Migration Checklist

- [x] Update all models with UserId property
- [x] Add IUserOwnedEntity interface
- [x] Update DbContext with indexes and constraints
- [x] Create and configure EF migration
- [x] Add data migration script
- [x] Create IUserContext interface and implementation
- [x] Update TokenService with IsAdmin claim
- [x] Update GenericCrudService with user filtering
- [x] Update all service implementations
- [x] Add [Authorize] to controllers
- [x] Build and verify compilation
- [ ] Apply database migration
- [ ] Test authentication flow
- [ ] Test data isolation
- [ ] Update frontend for authentication
- [ ] Add login/landing page
- [ ] Handle 401 responses in frontend
- [ ] Update API client with bearer tokens

## Configuration

### JWT Settings
Ensure JWT configuration in `appsettings.json` includes:
```json
{
  "Jwt": {
    "Key": "your-secret-key-here",
    "Issuer": "kollector-scum-api",
    "Audience": "kollector-scum-client",
    "ExpiryMinutes": 60
  }
}
```

### Admin User
Default admin user configured in migration:
- **User ID**: `12337b39-c346-449c-b269-33b2e820d74f`
- **Google Sub**: `112960998711458483443`
- **IsAdmin**: `true`

All existing data is assigned to this user after migration.

## Rollback Plan

If issues arise, rollback by:
1. `dotnet ef database update <PreviousMigration>`
2. Revert code changes: `git revert <commit-hash>`
3. Redeploy application

**Note:** Data created after migration cannot be rolled back. Backup database before applying migration in production.

## Contact & Support

For questions or issues with multi-tenant implementation:
- Review this document
- Check migration logs
- Verify JWT configuration
- Test with admin user first
