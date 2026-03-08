## Plan: Artists Page Implementation

Implement an MVP artists page that lists only artists with releases in the collection, sorted A-Z by default, and routes into the existing collection page by pre-populating the selected artist filter. In practice this should continue to use the existing `artistId` query-parameter flow so the collection page can resolve and display the artist name in the filter UI. The initial UI should closely mirror the music collection page grid layout, but with artist cards instead of release cards: placeholder image for now, artist name under the image, and the number of releases in the database for that artist shown beneath the name. Even though image/logo/link editing is out of scope for this MVP, the database schema and API contract should still be extended now so those fields can be added later without revisiting the core artist model.

**Steps**
1. Phase 0: Branching and setup
   - Implementation must be done on a new feature branch created from `dev`, not from `master`.
   - If `dev` is not current locally at implementation time, update from remote, switch to `dev`, and create the feature branch from there before making code changes.
2. Phase 1: Data model and persistence
   - Extend `Artist` in `/home/andy/Projects/kollector-scum/backend/KollectorScum.Api/Models/Artist.cs` with nullable fields for the stored artist image path and logo path.
   - Add a new `ArtistLink` entity with `Id`, `ArtistId`, `Url`, optional `DisplayText` or `Label` if desired, and an integer `SortOrder`. This keeps links normalized and easy to validate/edit.
   - Update EF Core configuration and create a migration adding the new artist columns plus the `ArtistLinks` table.
   - Ensure the list query for artists only returns artists that are associated with at least one release in the user’s collection, since that is the page requirement.
3. Phase 2: DTOs and API contract
   - Expand `ArtistDto` in `/home/andy/Projects/kollector-scum/backend/KollectorScum.Api/DTOs/ApiDtos.cs` to include `artistImageUrl`, `artistLogoUrl`, `releaseCount`, and a `links` collection.
   - Introduce dedicated request DTOs rather than overloading the current minimal `ArtistDto`: keep `CreateArtistDto`/`UpdateArtistDto` and add fields needed for metadata editing.
   - Add an `ArtistLinkDto` plus create/update DTOs for link editing so validation stays explicit.
   - Update the mapping layer or CRUD service projection so artist image and logo values are returned as public URLs using `IStorageService.GetPublicUrl("artists", userId, fileName)` when a stored filename exists, and ensure `releaseCount` is populated from the number of releases tied to each artist.
4. Phase 3: API groundwork for future metadata
   - Keep `IStorageService` reuse in mind for future artist assets by standardizing on an `artists` bucket/folder parallel to `cover-art`, even though upload/edit functionality is not part of this MVP.
   - Extend `/home/andy/Projects/kollector-scum/backend/KollectorScum.Api/Controllers/ArtistsController.cs` so GET responses return the enriched `ArtistDto`, including `releaseCount` plus nullable image/logo/link fields for future use.
   - Decide whether to expose the future metadata fields on the existing artist endpoints now, returning null or empty values until edit support is added. Recommendation: yes, so the contract is established early.
   - If the current generic CRUD service becomes too restrictive for the richer artist shape, note now that artist-specific service methods may be needed when edit functionality is added later.
5. Phase 4: Artists page MVP
   - Replace the placeholder in `/home/andy/Projects/kollector-scum/frontend/app/artists/page.tsx` with a client page that fetches paged artists from `/api/artists`, sorted by name ascending by default.
   - Match the visual structure of the music collection grid in `/home/andy/Projects/kollector-scum/frontend/app/components/MusicReleaseList.tsx`, including the same responsive grid breakpoints and card proportions where practical, so the artists page feels like a sibling view.
   - Render artist cards with the placeholder image in MVP, then show artist name and release count beneath the image. The card should be ready to switch to real artist images later once edit/upload support exists.
   - Show only artists that have releases in the collection; if the backend already guarantees that, the frontend should not need special filtering logic.
   - Clicking an artist row/card should navigate to `/collection?artistId={id}` using the existing URL-driven filter model already consumed by `/home/andy/Projects/kollector-scum/frontend/app/collection/page.tsx` and `/home/andy/Projects/kollector-scum/frontend/app/components/SearchAndFilter.tsx`, which will display the selected artist by name.
6. Phase 5: Frontend types and data flow
   - Expand the frontend artist type in `/home/andy/Projects/kollector-scum/frontend/app/lib/types.ts`; it is currently just `LookupItemDto`, so this must become a richer object for the artists page while preserving lightweight lookup types used elsewhere.
   - Add artist page fetch helpers in `/home/andy/Projects/kollector-scum/frontend/app/lib/api.ts` for the enriched artist response.
   - Keep artist image/logo/link fields in the frontend type even if the MVP UI does not yet render them, so the page and API stay aligned.
7. Phase 6: Testing and documentation
   - Backend tests: extend artist controller/service tests to cover the enriched DTO shape, `releaseCount`, and the "only artists with releases" query behavior.
   - Frontend tests: add artists page tests for A-Z ordering, collection-style grid rendering, placeholder rendering, and navigation to `/collection?artistId=...`.
   - Add a documentation summary markdown file under `/home/andy/Projects/kollector-scum/documentation` following the project convention once implementation is complete.

