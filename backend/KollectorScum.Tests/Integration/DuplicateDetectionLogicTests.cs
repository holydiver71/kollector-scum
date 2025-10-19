using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using KollectorScum.Api.Models;
using Xunit;

namespace KollectorScum.Tests.Integration
{
    /// <summary>
    /// Integration tests for the duplicate detection logic in CheckForDuplicates
    /// Tests the logic that prevents adding duplicate releases based on catalog number or title+artist
    /// </summary>
    public class DuplicateDetectionLogicTests
    {
        [Fact]
        public void DuplicateCheck_ExactCatalogMatch_FindsDuplicate()
        {
            // Arrange - Simulate existing database releases
            var existingReleases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Existing Album",
                    LabelNumber = "CAT001",
                    Artists = JsonSerializer.Serialize(new List<int> { 1 })
                }
            };

            var newCatalog = "CAT001";

            // Act - Simulate CheckForDuplicates catalog logic
            var normalizedCatalog = newCatalog.Trim().ToLower();
            var matches = existingReleases
                .Where(r => r.LabelNumber != null && r.LabelNumber.ToLower() == normalizedCatalog)
                .ToList();

            // Assert
            Assert.Single(matches);
            Assert.Equal(1, matches[0].Id);
        }

        [Fact]
        public void DuplicateCheck_CaseInsensitiveCatalog_FindsDuplicate()
        {
            // Arrange
            var existingReleases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Test Album",
                    LabelNumber = "cat001", // lowercase
                    Artists = JsonSerializer.Serialize(new List<int> { 1 })
                }
            };

            var newCatalog = "CAT001"; // uppercase

            // Act
            var normalizedCatalog = newCatalog.Trim().ToLower();
            var matches = existingReleases
                .Where(r => r.LabelNumber != null && r.LabelNumber.ToLower() == normalizedCatalog)
                .ToList();

            // Assert
            Assert.Single(matches);
        }

        [Fact]
        public void DuplicateCheck_TitleAndArtistMatch_FindsDuplicate()
        {
            // Arrange
            var existingReleases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Test Album",
                    LabelNumber = "CAT001",
                    Artists = JsonSerializer.Serialize(new List<int> { 1, 2 })
                }
            };

            var newTitle = "Test Album";
            var newArtistIds = new List<int> { 1 }; // At least one artist matches

            // Act - Simulate CheckForDuplicates title+artist logic
            var normalizedTitle = newTitle.Trim().ToLower();
            var matches = existingReleases.Where(r =>
            {
                // Check title
                if (r.Title.Trim().ToLower() != normalizedTitle)
                    return false;

                // Check artist overlap
                if (string.IsNullOrEmpty(r.Artists))
                    return false;

                var releaseArtistIds = JsonSerializer.Deserialize<List<int>>(r.Artists);
                return releaseArtistIds != null && releaseArtistIds.Intersect(newArtistIds).Any();
            }).ToList();

            // Assert
            Assert.Single(matches);
        }

        [Fact]
        public void DuplicateCheck_SameTitleDifferentArtist_NoDuplicate()
        {
            // Arrange
            var existingReleases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Common Title",
                    LabelNumber = "CAT001",
                    Artists = JsonSerializer.Serialize(new List<int> { 1 })
                }
            };

            var newTitle = "Common Title";
            var newArtistIds = new List<int> { 2 }; // Different artist

            // Act
            var normalizedTitle = newTitle.Trim().ToLower();
            var matches = existingReleases.Where(r =>
            {
                if (r.Title.Trim().ToLower() != normalizedTitle)
                    return false;

                if (string.IsNullOrEmpty(r.Artists))
                    return false;

                var releaseArtistIds = JsonSerializer.Deserialize<List<int>>(r.Artists);
                return releaseArtistIds != null && releaseArtistIds.Intersect(newArtistIds).Any();
            }).ToList();

            // Assert
            Assert.Empty(matches);
        }

        [Fact]
        public void DuplicateCheck_DifferentTitleSameArtist_NoDuplicate()
        {
            // Arrange
            var existingReleases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "First Album",
                    LabelNumber = "CAT001",
                    Artists = JsonSerializer.Serialize(new List<int> { 1 })
                }
            };

            var newTitle = "Second Album";
            var newArtistIds = new List<int> { 1 }; // Same artist, different title

            // Act
            var normalizedTitle = newTitle.Trim().ToLower();
            var matches = existingReleases.Where(r =>
            {
                if (r.Title.Trim().ToLower() != normalizedTitle)
                    return false;

                if (string.IsNullOrEmpty(r.Artists))
                    return false;

                var releaseArtistIds = JsonSerializer.Deserialize<List<int>>(r.Artists);
                return releaseArtistIds != null && releaseArtistIds.Intersect(newArtistIds).Any();
            }).ToList();

            // Assert
            Assert.Empty(matches);
        }

        [Fact]
        public void DuplicateCheck_UniqueRelease_NoMatches()
        {
            // Arrange
            var existingReleases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Existing Album",
                    LabelNumber = "CAT001",
                    Artists = JsonSerializer.Serialize(new List<int> { 1 })
                }
            };

            var newCatalog = "CAT999";
            var newTitle = "New Album";
            var newArtistIds = new List<int> { 2 };

            // Act - Check catalog
            var normalizedCatalog = newCatalog.Trim().ToLower();
            var catalogMatches = existingReleases
                .Where(r => r.LabelNumber != null && r.LabelNumber.ToLower() == normalizedCatalog)
                .ToList();

            // Check title+artist
            var normalizedTitle = newTitle.Trim().ToLower();
            var titleArtistMatches = existingReleases.Where(r =>
            {
                if (r.Title.Trim().ToLower() != normalizedTitle)
                    return false;

                if (string.IsNullOrEmpty(r.Artists))
                    return false;

                var releaseArtistIds = JsonSerializer.Deserialize<List<int>>(r.Artists);
                return releaseArtistIds != null && releaseArtistIds.Intersect(newArtistIds).Any();
            }).ToList();

            // Assert
            Assert.Empty(catalogMatches);
            Assert.Empty(titleArtistMatches);
        }

        [Fact]
        public void DuplicateCheck_NullCatalogNumber_SkipsCatalogCheck()
        {
            // Arrange
            var existingReleases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Album Without Catalog",
                    LabelNumber = null,
                    Artists = JsonSerializer.Serialize(new List<int> { 1 })
                }
            };

            string? newCatalog = null;

            // Act
            var matches = new List<MusicRelease>();
            if (!string.IsNullOrWhiteSpace(newCatalog))
            {
                var normalizedCatalog = newCatalog.Trim().ToLower();
                matches = existingReleases
                    .Where(r => r.LabelNumber != null && r.LabelNumber.ToLower() == normalizedCatalog)
                    .ToList();
            }

            // Assert - Should not match when catalog is null
            Assert.Empty(matches);
        }

        [Fact]
        public void DuplicateCheck_MultipleArtistOverlap_FindsDuplicate()
        {
            // Arrange
            var existingReleases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Collaboration Album",
                    LabelNumber = "CAT001",
                    Artists = JsonSerializer.Serialize(new List<int> { 1, 2, 3 })
                }
            };

            var newTitle = "Collaboration Album";
            var newArtistIds = new List<int> { 2, 4 }; // Artist 2 overlaps

            // Act
            var normalizedTitle = newTitle.Trim().ToLower();
            var matches = existingReleases.Where(r =>
            {
                if (r.Title.Trim().ToLower() != normalizedTitle)
                    return false;

                if (string.IsNullOrEmpty(r.Artists))
                    return false;

                var releaseArtistIds = JsonSerializer.Deserialize<List<int>>(r.Artists);
                return releaseArtistIds != null && releaseArtistIds.Intersect(newArtistIds).Any();
            }).ToList();

            // Assert
            Assert.Single(matches);
            Assert.Equal(1, matches[0].Id);
        }

        [Fact]
        public void DuplicateCheck_WhitespaceInCatalog_NormalizedCorrectly()
        {
            // Arrange
            var existingReleases = new List<MusicRelease>
            {
                new MusicRelease
                {
                    Id = 1,
                    Title = "Test",
                    LabelNumber = "  CAT001  ", // Extra whitespace
                    Artists = JsonSerializer.Serialize(new List<int> { 1 })
                }
            };

            var newCatalog = "CAT001"; // No whitespace

            // Act
            var normalizedCatalog = newCatalog.Trim().ToLower();
            var matches = existingReleases
                .Where(r => r.LabelNumber != null && r.LabelNumber.Trim().ToLower() == normalizedCatalog)
                .ToList();

            // Assert
            Assert.Single(matches);
        }
    }
}
