# Phase Summary — Admin User Impersonation

**Branch:** `feature/admin-user-impersonation`  
**Date:** June 2025  
**Status:** ✅ Complete

---

## Feature Overview

Admin User Impersonation allows a superadmin to temporarily act as any non-admin user in the system, without knowing or requiring the user's credentials. While impersonating, the admin sees the same data, UI, and API responses that the target user would see — enabling support, debugging, and QA workflows.

The feature was built as a transparent layer: all existing API endpoints (profile, releases, collections, etc.) behave identically for the impersonated user without modification, because the user resolution logic lives in a single shared `IUserContext` service.

---

## Architecture Decisions

| Decision | Rationale |
|---|---|
| **Full read-write impersonation** | Admin needs to reproduce user-reported bugs, which may involve writes |
| **Server-log audit only** | Audit trail lives in structured server logs (`LogWarning`), avoiding DB schema changes while still being searchable in production log aggregators |
| **localStorage state** | Impersonation persists across page refreshes (the user is not logged out) without requiring a new JWT, keeping the implementation simple |
| **JWT retained (not swapped)** | The admin's JWT is kept; only a lightweight header (`X-Admin-Act-As`) is injected. The target user's JWT is never issued or exposed |
| **Admin-to-admin blocked** | Prevents privilege escalation chains; an impersonated admin could further impersonate, which is explicitly blocked at the API layer |
| **Self-impersonation blocked** | Meaningless operation; blocked with a clear 400 response |

---

## Backend Changes

| File | Change |
|---|---|
| `backend/KollectorScum.Api/Controllers/ProfileController.cs` | Injects `IUserContext`; `GetProfile()` and `UpdateProfile()` now call `_userContext.GetActingUserId()` instead of reading the claim directly — enabling transparent impersonation without endpoint-specific changes |
| `backend/KollectorScum.Api/Services/UserContext.cs` | Replaced TODO comments with structured `LogWarning` audit logging for header-based impersonation, query-param impersonation, and invalid GUID handling |
| `backend/KollectorScum.Api/Controllers/AdminController.cs` | New `POST /api/admin/impersonate/{userId}` endpoint: requires `[Authorize(Roles = "Admin")]`, blocks self-impersonation and admin-to-admin impersonation (both 400), returns `ImpersonationDto`, emits audit log on success |
| `backend/KollectorScum.Api/DTOs/ImpersonationDto.cs` | New DTO with `UserId`, `Email`, and `DisplayName` properties for the impersonation response payload |

---

## Frontend Changes

| File | Change |
|---|---|
| `frontend/app/contexts/ImpersonationContext.tsx` | New React context providing `startImpersonation()` / `endImpersonation()` / `isImpersonating` / `impersonatedUser`; state persisted to `localStorage`; fires `authChanged` custom event; navigates to `/dashboard` on start and `/admin` on end |
| `frontend/app/lib/api.ts` | `fetchJson()` reads `localStorage` for active impersonation and injects `X-Admin-Act-As: {userId}` header on every API request when impersonating |
| `frontend/app/lib/admin.ts` | New `impersonateUser(userId: string)` function calling `POST /api/admin/impersonate/{userId}`, returning the `ImpersonationDto` payload |
| `frontend/app/components/AdminDashboard.tsx` | "Impersonate" button added per row, visible only for non-admin users; calls `startImpersonation()` from context |
| `frontend/app/components/ImpersonationBanner.tsx` | New amber/orange sticky banner rendered at the top of the page while impersonating; displays impersonated user's display name and email; contains "Exit Impersonation" button wired to `endImpersonation()` |
| `frontend/app/layout.tsx` | `ImpersonationProvider` wrapped around the app; `ImpersonationBanner` inserted inside the provider so it has access to context |

---

## Security Considerations

- **Audit logging** — every impersonation attempt (start, header resolution, query-param resolution, invalid GUID) is logged at `Warning` level with the admin's user ID and the target user ID, enabling traceability in production log aggregators
- **Admin-to-admin blocked** — `AdminController` checks the target user's role before issuing impersonation; returns `400 Bad Request` with a clear error message if the target is an admin
- **Self-impersonation blocked** — controller compares the acting user's ID to the target ID; returns `400 Bad Request`
- **JWT never swapped** — the admin's JWT is unchanged throughout the session; the `X-Admin-Act-As` header carries only the target user's GUID, not any credential
- **Admin role required** — `[Authorize(Roles = "Admin")]` attribute on the impersonation endpoint prevents non-admins from initiating impersonation
- **UI button hidden for admins** — the Impersonate button in `AdminDashboard` is conditionally rendered only for non-admin users, providing defence-in-depth at the UI layer

---

## Test Coverage

### Backend — 779 tests total (24 new)

| Test File | New Tests | What's Covered |
|---|---|---|
| `UserContextTests.cs` | 14 | Header impersonation resolution, query-param impersonation, invalid GUID handling, normal user resolution, audit log output |
| `AdminControllerTests.cs` | 6 | Successful impersonation, self-impersonation rejection, admin-to-admin rejection, non-existent user, unauthorized access, audit log |
| `ProfileControllerTests.cs` | 4 | `GetProfile()` and `UpdateProfile()` using `IUserContext` mock for both normal and impersonated contexts |

### Frontend — 512 tests total (30 new)

| Test File | New Tests | What's Covered |
|---|---|---|
| `ImpersonationContext.test.tsx` | 11 | `startImpersonation()`, `endImpersonation()`, localStorage persistence, `authChanged` event, navigation, initial state |
| `ImpersonationBanner.test.tsx` | 5 | Banner render when impersonating, hidden when not, displays user info, exit button click |
| `AdminDashboard.test.tsx` | 7 | Impersonate button shown for non-admin users, hidden for admin users, click triggers `startImpersonation()` |
| `api.test.ts` | 3 | Header injected when impersonating, not injected when not, correct header value |
| `admin.test.ts` | 4 | `impersonateUser()` happy path, API error propagation, correct endpoint called, return type |

---

## Verification Steps

Follow these steps to manually verify the feature in a running environment:

1. **Start impersonation**
   - Log in as an admin user
   - Navigate to `/admin`
   - Find a non-admin user in the user table
   - Click the **Impersonate** button
   - Confirm you are redirected to `/dashboard`

2. **Verify impersonation UI**
   - An amber/orange banner should be visible at the top of every page
   - The banner should display the impersonated user's name and email

3. **Verify data context**
   - Browse to the user's collection / releases
   - Data shown should belong to the impersonated user, not the admin

4. **Verify admin page is inaccessible while impersonating**
   - Navigate to `/admin` while impersonating
   - You should be redirected away (the profile now resolves as a non-admin role)

5. **End impersonation**
   - Click **Exit Impersonation** in the amber banner
   - Confirm you are redirected back to `/admin`
   - Confirm the banner disappears and admin profile is restored

6. **Verify impersonate button hidden for admin users**
   - In the admin user table, rows for admin-role users should not show an Impersonate button

7. **Verify API-level blocks**
   - `POST /api/admin/impersonate/{yourOwnId}` → `400 Bad Request` (self-impersonation)
   - `POST /api/admin/impersonate/{anotherAdminId}` → `400 Bad Request` (admin-to-admin)
   - Both responses should include a descriptive error message

8. **Verify audit logs**
   - Check the server console/log output during steps 1–7
   - Each impersonation resolution should produce a `WARN` log entry containing the admin user ID and target user ID

---

## Related Files

- Plan document: `plan-adminUserImpersonation.prompt.md`
- Architecture overview: `V2architecture.md`
