# Add Release Wizard Migration Summary

## Overview

Migrated the 8-step Add Release wizard from the prototype (`frontend/mock-up/`) to a fully integrated production implementation in `frontend/app/components/wizard/`. The `/add` page now uses the new wizard instead of the legacy `AddReleaseForm` component.

**Branch:** `feature/add-release-wizard-migration`  
**Commit:** `c1ee834`

---

## Files Created

### Core

| File | Purpose |
|------|---------|
| `wizard/types.ts` | Shared types (`WizardFormData`, `LookupItem`, `WizardStep`, etc.), `WIZARD_STEPS`, `EMPTY_FORM_DATA`, `toCreateDto`, `fromCreateDto`, `ValidationErrors` |
| `wizard/useReleaseLookups.ts` | Custom hook — fetches 7 lookup lists in parallel on mount; exposes `loading`, `error`, and all list arrays |
| `wizard/StepIndicator.tsx` | Clickable step-progress indicator |
| `wizard/AddReleaseWizard.tsx` | Wizard shell — navigation, step validation, real `POST /api/musicreleases` submit |

### Panels (8 steps)

| Panel | Step | Description |
|-------|------|-------------|
| `BasicInformationPanel.tsx` | 0 ★ | Title + artist autocomplete (required) |
| `ClassificationPanel.tsx` | 1 | Genre, format, packaging, country, live/studio |
| `LabelInformationPanel.tsx` | 2 | Label autocomplete, years, catalogue number, UPC |
| `PurchaseInformationPanel.tsx` | 3 | Store autocomplete, price, currency, date, notes |
| `ImagesPanel.tsx` | 4 | Cover front/back/thumbnail URLs with preview |
| `TrackListingPanel.tsx` | 5 | Multi-disc track editor with M:SS duration parsing |
| `ExternalLinksPanel.tsx` | 6 | Typed external links (Discogs, Spotify, etc.) |
| `DraftPreviewPanel.tsx` | 7 | Full release preview + submit/error handling |

★ Required — Next is blocked if title or artist is missing.

### Tests (54 passing)

| Test File | Coverage |
|-----------|---------|
| `__tests__/toCreateDto.test.ts` | `toCreateDto` / `fromCreateDto` pure-function mapping: years, images, media, links, purchaseInfo |
| `__tests__/useReleaseLookups.test.ts` | Loading states, parallel fetching, error handling, endpoint correctness |
| `__tests__/AddReleaseWizard.test.tsx` | Loading spinner, step indicators, step-0 validation, forward/back navigation, Cancel, submit success/failure, disabled button during submission |

---

## Files Modified

| File | Change |
|------|--------|
| `frontend/app/add/page.tsx` | Replaced `<AddReleaseForm>` with `<AddReleaseWizard>`; import updated; container widened to `max-w-4xl` |
| `wizard/types.ts` | Fixed `toCreateDto` — `artistIds`/`genreIds` are always `number[]`, never `undefined` |
| `wizard/panels/BasicInformationPanel.tsx` | Fixed import paths (`../types`, `../useReleaseLookups`) |

---

## Key Design Decisions

- **Lookup data hoisted to wizard shell** — `useReleaseLookups` runs once in `AddReleaseWizard`, then all 7 lists are passed down via props, preventing redundant fetches.
- **`toCreateDto` / `fromCreateDto`** — clear boundary between wizard form state and API DTO. Handles year → ISO date conversion, URL → filename stripping for images, and conditional `purchaseInfo` presence.
- **Only step 0 is required** — all other steps can be skipped; validation only blocks `Next` on step 0.
- **Panel mocking in tests** — `AddReleaseWizard.test.tsx` replaces each panel with a minimal stub so test focus stays on the wizard shell (navigation, validation, submit).

---

## TypeScript

`npx tsc --noEmit` reports zero errors.

---

## Next Steps (not in scope)

- Remove `app/components/AddReleaseForm.tsx` and its tests once wizard is confirmed stable in production.
- Delete legacy scaffold panels retained from earlier phases if unused.
