# Add Release Wizard – Discogs Flow Plan

**Goal:** Turn the Discogs import path on `/add` into a guided wizard with explicit steps:

1. Search Discogs parameters
2. Search results with mandatory selection before continuing
3. View details using the same preview surface as manual entry

From the details step, the user can either add the release directly to the collection or continue into the existing edit wizard with Discogs data prefilled.

---

## Current State

| Item | Current state |
|---|---|
| Route | `/add` → `frontend/app/add/page.tsx` |
| Manual flow | `frontend/app/components/wizard/AddReleaseWizard.tsx` is already the production manual-entry wizard |
| Discogs flow | `page.tsx` renders a tabbed flow using `DiscogsSearch`, `DiscogsSearchResults`, and `DiscogsReleasePreview` |
| Transition between flows | `handleEditManually` maps Discogs data to `CreateMusicReleaseDto` and switches tabs into the manual wizard |
| Preview duplication | Discogs details preview and manual draft preview are separate UIs with overlapping responsibilities |

The remaining work is not building the manual wizard. It is restructuring the Discogs import journey so it becomes the front half of the same guided add-release experience.

---

## Target User Flow

### Step 1 – Search Discogs

- User enters catalogue number and optional filters.
- Search submission moves the wizard forward only when at least one result is returned.
- Search criteria remain editable when navigating backward.

### Step 2 – Select a Result

- User sees Discogs matches in a dedicated results step.
- A result must be selected before continuing.
- Going back returns to the search step without losing criteria or results.

### Step 3 – View Details

- Wizard fetches the full Discogs release payload for the selected result.
- The release is shown through the same preview presentation used by manual entry so users see a single, consistent preview model.
- User can either:
  - add directly to collection
  - edit release, which hands the mapped data into the existing release-edit wizard

If the user chooses Edit Release, the existing multi-step release wizard becomes the continuation of the flow rather than a separate tab.

---

## Implementation Steps

### Step 1 – Extract Discogs flow state from the page

**Files:** `frontend/app/add/page.tsx`, new `frontend/app/components/wizard/discogs/*`

- Move Discogs-specific state and handlers out of `page.tsx`:
  - search criteria
  - search results
  - selected result
  - details-loading state
  - mapped Discogs form data
  - Discogs image download metadata
- Replace the current tab-based orchestration with a dedicated container component such as `DiscogsAddReleaseWizard`.
- Keep the page thin: it should decide which top-level flow to render, not manage each Discogs interaction itself.

### Step 2 – Define Discogs wizard steps and state machine

**New file:** `frontend/app/components/wizard/discogs/types.ts`

- Introduce a Discogs-specific step model:
  - `search`
  - `results`
  - `details`
  - optionally `edit` if the handoff to `AddReleaseWizard` is represented inside the same shell
- Add a small state model for the flow, for example:

```ts
interface DiscogsWizardState {
  search: DiscogsSearchRequest;
  results: DiscogsSearchResult[];
  selectedResult: DiscogsSearchResult | null;
  selectedRelease: DiscogsRelease | null;
  mappedDraft?: Partial<CreateMusicReleaseDto>;
  sourceImages: { cover: string | null; thumbnail: string | null };
}
```

- Enforce step guards:
  - cannot enter results without a completed search
  - cannot enter details without a selected result
  - cannot enter edit wizard without mapped release data

### Step 3 – Convert the search form into wizard step 1

**Reuse:** `frontend/app/components/DiscogsSearch.tsx`

- Either wrap the existing component or move it under the wizard folder as `DiscogsSearchStep.tsx`.
- Keep the current inputs and validation, but adapt the component API to wizard semantics:
  - `onSearchSuccess(results, request)`
  - `onSearchError(message)`
  - `initialValues`
- Make successful search advance the wizard to step 2 automatically.
- Preserve the submitted request so the user can go back and refine it without retyping.

### Step 4 – Convert results into wizard step 2 with required selection

**Reuse:** `frontend/app/components/DiscogsSearchResults.tsx`

- Make result selection explicit state, not just a click-through action.
- Add a footer action bar with `Back` and `Continue` so the user selects first, then proceeds.
- Keep a single-click shortcut if desired, but the step should still model selection as required state.
- Support replacing the selected result without forcing a new search.

Validation for this step:

- block `Continue` until one result is selected
- show inline error text if the user attempts to proceed without a selection

### Step 5 – Replace the Discogs details screen with a shared preview surface

**Files:** `frontend/app/components/DiscogsReleasePreview.tsx`, `frontend/app/components/wizard/panels/DraftPreviewPanel.tsx`

