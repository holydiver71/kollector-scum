-- remove_unreferenced_genres.sql
-- Preview and delete Genres not referenced by any MusicRelease.Genres JSON array
-- WARNING: Test before running on production. Backup your DB first.

-- 1) Preview unreferenced genres (rows that would be deleted)
-- This returns the id and name of genres that are not referenced by any music release

WITH referenced AS (
  SELECT DISTINCT (jsonb_array_elements_text(genres::jsonb))::int AS id
  FROM musicreleases
  WHERE genres IS NOT NULL AND trim(genres) <> ''
)
SELECT g.id, g.name
FROM genres g
WHERE g.id NOT IN (SELECT id FROM referenced)
ORDER BY g.id;

-- 2) (Optional) Count how many would be deleted
-- SELECT COUNT(*) FROM genres g WHERE g.id NOT IN (SELECT id FROM referenced);

-- 3) Delete unreferenced genres inside a transaction
-- Uncomment the DELETE section when you are ready to remove them.

-- BEGIN;
-- WITH referenced AS (
--   SELECT DISTINCT (jsonb_array_elements_text(genres::jsonb))::int AS id
--   FROM musicreleases
--   WHERE genres IS NOT NULL AND trim(genres) <> ''
-- )
-- DELETE FROM genres g
-- WHERE g.id NOT IN (SELECT id FROM referenced);
-- COMMIT;

-- Notes:
-- - This script assumes `musicreleases` stores genre IDs as a JSON array of integers (e.g. "[1,2]").
-- - If your `genres` table or `musicreleases` table names are different, adjust them accordingly.
-- - Deleting a genre will cascade to `kollectiongenres` (per EF model configuration).
-- - Run the preview SELECT first, then run the DELETE within a transaction and verify results.
