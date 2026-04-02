# Phase - Discogs Import Background Jobs Summary

## Overview

This phase replaces the blocking Discogs import request/response flow with a queued background-job model while keeping the existing import implementation as the worker payload.

## What Changed

- Added a durable `DiscogsImportJob` model with persisted status, counters, timestamps, and error payloads.
- Added an in-process `Channel`-backed queue and hosted background worker to process Discogs imports outside the request thread.
- Updated the import API so `POST /api/import/discogs` now queues a job and returns accepted job metadata.
- Updated `GET /api/import/discogs/status` to return job-oriented status, optionally scoped by `jobId`.
- Updated the Discogs import dialog to submit a job, poll status by `jobId`, and render the existing progress/result UI without holding a 30-minute POST open.
- Added backend tests for job submission and live-status projection.

## Current State

- The browser is no longer pinned to a long-running import request.
- Import jobs now survive application restarts at the database level, and pending jobs are re-queued on startup.
- The worker still delegates to the existing `DiscogsCollectionImportService`, so the persistence strategy is still the original per-release flow for now.

## Remaining Work

- Replace the current per-release persistence path with staging-table plus PostgreSQL `COPY` ingestion.
- Add Discogs rate-limit header inspection and bounded retry/backoff logic.
- Move full release enrichment and image download out of the critical import path.
- Add migration artifacts and broader integration coverage for the new job model.