# Wizard Step 5 – Image Search & Upload Implementation Summary

## Overview

Wizard Step 5 (Images) has been enhanced from plain text filename inputs to a full two-mode image picker for the **Front Cover** field. Users can now:

1. **Search Web** – searches MusicBrainz and fetches cover art directly from the Cover Art Archive (both completely free and open).
2. **Upload File** – select a local image file (JPEG, PNG, GIF, WebP, BMP or TIFF, max 5 MB).

Both modes automatically generate a 300 px thumbnail server-side. The **Back Cover** and **Thumbnail** fields retain their original text-input behaviour (thumbnail shows an auto-fill hint).

No paid image search APIs are used. Google Custom Search was explicitly excluded.

---

## Design Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Image search provider | MusicBrainz + Cover Art Archive | Free, open-data, highly accurate for music releases |
| Google Custom Search | **Not used** | Paid per query; low precision; not required for structured music metadata |
| Resize library | SixLabors.ImageSharp 3.1.12 | Battle-tested, .NET-native, no OS dependencies |
| Cover resize limit | 1 600 px | Balances quality vs. storage |
| Thumbnail size | 300 px square (centre-crop) | Consistent with existing thumbnail convention |
| Max upload size | 5 MB | Prevents abuse while accommodating high-res scans |
| Auto-search while typing | Debounced 400 ms | UX improvement #1 from Image Search Research |
| Confidence indicators | Exact / Good / Possible | UX improvement #2 from Image Search Research |

---

## Architecture

### Backend

#### New Interfaces

| Interface | Purpose |
|---|---|
| `IImageResizerService` | Resize image to ≤ N px; generate square thumbnail |
| `ICoverArtSearchService` | Search MusicBrainz + Cover Art Archive |

#### New Services

| Service | Key Behaviour |
|---|---|
| `ImageResizerService` | Uses ImageSharp; outputs JPEG @ quality 85; `ResizeAsync` (max-dimension) + `GenerateThumbnailAsync` (crop-to-square) |
| `CoverArtSearchService` | Calls `https://musicbrainz.org/ws/2/release/?query=…` then resolves cover art from `https://coverartarchive.org/release/{mbid}`; returns up to 10 results with normalised confidence scores |

#### New / Updated Endpoints

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/images/upload` | Accepts `multipart/form-data`; resizes to ≤1600 px; optionally generates 300 px thumbnail (`?generateThumbnail=true`); returns `ImageUploadResponseDto` |
| `GET` | `/api/images/search` | Proxies MusicBrainz + CAA; accepts `?q=...&limit=4`; returns `CoverArtSearchResultDto[]` or 204 |
| `POST` | `/api/images/download` | Updated to resize downloaded image to ≤1600 px + optional thumbnail (`?generateThumbnail=true`); added SSRF mitigation (only absolute HTTP/HTTPS URLs accepted) |

#### New DTOs (`ImageDtos.cs`)

- `CoverArtSearchResultDto` – includes `MbId`, `Artist`, `Title`, `Year`, `Format`, `Country`, `Label`, `ImageUrl`, `ThumbnailUrl`, `Confidence`, `ConfidenceLabel`
- `ImageUploadResponseDto` – `Filename`, `ThumbnailFilename`, `PublicUrl`, `ThumbnailPublicUrl`, `Size`

#### DI Registration (`Program.cs`)

```csharp
builder.Services.AddScoped<IImageResizerService, ImageResizerService>();
builder.Services.AddScoped<ICoverArtSearchService, CoverArtSearchService>();

