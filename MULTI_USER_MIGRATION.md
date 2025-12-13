# Multi-User Architecture Migration Guide

## Overview

This document describes the migration from a single-tenant application (shared database) to a multi-tenant application with user-isolated data. This is a **major architectural change** that was requested after the initial JWT authentication system was implemented.

## Status: WORK IN PROGRESS ⚠️

The migration is **partially complete**. The application is currently in a broken state and requires additional work before it can be deployed.

## What Changed

### Original Architecture
- **Single-tenant**: All users shared the same database records
- **No authentication required**: Users could browse without signing in
- **Kollections were global**: All users saw the same kollections

### New Architecture  
- **Multi-tenant**: Each user has their own isolated data
- **Authentication required**: Users must sign in to access any data
- **User-isolated collections**: Each user only sees their own data

## Completed Changes

### Backend (Partial)

#### 1. Data Model Changes
Added `UserId` field (Guid, Required, FK to ApplicationUser) to:
- ✅ `MusicRelease` model
- ✅ `Kollection` model
- ✅ `List` model

#### 2. Database Relationships
- ✅ Added foreign key relationships from entities to ApplicationUser
- ✅ Created composite unique index on Kollection (UserId, Name)
- ✅ Removed global unique constraint on Kollection.Name
- ✅ Added indexes on UserId fields for performance
- ✅ Created EF migration: `AddUserOwnershipToEntities`

#### 3. Services & Infrastructure
- ✅ Created `ICurrentUserService` and `CurrentUserService`
- ✅ Registered `HttpContextAccessor` in DI container
- ✅ Updated `Program.cs` to register CurrentUserService

#### 4. Query Filtering
- ✅ Updated `MusicReleaseQueryService` to inject ICurrentUserService
- ✅ Modified `BuildFilterExpression` to always filter by UserId
- ✅ Updated `GetMusicReleaseAsync` to check user ownership
- ✅ Updated `GetSearchSuggestionsAsync` to filter by UserId

#### 5. Authorization
- ✅ Added `[Authorize]` attribute to `MusicReleasesController`

### Frontend (Partial)

#### 1. Landing Page
- ✅ Created `LandingPage` component for unauthenticated users
- ✅ Shows app features and Google Sign-In button

#### 2. Conditional Rendering
- ✅ Updated `Sidebar` to hide when not authenticated
- ✅ Updated `page.tsx` to show landing page for unauthenticated users
- ✅ Added authentication state management

#### 3. User Experience
- ✅ Sidebar sets `--sidebar-offset` to 0px when hidden
- ✅ Dashboard only loads data when authenticated

## Remaining Work

### Backend (Critical)

#### 1. Command Services
- [ ] Update `MusicReleaseCommandService` to set UserId on create/update
- [ ] Update `KollectionService` to filter and set UserId
- [ ] Update `ListService` to filter and set UserId
- [ ] Add user ownership validation on delete operations

#### 2. Controller Authorization
Add `[Authorize]` attribute to:
- [ ] `ArtistsController`
- [ ] `GenresController`
- [ ] `LabelsController`
- [ ] `KollectionsController`
- [ ] `ListsController`
- [ ] `NowPlayingController`
- [ ] `SeedController` (or make admin-only)
- [ ] `ImagesController`
- [ ] Other controllers as needed

#### 3. Statistics & Aggregations
- [ ] Update `CollectionStatisticsService` to filter by UserId
- [ ] Update any aggregate queries to be user-scoped

#### 4. Additional Entities
Consider adding UserId to:
- [ ] `NowPlaying` (track who played what)
- [ ] Any other user-specific data

#### 5. Repository Updates
- [ ] Update base Repository<T> class if needed
- [ ] Ensure all CRUD operations respect user boundaries

#### 6. Integration Tests
- [ ] Update all existing tests to work with user context
- [ ] Add tests for unauthorized access attempts
- [ ] Test data isolation between users
- [ ] Add tests for CurrentUserService

### Frontend (Critical)

#### 1. Protected Routes
- [ ] Add route protection to all pages
- [ ] Redirect to landing page on 401 responses
- [ ] Add loading states during auth checks

#### 2. API Error Handling
- [ ] Global 401 response handler
- [ ] Clear token and redirect to login on 401
- [ ] Show appropriate error messages

#### 3. Page Updates
Update all pages to require authentication:
- [ ] `/collection`
- [ ] `/kollections`
- [ ] `/lists`
- [ ] `/add`
- [ ] `/artists`
- [ ] `/genres`
- [ ] `/statistics`
- [ ] `/import`
- [ ] `/export`
- [ ] `/settings`
- [ ] `/profile`

#### 4. User Experience
- [ ] Add logout button in header/sidebar
- [ ] Show user email/name in UI
- [ ] Add loading spinner during auth check
- [ ] Handle authentication state changes

## Critical Data Migration Issues

### Problem: Existing Data Has No UserId

The migration adds `UserId` as a **NOT NULL** field to:
- MusicReleases
- Kollections
- Lists

