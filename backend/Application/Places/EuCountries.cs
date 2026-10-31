namespace Application.Places;

/// <summary>Verified addresses are limited to the EU; anything else is saved as free text.</summary>
public static class EuCountries
{
    private static readonly HashSet<string> Codes = new(StringComparer.OrdinalIgnoreCase)
    {
        "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", "DE", "GR", "HU", "IE",
        "IT", "LV", "LT", "LU", "MT", "NL", "PL", "PT", "RO", "SK", "SI", "ES", "SE",
    };

    public static bool Contains(string countryCode) => Codes.Contains(countryCode);
}
