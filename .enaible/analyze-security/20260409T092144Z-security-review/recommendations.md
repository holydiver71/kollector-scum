# Recommendations — 2026-04-09 Security Review

| Priority | ID | Action | OWASP |
|---|---|---|---|
| 🔴 Fix now | gap-005 / D3-1 | Remove `minimst` from root `package.json` (malicious package MAL-2025-26438) | A06 |
| 🟡 This week | gap-001 / D1-1 | Suppress semgrep csharp-sqli in QueryController with a documented inline exception OR refactor to use EF Core parameterised queries rather than raw ADO.NET CommandText | A03 |
| 🟡 This week | gap-006 / D2-1 | Add base-directory validation to `resize-cover-images.js:95` — check resolved path is a child of the expected base dir | A01 |
| 🟡 This week | gap-007 / D4-1 | Run `npm audit fix` in `frontend/` and root to patch flatted, minimatch, picomatch, axios, brace-expansion, ajv | A06 |
| 🟡 This week | gap-007 / D4-2 | Update `next` to latest patch release to address GHSA-ggv3-7p47-pfv8 (HTTP smuggling), GHSA-5f7q-jpqc-wp7h (PPR memory), GHSA-3x4c-7xq6-9pq8 (disk cache) | A05 |
| 🟠 This sprint | gap-008 / D5-1 | Review 5 unsafe-formatstring locations in AddReleaseForm.tsx, LookupComponents.tsx, MusicReleaseList.tsx, useReleaseLookups.ts | A03 |
| 🟠 This sprint | gap-009 / D6-1 | Verify ci.yml:31 detect-secrets finding is a false positive and add inline allowlist if confirmed | A02 |