**Impact**: Running `dotnet ef database update` will **FAIL** on existing databases with data because these fields cannot be null but have no value.

### Solutions

#### Option 1: Fresh Start (Development Only)
```bash
# Drop and recreate database
dotnet ef database drop --force
dotnet ef database update
```
⚠️ **WARNING**: This deletes all data!

#### Option 2: Migration with Default User
Create a migration that:
1. Adds UserId as nullable first
2. Creates a "system" or "legacy" user
3. Updates all records to assign to that user
4. Makes UserId NOT NULL

Example migration code:
```csharp
// Step 1: Add nullable column
migrationBuilder.AddColumn<Guid>(
    name: "UserId",
    table: "MusicReleases",
    nullable: true);

// Step 2: Create system user or use existing
var systemUserId = /* get or create system user */;

// Step 3: Update all records
migrationBuilder.Sql($@"
    UPDATE MusicReleases SET UserId = '{systemUserId}';
    UPDATE Kollections SET UserId = '{systemUserId}';
    UPDATE Lists SET UserId = '{systemUserId}';
");

// Step 4: Make NOT NULL
migrationBuilder.AlterColumn<Guid>(
    name: "UserId",
    table: "MusicReleases",
    nullable: false);
```

#### Option 3: User Assignment Tool
Create a tool to:
1. List all unassigned records
2. Allow admin to assign records to users
3. Provide bulk assignment options

## Testing Strategy

### Unit Tests
- [ ] Test CurrentUserService with various claims
- [ ] Test query filtering logic
- [ ] Test authorization attributes

### Integration Tests
- [ ] Test complete auth flow
- [ ] Test data isolation (user A cannot see user B's data)
- [ ] Test unauthorized access returns 401
- [ ] Test CRUD operations with different users

### End-to-End Tests
- [ ] Sign in flow
- [ ] Data creation and retrieval
- [ ] Logout and re-login
- [ ] Multiple users simultaneously

## Deployment Considerations

### Database Backup
⚠️ **CRITICAL**: Backup database before applying migration!

### Migration Steps
1. Backup production database
2. Test migration on copy of production data
3. Verify data migration script works
4. Apply migration during maintenance window
5. Verify all users can access their data
6. Have rollback plan ready

### Configuration
Ensure these are set:
- `Jwt:Key` - Secure random string
- `Google:ClientId` - Google OAuth client ID
- Connection string with appropriate permissions

### Monitoring
After deployment, monitor for:
- 401 unauthorized errors
- Missing data (user filtering too aggressive)
- Performance impact of additional joins
- User sign-in issues

## Rollback Plan

If issues occur:
1. Restore database from backup
2. Revert to previous git commit
3. Redeploy previous version

## Performance Considerations

### Indexes Added
- `IX_MusicReleases_UserId`
- `IX_Kollections_UserId_Name` (composite, unique)
- `IX_Lists_UserId`

### Query Impact
- All queries now join to ApplicationUser table
- Additional WHERE UserId = X clause on all queries
- Consider query performance with large datasets

### Optimization Ideas
- [ ] Add caching for user data
- [ ] Consider read replicas for heavy read workloads
- [ ] Monitor slow queries and add indexes as needed

## Security Implications

### Improved Security
- ✅ Data isolation between users
- ✅ Cannot access other users' data
- ✅ Authorization at API level

### Security Checklist
- [ ] Verify all endpoints require authentication
- [ ] Test that users cannot bypass filters
- [ ] Ensure admin endpoints are properly secured
- [ ] Review CORS settings
- [ ] Check JWT token security settings

## Documentation Updates Needed

- [ ] Update README with auth requirements
- [ ] Document Google OAuth setup process
- [ ] Add user guide for new users
- [ ] Update API documentation
- [ ] Add troubleshooting guide

## Timeline Estimate

Given the scope of remaining work:
- **Backend completion**: 2-3 days
- **Frontend completion**: 1-2 days
- **Testing**: 2-3 days
- **Data migration strategy**: 1 day
- **Documentation**: 1 day

**Total**: ~7-10 days of development time

## Recommendations

1. **Pause and Review**: This is a fundamental architectural change that should be reviewed by the team
2. **Data Migration Plan**: Create and test data migration strategy before proceeding
3. **Split the PR**: Consider splitting into smaller, reviewable PRs
4. **Testing First**: Complete the backend changes and test thoroughly before finishing frontend
5. **Staging Environment**: Test on staging with production-like data before deploying

## Questions to Answer

1. What should happen to existing data? (assign to which user?)
2. Should there be an admin role with access to all data?
3. Should importing/exporting require special permissions?
4. What happens to shared kollections (if any)?
5. How to handle users who want to share collections?

## Contact

For questions about this migration, please contact the development team.

## Status History

- 2025-12-13: Initial WIP commits for user ownership
- 2025-12-13: Frontend landing page and conditional sidebar
- [Status to be updated as work progresses]
