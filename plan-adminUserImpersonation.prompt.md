# Plan: Admin User Impersonation

## TL;DR
Add full impersonation capability: admin clicks "Impersonate" on a non-admin user in the admin dashboard, is taken to the dashboard as that user (full read-write), sees an orange banner to exit and return to their own admin session. Backend already has `X-Admin-Act-As` header support in `UserContext.GetActingUserId()` and all services already call it. Main work: wire up the frontend context + header injection + banner UI, fix `ProfileController` to use `GetActingUserId()`, add a validation/initiation endpoint, and add audit logging.

---

## Decisions
- **Full read-write** impersonation (admin can do anything as the impersonated user)
- **Server log (ILogger) only** — no DB audit table
- Impersonation state stored in localStorage (survives page refresh)
- Admin retains their own JWT token throughout; `X-Admin-Act-As` header carries the target user ID
- When impersonating, `/api/profile` returns the impersonated user's profile (non-admin). Admin page will redirect away — correct behavior. Banner always visible for exit.
- Exiting impersonation: clears localStorage state, fires 'authChanged', returns to `/admin`
- Admin-to-admin impersonation is blocked (backend validates target is non-admin)

---

## Phase 1: Backend (parallel-capable internally)

### Step 1.1 — Fix ProfileController to respect impersonation
- File: `backend/KollectorScum.Api/Controllers/ProfileController.cs`
- Inject `IUserContext` into `ProfileController` constructor
- Replace all `GetUserIdFromClaims()` calls with `_userContext.GetActingUserId()`
- Keep `GetUserIdFromClaims()` as fallback or remove (verify no other usage)

### Step 1.2 — Implement audit logging in UserContext
- File: `backend/KollectorScum.Api/Services/UserContext.cs`
- Replace TODO comments with `_logger.LogWarning("Admin impersonation: AdminId={AdminId} acting as UserId={TargetUserId} Path={Path}", adminId, actAsUserId, requestPath)`
- Log at Warning level to ensure visibility

### Step 1.3 — Add POST /api/admin/impersonate/{userId} endpoint
- File: `backend/KollectorScum.Api/Controllers/AdminController.cs`
- New endpoint: `[HttpPost("impersonate/{userId}")]`
- Admin-only protection (existing `IsUserAdminAsync()` pattern)
- Validates: target user exists, target user is NOT an admin
- Returns: `{ userId, email, displayName }` DTO (new `ImpersonationDto` or inline)
- Log: `_logger.LogWarning("Admin {AdminId} initiated impersonation of user {TargetId}", adminId, userId)`
- Returns 400 if target is admin, 404 if not found, 200 with user info

### Step 1.4 — NEW: UserContextTests.cs
- File: `backend/KollectorScum.Tests/Services/UserContextTests.cs`
- Set up IHttpContextAccessor mock with controllable Claims + Headers + Query
- GetActingUserId_AdminWithValidHeader_ReturnsTargetUserId
- GetActingUserId_AdminWithInvalidGuidInHeader_FallsBackToOwnId
- GetActingUserId_AdminWithEmptyHeader_FallsBackToOwnId
- GetActingUserId_AdminWithQueryParam_ReturnsTargetUserId
- GetActingUserId_HeaderTakesPrecedenceOverQueryParam
- GetActingUserId_NonAdminWithHeader_IgnoresHeaderReturnsOwnId
- GetActingUserId_AdminWithNoHeaderOrQuery_ReturnsOwnId
- GetActingUserId_UnauthenticatedUser_ReturnsNull
- IsAdmin_WithTrueIsAdminClaim_ReturnsTrue
- IsAdmin_WithFalseIsAdminClaim_ReturnsFalse
- IsAdmin_WithMissingClaim_ReturnsFalse
- GetUserId_WithValidGuidClaim_ReturnsUserId
- GetUserId_WithMissingClaim_ReturnsNull
- GetUserId_WithMalformedGuidClaim_ReturnsNull

### Step 1.5 — Additions to AdminControllerTests.cs
- ImpersonateUser_AsAdmin_WithValidNonAdminUser_Returns200WithUserInfo
- ImpersonateUser_AsNonAdmin_ReturnsForbidden (403)
- ImpersonateUser_UserNotFound_ReturnsNotFound (404)
- ImpersonateUser_TargetUserIsAdmin_ReturnsBadRequest (400)
- ImpersonateUser_AdminImpersonatingSelf_ReturnsBadRequest (400 — edge case: self-impersonation)
- ImpersonateUser_WhenUnauthenticated_ReturnsUnauthorized (401 via [Authorize])

