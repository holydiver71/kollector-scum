# Phase – Goldmine Condition Fields Summary

## Overview

This phase adds **SleeveCondition** and **MediaCondition** fields to the `MusicReleases` table, enabling users to record the physical condition of their records using the industry-standard **Goldmine Grading Guide**.

Conditions are:
- Stored as **dedicated nullable columns** on the `MusicReleases` table (not inside the `PurchaseInfo` JSON blob)
- Exposed via the `purchaseInfo` object in API responses
- Settable through the add/edit release wizard **Step 4 – Purchase Information**
- Automatically imported from Discogs during collection import
- Displayed in the **Collection Data** section of the release detail page

---

## Goldmine Grading Guide

| Code  | Description                                                         |
|-------|---------------------------------------------------------------------|
| M     | Mint – perfect in every way, often unplayed                         |
| NM    | Near Mint – nearly perfect, shows minimal signs of handling         |
| VG+   | Very Good Plus – shows some signs of play, plays near perfectly     |
| VG    | Very Good – obvious signs of handling, plays through without skips  |
| G+    | Good Plus – heavily played, plays through without skipping          |
| G     | Good – heavily worn, plays through with significant noise           |
| F     | Fair – dirty/cracked/warped, plays with difficulty                  |
| P     | Poor – barely playable                                              |

---

## Files Changed

### Backend

| File | Change |
|------|--------|
| `backend/KollectorScum.Api/Models/Enums/GoldmineGrade.cs` | **New** – Static helper class with grade constants, `ValidGrades` hashset, and `ParseDiscogsCondition()` parser |
| `backend/KollectorScum.Api/Models/MusicRelease.cs` | Added `SleeveCondition` (string?, `[StringLength(20)]`) and `MediaCondition` (string?, `[StringLength(20)]`) |
| `backend/KollectorScum.Api/DTOs/ApiDtos.cs` | Added `SleeveCondition` and `MediaCondition` to `MusicReleasePurchaseInfoDto` |
| `backend/KollectorScum.Api/DTOs/DiscogsDtos.cs` | Added `SleeveCondition` and `MediaCondition` to `DiscogsCollectionReleaseDto` |
| `backend/KollectorScum.Api/Services/DiscogsResponseMapper.cs` | Added `sleeve_condition` / `media_condition` JSON properties to `DiscogsCollectionReleaseResponse`; parse and map to DTO in `MapCollectionReleases()` |
| `backend/KollectorScum.Api/Services/DiscogsCollectionImportService.cs` | Set `SleeveCondition` / `MediaCondition` on entity in `MapToMusicReleaseAsync()` |
| `backend/KollectorScum.Api/Services/MusicReleaseMapperService.cs` | Attach conditions from entity columns to `MusicReleasePurchaseInfoDto` in `MapToFullDtoAsync()` |
| `backend/KollectorScum.Api/Services/MusicReleaseService.cs` | Extract and persist conditions from DTO in `CreateMusicReleaseAsync()` and `UpdateMusicReleaseAsync()` |
| `backend/KollectorScum.Api/Migrations/20260412222247_AddConditionFieldsToMusicRelease.cs` | **New EF migration** – adds `SleeveCondition` and `MediaCondition` columns |

### Frontend

| File | Change |
|------|--------|
| `frontend/app/components/wizard/types.ts` | Added `sleeveCondition?` and `mediaCondition?` to `WizardPurchaseInfo`; updated `toCreateDto()` and `fromCreateDto()` |
| `frontend/app/components/wizard/panels/PurchaseInformationPanel.tsx` | Added two `<select>` dropdowns (Sleeve Condition, Media Condition) with all Goldmine grade options |
| `frontend/app/components/AddReleaseForm.tsx` | Added `sleeveCondition?` / `mediaCondition?` to the `purchaseInfo` nested type |
| `frontend/app/releases/[id]/page.tsx` | Added fields to `PurchaseInfo` interface; display in **Collection Data** section |

---

## Discogs Integration

Discogs returns conditions as full-text strings at the **top level** of each collection item (e.g. `"Very Good Plus (VG+)"`). The `GoldmineGrade.ParseDiscogsCondition()` helper converts these to short codes by:
1. Checking for a direct match against the valid-grades set
2. Extracting the code from parentheses (Discogs standard format)
3. Falling back to a case-insensitive text lookup

---

## Architecture Notes

- Conditions are stored as **dedicated columns**, not embedded in the `PurchaseInfo` JSON blob, making them queryable and indexable.
- The conditions are surfaced through `MusicReleasePurchaseInfoDto` so the API contract is consistent.
- If a release has conditions but no other purchase info, a minimal `MusicReleasePurchaseInfoDto` is created to carry the condition values.
- The update flow clears conditions when `PurchaseInfo` is set to null, ensuring data consistency.
