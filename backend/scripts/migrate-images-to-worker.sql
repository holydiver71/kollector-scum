-- Migrate R2 account-style image hosts to the Worker public host
-- This replaces occurrences of the account-style R2 host (e.g. "https://<account>.r2.cloudflarestorage.com")
-- inside the JSON stored in the "Images" column of "MusicReleases" with the Worker host.
--
-- IMPORTANT: Run a backup or preview the affected rows before applying.

BEGIN;

-- Preview rows that would be affected
SELECT "Id", "Images"
FROM "MusicReleases"
WHERE "Images" ~ 'r2\\.cloudflarestorage\\.com';

-- Replace both https and http account-style hosts with the worker host.
-- Update the worker host below to your actual worker domain if it differs.
UPDATE "MusicReleases"
SET "Images" = regexp_replace(
    "Images",
    'https?://[^/]+\\.r2\\.cloudflarestorage\\.com',
    'https://kollector-images.1c96409b15483aeb26499da3e9a1cb2b.workers.dev',
    'g'
)
WHERE "Images" ~ 'r2\\.cloudflarestorage\\.com';

SELECT "Id", "Images"
FROM "MusicReleases"
WHERE "Images" ~ 'kollector-images\\.';

COMMIT;

-- Notes:
--  - This performs an in-place textual replacement inside the Images JSON column.
--  - If you prefer a dry-run, comment out the UPDATE and run only the first SELECT.
--  - If your worker host differs, change the replacement string above.
--  - After running, restart the API (if running) so any cached config is used.
