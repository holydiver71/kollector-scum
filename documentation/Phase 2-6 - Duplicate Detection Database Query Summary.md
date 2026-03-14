# Phase 2-6 - Duplicate Detection Database Query Summary

## Overview

Implemented Phase 2.6 from the Backend Optimization and Security Plan by replacing broad in-memory duplicate scanning with targeted database candidate queries in `MusicReleaseDuplicateService`.

## Completed Work

### 1. Catalog Duplicate Check Optimized

`CheckByCatalogNumberAsync(...)` now uses a database query over `IRepository<MusicRelease>.Query()` with provider-side filtering:

- user scope (`UserId`)
- normalized catalog match (`LabelNumber.ToLower()`)
- optional excluded release id

This removes unnecessary post-load filtering over a larger release set.

### 2. Title + Artist Duplicate Check Optimized

`CheckByTitleAndArtistAsync(...)` now performs a targeted database candidate query first:

- user scope (`UserId`)
- normalized title match (`Title.ToLower()`)
- non-null artists payload
- optional excluded release id

Only candidate rows are then materialized for JSON artist deserialization and artist-overlap checks.

Because artist ids are currently stored as serialized JSON in `MusicRelease.Artists`, artist overlap still requires in-memory parsing. This phase minimizes work by reducing the candidate set before deserialization.

### 3. Test Coverage Added

Created `MusicReleaseDuplicateServiceTests` to cover:

- catalog duplicate matching
- title+artist duplicate matching
- exclude-id behavior
- acting-user scope isolation
- no-acting-user behavior

## Files Changed

- `backend/KollectorScum.Api/Services/MusicReleaseDuplicateService.cs`
- `backend/KollectorScum.Tests/Services/MusicReleaseDuplicateServiceTests.cs`
- `documentation/Backend-Optimization-and-Security-Plan.md`
- `documentation/Phase 2-6 - Duplicate Detection Database Query Summary.md`

## Expected Outcome

- Reduced memory usage and CPU overhead during duplicate checks.
- Faster duplicate validation on users with large collections.
- No change to duplicate detection behavior for catalog and title+artist paths.