// Named HTTP clients
builder.Services.AddHttpClient("musicbrainz", …);
builder.Services.AddHttpClient("coverartarchive", …);
```

---

### Frontend

#### New Files

| File | Purpose |
|---|---|
| `useImageSearch.ts` | React hook managing search state; cancels stale requests via `AbortController`; exposes `search(query)`, `clear()`, `results`, `isLoading`, `error` |
| `ImageSearchModal.tsx` | Full-screen overlay modal; search bar with 400 ms debounce auto-search; up to 4 results in a 2–4 column grid with confidence badges; loading / empty / error states |

#### Modified Files

| File | Changes |
|---|---|
| `panels/ImagesPanel.tsx` | Cover Front field replaced with `CoverFrontField` component; "Search Web" opens `ImageSearchModal`; "Upload File" triggers hidden `<input type="file">`; auto-fills thumbnail; Back Cover and Thumbnail unchanged except thumbnail gets hint text |

---

## UX Enhancements (from Image Search Research)

1. **Auto-search while typing** – the search input in `ImageSearchModal` debounces user input at 400 ms, triggering a fresh search automatically without requiring the user to press Enter.
2. **Confidence indicators** – each result card shows a colour-coded badge: green "Exact match" (≥ 95 %), blue "Good match" (≥ 75 %), amber "Possible match" (< 75 %). Scores are derived from MusicBrainz search relevance scores.

---

## Security

- **SSRF mitigation**: the `/api/images/download` endpoint validates that only absolute `http://` or `https://` URLs are followed.
- **Upload validation**: MIME type and size checks on the server-side (`POST /api/images/upload`); client-side pre-validation for fast feedback.
- **Directory traversal**: existing prevention logic retained.
- **Rate limiting**: existing global rate limiter covers new endpoints.

---

## Tests Added

### Backend (`KollectorScum.Tests`)

| Test File | Tests |
|---|---|
| `Services/ImageResizerServiceTests.cs` | 8 tests – resize large image, small image pass-through, thumbnail square output, null/invalid input guards |
| `Services/CoverArtSearchServiceTests.cs` | 10 tests – empty query, MB failure, empty results, full happy path (mapping), CAA 404 skip, limit clamping, confidence label |
| `Controllers/ImagesControllerTests.cs` | 15 tests – upload validation (no file, empty, too large, wrong MIME), upload success with/without thumbnail, search validation, no results, with results |

### Frontend (`frontend`)

| Test File | Tests |
|---|---|
| `__tests__/useImageSearch.test.ts` | 6 tests – initial state, loading state, success, error, empty query, clear |
| `__tests__/ImageSearchModal.test.tsx` | 9 tests – renders default query, initial search, close button, Escape key, loading spinner, error state, empty state, result cards, select callback |
| `__tests__/ImagesPanel.test.tsx` | 9 tests – buttons render, back cover / thumbnail inputs, modal open/close, query pre-fill, onChange callback, file too large, invalid file type, thumbnail hint |

---

## Files Changed

```
backend/KollectorScum.Api/KollectorScum.Api.csproj             (added SixLabors.ImageSharp 3.1.12)
backend/KollectorScum.Api/Interfaces/IImageResizerService.cs   (new)
backend/KollectorScum.Api/Interfaces/ICoverArtSearchService.cs (new)
backend/KollectorScum.Api/DTOs/ImageDtos.cs                    (new)
backend/KollectorScum.Api/Services/ImageResizerService.cs      (new)
backend/KollectorScum.Api/Services/CoverArtSearchService.cs    (new)
backend/KollectorScum.Api/Controllers/ImagesController.cs      (updated)
backend/KollectorScum.Api/Program.cs                           (updated)
backend/KollectorScum.Tests/KollectorScum.Tests.csproj         (added SixLabors.ImageSharp)
backend/KollectorScum.Tests/Services/ImageResizerServiceTests.cs (new)
backend/KollectorScum.Tests/Services/CoverArtSearchServiceTests.cs (new)
backend/KollectorScum.Tests/Controllers/ImagesControllerTests.cs   (new)
frontend/app/components/wizard/useImageSearch.ts               (new)
frontend/app/components/wizard/ImageSearchModal.tsx            (new)
frontend/app/components/wizard/panels/ImagesPanel.tsx          (reworked)
frontend/app/components/wizard/__tests__/useImageSearch.test.ts    (new)
frontend/app/components/wizard/__tests__/ImageSearchModal.test.tsx (new)
frontend/app/components/wizard/__tests__/ImagesPanel.test.tsx      (new)
```
