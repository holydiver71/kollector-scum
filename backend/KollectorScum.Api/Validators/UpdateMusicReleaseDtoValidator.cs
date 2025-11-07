using FluentValidation;
using KollectorScum.Api.DTOs;

namespace KollectorScum.Api.Validators;

/// <summary>
/// Validator for UpdateMusicReleaseDto
/// </summary>
public class UpdateMusicReleaseDtoValidator : AbstractValidator<UpdateMusicReleaseDto>
{
    public UpdateMusicReleaseDtoValidator()
    {
        // Title validation - required but allowing empty for updates that focus on other fields
        RuleFor(x => x.Title)
            .MaximumLength(300)
            .WithMessage("Title cannot exceed 300 characters");

        // Artist IDs validation
        When(x => x.ArtistIds != null && x.ArtistIds.Any(), () =>
        {
            RuleFor(x => x.ArtistIds)
                .Must(ids => ids!.All(id => id > 0))
                .WithMessage("All artist IDs must be positive integers");
        });

        // Genre IDs validation
        When(x => x.GenreIds != null && x.GenreIds.Any(), () =>
        {
            RuleFor(x => x.GenreIds)
                .Must(ids => ids!.All(id => id > 0))
                .WithMessage("All genre IDs must be positive integers");
        });

        // Year validation
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

        // Label number validation
        When(x => !string.IsNullOrWhiteSpace(x.LabelNumber), () =>
        {
            RuleFor(x => x.LabelNumber)
                .MaximumLength(100)
                .WithMessage("Label number cannot exceed 100 characters");
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
