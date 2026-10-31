namespace Application.Places;

/// <summary>Google Places API (New), implemented in Infrastructure.</summary>
public interface IPlacesClient
{
    /// <summary>Suggestions (companies and addresses) as the user types. Empty on provider failure.</summary>
    Task<IReadOnlyList<PlaceSuggestion>> AutocompleteAsync(string text, string? sessionToken, CancellationToken ct);

    /// <summary>
    /// The street-level address of a place, or null when the place doesn't exist or isn't precise enough
    /// (no street). Throws <see cref="PlacesUnavailableException"/> when Google can't be reached.
    /// </summary>
    Task<PlaceAddress?> GetPlaceAsync(string placeId, string? sessionToken, CancellationToken ct);

    /// <summary>Best street-level match for a typed address, or null. Same failure rules as above.</summary>
    Task<PlaceAddress?> FindAddressAsync(AddressQuery query, CancellationToken ct);
}

public sealed record AddressQuery(string Country, string City, string ZipCode, string Street, string StreetNumber);

/// <summary>
/// A place's address split into our fields. <see cref="StreetNumber"/> can be empty (a company listed
/// without a house number). <see cref="CountryCode"/> is ISO 3166-1 alpha-2, upper case.
/// </summary>
public sealed record PlaceAddress(
    string PlaceId, string FormattedAddress,
    string Country, string CountryCode, string City, string ZipCode, string Street, string StreetNumber,
    double Latitude, double Longitude);

public sealed record PlaceSuggestion(string PlaceId, string Text, string MainText, string SecondaryText);

public sealed class PlacesUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
