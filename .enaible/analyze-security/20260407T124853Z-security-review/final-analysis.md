# RESULT

**Repository:** kollector-scum  
**Scope:** Full repository  
**Run:** 2026-04-07T12:48:53Z | security-review  
**Min Severity:** medium  
**Stack:** .NET 8 Web API + Next.js 15 + Cloudflare Worker + PostgreSQL  
**Analyzers:** semgrep, detect-secrets, osv-scanner

---

## RECONNAISSANCE

**Security-sensitive surfaces identified:**
- `QueryController` — LLM-to-SQL natural language query pipeline
- `AuthController` — Google OAuth + magic link authentication
- `AdminController` — user impersonation, invitations, admin operations
- `appsettings.Development.json` — credentials file tracked in git
- `.github/workflows/` — 3 CI/CD pipelines using user-controlled inputs
- `backend/Dockerfile` — container build
- `frontend/` — 5 unsafe format string locations, direct `fetch()` calls

---

## DETERMINISTIC FINDINGS

**Total: 5 critical | 14 high | 24 medium**

### Semgrep (12 findings)

| Sev | Rule | File | Line |
|---|---|---|---|
| CRITICAL | `run-shell-injection` | `.github/workflows/apply-migrations.yml` | 45 |
| CRITICAL | `run-shell-injection` | `.github/workflows/ci.yml` | 232 |
| CRITICAL | `run-shell-injection` | `.github/workflows/prevent-pr-to-master.yml` | 12 |
| CRITICAL | `missing-user-entrypoint` | `backend/Dockerfile` | 24 |
| CRITICAL | `csharp-sqli` | `backend/…/Controllers/QueryController.cs` | 112 |
| HIGH | `path-join-resolve-traversal` | `backend/scripts/resize-cover-images.js` | 95 |
| HIGH | `path-join-resolve-traversal` (dup) | `backend/scripts/resize-cover-images.js` | 95 |
| MEDIUM | `unsafe-formatstring` | `frontend/…/AddReleaseForm.tsx` | 426 |
| MEDIUM | `unsafe-formatstring` | `frontend/…/LookupComponents.tsx` | 232 |
| MEDIUM | `unsafe-formatstring` | `frontend/…/LookupComponents.tsx` | 253 |
| MEDIUM | `unsafe-formatstring` | `frontend/…/MusicReleaseList.tsx` | 322 |
| MEDIUM | `unsafe-formatstring` | `frontend/…/wizard/useReleaseLookups.ts` | 57 |

### detect-secrets (5 findings)

| Sev | Type | File | Line |
|---|---|---|---|
| MEDIUM | base64 high-entropy string | `appsettings.Development.json` | 13 |
| MEDIUM | base64 high-entropy string | `appsettings.Development.json` | 17 |
| MEDIUM | secret keyword | `appsettings.Development.json` | 30 |
| MEDIUM | hex high-entropy string | `wizard/discogs/__tests__/mapDiscogsRelease.test.ts` | 64 |
| MEDIUM | secret keyword | `.github/workflows/ci.yml` | 31 |

### OSV (26 findings — 14 unique packages)

| Sev | Package | Version | Vulnerability |
|---|---|---|---|
| HIGH | `flatted` | 3.4.0 / 3.3.3 | Prototype Pollution (GHSA-rf6f-7fwh-wjgh) |
| HIGH | `flatted` | 3.3.3 | Unbounded recursion DoS (GHSA-25h7-pfq9-p65f) |
| HIGH | `minimatch` | 3.1.2 / 9.0.5 | ReDoS × 3 CVEs (GHSA-23c5, GHSA-3ppc, GHSA-7r86) |
| HIGH | `picomatch` | 2.3.1 / 4.0.3 | ReDoS (GHSA-c2c7-rcm5-vvqj) |
| HIGH | `axios` | 1.13.2 | DoS via `__proto__` in mergeConfig (GHSA-43fc-jf86-j433) |
| MEDIUM | `next` | 15.5.12 | HTTP request smuggling in rewrites (GHSA-ggv3-7p47-pfv8) |
| MEDIUM | `next` | 15.5.12 | Unbounded memory via PPR endpoint (GHSA-5f7q-jpqc-wp7h) |
| MEDIUM | `next` | 15.5.12 | Unbounded disk cache growth (GHSA-3x4c-7xq6-9pq8) |
| MEDIUM | `minimst` | 0.0.1-security | **Malicious code** (MAL-2025-26438) |
| MEDIUM | `ajv` | 6.12.6 | ReDoS with `$data` option (GHSA-2g4f-4pwh-qvx6) |
| MEDIUM | `brace-expansion` | 1.1.12 / 2.0.2 | Process hang / memory exhaustion (GHSA-f886-m6hf-6m8v) |
| MEDIUM | `picomatch` | 2.3.1 / 4.0.3 | Method injection (GHSA-3v7f-55p6-f55p) |

