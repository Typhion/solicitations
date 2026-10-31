namespace Application.Common;

/// <summary>Languages the app is available in (two-letter ISO 639-1 codes).</summary>
public static class Languages
{
    public const string En = "en";
    public const string Nl = "nl";
    public const string Default = En;
    public static readonly IReadOnlyList<string> All = [En, Nl];
}
