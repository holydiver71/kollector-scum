-- Fix all ID sequences to match current max IDs
-- This fixes the "duplicate key value violates unique constraint" error

SELECT setval('"MusicReleases_Id_seq"', COALESCE((SELECT MAX("Id") FROM "MusicReleases"), 0) + 1, false);
SELECT setval('"Artists_Id_seq"', COALESCE((SELECT MAX("Id") FROM "Artists"), 0) + 1, false);
SELECT setval('"Labels_Id_seq"', COALESCE((SELECT MAX("Id") FROM "Labels"), 0) + 1, false);
SELECT setval('"Genres_Id_seq"', COALESCE((SELECT MAX("Id") FROM "Genres"), 0) + 1, false);
SELECT setval('"Countries_Id_seq"', COALESCE((SELECT MAX("Id") FROM "Countries"), 0) + 1, false);
SELECT setval('"Formats_Id_seq"', COALESCE((SELECT MAX("Id") FROM "Formats"), 0) + 1, false);
SELECT setval('"Packagings_Id_seq"', COALESCE((SELECT MAX("Id") FROM "Packagings"), 0) + 1, false);
SELECT setval('"Stores_Id_seq"', COALESCE((SELECT MAX("Id") FROM "Stores"), 0) + 1, false);