### Step 1.6 — Additions to ProfileControllerTests.cs (after Step 1.1 refactor)
- ProfileController now accepts IUserContext (injected); update constructor in existing tests
- GetProfile_AdminImpersonatingNonAdminUser_ReturnsImpersonatedProfile (mock IUserContext.GetActingUserId → targetId)
- GetProfile_AdminWithNoImpersonation_ReturnsAdminOwnProfile
- GetProfile_NonAdminUser_CannotImpersonate_ReturnsOwnProfile (IUserContext returns own ID regardless)
- GetProfile_ImpersonatedUserNotFound_ReturnsNotFound (acting userId not in repo)

---

## Phase 2: Frontend (after Phase 1 plan is clear)

### Step 2.1 — Create ImpersonationContext
- New file: `frontend/app/contexts/ImpersonationContext.tsx`
- Follow `CollectionContext` pattern
- localStorage keys: `impersonation_userId`, `impersonation_email`, `impersonation_displayName`
- State: `{ isImpersonating, impersonatedUserId, impersonatedUserEmail, impersonatedUserDisplayName }`
- Actions:
  - `startImpersonation(user: { userId, email, displayName })` — sets localStorage, fires 'authChanged', redirects to `/dashboard`
  - `endImpersonation()` — clears localStorage, fires 'authChanged', redirects to `/admin`
- Reads localStorage on mount (survives refresh)
- Export `useImpersonation()` hook

### Step 2.2 — Update fetchJson() to inject X-Admin-Act-As header
- File: `frontend/app/lib/api.ts`
- When impersonation_userId is in localStorage, add `'X-Admin-Act-As': impersonatedUserId` to request headers
- Read directly from localStorage (same pattern as `getAuthToken()`)

### Step 2.3 — Add impersonateUser() API function
- File: `frontend/app/lib/admin.ts`
- New function: `impersonateUser(userId: string): Promise<{ userId: string; email: string; displayName: string }>`
- Calls `POST /api/admin/impersonate/{userId}`

### Step 2.4 — Add Impersonate button to AdminDashboard
- File: `frontend/app/components/AdminDashboard.tsx`
- In Active Users table, add "Impersonate" action button for non-admin users (alongside existing "Deactivate")
- On click: call `impersonateUser(userId)` to get user details, then call `startImpersonation(user)` from context
- Style: blue text button (consistent with "Activate" action style)

### Step 2.5 — Create ImpersonationBanner component
- New file: `frontend/app/components/ImpersonationBanner.tsx`
- Only renders when `isImpersonating` is true
- Shows: "⚠ Impersonating: [displayName or email] — Exit Impersonation"
- Distinct orange/amber styling (warning color) to be clearly visible
- "Exit Impersonation" button calls `endImpersonation()`
- Sticky positioning at top of viewport

### Step 2.6 — Wire ImpersonationProvider + Banner into layout
- File: `frontend/app/layout.tsx`
- Add `ImpersonationProvider` wrapping (inside `ThemeProvider`, outside `AuthGuard`)
- Add `<ImpersonationBanner />` inside the layout before `<Header />`

### Step 2.7 — Frontend tests

#### ImpersonationContext.test.tsx (new)
Location: `frontend/app/__tests__/ImpersonationContext.test.tsx`
- `initialState_withNoLocalStorage_isImpersonatingIsFalse`
- `initialState_withExistingLocalStorageKeys_hydratesState` (simulate page refresh)
- `startImpersonation_setsAllThreeLocalStorageKeys`
- `startImpersonation_setsIsImpersonatingTrue`
- `startImpersonation_firesAuthChangedEvent`
- `startImpersonation_navigatesToDashboard` (mock useRouter)
- `endImpersonation_clearsAllImpersonationLocalStorageKeys`
- `endImpersonation_setsIsImpersonatingFalse`
- `endImpersonation_firesAuthChangedEvent`
- `endImpersonation_navigatesToAdminPage` (mock useRouter)
- `endImpersonation_doesNotClearUnrelatedLocalStorageKeys` (auth_token etc. survive)

