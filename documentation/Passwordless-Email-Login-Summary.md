# Passwordless Email Login (Magic Link) Implementation Summary

## Overview

This document summarises the implementation of passwordless email login (magic link authentication) for Kollector Scüm. This feature allows invited users to sign in by entering their email address and clicking a time-limited link sent to their inbox, without requiring a password or a Google account.

## Feature Design

### How It Works

1. **User requests a sign-in link** – enters their email on the login page
2. **Backend validates the email** – checks the invite list (only invited emails can request a link)
3. **Magic link is generated and emailed** – a cryptographically secure, single-use, 15-minute token is created and the link is sent
4. **User clicks the link** – navigates to `/auth/magic-link?token=<token>`
5. **Token is verified** – backend validates the token (not used, not expired)
6. **User is signed in** – if the user is new, their account is created; a JWT is returned and stored
7. **User lands on the dashboard**

### Security Properties

- Tokens use `RandomNumberGenerator.GetBytes(32)` → 64-character hex string (256-bit entropy)
- Tokens expire after 15 minutes (configurable via `Email:MagicLinkExpiryMinutes`)
- Tokens are single-use (marked as used immediately on verification)
- Only email addresses on the admin-managed invitation list can request a link (prevents unauthorised sign-ups)
- The request endpoint always returns HTTP 200 regardless of whether the email is known (prevents email enumeration)
- `GoogleSub` made nullable to support users without a Google account; the unique index filters out NULLs to preserve uniqueness for Google-linked accounts

---

## Backend Changes

### New Model: `MagicLinkToken`

`backend/KollectorScum.Api/Models/MagicLinkToken.cs`

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `int` (PK) | Auto-increment identity |
| `Email` | `string` (255) | Email the token was issued for |
| `Token` | `string` (128) | Secure random hex token |
| `CreatedAt` | `DateTime` | Creation timestamp |
| `ExpiresAt` | `DateTime` | Expiry timestamp |
| `IsUsed` | `bool` | Whether the token has been redeemed |
| `UsedAt` | `DateTime?` | When the token was redeemed |

### New Interfaces

| Interface | Location | Purpose |
|-----------|----------|---------|
| `IMagicLinkTokenRepository` | `Interfaces/` | CRUD for `MagicLinkToken` |
| `IEmailService` | `Interfaces/` | Sends the magic link email |
| `IMagicLinkService` | `Interfaces/` | Orchestrates token creation, validation, and marking as used |

### New Service Implementations

| Class | Location | Description |
|-------|----------|-------------|
| `MagicLinkTokenRepository` | `Repositories/` | EF Core repository for tokens |
| `SmtpEmailService` | `Services/` | SMTP-based email sender; logs to console if SMTP not configured |
| `MagicLinkService` | `Services/` | Generates tokens, calls email service, validates tokens |

### Updated: `AuthController`

Two new endpoints added:

#### `POST /api/auth/magic-link/request`

Request body:
```json
{ "email": "user@example.com" }
```

Response (always 200 to prevent enumeration):
```json
{ "message": "If your email is registered, you will receive a sign-in link shortly." }
```

#### `POST /api/auth/magic-link/verify`

Request body:
```json
{ "token": "<hex-token>" }
```

Success response (200):
```json
{
  "token": "<jwt>",
  "profile": {
    "userId": "...",
    "email": "user@example.com",
    "displayName": "user@example.com",
    "selectedTheme": "dark",
    "isAdmin": false
  }
}
```

Error responses: `401 Unauthorized` (invalid/expired token), `403 Forbidden` (not invited)

### Database Migration: `AddMagicLinkTokens`

- Creates `MagicLinkTokens` table with unique index on `Token`, index on `Email` and `ExpiresAt`
- Alters `ApplicationUsers.GoogleSub` from `NOT NULL` to nullable
- Updates the `IX_ApplicationUsers_GoogleSub` unique index to use a partial index filter (`WHERE "GoogleSub" IS NOT NULL`)

### Configuration: `appsettings.json`

```json
"Email": {
  "SmtpHost": "",
  "SmtpPort": "587",
  "SmtpUsername": "",
  "SmtpPassword": "",
  "FromAddress": "noreply@kollector.app",
  "FromName": "Kollector Scüm",
  "EnableSsl": "true",
  "MagicLinkExpiryMinutes": "15"
}
```

Set via environment variables using `__` as separator:
```
Email__SmtpHost=smtp.yourmailprovider.com
Email__SmtpUsername=apikey
Email__SmtpPassword=<your-smtp-password>
```

When `SmtpHost` is blank, the service logs the magic link to the console (useful for local development).

---

## Frontend Changes

### Updated: `app/lib/auth.ts`

Two new exported functions:

```typescript
// Request a magic link email
requestMagicLink(email: string): Promise<void>

// Verify a magic link token and sign in
verifyMagicLink(token: string): Promise<AuthResponse>
```

### New Component: `MagicLinkLoginForm`

`app/components/MagicLinkLoginForm.tsx`

- Email input + "Send Link" button
- Shows a confirmation message after submission
- Handles loading and error states

### New Page: `/auth/magic-link`

`app/auth/magic-link/page.tsx`

- Reads `?token=` from the URL
- Calls `verifyMagicLink` against the backend
- On success: stores JWT and redirects to `/`
- On failure: shows a user-friendly error with a link back to the home page

### Updated: `IntroPage`

`app/components/IntroPage.tsx`

- Added `MagicLinkLoginForm` below the Google Sign-In button
- A divider ("or") separates the two sign-in methods

---

## Tests Added

### Backend (xUnit)

**`MagicLinkServiceTests`** (8 tests):
- Token creation and email dispatch
- Email normalisation to lowercase
- Magic link URL construction
- Token validation (valid, not found, used, expired)
- Mark-as-used behaviour (success and missing token)
- Configurable expiry minutes

**Updated `AuthControllerTests`** (+5 new tests):
- `RequestMagicLink_WithInvitedEmail_ReturnsOk`
- `RequestMagicLink_WithUninvitedEmail_ReturnsOkWithoutSendingToken`
- `VerifyMagicLink_WithValidToken_ExistingUser_ReturnsAuthResponse`
- `VerifyMagicLink_WithValidToken_NewUser_CreatesUserAndReturnsAuthResponse`
- `VerifyMagicLink_WithInvalidToken_ReturnsUnauthorized`
- `VerifyMagicLink_WithValidToken_UninvitedNewUser_ReturnsForbidden`

### Frontend (Jest)

**`auth.test.ts`** (7 tests):
- `requestMagicLink` calls correct endpoint with email
- `requestMagicLink` does not throw on success
- `requestMagicLink` propagates API errors
- `verifyMagicLink` calls correct endpoint with token
- `verifyMagicLink` stores JWT in localStorage
- `verifyMagicLink` returns the full auth response
- `verifyMagicLink` does not store a token on failure

---

## OWASP Considerations

| Concern | Mitigation |
|---------|------------|
| Brute force / token guessing | 256-bit token (64-hex chars), short expiry (15 min) |
| Token replay | Single-use enforcement (marked used on first redemption) |
| Email enumeration | Request endpoint always returns 200 |
| Unauthorised sign-ups | Invite-list gate on both request and verify endpoints |
| Token storage | Tokens stored in DB; no secrets in code |
| Injection | EF Core parameterised queries prevent SQL injection |

---

*Phase: Passwordless Email Login — Implementation completed March 2026*
