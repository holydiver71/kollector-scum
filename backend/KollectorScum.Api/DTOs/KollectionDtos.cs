namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// DTO for returning kollection summary information
    /// </summary>
    public class KollectionSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public int ItemCount { get; set; }
    }

    /// <summary>
    /// DTO for returning detailed kollection information
    /// </summary>
    public class KollectionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public List<MusicReleaseSummaryDto> Releases { get; set; } = new();
    }

    /// <summary>
    /// DTO for creating a new kollection
    /// </summary>
    public class CreateKollectionDto
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for updating a kollection
    /// </summary>
    public class UpdateKollectionDto
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for adding a release to a kollection
    /// </summary>
    public class AddToKollectionDto
    {
        public int MusicReleaseId { get; set; }
        public int? KollectionId { get; set; }
        public string? NewKollectionName { get; set; }
    }
}
