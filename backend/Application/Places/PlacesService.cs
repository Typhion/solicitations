using Application.Common.Exceptions;

namespace Application.Places;

/// <summary>Backs the address search in the solicitation form (proxied so the server key stays secret).</summary>
public sealed class PlacesService(IPlacesClient places)
{
    public Task<IReadOnlyList<PlaceSuggestion>> AutocompleteAsync(string text, string? sessionToken, CancellationToken ct)
        => places.AutocompleteAsync(text.Trim(), sessionToken, ct);

    /// <summary>The picked suggestion's address, to fill in the form fields.</summary>
    public async Task<PlaceAddress> GetAddressAsync(string placeId, string? sessionToken, CancellationToken ct)
    {
        try
        {
            return await places.GetPlaceAsync(placeId, sessionToken, ct)
                   ?? throw new NotFoundException("Place", placeId);
        }
        catch (PlacesUnavailableException)
        {
            throw new Domain.Core.DomainException("Address lookup is unavailable right now. Try again later.");
        }
    }
}
