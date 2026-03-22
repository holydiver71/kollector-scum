-- normalize-lookups.sql
-- Usage: run in a maintenance window. Test on a backup first.
-- This script normalizes lookup names by trimming and uppercasing,
-- merges duplicates per user, reassigns foreign keys in MusicReleases,
-- and removes duplicate lookup rows.

BEGIN;

-- Helper: normalize value
-- We will process each lookup table similarly: Formats, Labels, Countries, Artists, Genres

-- 1) Formats
-- Create normalized name column for staging
ALTER TABLE "Formats" ADD COLUMN IF NOT EXISTS "_NormalizedName" text;
UPDATE "Formats" SET "_NormalizedName" = upper(trim("Name"));

-- For each userId + normalized name, keep the smallest Id and delete others
WITH ranked AS (
  SELECT "Id", "UserId", "Name", "_NormalizedName",
         ROW_NUMBER() OVER (PARTITION BY "UserId", "_NormalizedName" ORDER BY "Id") AS rn
  FROM "Formats"
)
-- Update MusicReleases referencing duplicate formats to point to the canonical id
UPDATE "MusicReleases" mr
SET "FormatId" = r_keep."Id"
FROM (
  SELECT f_keep."Id", f_dup."Id" AS dup_id
  FROM ranked f_keep
  JOIN ranked f_dup ON f_keep."UserId" = f_dup."UserId" AND f_keep."_NormalizedName" = f_dup."_NormalizedName"
  WHERE f_keep.rn = 1 AND f_dup.rn > 1
) AS mapping(r_keep_id, dup_id)
JOIN "Formats" r_keep ON r_keep."Id" = mapping.r_keep_id
WHERE mr."FormatId" = mapping.dup_id;

-- Delete duplicate rows (keep rn = 1)
DELETE FROM "Formats" f
USING (
  SELECT "Id", "UserId", "_NormalizedName",
         ROW_NUMBER() OVER (PARTITION BY "UserId", "_NormalizedName" ORDER BY "Id") AS rn
  FROM "Formats"
) dups
WHERE f."Id" = dups."Id" AND dups.rn > 1;

-- Replace Name with normalized value for remaining rows and drop staging column
UPDATE "Formats" SET "Name" = "_NormalizedName";
ALTER TABLE "Formats" DROP COLUMN IF EXISTS "_NormalizedName";

-- Repeat for Labels
ALTER TABLE "Labels" ADD COLUMN IF NOT EXISTS "_NormalizedName" text;
UPDATE "Labels" SET "_NormalizedName" = upper(trim("Name"));

WITH ranked AS (
  SELECT "Id", "UserId", "Name", "_NormalizedName",
         ROW_NUMBER() OVER (PARTITION BY "UserId", "_NormalizedName" ORDER BY "Id") AS rn
  FROM "Labels"
)
UPDATE "MusicReleases" mr
SET "LabelId" = r_keep."Id"
FROM (
  SELECT f_keep."Id", f_dup."Id" AS dup_id
  FROM ranked f_keep
  JOIN ranked f_dup ON f_keep."UserId" = f_dup."UserId" AND f_keep."_NormalizedName" = f_dup."_NormalizedName"
  WHERE f_keep.rn = 1 AND f_dup.rn > 1
) AS mapping(r_keep_id, dup_id)
JOIN "Labels" r_keep ON r_keep."Id" = mapping.r_keep_id
WHERE mr."LabelId" = mapping.dup_id;

DELETE FROM "Labels" l
USING (
  SELECT "Id", "UserId", "_NormalizedName",
         ROW_NUMBER() OVER (PARTITION BY "UserId", "_NormalizedName" ORDER BY "Id") AS rn
  FROM "Labels"
) dups
WHERE l."Id" = dups."Id" AND dups.rn > 1;

UPDATE "Labels" SET "Name" = "_NormalizedName";
ALTER TABLE "Labels" DROP COLUMN IF EXISTS "_NormalizedName";

-- Countries
ALTER TABLE "Countries" ADD COLUMN IF NOT EXISTS "_NormalizedName" text;
UPDATE "Countries" SET "_NormalizedName" = upper(trim("Name"));

WITH ranked AS (
  SELECT "Id", "UserId", "Name", "_NormalizedName",
         ROW_NUMBER() OVER (PARTITION BY "UserId", "_NormalizedName" ORDER BY "Id") AS rn
  FROM "Countries"
)
UPDATE "MusicReleases" mr
SET "CountryId" = r_keep."Id"
FROM (
  SELECT f_keep."Id", f_dup."Id" AS dup_id
  FROM ranked f_keep
  JOIN ranked f_dup ON f_keep."UserId" = f_dup."UserId" AND f_keep."_NormalizedName" = f_dup."_NormalizedName"
  WHERE f_keep.rn = 1 AND f_dup.rn > 1
) AS mapping(r_keep_id, dup_id)
JOIN "Countries" r_keep ON r_keep."Id" = mapping.r_keep_id
WHERE mr."CountryId" = mapping.dup_id;

