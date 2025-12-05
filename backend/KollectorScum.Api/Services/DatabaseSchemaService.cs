using KollectorScum.Api.Interfaces;

namespace KollectorScum.Api.Services
{
    /// <summary>
    /// Service for generating database schema documentation for LLM context
    /// </summary>
    public class DatabaseSchemaService : IDatabaseSchemaService
    {
        private readonly string _schemaDocumentation;
        private readonly string _sampleQueries;

        public DatabaseSchemaService()
        {
            _schemaDocumentation = GenerateSchemaDocumentation();
            _sampleQueries = GenerateSampleQueries();
        }

        /// <inheritdoc />
        public string GetSchemaDocumentation() => _schemaDocumentation;

        /// <inheritdoc />
        public string GetSampleQueries() => _sampleQueries;

        private static string GenerateSchemaDocumentation()
        {
            return @"
DATABASE SCHEMA FOR MUSIC COLLECTION:

=== MAIN TABLE ===

TABLE: ""MusicReleases""
- ""Id"" (INTEGER, PRIMARY KEY) - Unique identifier
- ""Title"" (VARCHAR(300), NOT NULL) - Album/release title
- ""ReleaseYear"" (TIMESTAMP) - Release date (use EXTRACT(YEAR FROM ""ReleaseYear"") for year)
- ""OrigReleaseYear"" (TIMESTAMP) - Original release date if reissue
- ""Artists"" (TEXT) - JSON array of artist IDs, e.g. '[1,2]'
- ""Genres"" (TEXT) - JSON array of genre IDs, e.g. '[1,3]'
- ""Live"" (BOOLEAN) - True if live recording
- ""LabelId"" (INTEGER, FK) - References Labels.Id
- ""CountryId"" (INTEGER, FK) - References Countries.Id
- ""LabelNumber"" (VARCHAR(100)) - Catalog number
- ""LengthInSeconds"" (INTEGER) - Total duration
- ""FormatId"" (INTEGER, FK) - References Formats.Id
- ""PackagingId"" (INTEGER, FK) - References Packagings.Id
- ""Upc"" (VARCHAR(50)) - UPC barcode
- ""PurchaseInfo"" (TEXT) - JSON with purchase details (storeId, price, currency, purchaseDate, notes)
- ""Images"" (TEXT) - JSON with image paths (coverFront, coverBack, thumbnail)
- ""Links"" (TEXT) - JSON array of links (url, type, description)
- ""DateAdded"" (TIMESTAMP) - When added to collection
- ""LastModified"" (TIMESTAMP) - Last update time
- ""Media"" (TEXT) - JSON with track listing

=== LOOKUP TABLES ===

TABLE: ""Artists""
- ""Id"" (INTEGER, PRIMARY KEY)
- ""Name"" (VARCHAR(200), NOT NULL)

TABLE: ""Labels""
- ""Id"" (INTEGER, PRIMARY KEY)
- ""Name"" (VARCHAR(100), NOT NULL)

TABLE: ""Countries""
- ""Id"" (INTEGER, PRIMARY KEY)
- ""Name"" (VARCHAR(100), NOT NULL)

TABLE: ""Formats""
- ""Id"" (INTEGER, PRIMARY KEY)
- ""Name"" (VARCHAR(50), NOT NULL) - e.g. 'CD', 'Vinyl', 'Cassette', 'Digital'

TABLE: ""Genres""
- ""Id"" (INTEGER, PRIMARY KEY)
- ""Name"" (VARCHAR(50), NOT NULL) - e.g. 'Rock', 'Jazz', 'Classical'

TABLE: ""Packagings""
- ""Id"" (INTEGER, PRIMARY KEY)
- ""Name"" (VARCHAR(50), NOT NULL) - e.g. 'Jewel Case', 'Gatefold', 'Digipak'

TABLE: ""Stores""
- ""Id"" (INTEGER, PRIMARY KEY)
- ""Name"" (VARCHAR(100), NOT NULL) - Where purchased

TABLE: ""NowPlayings""
- ""Id"" (INTEGER, PRIMARY KEY)
- ""MusicReleaseId"" (INTEGER, FK) - References MusicReleases.Id
- ""PlayedAt"" (TIMESTAMP) - When the release was played

=== RELATIONSHIPS ===

- MusicReleases.LabelId -> Labels.Id
- MusicReleases.CountryId -> Countries.Id
- MusicReleases.FormatId -> Formats.Id
- MusicReleases.PackagingId -> Packagings.Id
- MusicReleases.Artists contains JSON array of Artist IDs
- MusicReleases.Genres contains JSON array of Genre IDs
- NowPlayings.MusicReleaseId -> MusicReleases.Id

=== IMPORTANT NOTES ===

1. This is PostgreSQL - use double quotes for column/table names with capitals
2. Artists and Genres are stored as JSON arrays in MusicReleases
3. To find releases by artist name, join with Artists table using JSON functions
4. Date fields are TIMESTAMP type, use EXTRACT(YEAR FROM field) to get year
5. Always use double quotes around identifiers: ""MusicReleases"", ""Title"", etc.
6. For JSON arrays, use PostgreSQL JSON functions like jsonb_array_elements
";
        }

