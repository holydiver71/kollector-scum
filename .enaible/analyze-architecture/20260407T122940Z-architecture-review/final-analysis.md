# RESULT

**Repository:** kollector-scum  
**Scope:** Full repository  
**Run:** 2026-04-07T12:29:40Z | architecture-review  
**Min Severity:** high  
**Stack:** .NET 8 Web API (C#) + Next.js 15 / React 19 (TypeScript) + Cloudflare Worker (JS) + PostgreSQL + EF Core

---

## RECONNAISSANCE

### Layout
Monorepo with three independently deployable surfaces:
- `backend/KollectorScum.Api/` — single-project .NET 8 API (Controllers / Services / Repositories / Models / DTOs all co-located)
- `backend/KollectorScum.Tests/` — xUnit integration + unit tests (75 test files)
- `frontend/` — Next.js 15 App Router SPA
- `worker/` — Minimal Cloudflare Worker CDN proxy for R2 cover-art (well-scoped, low risk)

### Detected Patterns
- Backend: service-layer architecture with generic `Repository<T>`, UnitOfWork, and interface-per-service
- Frontend: App Router pages with a mix of centralised API clients (`app/api.ts`, `app/lib/api.ts`) and direct `fetch()` in some components
- No Clean Architecture / DDD layering — no distinct Domain or Application layer

### Analyzers Run
| Analyzer | Status | Findings |
|---|---|---|
| architecture:patterns | ✅ ok | 20 high |
| architecture:dependency | ✅ ok | 0 |
| architecture:coupling | ✅ ok | 0 |
| architecture:scalability | ✅ ok | 7 high |

---

## DETERMINISTIC FINDINGS

**Total: 27 high | 0 medium | 0 low**

### Cyclomatic Complexity — Critical Hot Spots (patterns analyzer)

| Severity | Function | CCN | File |
|---|---|---|---|
| HIGH | `buildProgressSteps` | 92 | `frontend/…/DiscogsImportDialog.tsx` |
| HIGH | `fetchReleases` | 89 | `frontend/…/MusicReleaseList.tsx` |
| HIGH | `MusicReleaseService::GetMusicReleasesAsync` | 46 | `backend/…/Services/MusicReleaseService.cs` |
| HIGH | `Sidebar` | 41 | `frontend/…/Sidebar.tsx` |
| HIGH | `FormatIcon` | 41 | `frontend/…/FormatIcon.tsx` |
| HIGH | `collection/page (anon)` | 37 | `frontend/…/collection/page.tsx` |
| HIGH | `ReleaseLinks` | 37 | `frontend/…/ReleaseLinks.tsx` |
| HIGH | `DiscogsAddReleaseWizard` | 34 | `frontend/…/DiscogsAddReleaseWizard.tsx` |
| HIGH | `Header (anon)` | 33 | `frontend/…/Header.tsx` |
| HIGH | `DataSeedingOrchestrator::SeedFromDiscogsAsync` | 33 | `backend/…/Services/DataSeedingOrchestrator.cs` |
| HIGH | `DiscogsResultsStep "match"` | 55 | `frontend/…/DiscogsResultsStep.tsx` |
| HIGH | `TrackListEditor (anon)` | 31 | `frontend/…/TrackListEditor.tsx` |
| HIGH | `DiscogsSearchResults (anon)` | 31 | `frontend/…/DiscogsSearchResults.tsx` |
| HIGH | `MusicReleaseQueryService::BuildFilterExpression` | 29 | `backend/…/Services/MusicReleaseQueryService.cs` |
| HIGH | `MockComboBox` (test) | 28 | `frontend/…/__tests__/AddReleaseForm.test.tsx` |
| HIGH | `MusicReleaseMapperService::MapToSummaryDtosAsync` | 27 | `backend/…/Services/MusicReleaseMapperService.cs` |
| HIGH | `MusicReleaseCommandService::UpdateMusicReleaseAsync` | 25 | `backend/…/Services/MusicReleaseCommandService.cs` |
| HIGH | `page (anon)` | 25 | `frontend/…/page.tsx` |
| HIGH | `LookupComponents::clearLookupCache` | 25 | `frontend/…/LookupComponents.tsx` |
| HIGH | `TrackList (anon)` | 25 | `frontend/…/TrackList.tsx` |

### Scalability (scalability analyzer)

| Severity | Pattern | Context | File |
|---|---|---|---|
| HIGH | thread_safety (medium confidence) | Global CSS variable across sibling components | `Sidebar.tsx:82` |
| HIGH | N+1 (medium confidence) | `.map(id => allGenres.find(g => g.id === id))` | `ClassificationPanel.tsx:177` |
| HIGH | N+1 (medium confidence) | `.map(id => allArtists.find(a => a.id === id))` | `BasicInformationPanel.tsx:38` |
| HIGH | thread_safety (medium confidence) | Rate-limit comment triggered pattern match | `Program.cs:127,133,457` |
| HIGH | thread_safety (medium confidence) | Auth comment triggered pattern match | `AuthController.cs:122` |

> **Note:** The `Program.cs` and `AuthController.cs` thread-safety findings are false positives — triggered by inline comments about rate-limiting, not actual shared-state mutations. The N+1 findings are real. The `Sidebar.tsx` global CSS variable is a legitimate shared-state risk.

---

## INSPECTION / GAP ANALYSIS

### 1. No Clean Architecture Layering — HIGH RISK
**Area:** Backend structure  
**Finding:** All application logic lives in a single project (`KollectorScum.Api`). Controllers call Services directly; Services access `DbContext` via `Repository<T>`. There is no Domain layer, no Application layer, and no Infrastructure boundary. This violates the Clean Architecture principle required by your stated guidelines.  
**Evidence:** `backend/KollectorScum.Api/` contains Controllers, Services, Repositories, Models, DTOs, Middleware, and Data all at the same level. No separate `*.Domain`, `*.Application`, or `*.Infrastructure` projects exist.  
**Risk:** Business logic leaks into services and controllers; testing requires spinning up the full API context; changes ripple across all layers simultaneously.

### 2. Program.cs God-Object — HIGH RISK
**Area:** Backend bootstrap  
**Finding:** `Program.cs` is 580 lines. All DI registration, middleware configuration, rate limiting, CORS, auth, and pipeline setup is co-located in a single file. This is an architectural anti-pattern that becomes unmaintainable as features grow.  
**Evidence:** `wc -l Program.cs` → 580 lines; 3 scalability findings triggered by comments in this file.  
**Risk:** Hard to navigate, hard to test configuration in isolation, merge conflicts increase with team growth.

### 3. Missing Domain-Specific Repository — MEDIUM RISK
**Area:** Data access  
**Finding:** `IMusicReleaseRepository` interface is declared but there is no concrete implementation file (only the generic `Repository<T>` exists). Domain-specific query methods are instead pushed into `MusicReleaseQueryService` and `MusicReleaseQueryBuilder`, which bypass the repository abstraction.  
**Evidence:** No `MusicReleaseRepository.cs` found in `Repositories/`; `MusicReleaseQueryService::BuildFilterExpression` has CCN 29.  
**Risk:** The abstraction boundary is broken — persistence concerns bleed into the service layer.

### 4. Frontend Component God Functions — HIGH RISK
**Area:** Frontend components  
**Finding:** Several components contain anonymous functions with CCN 89–92, indicating that entire data-fetching pipelines, state machines, and render logic are collapsed into single functions with no decomposition.  
**Evidence:**  
- `fetchReleases` CCN 89 in `MusicReleaseList.tsx` (95 NLOC — dense)  
- `buildProgressSteps` CCN 92 in `DiscogsImportDialog.tsx` (270 NLOC)  
- `DiscogsResultsStep "match"` CCN 55 (219 NLOC)  
**Risk:** Impossible to unit-test individual branches; any change risks regression; cognitive load blocks new contributor ramp-up.

### 5. Missing Custom Hooks Abstraction — MEDIUM RISK
**Area:** Frontend data-fetching  
**Finding:** The project has `app/api.ts` and `app/lib/api.ts` as API clients but only 2 custom hooks (`useImageSearch`, `useReleaseLookups`). Most data fetching occurs directly inside component bodies or large anonymous functions. Direct `fetch()` calls exist in `DbConnectionStatus.tsx` and `wizard/panels/ImagesPanel.tsx` bypassing the API client entirely.  
**Risk:** API coupling is scattered; auth headers, error handling, and retry logic are inconsistently applied across call sites.

### 6. Duplicate Component — LOW RISK
**Area:** Frontend component library  
**Finding:** `ConfirmDialog` exists at both `components/ConfirmDialog.tsx` and `components/wizard/ConfirmDialog.tsx`.  
**Risk:** Divergence risk — UI and behaviour can drift between the two without a shared contract.

### 7. N+1 Lookups in Wizard Panels — HIGH RISK
**Area:** Frontend performance  
**Finding:** `ClassificationPanel.tsx:177` and `BasicInformationPanel.tsx:38` both perform linear `.find()` scans inside `.map()` calls against full lookup arrays on every render. At small collection sizes this is unnoticeable; at scale (hundreds of genres/artists) this degrades UI responsiveness.  
**Evidence:** `.map((id) => allGenres.find((g) => g.id === id))` — O(n²) lookup.  
**Risk:** React render performance degrades as lookup data grows; no memoisation in sight.

### 8. No Frontend State Management Strategy — MEDIUM RISK  
**Area:** Frontend architecture  
**Finding:** Data fetching state (`loading`, `error`, `data`) is managed locally in individual components using `useState`/`useEffect`. Contexts exist for `CollectionContext`, `ThemeContext`, and `ImpersonationContext` but there is no shared server-state management (React Query, SWR, or similar). This leads to duplicate API calls across pages and inconsistent loading/error UX.  
**Risk:** Stale data between views, unnecessary network requests, no cache invalidation strategy.

---

## RECOMMENDATIONS

### Priority 1 — Immediate (before new features)
1. **Decompose `MusicReleaseList.fetchReleases` and `DiscogsImportDialog.buildProgressSteps`** — both CCN > 89 and are in the critical path of the most-used UI flows. Extract state machine logic into custom hooks; extract render branches into sub-components.
2. **Fix N+1 lookups in wizard panels** — replace `.map(id => arr.find(...))` with `Map`/`Record` lookups built once outside the render function or memoised with `useMemo`.
3. **Modularise Program.cs** — extract DI registration into extension method classes (`AddAuthServices`, `AddDiscogsServices`, `AddStorageServices` etc.). Target < 100 lines in `Program.cs`.

### Priority 2 — Near-term (next sprint)
4. **Introduce custom data-fetching hooks** — move `fetch()` and API client calls out of component bodies. Centralise loading/error state. Consider adopting SWR or TanStack Query to gain caching, deduplication, and revalidation.
5. **Implement `MusicReleaseRepository`** — give the most queried aggregate its own repository with domain-specific methods (`FindByDiscogsId`, `SearchAsync`, `GetWithTracksAsync`). Retire query complexity from the service layer.
6. **Consolidate `ConfirmDialog`** — keep one component; delete the other.

### Priority 3 — Architectural evolution
7. **Move toward Clean Architecture layering** — introduce `KollectorScum.Domain` (models, interfaces, value objects) and `KollectorScum.Application` (commands, queries, handlers) as separate projects. This aligns with your stated SOLID / Clean Architecture requirements.
8. **Reduce `MusicReleaseService::GetMusicReleasesAsync` complexity (CCN 46)** — this is a single method handling filtering, paging, sorting, and projection. Break into a CQRS-style query handler with a dedicated pipeline.

---

## SUMMARY SCORECARD

| Area | Risk Level | Key Issue |
|---|---|---|
| Backend layering | 🔴 HIGH | Single-project, no Clean Architecture boundary |
| Program.cs | 🔴 HIGH | 580-line god bootstrap file |
| Frontend complexity | 🔴 HIGH | CCN 89–92 in core UI components |
| Data-fetching patterns | 🟡 MEDIUM | No custom hooks; inconsistent API client usage |
| Repository abstraction | 🟡 MEDIUM | IMusicReleaseRepository not implemented |
| N+1 render lookups | 🔴 HIGH | O(n²) in wizard panels |
| Dependency structure | 🟢 LOW | No circular deps detected |
| Coupling | 🟢 LOW | No high-coupling hot spots detected |
| Worker | 🟢 LOW | Minimal, well-scoped CDN proxy |
| Test coverage | 🟢 LOW RISK | 75 test files, good controller/service coverage |

**Report saved to:** `.enaible/analyze-architecture/20260407T122940Z-architecture-review/final-analysis.md`
