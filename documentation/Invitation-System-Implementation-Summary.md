# Invitation-Only Access System - Implementation Summary

## Overview
This document summarizes the implementation of the invitation-only access system for Kollector Scum.

## What Was Implemented

### Backend Changes

#### 1. Database Schema
- **UserInvitation Table**: Stores email invitations
  - `Id`: Primary key
  - `Email`: Email address (unique, indexed)
  - `CreatedAt`: Timestamp when invitation was created
  - `CreatedByUserId`: ID of admin who created the invitation
  - `IsUsed`: Boolean flag indicating if invitation was used
  - `UsedAt`: Timestamp when invitation was used

- **ApplicationUser Updates**: Added `IsAdmin` boolean field

#### 2. Repositories
- `IUserInvitationRepository` and `UserInvitationRepository`: CRUD operations for invitations
- Updated `IUserRepository` and `UserRepository`: Added `GetAllAsync()` and `DeleteAsync()` methods

#### 3. Controllers
- **AuthController**: Modified to validate email against invitations before allowing new user access
- **AdminController**: New controller with endpoints for managing invitations and users
  - GET /api/admin/invitations - List all invitations
  - POST /api/admin/invitations - Create new invitation
  - DELETE /api/admin/invitations/{id} - Delete invitation
  - GET /api/admin/users - List all users
  - DELETE /api/admin/users/{userId} - Revoke user access

#### 4. DTOs
- `UserInvitationDto`: DTO for invitation data
- `CreateInvitationRequest`: Request DTO for creating invitations
- `UserAccessDto`: DTO for user access information
- Updated `UserProfileDto`: Added `IsAdmin` field

#### 5. Database Migrations
- `20260103232652_AddUserInvitations`: Creates UserInvitations table
- `20260103232801_SetAdminUser`: Sets andy.shutt@googlemail.com as admin and creates initial invitation

### Frontend Changes

#### 1. Components
- **AdminDashboard.tsx**: Full-featured admin dashboard with:
  - Invitation management (create, list, revoke)
  - User management (list, revoke access)
  - Success/error message handling
- **AdminPage**: Protected route that verifies admin access before showing dashboard

#### 2. API Helpers
- **admin.ts**: API functions for admin operations
  - `getInvitations()`
  - `createInvitation(email)`
  - `deleteInvitation(id)`
  - `getUsers()`
  - `revokeUserAccess(userId)`

#### 3. UI Updates
- **GoogleSignIn.tsx**: 
  - Shows "Admin" link for admin users
  - Enhanced error handling for 403 Forbidden responses
- **auth.ts**: Updated `UserProfile` interface to include `isAdmin` field

### Testing

#### Unit Tests Added
- **AdminControllerTests.cs**: 8 tests covering:
  - Getting invitations (admin and non-admin)
  - Creating invitations (success and duplicate cases)
  - Deleting invitations
  - Getting users
  - Revoking access (with self-protection checks)

- **AuthControllerTests.cs**: Updated with 4 tests including:
  - New user with invitation
  - Existing user authentication
  - Invalid token
  - Uninvited user (403 Forbidden)

### Documentation
- Updated **AUTHENTICATION.md** with comprehensive documentation including:
  - Invitation system overview
  - Admin setup instructions
  - API endpoint documentation
  - Security considerations
  - Access control flow

## How to Use

### For Administrators

1. **First Time Setup**:
   - Run database migrations: `dotnet ef database update`
   - Sign in with andy.shutt@googlemail.com Google account
   - You will automatically have admin privileges

2. **Inviting Users**:
   - Sign in as admin
   - Click "Admin" link in header
   - Enter email address in "Invite New User" section
   - Click "Send Invitation"
   - User can now sign in with their Google account

3. **Managing Users**:
   - View all active users in "Active Users" section
   - Revoke access by clicking "Revoke Access" (cannot revoke admin users)

4. **Managing Invitations**:
   - View all invitations in "Pending Invitations" section
   - See which invitations have been used
   - Revoke unused invitations by clicking "Revoke"

### For New Users

1. Navigate to the application
2. Click "Sign in with Google"
3. If invited: Access granted, account created
4. If not invited: See message "Access is by invitation only. Please contact the administrator for access."

## Security Features

1. **Authorization**: All admin endpoints require authentication and admin role
2. **Self-Protection**: Admins cannot revoke their own access
3. **Admin Protection**: Cannot revoke access for other admin users
4. **Email Validation**: Server-side email format validation
5. **Case-Insensitive Email**: Emails are normalized to lowercase
6. **Invitation Tracking**: Invitations are marked as used when activated

## API Endpoints Summary

### Authentication
- POST /api/auth/google - Authenticate with Google (checks invitation for new users)

### Profile
- GET /api/profile - Get current user profile (includes isAdmin flag)
- PUT /api/profile - Update user profile

### Admin (Requires Admin Role)
- GET /api/admin/invitations - List invitations
- POST /api/admin/invitations - Create invitation
- DELETE /api/admin/invitations/{id} - Delete invitation
- GET /api/admin/users - List users
- DELETE /api/admin/users/{userId} - Revoke user access

## Database Schema Changes

```sql
-- New table: UserInvitations
CREATE TABLE "UserInvitations" (
    "Id" SERIAL PRIMARY KEY,
    "Email" VARCHAR(255) NOT NULL UNIQUE,
    "CreatedAt" TIMESTAMP NOT NULL,
    "CreatedByUserId" UUID NOT NULL,
    "IsUsed" BOOLEAN NOT NULL DEFAULT FALSE,
    "UsedAt" TIMESTAMP NULL
);

-- Updated table: ApplicationUsers
ALTER TABLE "ApplicationUsers" ADD COLUMN "IsAdmin" BOOLEAN NOT NULL DEFAULT FALSE;
```

## Files Changed

### Backend
- Controllers/AdminController.cs (new)
- Controllers/AuthController.cs (modified)
- Controllers/ProfileController.cs (modified)
- DTOs/InvitationDtos.cs (new)
- DTOs/ProfileDtos.cs (modified)
- Interfaces/IUserInvitationRepository.cs (new)
- Interfaces/IUserRepository.cs (modified)
- Models/UserInvitation.cs (new)
- Models/ApplicationUser.cs (existing - IsAdmin added)
- Repositories/UserInvitationRepository.cs (new)
- Repositories/UserRepository.cs (modified)
- Data/KollectorScumDbContext.cs (modified)
- Program.cs (modified)
- Migrations/20260103232652_AddUserInvitations.cs (new)
- Migrations/20260103232801_SetAdminUser.cs (new)

### Frontend
- app/admin/page.tsx (new)
- app/components/AdminDashboard.tsx (new)
- app/components/GoogleSignIn.tsx (modified)
- app/lib/admin.ts (new)
- app/lib/auth.ts (modified)

### Tests
- Tests/Controllers/AdminControllerTests.cs (new)
- Tests/Controllers/AuthControllerTests.cs (modified)

### Documentation
- AUTHENTICATION.md (updated)

## Deployment Checklist

- [ ] Run database migrations: `dotnet ef database update`
- [ ] Verify andy.shutt@googlemail.com can sign in
- [ ] Verify admin dashboard is accessible at /admin
- [ ] Test creating an invitation
- [ ] Test that uninvited users are blocked
- [ ] Test that invited users can sign in
- [ ] Test revoking user access
- [ ] Verify all tests pass: `dotnet test`

## Future Enhancements

- Email notifications when invitations are sent
- Invitation expiry dates
- Audit log for admin actions
- Bulk invitation import
- Custom invitation messages
- Role-based permissions (beyond admin/user)
