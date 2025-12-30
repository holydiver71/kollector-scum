using System;
using System.Collections.Generic;

namespace KollectorScum.Api;

public partial class MusicRelease
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public DateTime? ReleaseYear { get; set; }

    public DateTime? OrigReleaseYear { get; set; }

    public string? Artists { get; set; }

    public string? Genres { get; set; }

    public bool Live { get; set; }

    public int? LabelId { get; set; }

    public int? CountryId { get; set; }

    public string? LabelNumber { get; set; }

    public int? LengthInSeconds { get; set; }

    public int? FormatId { get; set; }

    public string? PurchaseInfo { get; set; }

    public int? PackagingId { get; set; }

    public string? Images { get; set; }

    public string? Links { get; set; }

    public DateTime DateAdded { get; set; }

    public DateTime LastModified { get; set; }

    public string? Media { get; set; }

    public int? ArtistId { get; set; }

    public int? GenreId { get; set; }

    public int? StoreId { get; set; }

    public string? Upc { get; set; }

    public Guid UserId { get; set; }

    public int? DiscogsId { get; set; }

    public string? Notes { get; set; }
}