        private static string GenerateSampleQueries()
        {
            return @"
SAMPLE QUERIES:

1. Count all releases:
SELECT COUNT(*) as total FROM ""MusicReleases""

2. Releases from a specific decade (1980s):
SELECT ""Title"", EXTRACT(YEAR FROM ""ReleaseYear"") as year 
FROM ""MusicReleases"" 
WHERE EXTRACT(YEAR FROM ""ReleaseYear"") >= 1980 
  AND EXTRACT(YEAR FROM ""ReleaseYear"") < 1990
ORDER BY ""ReleaseYear""

3. Releases by format (e.g., CD):
SELECT mr.""Title"", f.""Name"" as format
FROM ""MusicReleases"" mr
JOIN ""Formats"" f ON mr.""FormatId"" = f.""Id""
WHERE LOWER(f.""Name"") = 'cd'

4. Count by format:
SELECT f.""Name"" as format, COUNT(*) as count
FROM ""MusicReleases"" mr
JOIN ""Formats"" f ON mr.""FormatId"" = f.""Id""
GROUP BY f.""Name""
ORDER BY count DESC

5. Releases by artist name (using JSON):
SELECT mr.""Title"", a.""Name"" as artist
FROM ""MusicReleases"" mr
JOIN ""Artists"" a ON mr.""Artists""::jsonb @> to_jsonb(a.""Id"")
WHERE LOWER(a.""Name"") LIKE '%queen%'

6. Recently added releases:
SELECT ""Title"", ""DateAdded""
FROM ""MusicReleases""
ORDER BY ""DateAdded"" DESC
LIMIT 10

7. Releases by country:
SELECT mr.""Title"", c.""Name"" as country
FROM ""MusicReleases"" mr
JOIN ""Countries"" c ON mr.""CountryId"" = c.""Id""
WHERE LOWER(c.""Name"") = 'usa'

8. Live recordings:
SELECT ""Title"", EXTRACT(YEAR FROM ""ReleaseYear"") as year
FROM ""MusicReleases""
WHERE ""Live"" = true

9. Count releases by genre:
SELECT g.""Name"" as genre, COUNT(*) as count
FROM ""MusicReleases"" mr
JOIN ""Genres"" g ON mr.""Genres""::jsonb @> to_jsonb(g.""Id"")
GROUP BY g.""Name""
ORDER BY count DESC

10. Recently played releases:
SELECT mr.""Title"", np.""PlayedAt""
FROM ""NowPlayings"" np
JOIN ""MusicReleases"" mr ON np.""MusicReleaseId"" = mr.""Id""
ORDER BY np.""PlayedAt"" DESC
LIMIT 10
";
        }
    }
}
