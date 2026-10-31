namespace Domain.Solicitation;

/// <summary>
/// A company's postal address. Either <b>verified</b> (resolved to a Google place: carries the place id
/// and coordinates, and can be shown on a map) or <b>unverified</b> (free text, e.g. outside the EU;
/// never on the map). Use the factories: they keep "verified ⇔ has place id + coordinates" true.
/// </summary>
public sealed record Location
{
    private Location() { } // EF

    private Location(string country, string city, string zipCode, string street, string streetNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(zipCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(streetNumber);

        Country = country;
        City = city;
        ZipCode = zipCode;
        Street = street;
        StreetNumber = streetNumber;
    }

    public static Location Unverified(string country, string city, string zipCode, string street, string streetNumber)
        => new(country, city, zipCode, street, streetNumber);

    public static Location Verified(string country, string city, string zipCode, string street, string streetNumber,
        string placeId, double latitude, double longitude, DateTime verifiedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(placeId);
        ArgumentOutOfRangeException.ThrowIfLessThan(latitude, -90);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(latitude, 90);
        ArgumentOutOfRangeException.ThrowIfLessThan(longitude, -180);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(longitude, 180);

        return new(country, city, zipCode, street, streetNumber)
        {
            PlaceId = placeId,
            Latitude = latitude,
            Longitude = longitude,
            VerifiedAtUtc = verifiedAtUtc,
        };
    }

    public string Country { get; private init; } = null!;
    public string City { get; private init; } = null!;
    public string ZipCode { get; private init; } = null!;
    public string Street { get; private init; } = null!;
    public string StreetNumber { get; private init; } = null!;

    /// <summary>Google place id. Unlike coordinates, Google allows storing it indefinitely.</summary>
    public string? PlaceId { get; private init; }

    public double? Latitude { get; private init; }
    public double? Longitude { get; private init; }

    /// <summary>
    /// When the place (and so the coordinates) was last fetched from Google. Their terms allow caching
    /// coordinates for at most 30 days, so these are re-fetched before then.
    /// </summary>
    public DateTime? VerifiedAtUtc { get; private init; }

    public bool IsVerified => PlaceId is not null;

    /// <summary>The same address with freshly fetched coordinates (keeps the stored text).</summary>
    public Location Refreshed(double latitude, double longitude, DateTime verifiedAtUtc)
    {
        if (PlaceId is null) throw new InvalidOperationException("Only verified locations can be refreshed.");
        return Verified(Country, City, ZipCode, Street, StreetNumber, PlaceId, latitude, longitude, verifiedAtUtc);
    }

    /// <summary>Same postal address, ignoring case/whitespace and whether it was verified.</summary>
    public bool HasSameAddressAs(string country, string city, string zipCode, string street, string streetNumber)
        => Same(Country, country) && Same(City, city) && Same(ZipCode, zipCode)
           && Same(Street, street) && Same(StreetNumber, streetNumber);

    private static bool Same(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
