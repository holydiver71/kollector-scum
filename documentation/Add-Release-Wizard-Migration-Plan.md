# Add Release Wizard – Migration Plan

**Goal:** Replace the existing flat `AddReleaseForm` on the `/add` page with the multi-step wizard
layout built in the mock-up (`frontend/mock-up/`), wired to the real API.

---

## Background

| Item | Current state |
|---|---|
| Route | `/add` → `app/add/page.tsx` |
| Manual form | `app/components/AddReleaseForm.tsx` — single-page form, all fields visible at once |
| Discogs tab | unchanged — works independently, pre-populates the manual form via `initialData` |
| Mock-up wizard | `frontend/mock-up/` — 8-step wizard, no API calls, fixture data only |
| Mock-up route | `/mockup` → `app/mockup/page.tsx` (review only, not linked from nav) |

The wizard's `MockFormData` type already mirrors `CreateMusicReleaseDto` closely. The panels
use fixture data for lookups; that needs swapping for real API data.

---

## Steps

### Step 1 – Align the form data type  
**Files:** `mock-up/types.ts` → new `app/components/wizard/types.ts`

- Copy `MockFormData`, `MockTrack`, `MockMedia`, `MockLink`, `MockImages`, `MockPurchaseInfo`,
  `WizardStep`, and `LookupItem` into a new shared types file at
  `app/components/wizard/types.ts`.
- Extend `WizardFormData` (rename from `MockFormData`) with ID fields alongside name fields
  to match `CreateMusicReleaseDto`:
  - `artistIds?: number[]`
  - `genreIds?: number[]`
  - `labelId?: number`
  - `countryId?: number`
  - `formatId?: number`
  - `packagingId?: number`
  - `storeId?: number`
- Add a pure mapper function `toCreateDto(data: WizardFormData): CreateMusicReleaseDto`.
- Keep `MockFormData` in `mock-up/types.ts` pointing at the new type (re-export) to avoid
  breaking the mock-up during development.

---

### Step 2 – Create a shared lookup data hook  
**New file:** `app/components/wizard/useReleaseLookups.ts`

Extract the lookup fetching already done in `app/add/page.tsx` and `AddReleaseForm.tsx` into a
single custom hook:

```ts
export function useReleaseLookups() {
  // fetches: /api/artists, /api/labels, /api/genres,
  //          /api/countries, /api/formats, /api/packagings, /api/stores
  return { artists, labels, genres, countries, formats, packagings, stores, loading, error };
}
```

This ensures lookup data is fetched once at wizard mount and passed down as props, eliminating
duplicate fetches in each panel.

---

### Step 3 – Migrate and wire each panel  
**Source:** `frontend/mock-up/panels/`  
**Destination:** `app/components/wizard/panels/`

Copy all active panels and replace fixture lookups with real data props:

| Panel | Lookup fixtures to replace | Notes |
|---|---|---|
| `BasicInformationPanel` | `ARTISTS` fixture → `artists: LookupItem[]` prop | Use `ComboBox` component for artist autocomplete + free-text new-artist support (mirrors existing `AddReleaseForm` artist handling) |
| `ClassificationPanel` | `GENRES`, `FORMATS`, `PACKAGINGS`, `COUNTRIES` fixtures → props | Genre multi-tag remains; format/packaging/country become real dropdowns |
| `LabelInformationPanel` | `LABELS` fixture → `labels: LookupItem[]` prop | Free-text new-label support |
| `PurchaseInformationPanel` | `STORES` fixture → `stores: LookupItem[]` prop | Free-text new-store support |
| `ImagesPanel` | No lookups — keep as-is | Consider wiring `/api/images/download` for Discogs CDN URLs in a later iteration |
| `TrackListingPanel` | No lookups | Compare against existing `TrackListEditor` component; reuse or replace |
| `ExternalLinksPanel` | No lookups — keep as-is | |
| `DraftPreviewPanel` | No lookups | Replace mock save with real submit (see Step 4) |

**Do not migrate:**
- `ReleaseDatesPanel.tsx` — retired, fields merged into `LabelInformationPanel`
- `LiveRecordingPanel.tsx` — retired, control merged into `ClassificationPanel`

Panel component signature pattern:
```ts
interface PanelProps {
  data: WizardFormData;
  onChange: (patch: Partial<WizardFormData>) => void;
  errors: Partial<Record<keyof WizardFormData, string>>;
  lookups: ReturnType<typeof useReleaseLookups>;  // passed down from wizard shell
}
```

---

### Step 4 – Build the production wizard shell  
**New file:** `app/components/wizard/AddReleaseWizard.tsx`

Based on `mock-up/AddReleaseWizard.tsx` with the following changes:

1. **Props:**
   ```ts
   interface AddReleaseWizardProps {
     initialData?: Partial<WizardFormData>;   // pre-populated from Discogs
     onSuccess?: (releaseId: number) => void;
     onCancel?: () => void;
   }
   ```