- Extract the presentation layer from `DraftPreviewPanel` into a shared component, for example `ReleasePreviewCard` or `ReleasePreviewLayout`.
- Use that shared component in two places:
  - manual wizard draft preview
  - Discogs details step
- Keep each container responsible for its own actions:
  - manual preview keeps `Back` and `Save Release`
  - Discogs preview gets `Back to Results`, `Add to Collection`, and `Edit Release`

This is the key refactor. Without it, the product still has two competing preview experiences.

### Step 6 – Normalize Discogs release mapping once

**Files:** `frontend/app/add/page.tsx` logic to extract into a shared utility, new `frontend/app/components/wizard/discogs/mapDiscogsRelease.ts`

- Move `mapDiscogsToFormData` out of `page.tsx` into a reusable utility.
- Return both:
  - mapped `Partial<CreateMusicReleaseDto>` for the edit wizard handoff
  - source image URLs for post-save download
- Reuse that mapping for both actions:
  - direct add to collection
  - edit in wizard
- Keep duration parsing and filename generation in the shared mapper so the two paths cannot diverge.

### Step 7 – Wire the action branching at step 3

**Files:** `frontend/app/components/wizard/discogs/DiscogsAddReleaseWizard.tsx`, `frontend/app/components/wizard/AddReleaseWizard.tsx`

- `Add to Collection`:
  - map Discogs release to DTO
  - POST to `/api/musicreleases`
  - trigger image downloads afterward
  - redirect to the created release
- `Edit Release`:
  - map Discogs release to `initialData`
  - transition into `AddReleaseWizard`
  - preserve image source metadata so image download still happens after manual save

The important constraint is that both branches must share the same mapping and image-download logic.

### Step 8 – Simplify `/add` into a flow switcher instead of a tabbed page

**File:** `frontend/app/add/page.tsx`

- Remove the current manual/discogs tab UI.
- Replace it with a higher-level choice appropriate to the product direction:
  - render the Discogs wizard by default, with a path into manual entry
  - or render an initial source selection screen: `Discogs Import` or `Manual Entry`
- Ensure `AddReleaseWizard` remains usable directly for fully manual entry.

Recommended approach:

- initial source selection card
- Discogs path opens the new Discogs wizard
- Manual path opens `AddReleaseWizard`

This avoids forcing two tabs to coexist once both flows are already wizard-driven.

### Step 9 – Test the full Discogs wizard path

**New tests:** `frontend/app/components/wizard/discogs/__tests__/`

- Search step:
  - validation for missing catalogue number
  - successful search advances to results
  - no-results and API-error handling
- Results step:
  - continue blocked without selection
  - selected result persists when navigating back and forward
- Details step:
  - release details fetched on entry
  - add-to-collection calls correct endpoint
  - edit-release launches `AddReleaseWizard` with expected initial data
- Shared preview:
  - manual preview and Discogs preview render the same release metadata blocks

### Step 10 – Documentation and cleanup

- Update the existing wizard summary once implementation is complete.
- Add a new phase summary in `documentation/` describing the Discogs wizard migration.
- Remove obsolete page-level Discogs orchestration code from `page.tsx`.
- Keep or delete the legacy `DiscogsReleasePreview` only after the shared preview extraction is complete.

---

## Proposed File Structure

```text
frontend/app/components/wizard/
  AddReleaseWizard.tsx
  preview/
    ReleasePreviewLayout.tsx
  discogs/
    DiscogsAddReleaseWizard.tsx
    DiscogsSearchStep.tsx
    DiscogsResultsStep.tsx
    DiscogsDetailsStep.tsx
    mapDiscogsRelease.ts
    types.ts
    __tests__/
```

---

## Main Risks

| Risk | Mitigation |
|---|---|
| Preview duplication continues under new names | Extract one shared preview layout before changing step orchestration |
| Discogs direct-add and edit flows drift apart | Use one shared Discogs-to-DTO mapper and one shared image-download helper |
| The current page keeps too much state and becomes harder to reason about | Move the Discogs flow into its own wizard container and keep `page.tsx` as composition only |
| Step transitions become brittle | Model step guards explicitly instead of inferring them from `selectedResult`/`searchResults` presence |
| Manual entry becomes harder to access after the change | Add an explicit source-selection entry point or a clear switch to manual mode |

---

## Recommended Delivery Sequence

1. Extract shared Discogs mapper and image-download helper.
2. Build the Discogs wizard shell with search, results, and details state.
3. Extract the shared preview layout and rewire manual preview to use it.
4. Rebuild Discogs details on top of that shared preview.
5. Wire the edit handoff into `AddReleaseWizard`.
6. Simplify `/add` to use flow selection rather than tabs.
7. Add focused tests for search, selection, preview, and handoff.
