# Phase – Discogs Import Cooldown Overlay Summary

## Overview

This phase adds a user-facing countdown overlay to the Discogs import progress panel so that users can see exactly when the import is paused due to the Discogs API rate-limit (60 req/min). The countdown is displayed **every time** a cooldown period occurs during the import, not just once.

---

## Problem

During a Discogs collection import the `DiscogsHttpClient` proactively pauses for ~62 seconds whenever the API rate-limit window is nearly exhausted, and also backs off on HTTP 429 responses.  Previously the UI gave no indication of this pause — the progress panel simply stopped moving, which confused users.

---

## Solution

### Backend

| File | Change |
|---|---|
| `IDiscogsCollectionImportService.cs` | Added `DateTime? CooldownUntilUtc` property to `DiscogsImportProgress`. |
| `IDiscogsHttpClient.cs` | Added `Action<DateTime?>? CooldownCallback { get; set; }` property. |
| `DiscogsHttpClient.cs` | Implemented `CooldownCallback`; invoked with the cooldown end-time before each `Task.Delay` (both proactive and 429 paths), then invoked with `null` once the delay completes. |
| `IDiscogsService.cs` | Added `void SetCooldownCallback(Action<DateTime?> callback)` method. |
| `DiscogsService.cs` | Forwarded the callback to `_httpClient.CooldownCallback`. |
| `DiscogsCollectionImportService.cs` | Registers a cooldown callback at the start of each import; the callback writes `CooldownUntilUtc` to the in-memory `_progressStore` for the importing user. |
| `DiscogsImportJobDtos.cs` | Added `DateTime? CooldownUntilUtc` to `DiscogsImportJobStatusDto`. |
| `DiscogsImportJobService.cs` | Maps `liveProgress.CooldownUntilUtc` into the DTO inside `MapJob`. |

### Frontend (`DiscogsImportDialog.tsx`)

* Added `cooldownUntilUtc?: string | null` to the `ImportProgress` and `ImportJobStatus` interfaces.
* `normalizeStatus` now reads `cooldownUntilUtc` / `CooldownUntilUtc` from the poll response.
* Added `cooldownSecondsLeft` state to `DiscogsImportDialog`.
* A `useEffect` hook ticks down the seconds remaining using `window.setInterval` whenever `progress.cooldownUntilUtc` is non-null.
* `ImportProgressWheel` now accepts a `cooldownSecondsLeft: number | null` prop.
* When `cooldownSecondsLeft > 0` a styled countdown panel is rendered beneath the warning banner:
  * "API Rate Limit — Cooling Down" heading
  * Large numeric countdown (e.g. `30s`)
  * Explanatory note: "Discogs limits requests per minute. Resuming automatically…"
* The cooldown state is cleared when the dialog is closed.

---

## Tests Added / Updated

### Backend
* `DiscogsHttpClientTests.cs` — two new tests:
  * `CooldownCallback_InvokedWithEndTimeWhenRateLimitExhausted_AndClearedAfterDelay` — verifies proactive cooldown fires the callback with an end-time and then clears it.
  * `CooldownCallback_InvokedOnTooManyRequests_AndClearedAfterDelay` — verifies 429 retry path fires the callback correctly.
* `DiscogsCollectionImportIntegrationTests.cs` — `FakeDiscogsService` updated to satisfy the new `SetCooldownCallback` interface method (no-op).

### Frontend
* `DiscogsImportDialog.test.tsx` — new test `shows cooldown overlay with countdown timer when API rate limit is active` verifies that the cooldown panel renders and displays the timer when `cooldownUntilUtc` is present in the poll response.

---

## Test Results

* **Backend:** 74/74 Discogs tests passing.
* **Frontend:** 3/3 `DiscogsImportDialog` tests passing.
