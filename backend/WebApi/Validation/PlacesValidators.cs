using FluentValidation;
using WebApi.Api;

namespace WebApi.Validation;

// Google session tokens: URL- and filename-safe base64, at most 36 chars (a UUID fits).
internal static class PlacesRules
{
    public const string SessionTokenPattern = "^[A-Za-z0-9_-]{1,36}$";
}

public sealed class AutocompleteQueryValidator : AbstractValidator<PlacesEndpoints.AutocompleteQuery>
{
    public AutocompleteQueryValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.SessionToken).Matches(PlacesRules.SessionTokenPattern).When(x => x.SessionToken is not null);
    }
}

public sealed class PlaceQueryValidator : AbstractValidator<PlacesEndpoints.PlaceQuery>
{
    public PlaceQueryValidator()
    {
        RuleFor(x => x.PlaceId).NotEmpty().Matches("^[A-Za-z0-9_-]{10,300}$");
        RuleFor(x => x.SessionToken).Matches(PlacesRules.SessionTokenPattern).When(x => x.SessionToken is not null);
    }
}