**Relevant files**
- `/home/andy/Projects/kollector-scum/backend/KollectorScum.Api/Models/Artist.cs` — current artist entity with only `Id`, `UserId`, `Name`, and release navigation
- `/home/andy/Projects/kollector-scum/backend/KollectorScum.Api/DTOs/ApiDtos.cs` — current `ArtistDto`, `CreateArtistDto`, and `UpdateArtistDto` are minimal and need expansion
- `/home/andy/Projects/kollector-scum/backend/KollectorScum.Api/Controllers/ArtistsController.cs` — current CRUD endpoints for artists and likely place for enriched read responses
- `/home/andy/Projects/kollector-scum/backend/KollectorScum.Api/Interfaces/IStorageService.cs` — existing storage contract to reserve for future artist assets
- `/home/andy/Projects/kollector-scum/backend/KollectorScum.Api/Controllers/ImagesController.cs` — review only if you decide to formalize artist-asset API groundwork now rather than later
- `/home/andy/Projects/kollector-scum/frontend/app/artists/page.tsx` — current placeholder page to replace with the artists grid
- `/home/andy/Projects/kollector-scum/frontend/app/components/MusicReleaseList.tsx` — reference for the collection-style grid layout to mirror
- `/home/andy/Projects/kollector-scum/frontend/app/collection/page.tsx` — already reads `artistId` from query params and can be used as the navigation target
- `/home/andy/Projects/kollector-scum/frontend/app/components/SearchAndFilter.tsx` — already supports artist filter selection and URL-backed filtering
- `/home/andy/Projects/kollector-scum/frontend/app/lib/types.ts` — frontend artist type is currently too narrow for this feature
- `/home/andy/Projects/kollector-scum/frontend/app/lib/api.ts` — add artist page fetch helpers for the enriched artist response
- `/home/andy/Projects/kollector-scum/backend/KollectorScum.Tests/Controllers/ArtistsControllerTests.cs` — extend for enriched DTO and query behavior

**Verification**
1. Run backend tests covering artist CRUD, enriched `ArtistDto` responses, `releaseCount` population, and the "only artists with releases" query behavior.
2. Run frontend tests covering artist list ordering, collection-style grid rendering, placeholder rendering, and artist click navigation.
3. Manual smoke test:
   - Open the artists page and confirm artists are listed A-Z in the same responsive grid style as the collection page.
   - Confirm each card shows the placeholder image in MVP.
   - Confirm the artist name and release count render beneath the image.
   - Click the artist and confirm the app navigates to the collection page with the artist filter already applied and the selected artist shown in the filter UI.
4. Confirm the API returns null or empty future metadata fields consistently so image/link functionality can be added later without a contract change.

**Decisions**
- Links will be stored in a separate `ArtistLink` table as groundwork only; no link editing UI is included in MVP.
- Artist image and logo fields will be added to the database and API now, but the MVP UI will use the placeholder image only.
- Artist assets are planned to use an `artists` storage bucket/folder, parallel to `cover-art`, when upload support is added later.
- Navigation from artists to collection will continue to use `artistId` in the query string so the existing filter flow can resolve and display the artist name.
- Scope includes artist list, release counts, and collection-page navigation. It excludes artist editing, image upload, link management, biographies, and non-image media.

**Further Considerations**
1. If the generic CRUD service makes the enriched artist response awkward, break artist listing into an artist-specific read service now rather than overloading the generic abstraction.
2. Keep sorting fixed to A-Z for MVP unless there is a defined product need for alternate sort modes such as release count or recently updated.
3. When edit functionality is added later, re-use the existing release-cover storage and resize patterns rather than creating artist-specific upload rules from scratch.

---

This is ready for handoff as the MVP implementation plan.

**Checklist**
1. Backend
   - Add nullable artist image and artist logo fields to [backend/KollectorScum.Api/Models/Artist.cs](/home/andy/Projects/kollector-scum/backend/KollectorScum.Api/Models/Artist.cs).
   - Add the new `ArtistLink` entity/table and wire EF Core relationships.
   - Create the migration for artist metadata groundwork.
   - Extend [backend/KollectorScum.Api/DTOs/ApiDtos.cs](/home/andy/Projects/kollector-scum/backend/KollectorScum.Api/DTOs/ApiDtos.cs) so `ArtistDto` includes `releaseCount` plus nullable future metadata fields.
   - Update artist query/projection logic so the artists endpoint returns only artists with releases and populates `releaseCount`.
   - Update [backend/KollectorScum.Api/Controllers/ArtistsController.cs](/home/andy/Projects/kollector-scum/backend/KollectorScum.Api/Controllers/ArtistsController.cs) or the underlying service layer to return the enriched artist response.
2. Frontend
   - Replace the placeholder in [frontend/app/artists/page.tsx](/home/andy/Projects/kollector-scum/frontend/app/artists/page.tsx) with the artists grid.
   - Mirror the responsive card-grid layout from [frontend/app/components/MusicReleaseList.tsx](/home/andy/Projects/kollector-scum/frontend/app/components/MusicReleaseList.tsx).
   - Render placeholder artwork on every artist card for MVP.
   - Show artist name and release count beneath each card.
   - On click, navigate to [frontend/app/collection/page.tsx](/home/andy/Projects/kollector-scum/frontend/app/collection/page.tsx) using the existing `artistId` filter flow.
   - Expand [frontend/app/lib/types.ts](/home/andy/Projects/kollector-scum/frontend/app/lib/types.ts) and add artist fetch helpers in [frontend/app/lib/api.ts](/home/andy/Projects/kollector-scum/frontend/app/lib/api.ts).
3. Migration
   - Create the EF migration for new artist metadata fields and the `ArtistLink` table.
   - Verify the migration is additive and safe for existing artist/release data.
   - Keep storage-path conventions for future artist assets documented as `artists/{userId}/{file}` even though uploads are not in MVP.
4. Tests
   - Add backend tests for enriched artist responses, `releaseCount`, and filtering to artists with releases only.
   - Add frontend tests for A-Z ordering, grid rendering, placeholder rendering, and navigation into the collection page.
   - Add a manual verification pass for the artists page and the collection-page handoff.
5. Branching
   - Start implementation from a fresh feature branch off `dev`.
   - Keep all MVP work on that branch until ready for review.
