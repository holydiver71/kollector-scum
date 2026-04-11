-- Fix stuck Discogs import jobs (Queued=0, Running=1 -> Failed=3)
UPDATE "DiscogsImportJobs"
SET "Status" = 3,
    "LastUpdatedUtc" = NOW(),
    "ErrorMessage" = 'Manually reset: was stuck in Queued/Running state'
WHERE "Status" IN (0, 1);