---

## INSPECTION / GAP ANALYSIS

### 🔴 SEC-01: Unauthenticated LLM-to-SQL Endpoint — CRITICAL (OWASP A01 + A03)
**File:** `backend/KollectorScum.Api/Controllers/QueryController.cs`  
**Finding:** `QueryController` has no `[Authorize]` attribute, and `BaseApiController` provides no authentication requirement. The `POST /api/query/ask` endpoint is **publicly accessible without authentication**. Any caller can submit natural language queries that are converted by the LLM into SQL and executed directly against the database via `command.CommandText = sql`.  
**Mitigating factor:** `SqlValidationService` enforces SELECT-only with a table allowlist (user/auth tables excluded). However: the allowlist permits full content of `MusicReleases`, `Artists`, `Labels`, etc., raw SQL execution bypasses EF Core row-level filters (multi-tenant scoping), and the LLM-generated SQL is executed without row-ownership checks, so user A can read user B's collection data unauthenticated.  
**Fix:** Add `[Authorize]` to `QueryController`. Add tenant-scoping to `ExecuteQueryAsync` so the current user's context is applied even after SQL validation passes.

### 🔴 SEC-02: Real Discogs API Token in Git-Tracked File — CRITICAL (OWASP A02)
**File:** `backend/KollectorScum.Api/appsettings.Development.json`  
**Finding:** `appsettings.Development.json` is **tracked in git** (confirmed via `git ls-files`). It contains what appears to be a real Discogs personal access token (`dmIeuaRfMUWwzfZAqTUhlsLMnRYKYvlfpAYMtcSr`), a Google OAuth Client ID, and a JWT key. Even if these are "dev" credentials, they are committed to version history and accessible to anyone with repo access. The Google Client ID is a real cloud resource identifier.  
**Fix:** Add `appsettings.Development.json` to `.gitignore` immediately. Rotate the Discogs token. Use `dotnet user-secrets` or environment variables for local dev credentials. Audit git history (`git log --all -- appsettings.Development.json`) and consider a history rewrite if the repo is or will be public.

### 🔴 SEC-03: GitHub Actions Shell Injection — CRITICAL (OWASP A03)
**Files:** `apply-migrations.yml:45`, `ci.yml:232`, `prevent-pr-to-master.yml:12`  
**Finding:** All three workflows interpolate GitHub context expressions (`${{ github.event.inputs.* }}`, `${{ github.event.pull_request.base.ref }}`, `${{ github.ref_name }}`) directly into `run:` shell steps. An attacker who can control these values (e.g., via a crafted PR branch name or workflow_dispatch input) can inject arbitrary shell commands into the CI runner, which has access to repository secrets, cloud credentials, and the deployment pipeline.  
**Fix:** Assign context values to environment variables first (`env: MY_VAR: ${{ ... }}`), then reference `$MY_VAR` in shell. GitHub Actions does not expand `$MY_VAR` as an expression, breaking the injection vector.

### 🔴 SEC-04: Docker Container Runs as Root — CRITICAL (OWASP A05)
**File:** `backend/Dockerfile:24`  
**Finding:** No `USER` directive exists in the Dockerfile. The application container runs as root. If the container is compromised (e.g., via the SQL endpoint above), the attacker has full root access inside the container, making lateral movement and escape significantly easier.  
**Fix:** Add a non-root user: `RUN adduser --disabled-password --gecos '' appuser && USER appuser` before the `ENTRYPOINT`.

### 🟡 SEC-05: `minimst` Flagged as Malicious Package — HIGH (OWASP A06)
**File:** `package.json` (root)  
**Finding:** The root `package.json` explicitly depends on `minimst@^0.0.1-security`. OSV flags `minimst` as containing malicious code (MAL-2025-26438). The `0.0.1-security` suffix is an npm squatting placeholder, but OSV still records the advisory. This package should not be an explicit production dependency.  
**Fix:** Remove `minimst` from `package.json`. It appears to have been added as a typosquatting prevention placeholder — delete it entirely.

### 🟡 SEC-06: Path Traversal in Cover Image Resize Script — HIGH (OWASP A01)
**File:** `backend/scripts/resize-cover-images.js:95`  
**Finding:** The script uses `path.join` / `path.resolve` with values derived from directory traversal without sufficient sanitisation. If this script processes user-supplied filenames or is invoked from an API path, it could be used to read or write files outside the intended directory.  
**Fix:** Validate that resolved paths are always children of the expected base directory before any file operations. Use `path.relative(baseDir, resolvedPath).startsWith('..')` to detect traversal attempts.

