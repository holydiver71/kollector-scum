# Authentication System

This document describes the JWT-based authentication system with Google Sign-In integration.

## Overview

The authentication system allows users to sign in with their Google account and persist their preferences (e.g., selected kollection) across devices.

## Backend Components

### Models
- **ApplicationUser**: Represents an authenticated user with Google credentials
- **UserProfile**: Stores user preferences including selected kollection

### Controllers
- **AuthController** (`/api/auth/google`): Authenticates users via Google ID token
- **ProfileController** (`/api/profile`): Manages user profile data (requires authentication)

### Services
- **TokenService**: Generates JWT tokens for authenticated users
- **GoogleTokenValidator**: Validates Google ID tokens server-side

### Repositories
- **UserRepository**: CRUD operations for users
- **UserProfileRepository**: CRUD operations for user profiles

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
- Indexes and foreign keys

## API Endpoints

### POST /api/auth/google
Authenticate with Google ID token.

**Request:**
```json
{
  "idToken": "google-id-token-string"
}
```

**Response:**
```json
{
  "token": "jwt-token-string",
  "profile": {
    "userId": "guid",
    "email": "user@example.com",
    "displayName": "User Name",
    "selectedKollectionId": 5
  }
}
```

### GET /api/profile
Get current user's profile (requires authentication).

**Response:**
```json
{
  "userId": "guid",
  "email": "user@example.com",
  "displayName": "User Name",
  "selectedKollectionId": 5
}
```

### PUT /api/profile
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
  "selectedKollectionId": 10
}
```

## Security Considerations

1. **JWT Key**: Use a strong, random key of at least 32 characters
2. **Token Expiry**: Tokens expire after 60 minutes (configurable)
3. **HTTPS**: Always use HTTPS in production
4. **Token Storage**: Consider httpOnly cookies instead of localStorage for production
5. **Rate Limiting**: Consider adding rate limiting to `/api/auth/google`
6. **Validation**: All kollection IDs are validated to ensure they exist

## Testing

Run the authentication tests:

```bash
cd backend/KollectorScum.Tests
dotnet test --filter "FullyQualifiedName~AuthControllerTests|FullyQualifiedName~ProfileControllerTests"
```

All 7 tests should pass:
- 3 tests for AuthController
- 4 tests for ProfileController

## Backward Compatibility

The system maintains backward compatibility:
- Unauthenticated users can still use the application
- LocalStorage is used as fallback for kollection selection
- No breaking changes to existing endpoints

## Future Enhancements

1. Refresh tokens for long-lived sessions
2. HttpOnly cookies for token storage
3. User profile page with additional preferences
4. Migration tool to sync localStorage data to server on first login
5. Social features (sharing collections, etc.)
