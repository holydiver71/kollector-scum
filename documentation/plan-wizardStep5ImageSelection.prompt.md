# Plan: Rework Wizard Step 5 — Album Cover Search & Upload

**TL;DR**: Replace the plain filename/URL text inputs for Cover Front with a two-mode picker: a Google-backed web image search (proxied via the backend) and a local file upload. Both modes resize images server-side to fit within 1600px / 5 MB and auto-generate a 300px thumbnail. Back Cover and Thumbnail fields stay as text inputs.

---

## Decisions

- Image search provider: **Google Custom Search API** (key + engine ID in config)
- Search + Upload UI on **Cover Front only**; Back Cover and Thumbnail keep text entry
- Cover Front selection (search or upload) **auto-generates a thumbnail** (300px) in the same request
- No drag-and-drop, no Bing, no Google OAuth
- Limits: max 5 MB file size; max 1600px dimension for covers; max 300px for thumbnails
- Image resizing library: **SixLabors.ImageSharp** (NuGet) — C# .NET runtime only

---

## Phase 1 — Backend: Image Resize Infrastructure
*Steps 1–6, parallel with Phase 3*

1. Add `SixLabors.ImageSharp` NuGet package to `backend/KollectorScum.Api/KollectorScum.Api.csproj`
2. Create `IImageResizerService` interface (`Interfaces/IImageResizerService.cs`) — single method `ResizeAsync(Stream, maxDimension, contentType) → Stream`; aspect-ratio-preserving, no upscaling
3. Implement `ImageResizerService` using ImageSharp (`Services/ImageResizerService.cs`)
4. Register as scoped in `backend/KollectorScum.Api/Program.cs`
5. Inject into `ImagesController`; call `ResizeAsync(maxDimension:1600)` on downloaded bytes in `DownloadImage()` before passing to storage — *depends on step 4*
6. If `generateThumbnail=true` param present, also call `ResizeAsync(maxDimension:300)` and store with `thumb-` prefix; include `ThumbnailFilename` + `ThumbnailPublicUrl` in response

---

## Phase 2 — Backend: Direct Upload Endpoint
*depends on Phase 1*

7. Add `POST /api/images/upload` to `backend/KollectorScum.Api/Controllers/ImagesController.cs`:
   - Accepts `IFormFile file` + optional `bool generateThumbnail`
   - `[RequestSizeLimit(5_242_880)]` attribute enforces 5 MB at HTTP layer
   - Validates allowed extension list (`.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`); runs ImageResizerService; stores via `IStorageService`
   - Returns `{ Filename, ThumbnailFilename?, PublicUrl, ThumbnailPublicUrl? }`
8. xUnit tests: size rejection, bad extension, successful resize+store, thumbnail path

---

## Phase 3 — Backend: Google Image Search Proxy
*parallel with Phase 1*

9. Add config keys `"Google:ApiKey"` and `"Google:SearchEngineId"` to `backend/KollectorScum.Api/appsettings.json` (placeholders) and `appsettings.Development.json`
10. Add `GET /api/images/search?q=<query>` endpoint:
    - Validates `q` (non-empty, max 200 chars)
    - Calls Google Custom Search JSON API via named `IHttpClientFactory` client (`"google-image-search"`)
    - Maps to `ImageSearchResult[]` DTO: `{ Title, ImageUrl, ThumbnailUrl, Width, Height }`
    - Returns 200/204/400/502 appropriately
    - SSRF mitigation: only the sanitised `q` string is forwarded; no user-supplied URLs are followed
11. xUnit tests: mocked `HttpMessageHandler`, query validation, result mapping, error paths

---

## Phase 4 — Frontend: Data Layer
*parallel with Phase 3*

12. Create `frontend/app/hooks/useImageSearch.ts` — state: `query`, `results`, `isLoading`, `error`; exposes a `search(query)` function calling `GET /api/images/search`
13. Add `ImageSearchResult` TypeScript type

---

## Phase 5 — Frontend: ImageSearchModal
*depends on Phase 4*

14. Create `frontend/app/components/wizard/ImageSearchModal.tsx`:
    - Props: `defaultQuery`, `onSelect(imageUrl)`, `onClose`
    - Modal overlay (midnight theme); search bar pre-populated with `defaultQuery`; Enter key + button triggers search
    - Responsive 3–4 column result grid with lazy-loaded thumbnails
    - Loading spinner, empty state, error state
    - Images rendered with `referrerPolicy="no-referrer"` (security)
15. Jest/RTL tests: renders with default query, selecting a result calls `onSelect`, refine+re-search, error state

---

## Phase 6 — Frontend: Rework ImagesPanel
*depends on Phase 5*

16. Rework `frontend/app/components/wizard/panels/ImagesPanel.tsx`:
    - **Cover Front** gets two buttons: "Search Web" (opens `ImageSearchModal` pre-filled with `{artist} {title} {year} album cover`) and "Upload File" (triggers hidden `<input type="file">`)
    - **Search → select**: POSTs chosen URL to `/api/images/download?generateThumbnail=true` → stores cover + thumbnail; sets both `coverFront` and `thumbnail` in form state
    - **Upload → select**: client-side validates size ≤ 5 MB + extension; POSTs to `/api/images/upload?generateThumbnail=true`; sets both fields
    - **Thumbnail field**: adds note "Auto-generated from Cover Front"; text override remains
    - **Back Cover**: unchanged
17. Component tests: search button opens modal, upload client-side validation, thumbnail auto-fill

---

## Relevant Files

| File | Change |
|---|---|
| `backend/KollectorScum.Api/KollectorScum.Api.csproj` | Add ImageSharp package |
| `backend/KollectorScum.Api/Controllers/ImagesController.cs` | Upload endpoint + search endpoint + resize in download |
| `backend/KollectorScum.Api/Program.cs` | Register services + named HTTP client |
| `backend/KollectorScum.Api/appsettings.json` | Google API config keys |
| `backend/KollectorScum.Api/Interfaces/IImageResizerService.cs` | New |
| `backend/KollectorScum.Api/Services/ImageResizerService.cs` | New |
| `frontend/app/components/wizard/panels/ImagesPanel.tsx` | Rework Cover Front field |
| `frontend/app/components/wizard/ImageSearchModal.tsx` | New |
| `frontend/app/hooks/useImageSearch.ts` | New |

---

## Verification

1. `dotnet test` passes with >80% coverage including new tests
2. `curl -F "file=@cover.jpg" POST /api/images/upload?generateThumbnail=true` → JSON with Filename + ThumbnailFilename; stored file dimensions ≤ 1600px
3. `GET /api/images/search?q=iron+maiden+killers` → array of ≤ 10 result objects
4. In wizard Step 5: "Search Web" opens modal pre-filled with artist/album; selecting result stores cover + thumbnail, both filenames appear in fields
5. "Upload File" → oversized file shows inline error; valid file → cover + thumbnail auto-populated
6. Complete wizard to Step 8 (Draft Preview) → images shown; submit → images visible on release page

---

## Notes

> You'll need to set a real `Google:ApiKey` and `Google:SearchEngineId` in your environment config (e.g. `appsettings.Development.json` or environment variables) before the search feature goes live.
