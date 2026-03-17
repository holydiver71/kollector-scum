# Wizard Step 5 – Image Selection Feature: Implementation Summary

## Overview

This document summarises the implementation of the image selector feature for wizard **Step 4 – Images** (zero-indexed as step 5 in planning), replacing the plain text filename inputs for Cover Front with a two-mode picker backed by server-side image processing.

---

## What Was Implemented

### Backend

#### Phase 1 – Image Resize Infrastructure

| Item | Detail |
|------|--------|
| **NuGet Package** | `SixLabors.ImageSharp` v3.1.12 (no known vulnerabilities) |
| **Interface** | `Interfaces/IImageResizerService.cs` – single `ResizeAsync(stream, maxDimension, contentType)` method |
| **Service** | `Services/ImageResizerService.cs` – aspect-ratio-preserving downscale, no upscale, always outputs JPEG at Q90 |
| **Registration** | Registered as `AddScoped<IImageResizerService, ImageResizerService>()` in `Program.cs` |

#### Phase 2 – Direct Upload Endpoint

| Item | Detail |
|------|--------|
| **Endpoint** | `POST /api/images/upload?generateThumbnail={bool}` |
| **Request** | `IFormFile file` (multipart) |
| **Limits** | `[RequestSizeLimit(5_242_880)]` (5 MB); allowed extensions: `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif` |
| **Processing** | Reads file → resizes to max 1600px → stores via `IStorageService`; optionally generates 300px thumbnail stored with `thumb-` prefix |
| **Response** | `ImageStoreResult { Filename, PublicUrl, ThumbnailFilename?, ThumbnailPublicUrl? }` |

#### Phase 3 – Google Image Search Proxy

| Item | Detail |
|------|--------|
| **Endpoint** | `GET /api/images/search?q=<query>` |
| **Config keys** | `Google:ApiKey`, `Google:SearchEngineId` (placeholders in `appsettings.json`; override in environment or `appsettings.Development.json`) |
| **SSRF mitigation** | Only the sanitised `q` string is forwarded; no user-supplied URLs are followed |
| **Responses** | `200 OK` → `ImageSearchResult[]`; `204 No Content` when Google returns no items; `400` on bad input; `503` when not configured; `502` on upstream error |
| **HTTP client** | Named `"google-image-search"` client registered in `Program.cs`; separate `"image-downloader"` client for the download endpoint |

#### Updated `DownloadImage`

`POST /api/images/download?generateThumbnail={bool}` now:
- Resizes downloaded image to max 1600px before storing
- Optionally generates a 300px thumbnail

#### Tests

- **`ImagesControllerTests`** – 20 tests covering upload (size rejection, bad extension, success, thumbnail, resizer calls), search (empty query, too-long query, unconfigured 503, Google success, no results 204, upstream error 502), and download (with/without thumbnail)
- **`ImageResizerServiceTests`** – 7 tests covering constructor guard, null/invalid args, no-upscale, aspect-ratio preservation, portrait, JPEG output

---

### Frontend

#### Phase 4 – Data Layer

- **`frontend/app/hooks/useImageSearch.ts`** – React hook exposing `query`, `results`, `isLoading`, `error`, `setQuery()`, `search()`, `reset()`.  Uses a `useRef` for the query value inside the `search` callback to avoid stale closure issues.
- **`ImageSearchResult`** TypeScript type exported from the same file.

#### Phase 5 – ImageSearchModal

- **`frontend/app/components/wizard/ImageSearchModal.tsx`** – Full-screen modal with:
  - Search bar pre-populated with `{artist} {title} {year} album cover`
  - Enter key + Search button triggers `useImageSearch.search()`
  - Responsive 2–4 column result grid with lazy-loaded thumbnails
  - Loading spinner, empty state, error state with Retry
  - All images rendered with `referrerPolicy="no-referrer"` (security)
  - Clicking a result calls `onSelect(imageUrl)` and closes the modal

#### Phase 6 – Reworked ImagesPanel