### 🟡 SEC-07: Next.js HTTP Request Smuggling — MEDIUM (OWASP A05)
**Package:** `next@15.5.12` (GHSA-ggv3-7p47-pfv8)  
**Finding:** The installed Next.js version has a known HTTP request smuggling vulnerability in the rewrites feature. Combined with Next.js PPR memory consumption and disk cache exhaustion vulnerabilities in the same version, this is a cluster of DoS and potential security bypass risks in the framework itself.  
**Fix:** Run `npm update next` in `frontend/`. Check the Next.js changelog for the minimum safe version addressing all three CVEs.

### 🟡 SEC-08: Prototype Pollution in `flatted` — HIGH (OWASP A08)
**Package:** `flatted` in both `frontend/package-lock.json` and root `package-lock.json`  
**Finding:** `flatted` versions 3.3.3 and 3.4.0 have confirmed prototype pollution via `parse()` (GHSA-rf6f-7fwh-wjgh) and unbounded recursion DoS (GHSA-25h7-pfq9-p65f). Prototype pollution in a shared utility can lead to property injection across the application runtime.  
**Fix:** Update `flatted` to the patched version. Run `npm audit fix` in both the root and `frontend/` directories.

### 🟡 SEC-09: Unsafe Format Strings in Frontend — MEDIUM (OWASP A03)
**Files:** `AddReleaseForm.tsx:426`, `LookupComponents.tsx:232,253`, `MusicReleaseList.tsx:322`, `useReleaseLookups.ts:57`  
**Finding:** Semgrep detected template literals used in contexts that may be rendered or logged with user-controlled values. While React's JSX escapes HTML by default, template literals used in `console.error`, error messages, or non-JSX string construction can expose user input in logs or error UIs without sanitisation.  
**Fix:** Review each location. Ensure user-controlled values in template literals are not passed to `innerHTML`, `eval`, `dangerouslySetInnerHTML`, or logging sinks where they could leak PII or enable injection.

### 🟢 SEC-10: Magic Link Auth — Well-Implemented
**Finding:** `MagicLinkService` correctly enforces: single-use tokens (`IsUsed` check), expiry (15-minute default, configurable), token invalidation on use (`UsedAt` timestamp). No weaknesses found in the magic link flow itself.

### 🟢 SEC-11: Admin Impersonation — Adequately Guarded
**Finding:** `AdminController` has `[Authorize]` at class level. `UserImpersonationService` enforces that admins cannot impersonate other admins and cannot impersonate themselves. Impersonation events are logged. No privilege escalation path found in the service logic.

---

## RECOMMENDATIONS

| Priority | Action | OWASP |
|---|---|---|
| 🔴 Fix now | Add `[Authorize]` to `QueryController` + tenant-scope `ExecuteQueryAsync` | A01, A03 |
| 🔴 Fix now | Add `appsettings.Development.json` to `.gitignore` + rotate Discogs token | A02 |
| 🔴 Fix now | Fix shell injection in all 3 GitHub Actions workflows (use env vars) | A03 |
| 🔴 Fix now | Add non-root `USER` to Dockerfile | A05 |
| 🟡 This week | Remove `minimst` from root `package.json` | A06 |
| 🟡 This week | Update `next` to latest patch release | A05 |
| 🟡 This week | Run `npm audit fix` in `frontend/` and root to resolve flatted, minimatch, axios, picomatch | A06 |
| 🟡 This week | Add path validation in `resize-cover-images.js` | A01 |
| 🟠 This sprint | Review unsafe-formatstring locations in frontend (5 files) | A03 |
| 🟠 This sprint | Audit git history for committed secrets | A02 |

---

## SUMMARY SCORECARD

| Area | Risk | Finding |
|---|---|---|
| LLM SQL endpoint auth | 🔴 CRITICAL | Unauthenticated + unscoped SQL execution |
| Secrets in git | 🔴 CRITICAL | Real Discogs token committed |
| CI/CD shell injection | 🔴 CRITICAL | 3 workflows vulnerable |
| Container security | 🔴 CRITICAL | Runs as root |
| Dependency vulnerabilities | 🟡 HIGH | 14 high/medium CVEs across 8 packages |
| Malicious dependency | 🟡 HIGH | `minimst` flagged malicious |
| Next.js CVEs | 🟡 MEDIUM | HTTP smuggling + DoS in current version |
| Prototype pollution | 🟡 HIGH | `flatted` in 2 lockfiles |
| Magic link auth | 🟢 LOW | Well-implemented, single-use enforced |
| Impersonation | 🟢 LOW | Adequately guarded, logged |
| Input validation (backend) | 🟢 LOW | `SqlValidationService` allowlist is sound |

**Report saved to:** `.enaible/analyze-security/20260407T124853Z-security-review/final-analysis.md`