#### ImpersonationBanner.test.tsx (new)
Location: `frontend/app/__tests__/ImpersonationBanner.test.tsx`
- `doesNotRender_whenNotImpersonating`
- `renders_whenImpersonating_withDisplayName`
- `renders_whenImpersonating_fallsBackToEmailWhenDisplayNameAbsent`
- `exitButton_callsEndImpersonation_onClick`
- `bannerText_includesImpersonatedUserIdentity`

#### AdminDashboard.test.tsx (new)
Location: `frontend/app/__tests__/AdminDashboard.test.tsx`
- `impersonateButton_visibleForNonAdminUsers`
- `impersonateButton_notVisibleForAdminUsers`
- `impersonateButton_notVisibleWhileLoading`
- `clickingImpersonate_callsImpersonateUserApiWithCorrectUserId`
- `clickingImpersonate_onSuccess_callsStartImpersonation`
- `clickingImpersonate_onApiError_displaysErrorMessage`
- `clickingImpersonate_onApiError_doesNotCallStartImpersonation`

#### api.test.ts (new)
Location: `frontend/app/__tests__/api.test.ts`
- `fetchJson_withImpersonationInLocalStorage_includesXAdminActAsHeader`
- `fetchJson_withoutImpersonationInLocalStorage_doesNotIncludeXAdminActAsHeader`
- `fetchJson_withImpersonation_stillIncludesAuthorizationHeader`

#### admin.test.ts (new or additions)
Location: `frontend/app/__tests__/admin.test.ts`
- `impersonateUser_makesPostToCorrectEndpoint`
- `impersonateUser_returnsUserDataOnSuccess`
- `impersonateUser_throwsOnForbiddenResponse` (403 — non-admin calling)
- `impersonateUser_throwsOnNotFoundResponse` (404 — user not found)

---

## Relevant Files

### Backend
- `backend/KollectorScum.Api/Controllers/AdminController.cs` — add impersonate endpoint
- `backend/KollectorScum.Api/Controllers/ProfileController.cs` — fix to use GetActingUserId()
- `backend/KollectorScum.Api/Services/UserContext.cs` — add audit logging
- `backend/KollectorScum.Api/Interfaces/IUserContext.cs` — no changes needed (interface already has GetActingUserId)
- `backend/KollectorScum.Tests/Services/UserContextTests.cs` — new file
- `backend/KollectorScum.Tests/Controllers/AdminControllerTests.cs` — additions
- `backend/KollectorScum.Tests/Controllers/ProfileControllerTests.cs` — additions + constructor update

### Frontend
- `frontend/app/contexts/ImpersonationContext.tsx` — new file
- `frontend/app/lib/api.ts` — add X-Admin-Act-As header injection
- `frontend/app/lib/admin.ts` — add impersonateUser() function
- `frontend/app/components/AdminDashboard.tsx` — add Impersonate button
- `frontend/app/components/ImpersonationBanner.tsx` — new file
- `frontend/app/layout.tsx` — add ImpersonationProvider + ImpersonationBanner
- `frontend/app/__tests__/ImpersonationContext.test.tsx` — new file
- `frontend/app/__tests__/ImpersonationBanner.test.tsx` — new file
- `frontend/app/__tests__/AdminDashboard.test.tsx` — new file
- `frontend/app/__tests__/api.test.ts` — new file
- `frontend/app/__tests__/admin.test.ts` — new or additions

---

## Verification
1. Run backend tests: dotnet test in `backend/KollectorScum.Tests/`
2. Manual: Log in as admin → admin page → click Impersonate on a user → redirected to dashboard, orange banner visible
3. Manual: Navigate collection/releases — data shown is the impersonated user's
4. Manual: Visit `/admin` while impersonating — redirect away (isAdmin=false on profile)
5. Manual: Click "Exit Impersonation" in banner → returned to `/admin`, admin profile restored
6. Manual: Impersonate button not shown for admin users in the table
7. Manual: Second admin cannot be impersonated (POST endpoint returns 400)
8. Verify: `X-Admin-Act-As` without valid admin token → 401/403 (backend rejects)
9. Run frontend tests: npm test in `frontend/`
