# Authentication System

This document describes the JWT-based authentication system with Google Sign-In integration and invitation-only access control.

## Overview

The authentication system allows users to sign in with their Google account and persist their preferences (e.g., selected kollection) across devices. Access to the application is restricted to invited users only, with admin users having the ability to manage invitations and user access.

## Features

- **Google Sign-In Integration**: Users authenticate using their Google account
- **Invitation-Only Access**: New users must be invited by an admin to access the application
- **Admin Management**: Admin users can invite new users, revoke invitations, and manage user access
- **Role-Based Access Control**: Admin users have additional privileges for managing the application

## Backend Components

### Models
- **ApplicationUser**: Represents an authenticated user with Google credentials
  - `IsAdmin`: Boolean flag indicating if the user has admin privileges
- **UserProfile**: Stores user preferences including selected kollection
- **UserInvitation**: Represents an email invitation for accessing the application
  - `Email`: Email address of the invited user
  - `IsUsed`: Whether the invitation has been used
  - `CreatedAt`: When the invitation was created
  - `UsedAt`: When the invitation was used

### Controllers
- **AuthController** (`/api/auth/google`): Authenticates users via Google ID token
  - Validates user email against invitations before allowing access
  - Automatically marks invitations as used when a new user signs in
- **ProfileController** (`/api/profile`): Manages user profile data (requires authentication)
- **AdminController** (`/api/admin/*`): Manages invitations and user access (admin only)

### Services
- **TokenService**: Generates JWT tokens for authenticated users
- **GoogleTokenValidator**: Validates Google ID tokens server-side

### Repositories
- **UserRepository**: CRUD operations for users
- **UserProfileRepository**: CRUD operations for user profiles
- **UserInvitationRepository**: CRUD operations for invitations

## Configuration

### Required Settings

Add the following to `appsettings.json` or environment variables:

```json
{
  "Jwt": {
    "Key": "YourSecureKeyHere-MustBeAtLeast32CharactersLong",
    "Issuer": "KollectorScumApi",
    "Audience": "KollectorScumClient",
    "ExpiryMinutes": "60"
  },
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com"
  }
}
```

### Environment Variables (Production)

For production, set these as environment variables:
- `Jwt__Key`: Secure signing key (min 32 characters)
- `Google__ClientId`: Your Google OAuth client ID

### Google Cloud Console Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing
3. Enable Google+ API
4. Create OAuth 2.0 credentials (Web application)
5. Add authorized JavaScript origins (e.g., `http://localhost:3000`)
6. Copy the Client ID to configuration

## Frontend Integration

### Installation

The frontend components are already included:
- `app/lib/auth.ts`: Authentication helpers
- `app/lib/api.ts`: Updated to include JWT bearer token
- `app/components/GoogleSignIn.tsx`: Google Sign-In button component

### Environment Variables

Add to `.env.local`:
```
NEXT_PUBLIC_GOOGLE_CLIENT_ID=YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com
NEXT_PUBLIC_API_BASE_URL=http://localhost:5072
```

### Usage Example

```tsx
import { GoogleSignIn } from './components/GoogleSignIn';
import { getUserProfile, updateUserProfile } from './lib/auth';

export default function MyPage() {
  const handleSignIn = async (profile) => {
    console.log('User signed in:', profile);
    // Handle post-sign-in logic
  };

  return (
    <div>
      <GoogleSignIn onSignIn={handleSignIn} />
    </div>
  );
}
```

### API Calls with Authentication

All API calls through `fetchJson` automatically include the JWT token if the user is authenticated.

```typescript
import { fetchJson } from './lib/api';

// This will include Authorization header if user is signed in
const data = await fetchJson('/api/profile');
```

## Database Migration

Run the migration to create the necessary tables:

```bash
cd backend/KollectorScum.Api
dotnet ef database update
```

This creates:
- `ApplicationUsers` table
- `UserProfiles` table
- `UserInvitations` table
- Indexes and foreign keys

