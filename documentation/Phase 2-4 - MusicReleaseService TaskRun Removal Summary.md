# Phase 2-4 - MusicReleaseService TaskRun Removal Summary

## Overview

Implemented Phase 2.4 from the Backend Optimization and Security Plan by removing an unnecessary `Task.Run()` wrapper from `MusicReleaseService`.

## Completed Work

### 1. Removed Redundant Task Scheduling

Updated `GetMusicReleasesAsync(...)` in `MusicReleaseService` to map paged `MusicRelease` entities to `MusicReleaseSummaryDto` values directly instead of wrapping synchronous LINQ projection in `Task.Run()`.

Previous behavior:

- Scheduled synchronous CPU-light mapping work onto the thread pool.
- Added context-switch overhead with no throughput benefit.
- Risked wrapping synchronous mapper exceptions in task-related exception flow.

Current behavior:

- Performs the projection inline after awaited repository access completes.
- Preserves the same returned DTO shape and paging metadata.
- Keeps exception flow direct and easier to reason about.

### 2. Regression Test Coverage Added

Added a unit test to verify that when `MapToSummaryDto(...)` throws, `GetMusicReleasesAsync(...)` surfaces the original `InvalidOperationException` directly.

## Files Changed

- `backend/KollectorScum.Api/Services/MusicReleaseService.cs`
- `backend/KollectorScum.Tests/Services/MusicReleaseServiceTests.cs`
- `documentation/Backend-Optimization-and-Security-Plan.md`
- `documentation/Phase 2-4 - MusicReleaseService TaskRun Removal Summary.md`

## Expected Outcome

- Lower thread-pool and context-switch overhead on collection list reads.
- Simpler async flow in the service layer.
- Clearer exception propagation during summary mapping failures.
