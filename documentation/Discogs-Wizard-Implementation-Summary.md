# Discogs Add-Release Wizard Implementation Summary

## Overview

Implements the three-step Discogs import wizard as specified in
`documentation/Add-Release-Wizard-Migration-Plan.md`.

The `/add` page is restructured from a two-tab UI into a **source-selection
card screen** that branches into two independent wizard flows:

- **Search Discogs** → the new guided Discogs wizard
- **Manual Entry** → the existing `AddReleaseWizard`

Both flows share the same Discogs-to-DTO mapper and image-download helper so
their data paths cannot diverge.

**Branch:** `copilot/add-wizard-for-releases`

---

## Files Created

### Discogs wizard

| File | Purpose |
|------|---------|
| `wizard/discogs/types.ts` | Step model (`DiscogsWizardStep`), state shape (`DiscogsWizardState`), step-guard helpers (`canEnterResults`, `canEnterDetails`) |
| `wizard/discogs/mapDiscogsRelease.ts` | Reusable Discogs → DTO mapper (`mapDiscogsRelease`); pure helpers (`parseDuration`, `sanitizeFilename`, `generateImageFilename`, `extractFilenameFromUrl`); `downloadDiscogsImages` helper |
| `wizard/discogs/DiscogsSearchStep.tsx` | Step 1 – catalogue number form; validates required field and year range; calls `onSearchSuccess` with results and request |
| `wizard/discogs/DiscogsResultsStep.tsx` | Step 2 – result list with mandatory selection; shows inline error if Continue is clicked without a selection |
| `wizard/discogs/DiscogsDetailsStep.tsx` | Step 3 – fetches full release details; new-entity detection badges; Add to Collection / Edit Release actions |
| `wizard/discogs/DiscogsAddReleaseWizard.tsx` | Wizard shell; orchestrates step transitions; posts to `/api/musicreleases`; triggers image downloads; calls `onSuccess` or `onEditRelease` |

### Tests

| File | Tests |
|------|-------|
| `discogs/__tests__/mapDiscogsRelease.test.ts` | 30 unit tests for all pure functions |
| `discogs/__tests__/DiscogsSearchStep.test.tsx` | 21 tests – rendering, validation, callbacks |
| `discogs/__tests__/DiscogsResultsStep.test.tsx` | 16 tests – rendering, selection guards, keyboard accessibility |
| `discogs/__tests__/DiscogsDetailsStep.test.tsx` | 15 tests – loading/error states, metadata display, entity badges, callbacks |
| `discogs/__tests__/DiscogsAddReleaseWizard.test.tsx` | 11 tests – step transitions, add-to-collection, edit handoff, error handling |

### Page

| File | Change |
|------|--------|
| `app/add/page.tsx` | Replaced tab UI with a two-card source-selection screen |
| `app/add/__tests__/page.test.tsx` | Aligned with new page structure |

---

## Wizard Flow

```
/add (source selection)
 ├─ Search Discogs
 │   └─ DiscogsAddReleaseWizard
 │       ├─ Step 1: DiscogsSearchStep  (catalogue number + filters)
 │       ├─ Step 2: DiscogsResultsStep (result list, selection required)
 │       └─ Step 3: DiscogsDetailsStep (full details)
 │           ├─ Add to Collection → POST /api/musicreleases → download images → redirect
 │           └─ Edit Release → hand off to AddReleaseWizard with pre-filled data
 └─ Manual Entry
     └─ AddReleaseWizard (unchanged 8-step wizard)
```

---

## Key Design Decisions

### Shared mapper
`mapDiscogsRelease` is the single source of truth for Discogs → DTO conversion.
Both "Add to Collection" and "Edit Release" call it to guarantee the same data
shape regardless of which branch the user takes.

### Step guards
Step transitions are validated before advancing:
- Cannot reach results without at least one search result
- Cannot reach details without a selected result
- The Continue button in the results step is disabled until a row is selected

### Image downloads are best-effort
`downloadDiscogsImages` runs in the background after a successful save.
Errors are swallowed so a Discogs rate-limit or network hiccup never blocks the
user from seeing their newly-added release.

### Backward navigation preserves state
When the user goes back to the search step, their previous criteria and results
are retained so they can refine without starting over.

---

## Test Coverage

| Suite | Tests | Status |
|-------|-------|--------|
| `mapDiscogsRelease` | 30 | ✅ |
| `DiscogsSearchStep` | 21 | ✅ |
| `DiscogsResultsStep` | 16 | ✅ |
| `DiscogsDetailsStep` | 15 | ✅ |
| `DiscogsAddReleaseWizard` | 11 | ✅ |
| Existing suite (unchanged) | 565 | ✅ |
| **Total** | **658** | ✅ |
