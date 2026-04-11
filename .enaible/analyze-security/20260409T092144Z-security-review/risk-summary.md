# Risk Summary — 2026-04-09

## Previous scan: 5 CRITICAL | 14 HIGH | 24 MEDIUM

## This scan: 1 CRITICAL (semgrep SQL pattern) | 17 HIGH (OSV 15 + path traversal 2) | 17 MEDIUM

## Status of Previously Identified Issues

| ID | Finding | Status |
|---|---|---|
| SEC-01 | Unauthenticated LLM-to-SQL + no tenant-scoping | ✅ FIXED (auth + scoping added; raw SQL pattern remains flagged) |
| SEC-02 | Real Discogs token in git-tracked file | ✅ FIXED (file removed from tracking, example added) |
| SEC-03 | GitHub Actions shell injection (3 workflows) | ✅ FIXED (env vars used in all 3 workflows) |
| SEC-04 | Docker container runs as root | ✅ FIXED (appuser added, USER directive present) |
| SEC-05 | minimst malicious package | ❌ NOT FIXED (still in package.json) |
| SEC-06 | Path traversal in resize-cover-images.js | ❌ NOT FIXED |
| SEC-07 | Next.js CVEs (HTTP smuggling + DoS) | ❌ NOT FIXED |
| SEC-08 | Prototype pollution in flatted | ❌ NOT FIXED |
| SEC-09 | Unsafe format strings (5 frontend files) | ❌ NOT FIXED |

## Risk Posture Change
- 4 of 9 previously identified issues FIXED (all 4 critical fixes applied)
- 5 issues remain open (1 high-priority malicious dep, dependency CVEs, path traversal, frontend format strings)
- Remaining CRITICAL semgrep flag on QueryController is the dynamic SQL execution pattern (mitigated but code pattern unchanged)
