using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using KollectorScum.Api.Models;
using Xunit;

namespace KollectorScum.Tests.Integration
{
    /// <summary>
    /// Integration tests for mapping Discogs data to MusicRelease entities
    /// Validates that data from Discogs API can be properly stored in our model
    /// </summary>
    public class DiscogsDataMappingTests
    {
        [Fact]
        public void MusicRelease_CanStoreDiscogsBasicData()
        {
            // Arrange - Simulate Discogs data
            var discogsTitle = "Test Album";
            var discogsYear = 2020;
            var discogsCatalog = "CAT001";

            // Act - Create MusicRelease
            var release = new MusicRelease
            {
                Title = discogsTitle,
                ReleaseYear = new DateTime(discogsYear, 1, 1),
                LabelNumber = discogsCatalog
            };

            // Assert
            Assert.Equal("Test Album", release.Title);
            Assert.NotNull(release.ReleaseYear);
            Assert.Equal(2020, release.ReleaseYear.Value.Year);
            Assert.Equal("CAT001", release.LabelNumber);
        }

        [Fact]
        public void MusicRelease_CanStoreArtistsAsJson()
        {
            // Arrange - Discogs returns multiple artists
            var discogsArtists = new List<int> { 1, 2, 3 };

            // Act - Store as JSON string
            var release = new MusicRelease
            {
                Title = "Collaboration Album",
                Artists = JsonSerializer.Serialize(discogsArtists)
            };

            // Assert - Can deserialize back
            Assert.NotNull(release.Artists);
            var deserializedArtists = JsonSerializer.Deserialize<List<int>>(release.Artists);
            Assert.NotNull(deserializedArtists);
            Assert.Equal(3, deserializedArtists.Count);
            Assert.Contains(1, deserializedArtists);
            Assert.Contains(2, deserializedArtists);
        }

        [Fact]
        public void MusicRelease_CanStoreImagesAsJson()
        {
            // Arrange - Discogs image URLs
            var imagesObject = new
            {
                CoverFront = "https://example.com/front.jpg",
                CoverBack = "https://example.com/back.jpg",
                Thumbnail = "https://example.com/thumb.jpg"
            };

            // Act - Store as JSON
            var release = new MusicRelease
            {
                Title = "Album With Images",
                Images = JsonSerializer.Serialize(imagesObject)
            };

            // Assert
            Assert.NotNull(release.Images);
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>(release.Images);
            Assert.NotNull(deserialized);
            Assert.Equal("https://example.com/front.jpg", deserialized["CoverFront"]);
        }

        [Fact]
        public void MusicRelease_CanStoreLinksAsJson()
        {
            // Arrange - Discogs link
            var linksObject = new[]
            {
                new { Description = "Discogs", Url = "https://www.discogs.com/release/12345", UrlType = "Discogs" }
            };

            // Act
            var release = new MusicRelease
            {
                Title = "Test Release",
                Links = JsonSerializer.Serialize(linksObject)
            };

            // Assert
            Assert.NotNull(release.Links);
            Assert.Contains("12345", release.Links);
        }

        [Fact]
        public void MusicRelease_CanStoreMediaAsJson()
        {
            // Arrange - Discogs tracklist
            var mediaObject = new[]
            {
                new
                {
                    Title = "CD",
                    FormatId = 1,
                    Index = 1,
                    Tracks = new[]
                    {
                        new { Title = "Track 1", LengthSecs = 225, Index = 1 },
                        new { Title = "Track 2", LengthSecs = 260, Index = 2 }
                    }
                }
            };

            // Act
            var release = new MusicRelease
            {
                Title = "Album With Tracks",
                Media = JsonSerializer.Serialize(mediaObject)
            };

            // Assert
            Assert.NotNull(release.Media);
            Assert.Contains("Track 1", release.Media);
            Assert.Contains("Track 2", release.Media);
        }
    }
}
