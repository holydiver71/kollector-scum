# RESULT (Scope: repo-wide)

- Summary: Security analysis completed for n/a.
- Artifacts: /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/

## RECONNAISSANCE

- Project type: multi_package
- Primary stack: node_js, dotnet
- Detected languages: csharp, javascript, typescript
- Auto-excluded: .enaible/, dist/, build/, node_modules/, __pycache__/, .next/, vendor/, bin/, obj/, coverage/

## DETERMINISTIC FINDINGS (TOOL BASED)

- Deterministic summary artifact: /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/security-deterministic-summary.json
- Deterministic rows surfaced: 10 of 35 summary rows at threshold medium
- Additional deterministic context remains in the retained analyzer artifacts listed above.

| Severity | OWASP Category | Location / Asset | Finding | source_kind | evidence_artifact |
| --- | --- | --- | --- | --- | --- |
| critical | TBD | Controllers/QueryController.cs | Security issue detected (csharp.lang.security.sqli.csharp-sqli.csharp-sqli) | artifact-backed | semgrep.json |
| high | TBD | scripts/resize-cover-images.js | Security issue detected (javascript.lang.security.audit.path-traversal.path-join-resolve-traversal.path-join-resolve-traversal) | artifact-backed | semgrep.json |
| high | TBD | scripts/resize-cover-images.js | Security issue detected (javascript.lang.security.audit.path-traversal.path-join-resolve-traversal.path-join-resolve-traversal) | artifact-backed | semgrep.json |
| high | TBD | frontend/package-lock.json | OSV: GHSA-rf6f-7fwh-wjgh | artifact-backed | osv.json |
| high | TBD | frontend/package-lock.json | OSV: GHSA-23c5-xmqv-rm74 | artifact-backed | osv.json |
| high | TBD | frontend/package-lock.json | OSV: GHSA-3ppc-4f35-3m26 | artifact-backed | osv.json |
| high | TBD | frontend/package-lock.json | OSV: GHSA-7r86-cg39-jmmj | artifact-backed | osv.json |
| high | TBD | frontend/package-lock.json | OSV: GHSA-23c5-xmqv-rm74 | artifact-backed | osv.json |
| high | TBD | frontend/package-lock.json | OSV: GHSA-3ppc-4f35-3m26 | artifact-backed | osv.json |
| high | TBD | frontend/package-lock.json | OSV: GHSA-7r86-cg39-jmmj | artifact-backed | osv.json |

## NON-DETERMNISTIC GAP ANALYSIS (LLM BASED)

- Gap analysis artifact: /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/gap-analysis.json
- Gap rows surfaced: 6 of 6 attention rows at threshold medium
- Full coverage matrix retained: 11 rows in gap-analysis.json

| Area | Category | Status | Severity | Finding | Confidence | Files | source_kind | evidence_artifact |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| . | Data flow assumptions | flagged | high | minimst@^0.0.1-security remains an explicit dependency in root package.json (line 17). OSV still flags MAL-2025-26438 (malicious code). This package should not be a production dependency. NOT FIXED. | High | package.json | direct-inspection + artifact-backed | osv.json |
| backend/KollectorScum.Api/Controllers | Business logic authorization | flagged | high | QueryController now has [Authorize] and ApplyTenantScoping with parameterised @userId. However semgrep still flags command.CommandText = scopedSql (csharp-sqli). The LLM-generated SQL reaches a raw ADO.NET ExecuteReader after CTE wrapping — SqlValidationService allowlist + CTE scoping are compensating controls, but the raw dynamic SQL execution pattern remains. Risk is now medium-high (not critical) since auth gates the endpoint. | High | Controllers/QueryController.cs | direct-inspection | semgrep.json |
| backend/scripts | Business logic authorization | flagged | high | backend/scripts/resize-cover-images.js:95 still flagged with path-join-resolve-traversal (2 findings, both HIGH). No path sanitisation or base-directory validation added. NOT FIXED. | High | scripts/resize-cover-images.js | artifact-backed | semgrep.json |
| frontend | Data flow assumptions | flagged | high | OSV flags 25 findings across flatted (prototype pollution + DoS), minimatch (ReDoS x3), picomatch (ReDoS + method injection), axios (DoS), next (HTTP smuggling + PPR memory + disk cache), ajv (ReDoS), brace-expansion (process hang). No npm audit fix run. NOT FIXED. | High | frontend/package-lock.json, package-lock.json | artifact-backed | osv.json |
| .github | Data flow assumptions | requires_manual_verification | medium | detect-secrets still flags ci.yml:31 as secret_keyword. Inspection shows this is a reference to ${{ secrets.* }} GitHub Actions secret expressions (not a hardcoded secret). Likely false positive but requires manual verification to confirm no literal secret value is embedded. | Medium | workflows/ci.yml | direct-inspection | detect-secrets.json |
| frontend/app/components | Data flow assumptions | flagged | medium | 5 unsafe-formatstring findings remain in AddReleaseForm.tsx:426, LookupComponents.tsx:232,253, MusicReleaseList.tsx:322, wizard/useReleaseLookups.ts:57. No remediation applied. NOT FIXED. | High | components/AddReleaseForm.tsx, components/LookupComponents.tsx, components/MusicReleaseList.tsx, wizard/useReleaseLookups.ts | artifact-backed | semgrep.json |

## RECOMMENDATIONS

| Priority | ID | Action | OWASP |
| --- | --- | --- | --- |
| 🔴 Fix now | gap-005 | Remove `minimst` from root `package.json` — malicious package MAL-2025-26438 | A06 |
| 🟡 This week | gap-001 | Add semgrep inline suppression with documented justification OR refactor `QueryController.ExecuteQueryAsync` to use EF Core parameterised queries instead of raw ADO.NET `CommandText` | A03 |
| 🟡 This week | gap-006 | Add base-directory validation to `scripts/resize-cover-images.js:95` — reject paths that escape the intended directory | A01 |
| 🟡 This week | gap-007 | Run `npm audit fix` in `frontend/` and root — patches flatted, minimatch, picomatch, axios, brace-expansion, ajv | A06 |
| 🟡 This week | gap-007 | Upgrade `next` to latest patch — addresses GHSA-ggv3-7p47-pfv8 (HTTP smuggling), GHSA-5f7q-jpqc-wp7h, GHSA-3x4c-7xq6-9pq8 | A05 |
| 🟠 This sprint | gap-008 | Review 5 unsafe-formatstring locations in `components/AddReleaseForm.tsx`, `components/LookupComponents.tsx`, `components/MusicReleaseList.tsx`, `wizard/useReleaseLookups.ts` | A03 |
| 🟠 This sprint | gap-009 | Verify `workflows/ci.yml:31` detect-secrets finding is a false positive and add inline allowlist entry if confirmed | A02 |

## ATTACHMENTS

- stack-analysis -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/stack-analysis.json
- project structure -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/project-structure.json
- deterministic summary -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/security-deterministic-summary.json
- security:semgrep -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/semgrep.json
- security:detect_secrets -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/detect-secrets.json
- security:osv -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/osv.json
- gap analysis -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/gap-analysis.json
- risk summary -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/risk-summary.md
- retained report -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/final-analysis.md
- optional run metadata -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/run-metadata.env
- optional exclusions snapshot -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/exclusions.txt
- optional analyzer status -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/analyzer-status.txt
- optional stack analyze log -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/stack-analyze.log
- optional analyzer logs -> /Users/andy.shutt/dotnetProjects/kollector-scum/.enaible/analyze-security/20260409T092144Z-security-review/*.log