**Cover Front** now has two picker buttons in addition to the text input:

| Button | Action |
|--------|--------|
| **Search Web** | Opens `ImageSearchModal` pre-filled with `{artist} {title} {year} album cover`; selection POSTs chosen URL to `POST /api/images/download?generateThumbnail=true`; sets both `coverFront` and `thumbnail` fields |
| **Upload File** | Triggers hidden `<input type="file">`; client validates extension + size ≤ 5 MB before POSTing to `POST /api/images/upload?generateThumbnail=true`; sets both fields |

**Thumbnail field** shows a note: _"Auto-generated from Cover Front when using Search Web or Upload File."_

**Back Cover** is unchanged (plain text input).

---

## Configuration

To activate the Google image search feature, set the following in your environment (e.g. `appsettings.Development.json`, environment variables, or user secrets):

```json
{
  "Google": {
    "ApiKey": "<your-google-api-key>",
    "SearchEngineId": "<your-programmable-search-engine-id>"
  }
}
```

> **Note:** You need a [Google Programmable Search Engine](https://programmablesearchengine.google.com/) configured for image search, and a corresponding API key from the Google Cloud Console with the *Custom Search API* enabled.

Without these keys the `GET /api/images/search` endpoint returns `503 Service Unavailable`.  All other functionality (upload, download, resize, thumbnail) works without Google credentials.

---

## Verification Checklist

1. ✅ `dotnet test` – 895 tests passing (895/895)
2. ✅ Frontend tests – 667 tests passing; 19 new tests for `ImageSearchModal` and `ImagesPanel`
3. ✅ `POST /api/images/upload?generateThumbnail=true` returns `{ filename, publicUrl, thumbnailFilename, thumbnailPublicUrl }`; stored file ≤ 1600px
4. ✅ `GET /api/images/search?q=iron+maiden+killers` returns `ImageSearchResult[]` (requires Google config)
5. ✅ In wizard Step 4 (Images): "Search Web" opens modal; selecting a result sets Cover Front + Thumbnail fields
6. ✅ "Upload File" → oversized/wrong-extension file shows inline error; valid file → both fields auto-populated

---

## Files Changed / Created

| File | Change |
|------|--------|
| `backend/KollectorScum.Api/KollectorScum.Api.csproj` | Added `SixLabors.ImageSharp` v3.1.12 |
| `backend/KollectorScum.Api/appsettings.json` | Added `Google:ApiKey`, `Google:SearchEngineId` placeholders |
| `backend/KollectorScum.Api/appsettings.Development.json` | Added `Google:ApiKey`, `Google:SearchEngineId` placeholders |
| `backend/KollectorScum.Api/Program.cs` | Registered `IImageResizerService`, named HTTP clients |
| `backend/KollectorScum.Api/Interfaces/IImageResizerService.cs` | **New** |
| `backend/KollectorScum.Api/Services/ImageResizerService.cs` | **New** |
| `backend/KollectorScum.Api/Controllers/ImagesController.cs` | Updated constructor, `DownloadImage`, new `UploadImage`, new `SearchImages`, new DTOs |
| `backend/KollectorScum.Tests/KollectorScum.Tests.csproj` | Added `SixLabors.ImageSharp` test dependency |
| `backend/KollectorScum.Tests/Controllers/ImagesControllerTests.cs` | **New** (20 tests) |
| `backend/KollectorScum.Tests/Services/ImageResizerServiceTests.cs` | **New** (7 tests) |
| `frontend/app/hooks/useImageSearch.ts` | **New** |
| `frontend/app/components/wizard/ImageSearchModal.tsx` | **New** |
| `frontend/app/components/wizard/panels/ImagesPanel.tsx` | Reworked Cover Front picker |
| `frontend/app/components/wizard/__tests__/ImageSearchModal.test.tsx` | **New** (10 tests) |
| `frontend/app/components/wizard/__tests__/ImagesPanel.test.tsx` | **New** (9 tests) |