The migration also automatically:
- Sets `andy.shutt@googlemail.com` as an admin user (if the user already exists)
- Creates an invitation for `andy.shutt@googlemail.com` (if the user doesn't exist yet)

## API Endpoints

### Authentication Endpoints

#### POST /api/auth/google
Authenticate with Google ID token.

**Request:**
```json
{
  "idToken": "google-id-token-string"
}
```

**Response (Success):**
```json
{
  "token": "jwt-token-string",
  "profile": {
    "userId": "guid",
    "email": "user@example.com",
    "displayName": "User Name",
    "selectedKollectionId": 5,
    "isAdmin": false
  }
}
```

**Response (Forbidden - No Invitation):**
```json
{
  "message": "Access is by invitation only. Please contact the administrator for access."
}
```

### Profile Endpoints

#### GET /api/profile
Get current user's profile (requires authentication).

**Response:**
```json
{
  "userId": "guid",
  "email": "user@example.com",
  "displayName": "User Name",
  "selectedKollectionId": 5,
  "isAdmin": false
}
```

#### PUT /api/profile
Update current user's profile (requires authentication).

**Request:**
```json
{
  "selectedKollectionId": 10
}
```

**Response:**
```json
{
  "userId": "guid",
  "email": "user@example.com",
  "displayName": "User Name",
  "selectedKollectionId": 10,
  "isAdmin": false
}
```

### Admin Endpoints (Admin Only)

#### GET /api/admin/invitations
List all invitations.

**Response:**
```json
[
  {
    "id": 1,
    "email": "invited@example.com",
    "createdAt": "2024-01-01T00:00:00Z",
    "isUsed": false,
    "usedAt": null
  }
]
```

#### POST /api/admin/invitations
Create a new invitation.

**Request:**
```json
{
  "email": "newuser@example.com"
}
```

**Response:**
```json
{
  "id": 2,
  "email": "newuser@example.com",
  "createdAt": "2024-01-01T00:00:00Z",
  "isUsed": false,
  "usedAt": null
}
```

#### DELETE /api/admin/invitations/{id}
Revoke an invitation.

**Response:** 204 No Content

#### GET /api/admin/users
List all users with access.

**Response:**
```json
[
  {
    "userId": "guid",
    "email": "user@example.com",
    "displayName": "User Name",
    "createdAt": "2024-01-01T00:00:00Z",
    "isAdmin": false
  }
]
```

#### DELETE /api/admin/users/{userId}
Revoke a user's access.

**Response:** 204 No Content

## Security Considerations

1. **JWT Key**: Use a strong, random key of at least 32 characters
2. **Token Expiry**: Tokens expire after 60 minutes (configurable)
3. **HTTPS**: Always use HTTPS in production
4. **Token Storage**: Consider httpOnly cookies instead of localStorage for production
5. **Rate Limiting**: Consider adding rate limiting to `/api/auth/google`
6. **Validation**: All kollection IDs are validated to ensure they exist
7. **Invitation System**: Only invited users can access the application
8. **Admin Access**: Admin operations require authentication and admin role verification
9. **Self-Protection**: Admins cannot revoke their own access or other admin users' access

## Admin User Setup

The admin user is configured during database migration:
- Email: `andy.shutt@googlemail.com`
- The migration automatically sets this user as admin if they already exist
- If they don't exist, an invitation is created for them
- Once they sign in with their Google account, they will have admin privileges

## Frontend Integration (Admin Features)

### Admin Dashboard
Admin users will see an "Admin" link in the header after signing in. The admin dashboard provides:
- **Invitation Management**: Create and revoke invitations
- **User Management**: View all users and revoke access for non-admin users

### Usage Example

```tsx
import { getUserProfile } from './lib/auth';

// Check if user is admin
const profile = await getUserProfile();
if (profile?.isAdmin) {
  // Show admin features
}
```

## Testing

Run the authentication and admin tests:

```bash
cd backend/KollectorScum.Tests
dotnet test --filter "FullyQualifiedName~AuthControllerTests|FullyQualifiedName~AdminControllerTests|FullyQualifiedName~ProfileControllerTests"
```

All tests should pass:
- 4 tests for AuthController (including invitation validation)
- 8 tests for AdminController (invitation and user management)
- 4 tests for ProfileController

## Backward Compatibility

The system maintains backward compatibility:
- Existing authenticated users continue to have access (they are grandfathered in)
- New users must be invited to access the application
- LocalStorage is used as fallback for kollection selection
- No breaking changes to existing endpoints

## Access Control Flow

1. **New User Signs In**:
   - User authenticates with Google
   - System checks if user exists in database
   - If user doesn't exist, system checks for invitation
   - If no invitation exists, access is denied with 403 Forbidden
   - If invitation exists, user account is created and invitation is marked as used

2. **Existing User Signs In**:
   - User authenticates with Google
   - System recognizes existing user
   - JWT token is generated and returned
   - User gains access to the application

3. **Admin User**:
   - Admin users have `IsAdmin = true` in the database
   - Admin users see an "Admin" link in the header
   - Admin dashboard provides invitation and user management features

## Future Enhancements

1. Email notification when invitations are sent
2. Invitation expiry dates
3. Refresh tokens for long-lived sessions
4. HttpOnly cookies for token storage
5. User profile page with additional preferences
6. Migration tool to sync localStorage data to server on first login
7. Social features (sharing collections, etc.)
8. Audit log for admin actions
