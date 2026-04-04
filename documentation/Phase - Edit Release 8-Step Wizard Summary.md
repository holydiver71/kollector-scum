# Phase – Edit Release 8-Step Wizard Summary

## Overview

The Edit Release page (`/releases/[id]/edit`) has been updated to use the same
8-step wizard format that is already used when adding a release manually or via
Discogs import. Previously the page used the legacy `AddReleaseForm` component;
it now uses `AddReleaseWizard` in **edit mode**.

---

## Changes Made

### 1. `frontend/app/components/wizard/panels/DraftPreviewPanel.tsx`

- Added optional `submitLabel?: string` prop (default `"Add to Collection"`).
- The final submit button now renders `{submitLabel}` so callers can customise
  the label without duplicating the panel.

### 2. `frontend/app/components/wizard/AddReleaseWizard.tsx`

- Added `releaseId?: number` prop. When supplied the wizard operates in
  **edit mode**:
  - The final step sends a `PUT /api/musicreleases/{releaseId}` via the
    existing `updateRelease` helper instead of a `POST`.
  - `onSuccess` is called with the same `releaseId` (no parsing required).
  - `DraftPreviewPanel` is rendered with `submitLabel="Save Changes"`.
- Added `prebuiltFormData?: WizardFormData` prop. When provided it takes
  precedence over `initialData` for the initial form state. This lets callers
  populate display-only fields (e.g. `artistDisplayNames`) that have no
  equivalent in `CreateMusicReleaseDto` without polluting the create/update DTO.

### 3. `frontend/app/releases/[id]/edit/page.tsx`

- Removed import of legacy `AddReleaseForm` and `InitialSelectedItems`.
- Fetches the release from the API as before.
- Builds a `Partial<CreateMusicReleaseDto>` with **only ID fields** for
  artists/genres/label/etc. (no `artistNames`/`genreNames` so they are not
  mistakenly treated as new-entity creation requests).
- Calls `fromCreateDto(dto)` and manually overrides `artistDisplayNames` with
  the resolved name strings from the API response so the Draft Preview panel
  shows correct names immediately.
- Renders `<AddReleaseWizard prebuiltFormData={…} releaseId={id} />`.

---

## 8 Wizard Steps (unchanged)

| Step | Panel                    | Required |
|------|--------------------------|----------|
| 1    | Basic Information         | ✓        |
| 2    | Release Information       |          |
| 3    | Label & Dates             |          |
| 4    | Purchase Information      |          |
| 5    | Images                    |          |
| 6    | Track Listing             |          |
| 7    | External Links            |          |
| 8    | Draft Preview             |          |

---

## Tests

Five new tests added to
`frontend/app/components/wizard/__tests__/AddReleaseWizard.test.tsx` under the
`AddReleaseWizard – edit mode (releaseId provided)` describe block:

1. Calls `updateRelease` (PUT) instead of `fetchJson` (POST) when `releaseId`
   is set.
2. Calls `onSuccess` with the existing `releaseId` after a successful update.
3. Shows a submit error when `updateRelease` throws.
4. Passes `submitLabel="Save Changes"` to `DraftPreviewPanel` in edit mode.
5. Accepts `prebuiltFormData` as the initial form state.

Total: **40 tests passing** (35 existing + 5 new).

---

## Backward Compatibility

- All existing `AddReleaseWizard` usages (add via Discogs, add manually) are
  unaffected; both new props are optional with sensible defaults.
- `DraftPreviewPanel`'s `submitLabel` defaults to `"Add to Collection"`, so
  all existing callers are unaffected.
