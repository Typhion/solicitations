using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Places;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Places;

/// <summary>
/// Google Places API (New): https://developers.google.com/maps/documentation/places/web-service/op-overview
/// Only Essentials-tier fields are requested (address, location, types) to stay in the cheapest SKU.
/// </summary>
internal sealed partial class GooglePlacesClient(
    HttpClient http,
    IOptions<GoogleMapsSettings> options,
    IMemoryCache cache,
    ILogger<GooglePlacesClient> logger) : IPlacesClient
{
    private const string DetailsFields = "id,formattedAddress,addressComponents,location";
    private const string SearchFields =
        "places.id,places.formattedAddress,places.addressComponents,places.location";

    // Picking a suggestion fetches its details for the form, and saving fetches them again to verify.
    // A short in-memory cache makes the second call free. (Temporary caching is allowed by Google.)
    private static readonly TimeSpan DetailsCacheTime = TimeSpan.FromMinutes(15);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly GoogleMapsSettings _settings = options.Value;

    public async Task<IReadOnlyList<PlaceSuggestion>> AutocompleteAsync(string text, string? sessionToken, CancellationToken ct)
    {
        var body = new
        {
            input = text,
            languageCode = _settings.Language,
            regionCode = _settings.RegionCode,
            sessionToken,
            locationBias = new
            {
                circle = new
                {
                    center = new { latitude = _settings.BiasLatitude, longitude = _settings.BiasLongitude },
                    radius = _settings.BiasRadiusMeters,
                },
            },
        };

        try
        {
            using var request = Request(HttpMethod.Post, "v1/places:autocomplete",
                "suggestions.placePrediction.placeId,suggestions.placePrediction.text.text,suggestions.placePrediction.structuredFormat");
            request.Content = JsonContent.Create(body, options: Json);
            using var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AutocompleteResponse>(Json, ct);

            return (result?.Suggestions ?? [])
                .Select(s => s.PlacePrediction)
                .OfType<PlacePrediction>()
                .Select(p => new PlaceSuggestion(
                    p.PlaceId,
                    p.Text?.Text ?? "",
                    p.StructuredFormat?.MainText?.Text ?? p.Text?.Text ?? "",
                    p.StructuredFormat?.SecondaryText?.Text ?? ""))
                .ToList();
        }
        catch (Exception ex) when (IsProviderFailure(ex, ct))
        {
            // Suggestions are a convenience: degrade to "none" rather than failing the form.
            logger.LogWarning(ex, "Google Places autocomplete failed");
            return [];
        }
    }

    public async Task<PlaceAddress?> GetPlaceAsync(string placeId, string? sessionToken, CancellationToken ct)
    {
        // Place ids are opaque but URL-safe; anything else never reaches Google's URL.
        if (!PlaceIdPattern().IsMatch(placeId)) return null;

        if (cache.TryGetValue(CacheKey(placeId), out PlaceAddress? cached)) return cached;

        var url = $"v1/places/{placeId}?languageCode={_settings.Language}&regionCode={_settings.RegionCode}";
        if (!string.IsNullOrWhiteSpace(sessionToken)) url += $"&sessionToken={Uri.EscapeDataString(sessionToken)}";

        Place? place;
        try
        {
            using var request = Request(HttpMethod.Get, url, DetailsFields);
            using var response = await http.SendAsync(request, ct);
            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest) return null;
            response.EnsureSuccessStatusCode();
            place = await response.Content.ReadFromJsonAsync<Place>(Json, ct);
        }
        catch (Exception ex) when (IsProviderFailure(ex, ct))
        {
            logger.LogWarning(ex, "Google Place Details failed");
            throw new PlacesUnavailableException("Google Places unavailable.", ex);
        }

        var address = ToAddress(place);
        if (address is not null) cache.Set(CacheKey(placeId), address, DetailsCacheTime);
        return address;
    }

    public async Task<PlaceAddress?> FindAddressAsync(AddressQuery query, CancellationToken ct)
    {
        var body = new
        {
            textQuery = $"{query.Street} {query.StreetNumber}, {query.ZipCode} {query.City}, {query.Country}",
            languageCode = _settings.Language,
            regionCode = _settings.RegionCode,
            pageSize = 1,
        };

        SearchResponse? result;
        try
        {
            using var request = Request(HttpMethod.Post, "v1/places:searchText", SearchFields);
            request.Content = JsonContent.Create(body, options: Json);
            using var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            result = await response.Content.ReadFromJsonAsync<SearchResponse>(Json, ct);
        }
        catch (Exception ex) when (IsProviderFailure(ex, ct))
        {
            logger.LogWarning(ex, "Google Places text search failed");
            throw new PlacesUnavailableException("Google Places unavailable.", ex);
        }

        return ToAddress(result?.Places?.FirstOrDefault());
    }

    private HttpRequestMessage Request(HttpMethod method, string url, string fieldMask)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Goog-Api-Key", _settings.ServerApiKey);
        request.Headers.Add("X-Goog-FieldMask", fieldMask);
        return request;
    }

    /// <summary>Maps Google's address components to our fields; null unless the place has a street.</summary>
    internal static PlaceAddress? ToAddress(Place? place)
    {
        if (place?.Id is null || place.Location is null) return null;

        string? Part(string type, bool shortText = false)
        {
            var c = place.AddressComponents?.FirstOrDefault(c => c.Types?.Contains(type) == true);
            return shortText ? c?.ShortText : c?.LongText;
        }

        var street = Part("route");
        var countryCode = Part("country", shortText: true);
        var city = Part("locality") ?? Part("postal_town") ?? Part("administrative_area_level_2");
        if (street is null || countryCode is null || city is null) return null; // not street-level

        return new PlaceAddress(
            place.Id,
            place.FormattedAddress ?? "",
            Part("country") ?? countryCode,
            countryCode.ToUpperInvariant(),
            city,
            Part("postal_code") ?? "",
            street,
            Part("street_number") ?? "",
            place.Location.Latitude,
            place.Location.Longitude);
    }

    private static bool IsProviderFailure(Exception ex, CancellationToken ct)
        => ex is HttpRequestException or TaskCanceledException or JsonException && !ct.IsCancellationRequested;

    private static string CacheKey(string placeId) => $"google-place:{placeId}";

    [GeneratedRegex("^[A-Za-z0-9_-]{10,300}$")]
    private static partial Regex PlaceIdPattern();

    // ---- Google JSON shapes (only the fields we request) ----

    internal sealed record Place(
        string? Id, string? FormattedAddress, List<AddressComponent>? AddressComponents, LatLng? Location);

    internal sealed record AddressComponent(string? LongText, string? ShortText, List<string>? Types);

    internal sealed record LatLng(double Latitude, double Longitude);

    private sealed record SearchResponse(List<Place>? Places);

    private sealed record AutocompleteResponse(List<Suggestion>? Suggestions);

    private sealed record Suggestion(PlacePrediction? PlacePrediction);

    private sealed record PlacePrediction(string PlaceId, FormattableText? Text, StructuredFormat? StructuredFormat);

    private sealed record StructuredFormat(FormattableText? MainText, FormattableText? SecondaryText);

    private sealed record FormattableText(string? Text);
}

/// <summary>Used when no server key is configured: no suggestions, and verification reports "unavailable".</summary>
internal sealed class DisabledPlacesClient : IPlacesClient
{
    public Task<IReadOnlyList<PlaceSuggestion>> AutocompleteAsync(string text, string? sessionToken, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<PlaceSuggestion>>([]);

    public Task<PlaceAddress?> GetPlaceAsync(string placeId, string? sessionToken, CancellationToken ct)
        => throw new PlacesUnavailableException("Google Places is not configured (GoogleMaps:ServerApiKey).");

    public Task<PlaceAddress?> FindAddressAsync(AddressQuery query, CancellationToken ct)
        => throw new PlacesUnavailableException("Google Places is not configured (GoogleMaps:ServerApiKey).");
}