DELETE FROM "Countries" c
USING (
  SELECT "Id", "UserId", "_NormalizedName",
         ROW_NUMBER() OVER (PARTITION BY "UserId", "_NormalizedName" ORDER BY "Id") AS rn
  FROM "Countries"
) dups
WHERE c."Id" = dups."Id" AND dups.rn > 1;

UPDATE "Countries" SET "Name" = "_NormalizedName";
ALTER TABLE "Countries" DROP COLUMN IF EXISTS "_NormalizedName";

-- Artists
ALTER TABLE "Artists" ADD COLUMN IF NOT EXISTS "_NormalizedName" text;
UPDATE "Artists" SET "_NormalizedName" = upper(trim("Name"));

WITH ranked AS (
  SELECT "Id", "UserId", "Name", "_NormalizedName",
         ROW_NUMBER() OVER (PARTITION BY "UserId", "_NormalizedName" ORDER BY "Id") AS rn
  FROM "Artists"
)
-- Update serialized Artists field in MusicReleases (stored as JSON array of IDs)
-- We need to remap any occurrences of duplicate artist ids in JSON arrays to the kept id.
-- This operation is best-effort and works for simple JSON arrays of integers.

-- For each mapping, replace dup_id with keep_id in MusicReleases."Artists" JSON
DO $$
DECLARE
  rec RECORD;
BEGIN
  FOR rec IN
  SELECT f_keep."Id" AS keep_id, f_dup."Id" AS dup_id
  FROM ranked f_keep
  JOIN ranked f_dup ON f_keep."UserId" = f_dup."UserId" AND f_keep."_NormalizedName" = f_dup."_NormalizedName"
  WHERE f_keep.rn = 1 AND f_dup.rn > 1
  LOOP
    UPDATE "MusicReleases"
    SET "Artists" = regexp_replace("Artists"::text, '\\b' || rec.dup_id::text || '\\b', rec.keep_id::text, 'g')::jsonb
    WHERE "Artists" IS NOT NULL AND "Artists"::text LIKE '%' || rec.dup_id::text || '%';
  END LOOP;
END$$;

-- Delete duplicate artist rows
DELETE FROM "Artists" a
USING (
  SELECT "Id", "UserId", "_NormalizedName",
         ROW_NUMBER() OVER (PARTITION BY "UserId", "_NormalizedName" ORDER BY "Id") AS rn
  FROM "Artists"
) dups
WHERE a."Id" = dups."Id" AND dups.rn > 1;

UPDATE "Artists" SET "Name" = "_NormalizedName";
ALTER TABLE "Artists" DROP COLUMN IF EXISTS "_NormalizedName";

-- Genres
ALTER TABLE "Genres" ADD COLUMN IF NOT EXISTS "_NormalizedName" text;
UPDATE "Genres" SET "_NormalizedName" = upper(trim("Name"));

WITH ranked AS (
  SELECT "Id", "UserId", "Name", "_NormalizedName",
         ROW_NUMBER() OVER (PARTITION BY "UserId", "_NormalizedName" ORDER BY "Id") AS rn
  FROM "Genres"
)

DO $$
DECLARE
  rec RECORD;
BEGIN
  FOR rec IN
  SELECT f_keep."Id" AS keep_id, f_dup."Id" AS dup_id
  FROM ranked f_keep
  JOIN ranked f_dup ON f_keep."UserId" = f_dup."UserId" AND f_keep."_NormalizedName" = f_dup."_NormalizedName"
  WHERE f_keep.rn = 1 AND f_dup.rn > 1
  LOOP
    UPDATE "MusicReleases"
    SET "Genres" = regexp_replace("Genres"::text, '\\b' || rec.dup_id::text || '\\b', rec.keep_id::text, 'g')::jsonb
    WHERE "Genres" IS NOT NULL AND "Genres"::text LIKE '%' || rec.dup_id::text || '%';
  END LOOP;
END$$;

DELETE FROM "Genres" g
USING (
  SELECT "Id", "UserId", "_NormalizedName",
         ROW_NUMBER() OVER (PARTITION BY "UserId", "_NormalizedName" ORDER BY "Id") AS rn
  FROM "Genres"
) dups
WHERE g."Id" = dups."Id" AND dups.rn > 1;

UPDATE "Genres" SET "Name" = "_NormalizedName";
ALTER TABLE "Genres" DROP COLUMN IF EXISTS "_NormalizedName";

COMMIT;

-- Notes:
-- 1) Run this in a transaction and test on a copy first.
-- 2) The JSON replace approach is simplistic; if the JSON structure differs you should adapt parsing logic.
-- 3) After cleanup, consider adding a migration that enforces a check constraint or index pre-normalized values if desired.
