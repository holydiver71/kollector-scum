using FluentValidation;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Validators;

/// <summary>
/// Validator for CreateMusicReleaseDto
/// </summary>
public class CreateMusicReleaseDtoValidator : AbstractValidator<CreateMusicReleaseDto>
{
    public CreateMusicReleaseDtoValidator()
    {
        // Title validation
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(300)
            .WithMessage("Title cannot exceed 300 characters");

        // Artist validation - must have either artist IDs or artist names
        RuleFor(x => x)
            .Must(x => (x.ArtistIds != null && x.ArtistIds.Any()) || 
                      (x.ArtistNames != null && x.ArtistNames.Any()))
            .WithMessage("At least one artist ID or artist name is required")
            .WithName("Artists");

        // Artist IDs validation
        When(x => x.ArtistIds != null && x.ArtistIds.Any(), () =>
        {
            RuleFor(x => x.ArtistIds)
                .Must(ids => ids!.All(id => id > 0))
                .WithMessage("All artist IDs must be positive integers");
        });

        // Artist names validation
        When(x => x.ArtistNames != null && x.ArtistNames.Any(), () =>
        {
            RuleFor(x => x.ArtistNames)
                .Must(names => names!.All(n => !string.IsNullOrWhiteSpace(n)))
                .WithMessage("Artist names cannot be empty or whitespace");
        });

        // Genre IDs validation
        When(x => x.GenreIds != null && x.GenreIds.Any(), () =>
        {
            RuleFor(x => x.GenreIds)
                .Must(ids => ids!.All(id => id > 0))
                .WithMessage("All genre IDs must be positive integers");
        });

        // Genre names validation
        When(x => x.GenreNames != null && x.GenreNames.Any(), () =>
        {
            RuleFor(x => x.GenreNames)
                .Must(names => names!.All(n => !string.IsNullOrWhiteSpace(n)))
                .WithMessage("Genre names cannot be empty or whitespace");
        });

        // Year validation - must be between 1900 and current year + 1 (for upcoming releases)
        When(x => x.ReleaseYear.HasValue, () =>
        {
            RuleFor(x => x.ReleaseYear!.Value.Year)
                .InclusiveBetween(1900, DateTime.UtcNow.Year + 1)
                .WithMessage($"Release year must be between 1900 and {DateTime.UtcNow.Year + 1}");
        });

        When(x => x.OrigReleaseYear.HasValue, () =>
        {
            RuleFor(x => x.OrigReleaseYear!.Value.Year)
                .InclusiveBetween(1900, DateTime.UtcNow.Year + 1)
                .WithMessage($"Original release year must be between 1900 and {DateTime.UtcNow.Year + 1}");
        });

        // Label validation - cannot have both ID and name
        RuleFor(x => x)
            .Must(x => !(x.LabelId.HasValue && !string.IsNullOrWhiteSpace(x.LabelName)))
            .WithMessage("Cannot specify both LabelId and LabelName")
            .WithName("Label");

        // Country validation - cannot have both ID and name
        RuleFor(x => x)
            .Must(x => !(x.CountryId.HasValue && !string.IsNullOrWhiteSpace(x.CountryName)))
            .WithMessage("Cannot specify both CountryId and CountryName")
            .WithName("Country");

        // Format validation - cannot have both ID and name
        RuleFor(x => x)
            .Must(x => !(x.FormatId.HasValue && !string.IsNullOrWhiteSpace(x.FormatName)))
            .WithMessage("Cannot specify both FormatId and FormatName")
            .WithName("Format");

        // Packaging validation - cannot have both ID and name
        RuleFor(x => x)
            .Must(x => !(x.PackagingId.HasValue && !string.IsNullOrWhiteSpace(x.PackagingName)))
            .WithMessage("Cannot specify both PackagingId and PackagingName")
            .WithName("Packaging");

        // Label number validation
        When(x => !string.IsNullOrWhiteSpace(x.LabelNumber), () =>
        {
            RuleFor(x => x.LabelNumber)
                .MaximumLength(100)
                .WithMessage("Label number cannot exceed 100 characters");
        });

        // UPC validation
        When(x => !string.IsNullOrWhiteSpace(x.Upc), () =>
        {
            RuleFor(x => x.Upc)
                .MaximumLength(50)
                .WithMessage("UPC cannot exceed 50 characters");
        });

        // Length validation
        When(x => x.LengthInSeconds.HasValue, () =>
        {
            RuleFor(x => x.LengthInSeconds)
                .GreaterThan(0)
                .WithMessage("Length must be a positive number");
        });

        // Purchase info validation
        When(x => x.PurchaseInfo != null, () =>
        {
            RuleFor(x => x.PurchaseInfo!.Price)
                .GreaterThanOrEqualTo(0)
                .When(x => x.PurchaseInfo!.Price.HasValue)
                .WithMessage("Price must be a non-negative number");

            RuleFor(x => x.PurchaseInfo!.Currency)
                .NotEmpty()
                .When(x => x.PurchaseInfo!.Price.HasValue)
                .WithMessage("Currency is required when price is specified");

            RuleFor(x => x.PurchaseInfo!)
                .Must(p => !(p.StoreId.HasValue && !string.IsNullOrWhiteSpace(p.StoreName)))
                .WithMessage("Cannot specify both StoreId and StoreName");
        });

        // Links URL validation
        When(x => x.Links != null && x.Links.Any(), () =>
        {
            RuleForEach(x => x.Links)
                .ChildRules(link =>
                {
                    link.RuleFor(l => l.Url)
                        .NotEmpty()
                        .WithMessage("Link URL is required")
                        .Must(BeAValidUrl)
                        .WithMessage("Link URL must be a valid URL");
                });
        });

        // Images URL validation
        When(x => x.Images != null, () =>
        {
            RuleFor(x => x.Images!.CoverFront)
                .Must(BeAValidUrl!)
                .When(x => !string.IsNullOrWhiteSpace(x.Images!.CoverFront))
                .WithMessage("Cover front URL must be a valid URL");

            RuleFor(x => x.Images!.CoverBack)
                .Must(BeAValidUrl!)
                .When(x => !string.IsNullOrWhiteSpace(x.Images!.CoverBack))
                .WithMessage("Cover back URL must be a valid URL");

            RuleFor(x => x.Images!.Thumbnail)
                .Must(BeAValidUrl!)
                .When(x => !string.IsNullOrWhiteSpace(x.Images!.Thumbnail))
                .WithMessage("Thumbnail URL must be a valid URL");
        });
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
