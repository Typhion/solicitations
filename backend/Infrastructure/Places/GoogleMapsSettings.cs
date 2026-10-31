namespace Infrastructure.Places;

/// <summary>
/// Google Maps Platform. Two keys, both secrets (user-secrets in dev, server .env in prod):
/// <list type="bullet">
/// <item><see cref="ServerApiKey"/>: Places API (New), called only from this API. Restrict it by IP.</item>
/// <item><see cref="BrowserApiKey"/>: Maps JavaScript API, handed to the SPA to draw maps. Restrict it
/// by HTTP referrer. It ends up in the browser, so it must never have server APIs enabled.</item>
/// </list>
/// Leave <see cref="ServerApiKey"/> empty to disable verification (addresses are then free text only).
/// </summary>
public sealed class GoogleMapsSettings
{
    public const string SectionName = "GoogleMaps";

    public string? ServerApiKey { get; init; }
    public string? BrowserApiKey { get; init; }

    /// <summary>Map ID for vector maps / Advanced Markers. DEMO_MAP_ID works for development.</summary>
    public string MapId { get; init; } = "DEMO_MAP_ID";

    public string BaseUrl { get; init; } = "https://places.googleapis.com/";

    /// <summary>Region (ccTLD) used to bias suggestions and format addresses.</summary>
    public string RegionCode { get; init; } = "be";

    /// <summary>Suggestions are biased (not restricted) to this circle; 50 km is Google's maximum radius.</summary>
    public double BiasLatitude { get; init; } = 50.85;
    public double BiasLongitude { get; init; } = 4.35;
    public double BiasRadiusMeters { get; init; } = 50_000;

    /// <summary>Google allows caching coordinates for at most 30 days; refresh them before that.</summary>
    public int RefreshAfterDays { get; init; } = 25;

    public int TimeoutSeconds { get; init; } = 5;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ServerApiKey);
}
