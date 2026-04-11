# SEC-02: Secrets Purge from Git History — Summary

**Date:** 2026-04-11  
**Severity:** CRITICAL (OWASP A02 — Cryptographic Failures / Secrets Management)  
**Status:** ✅ Fully Resolved

---

## What Was the Problem?

`backend/KollectorScum.Api/appsettings.Development.json` was tracked in git and contained real credentials committed to a **public** repository:

| Secret | Value (now rotated/invalidated) | Risk |
|---|---|---|
| Discogs API Token | `dmIeuaRfMUWwzfZAqTUhlsLMnRYKYvlfpAYMtcSr` | API abuse, data scraping |
| Google OAuth Client ID | `705993289772-ojhjuqlin974vl84uilcm1q9udvv7i0a.apps.googleusercontent.com` | OAuth phishing |
| JWT Key | `DevSecureKeyForJWT-AtLeast32Characters-DoNotUseInProduction` | Token forgery if used in prod |
| Bootstrap Secret | `dev-bootstrap-secret-change-me` | Dev endpoint exposure |

The file appeared in **17+ commits** across the full git history.

---

## What Was Done

### Phase 1 — Stop tracking the file (previously completed)
- `backend/KollectorScum.Api/appsettings.Development.json` added to `.gitignore`
- Redacted example file added: `backend/KollectorScum.Api/appsettings.Development.json.example`
- File removed from git index

### Phase 2 — Rotate credentials
- Discogs API token rotated by the repository owner ✅
- Google OAuth Client ID: review whether this needs to be rotated/restricted in Google Cloud Console

### Phase 3 — Rewrite git history (completed 2026-04-11)
- Installed `git-filter-repo`
- Ran `git-filter-repo --path backend/KollectorScum.Api/appsettings.Development.json --invert-paths --force` to purge the file from all 914 commits across all branches
- Re-added the `origin` remote (filter-repo removes it as a safety measure)
- Force-pushed all 34 branches to `origin`

**Verification:** `git log --all --full-history -- "backend/KollectorScum.Api/appsettings.Development.json"` returns 0 results.

---

## Residual Risks & Actions Required

| Risk | Action | Owner |
|---|---|---|
| GitHub may cache the old objects briefly | Wait ~24 hours for GitHub's GC to run | Automatic |
| Google OAuth Client ID still valid | Review usage in Google Cloud Console; restrict allowed origins/redirect URIs | Developer |
| Any forks of the repo still have the old history | Notify any known forks | Developer |
| CI/CD cache or artifact storage may reference old SHAs | Re-trigger CI on all active PRs | Developer |

---

## Prevention Going Forward

- `appsettings.Development.json` is permanently in `.gitignore`
- Use `dotnet user-secrets` or environment variables for local credentials
- The example file (`appsettings.Development.json.example`) documents all required keys with placeholder values
- Consider adding a pre-commit hook or GitHub secret scanning to catch future leaks

---

## References
- [git-filter-repo documentation](https://github.com/newren/git-filter-repo)
- [GitHub: Removing sensitive data from a repository](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository)
- Original security finding: `.enaible/analyze-security/20260407T124853Z-security-review/final-analysis.md` — SEC-02
