# Admin Impersonation Tests Summary

**Branch:** `feature/admin-user-impersonation`  
**Date:** 2025  
**Test framework:** xUnit + Moq  

---

## Overview

Comprehensive unit tests were written for the admin user impersonation feature across three test files. All **779 tests pass** (24 new tests added).

---

## Files Created / Modified

### New: `backend/KollectorScum.Tests/Services/UserContextTests.cs`

14 unit tests for `UserContext`, covering the full `GetActingUserId()` impersonation logic, `IsAdmin()`, and `GetUserId()`.

| Test | Scenario |
|------|----------|
| `GetActingUserId_AdminWithValidHeader_ReturnsTargetUserId` | Admin + valid `X-Admin-Act-As` header → target GUID returned |
| `GetActingUserId_AdminWithInvalidGuidInHeader_FallsBackToOwnId` | Admin + malformed header value → own ID returned |
| `GetActingUserId_AdminWithEmptyHeader_FallsBackToOwnId` | Admin + no header → own ID returned |
| `GetActingUserId_AdminWithQueryParam_ReturnsTargetUserId` | Admin + valid `userId` query param → target GUID returned |
| `GetActingUserId_HeaderTakesPrecedenceOverQueryParam` | Both header and query set → header wins |
| `GetActingUserId_NonAdminWithHeader_IgnoresHeaderReturnsOwnId` | Non-admin + header → header ignored, own ID returned |
| `GetActingUserId_AdminWithNoHeaderOrQuery_ReturnsOwnId` | Admin + no signals → own ID returned |
| `GetActingUserId_UnauthenticatedUser_ReturnsNull` | No claims → null |
| `IsAdmin_WithTrueIsAdminClaim_ReturnsTrue` | `IsAdmin` claim = "True" → true |
| `IsAdmin_WithFalseIsAdminClaim_ReturnsFalse` | `IsAdmin` claim = "False" → false |
| `IsAdmin_WithMissingClaim_ReturnsFalse` | No `IsAdmin` claim → false |
| `GetUserId_WithValidGuidClaim_ReturnsUserId` | Valid `NameIdentifier` → GUID returned |
| `GetUserId_WithMissingClaim_ReturnsNull` | No `NameIdentifier` → null |
| `GetUserId_WithMalformedGuidClaim_ReturnsNull` | Non-GUID `NameIdentifier` → null |

**Setup pattern:** `DefaultHttpContext` + `IHttpContextAccessor` mock; claims set via `ClaimsPrincipal`; headers via `Request.Headers`; query via `Request.QueryString`.

---

### Modified: `backend/KollectorScum.Tests/Controllers/AdminControllerTests.cs`

6 new tests plus a `SetupAdminUser()` helper method for `AdminController.ImpersonateUser`.

| Test | Expected Result |
|------|----------------|
| `ImpersonateUser_AsAdmin_WithValidNonAdminUser_Returns200WithUserInfo` | 200 OK with `ImpersonationDto` (userId, email, displayName) |
| `ImpersonateUser_AsNonAdmin_ReturnsForbidden` | 403 Forbid |
| `ImpersonateUser_UserNotFound_ReturnsNotFound` | 404 Not Found |
| `ImpersonateUser_TargetUserIsAdmin_ReturnsBadRequest` | 400 Bad Request |
| `ImpersonateUser_AdminImpersonatingSelf_ReturnsBadRequest` | 400 Bad Request |
| `ImpersonateUser_WhenUnauthenticated_ReturnsForbidden` | 403 Forbid |

Uses the existing in-memory EF Core database. `SetupAdminUser()` helper adds the admin to `ApplicationUsers` DbSet and configures the mock repository.

---

### Modified: `backend/KollectorScum.Tests/Controllers/ProfileControllerTests.cs`

4 new tests for impersonation via `IUserContext.GetActingUserId()` in `ProfileController.GetProfile()`.

| Test | Scenario |
|------|----------|
| `GetProfile_AdminImpersonatingNonAdminUser_ReturnsImpersonatedProfile` | Mock returns target GUID → profile is for target user |
| `GetProfile_AdminWithNoImpersonation_ReturnsAdminOwnProfile` | Mock returns admin GUID → admin profile returned |
| `GetProfile_NonAdminUser_ReturnsOwnProfile` | Mock returns own GUID → own profile returned |
| `GetProfile_ImpersonatedUserNotFound_ReturnsNotFound` | Target user missing in repo → 404 |

These tests use the `Mock<IUserContext>` already injected into the test class constructor.

---

## Test Results

```
Passed!  - Failed: 0, Passed: 779, Skipped: 0, Total: 779, Duration: 7s
```
