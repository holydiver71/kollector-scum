# Discogs Wizard Migration Summary

## Overview

This phase restructured the Discogs import path on `/add` from a flat tab-based layout into a guided three-step wizard, and replaced the dual-tab page with a clean flow-selection screen.

---

## Goals

- Unify the Discogs import and manual entry flows under a single entry point.
- Make the Discogs journey wizard-driven with explicit step guards (you cannot skip ahead).
- Extract the Discogs mapper into a shared utility used by both the direct-add and edit paths.
- Maintain full test coverage for all new components and the updated page.

---

## New File Structure

```text
frontend/app/components/wizard/discogs/
  types.ts                         – DiscogsWizardState and step type
  mapDiscogsRelease.ts             – Shared Discogs → DTO mapper + helpers
  DiscogsSearchStep.tsx            – Step 1: catalogue-number search form
  DiscogsResultsStep.tsx           – Step 2: results list with required selection
  DiscogsDetailsStep.tsx           – Step 3: full release preview with Add/Edit actions
  DiscogsAddReleaseWizard.tsx      – Container wizard (step machine + routing)
  __tests__/
    mapDiscogsRelease.test.ts      – 31 unit tests for mapper and helpers
    DiscogsSearchStep.test.tsx     – 11 tests for search step
    DiscogsResultsStep.test.tsx    – 12 tests for results step
    DiscogsDetailsStep.test.tsx    – 14 tests for details step
    DiscogsAddReleaseWizard.test.tsx – 16 tests for wizard container
```

---

## Changes to Existing Files

### `frontend/app/add/page.tsx`

**Before:** Rendered a tab strip with "Search Discogs" and "Manual Entry" tabs. Managed all Discogs state (search results, selected result, form data, image URLs) inline alongside the manual wizard.

**After:** Renders a flow-selection card with two choices:
- **Search Discogs** → `DiscogsAddReleaseWizard`
- **Manual Entry** → `AddReleaseWizard`

A "Change method" link allows the user to return to the selection screen at any time. The page no longer manages Discogs state directly.

---

## Three-Step Discogs Wizard

### Step 1 – Search (`DiscogsSearchStep`)

- Catalogue number (required) plus optional format, country, and year filters.
- Search button is disabled until a catalogue number is entered.
- On success: calls `onSearchSuccess(results, request)` to advance the wizard.
- On error / zero results: calls `onSearchError(message)`.
- Accepts `initialValues` to pre-populate all fields on Back navigation.

### Step 2 – Select a Result (`DiscogsResultsStep`)

- Renders the results list with album art thumbnails and metadata.
- Selection is explicit state (highlighted radio-style card); clicking a card does not immediately advance.
- Continue button is disabled until a result is selected.
- Status hint is shown when nothing is selected.
- Back button returns to the search step without losing results.

### Step 3 – View Details (`DiscogsDetailsStep`)

- Fetches the full release payload via `getDiscogsRelease(id)` on mount.
- Renders cover art, metadata, tracklist, and notes.
- Three actions:
  - **Back to Results** – returns to step 2.
  - **Edit Release** – maps the release and hands off to `AddReleaseWizard`.
  - **Add to Collection** – maps the release, POSTs to `/api/musicreleases`, downloads images, redirects.

---

## Shared Mapper: `mapDiscogsRelease`

Extracted from `page.tsx` into a standalone utility. Returns:

```ts
{
  formData: Partial<CreateMusicReleaseDto>;  // Ready to POST or prefill the wizard
  sourceImages: { cover: string | null; thumbnail: string | null }; // For background download
}
```

Both the direct-add and edit-wizard paths call this single function, ensuring they cannot diverge.

Helper functions exported for direct testing:
- `sanitizeFilename(str)` – makes a string safe for use in a filename
- `generateImageFilename(artist, title, year?)` – deterministic image filename
- `parseDuration(str)` – parses `"M:SS"` / `"H:MM:SS"` to seconds
- `extractFilenameFromUrl(url)` – extracts the last path segment

---

## Step Guards

| Target step | Guard |
|-------------|-------|
| `results`   | `searchRequest` must be non-null and have produced results |
| `details`   | `selectedResult` must be non-null |
| edit wizard | `mappedDraft` must be non-null |

---

## Test Coverage

| File | Tests |
|------|-------|
| `mapDiscogsRelease.test.ts` | 31 |
| `DiscogsSearchStep.test.tsx` | 11 |
| `DiscogsResultsStep.test.tsx` | 12 |
| `DiscogsDetailsStep.test.tsx` | 14 |
| `DiscogsAddReleaseWizard.test.tsx` | 16 |
| `add/page.test.tsx` (updated) | 9 |
| **Total new/updated** | **93** |

All 657 frontend tests pass after this change.

---

## Design Decisions

1. **Step guards as explicit state** – the wizard uses a typed `DiscogsWizardStep` union rather than inferring the step from data presence. This makes transitions explicit and testable.

2. **Single mapper for both paths** – `mapDiscogsRelease` is the only place that maps a Discogs release to the internal DTO. The direct-add and edit paths both call it, so they cannot produce different results.

3. **Non-blocking image downloads** – image downloads are fire-and-forget (`Promise.allSettled`) so a failed download never blocks the user from viewing their newly added release.

4. **Legacy components preserved** – `DiscogsSearch`, `DiscogsSearchResults`, and `DiscogsReleasePreview` are not deleted; they remain available for reference or future reuse.
