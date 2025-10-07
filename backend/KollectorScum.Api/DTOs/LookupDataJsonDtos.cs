namespace KollectorScum.Api.DTOs
{
    /// <summary>
    /// DTO for country JSON data
    /// </summary>
    public class CountryJsonDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Container for countries JSON data
    /// </summary>
    public class CountriesJsonContainer
    {
        public List<CountryJsonDto> Countrys { get; set; } = new List<CountryJsonDto>();
    }

    /// <summary>
    /// DTO for store JSON data
    /// </summary>
    public class StoreJsonDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Container for stores JSON data
    /// </summary>
    public class StoresJsonContainer
    {
        public List<StoreJsonDto> Stores { get; set; } = new List<StoreJsonDto>();
    }

    /// <summary>
    /// DTO for format JSON data
    /// </summary>
    public class FormatJsonDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Container for formats JSON data
    /// </summary>
    public class FormatsJsonContainer
    {
        public List<FormatJsonDto> Formats { get; set; } = new List<FormatJsonDto>();
    }

    /// <summary>
    /// DTO for genre JSON data
    /// </summary>
    public class GenreJsonDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Container for genres JSON data
    /// </summary>
    public class GenresJsonContainer
    {
        public List<GenreJsonDto> Genres { get; set; } = new List<GenreJsonDto>();
    }

    /// <summary>
    /// DTO for label JSON data
    /// </summary>
    public class LabelJsonDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Container for labels JSON data
    /// </summary>
    public class LabelsJsonContainer
    {
        public List<LabelJsonDto> Labels { get; set; } = new List<LabelJsonDto>();
    }

    /// <summary>
    /// DTO for artist JSON data
    /// </summary>
    public class ArtistJsonDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Container for artists JSON data
    /// </summary>
    public class ArtistsJsonContainer
    {
        public List<ArtistJsonDto> Artists { get; set; } = new List<ArtistJsonDto>();
    }

    /// <summary>
    /// DTO for packaging JSON data
    /// </summary>
    public class PackagingJsonDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Container for packagings JSON data
    /// </summary>
    public class PackagingsJsonContainer
    {
        public List<PackagingJsonDto> Packagings { get; set; } = new List<PackagingJsonDto>();
    }
}