2. **Lookup data:** Call `useReleaseLookups()` inside the wizard and pass the result to every
   panel as a `lookups` prop.

3. **Submit handler** (replaces `handleSaveMock`):
   ```ts
   async function handleSubmit() {
     const dto = toCreateDto(formData);
     const result = await fetchJson<CreateMusicReleaseResponseDto>(
       '/api/musicreleases', { method: 'POST', body: JSON.stringify(dto) }
     );
     onSuccess?.(result.release.id);
   }
   ```

4. **Error handling:** Show API errors in the DraftPreviewPanel footer (not a page-level toast,
   so the user stays in context).

5. **Loading state:** Disable the submit button and show a spinner while the POST is in flight.

6. **Keep `StepIndicator.tsx` as-is** — already production quality, just move it to
   `app/components/wizard/StepIndicator.tsx`.

---

### Step 5 – Replace the manual tab in `/add`  
**File:** `app/add/page.tsx`

- Replace the `<AddReleaseForm ... />` render with `<AddReleaseWizard ... />`.
- Wire `initialData` from the Discogs preview mapper — `discogsFormData` already maps to
  `Partial<CreateMusicReleaseDto>`; add a thin adapter to `Partial<WizardFormData>`.
- Remove the `existingArtists`/`existingLabels`/etc. state from the page — these move into
  `useReleaseLookups` inside the wizard.
- Keep the Discogs tab, search state, and image URL download logic completely unchanged.

---

### Step 6 – Validation parity  
**File:** `app/components/wizard/AddReleaseWizard.tsx`

The mock-up validates step 0 (title + at least one artist). Add parity with `AddReleaseForm`
validation:

| Step | Required validation |
|---|---|
| 0 – Basic Information | `title` not empty, at least one artist |
| All others | Optional — user can skip or leave blank |

Real-time field-level error clearing on change already exists in the mock-up wizard; keep it.

---

### Step 7 – Tests  
**New files:** `app/components/wizard/__tests__/`

Write unit tests for:

- `toCreateDto` mapper — input/output shape validation
- `useReleaseLookups` hook — mock fetch, loading/error states
- `AddReleaseWizard` — step navigation, validation on step 0, submit calls correct endpoint
- Each panel — renders fields, onChange fires with correct patch shape

Target: ≥ 80% coverage on the wizard directory.

---

### Step 8 – Clean up  
After the new wizard is live and tests pass:

- [ ] Delete `app/components/AddReleaseForm.tsx` (replaced by wizard)
- [ ] Delete `frontend/mock-up/panels/ReleaseDatesPanel.tsx` (retired)
- [ ] Delete `frontend/mock-up/panels/LiveRecordingPanel.tsx` (retired)
- [ ] Decide whether to keep `frontend/mock-up/` as a design reference or remove entirely
- [ ] Remove `/mockup` route (`app/mockup/`) if no longer needed

---

## File Structure After Migration

```
app/
  add/
    page.tsx                          ← updated (uses AddReleaseWizard)
  components/
    wizard/
      AddReleaseWizard.tsx            ← new (production shell)
      StepIndicator.tsx               ← moved from mock-up/
      types.ts                        ← new (WizardFormData, toCreateDto)
      useReleaseLookups.ts            ← new (shared hook)
      panels/
        BasicInformationPanel.tsx     ← migrated + real lookups
        ClassificationPanel.tsx       ← migrated + real lookups
        LabelInformationPanel.tsx     ← migrated + real lookups
        PurchaseInformationPanel.tsx  ← migrated + real lookups
        ImagesPanel.tsx               ← migrated (no lookups)
        TrackListingPanel.tsx         ← migrated (review vs TrackListEditor)
        ExternalLinksPanel.tsx        ← migrated (no lookups)
        DraftPreviewPanel.tsx         ← migrated + real submit
      __tests__/
        toCreateDto.test.ts
        useReleaseLookups.test.ts
        AddReleaseWizard.test.tsx
        (panel tests)
mock-up/                              ← kept as design reference until Step 8
```

---

## Risks & Notes

| Risk | Mitigation |
|---|---|
| `ComboBox` API (for artists/labels) is tightly coupled to `AddReleaseForm` internals | Read `ComboBox.tsx` before migrating `BasicInformationPanel` — may need minor prop adjustments |
| `TrackListEditor` vs mock `TrackListingPanel` | Compare both before deciding which to carry forward; mock version has better visual styling |
| Discogs `initialData` mapping to `WizardFormData` | The existing mapper in `page.tsx` targets `CreateMusicReleaseDto`; write a thin adapter rather than rewriting the mapper |
| API lookup performance on wizard mount | `useReleaseLookups` should use `Promise.all` so all 7 endpoints fire in parallel |
