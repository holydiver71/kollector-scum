---
name: kollector-scum-discogs
version: 0.2.0
description: Discogs import integration guidance and job patterns
tags: [copilot-skill, discogs, import]
owner: @holydiver71
stability: stable
scope: backend import jobs, rate-limit handling
entry_point: .github/skills/kollector-scum-discogs/SKILL.md
last_updated: 2026-04-26
intent: [import-discogs, manage-jobs, handle-cooldown]
---

# Discogs Integration Pattern

- Discogs imports are asynchronous via a job queue (IDiscogsImportJobQueue) and background worker (DiscogsImportBackgroundService).
- Persist per-user job status in DiscogsImportJob.
- Frontend should poll job status after starting an import; do not block UI.
- Preserve Discogs rate-limit awareness (cooldown handling). Use the Upsert/Job patterns for long-running work.
