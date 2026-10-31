using Application.Places;
using Asp.Versioning.Builder;
using Infrastructure.Places;
using Microsoft.Extensions.Options;

namespace WebApi.Api;

public static class PlacesEndpoints
{
    public sealed record AutocompleteQuery(string? Text, string? SessionToken);
    public sealed record PlaceQuery(string PlaceId, string? SessionToken);

    /// <summary>What the SPA needs to draw Google maps. The browser key is referrer-restricted.</summary>
    public sealed record MapsConfig(string? BrowserApiKey, string MapId);

    public static IEndpointRouteBuilder MapPlacesEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        // Proxied so the server key (Places API) never reaches the browser.
        var places = app.MapGroup("/api/places")
            .RequireAuthorization("CanManageSolicitations")
            .RequireRateLimiting("places")
            .WithApiVersionSet(versionSet)
            .WithTags("Places");

        // GET /api/places/autocomplete?text=&sessionToken= — company/address suggestions.
        places.MapGet("/autocomplete", async ([AsParameters] AutocompleteQuery query, PlacesService service, CancellationToken ct) =>
                Results.Ok(await service.AutocompleteAsync(query.Text!, query.SessionToken, ct)))
            .AddEndpointFilter<ValidationFilter<AutocompleteQuery>>();

        // GET /api/places/{placeId}?sessionToken= — the picked suggestion's address, to fill the form.
        places.MapGet("/{placeId}", async ([AsParameters] PlaceQuery query, PlacesService service, CancellationToken ct) =>
                Results.Ok(await service.GetAddressAsync(query.PlaceId, query.SessionToken, ct)))
            .AddEndpointFilter<ValidationFilter<PlaceQuery>>();

        // GET /api/maps/config — browser key + map id for the Maps JavaScript API.
        app.MapGet("/api/maps/config", (IOptions<GoogleMapsSettings> options) =>
                Results.Ok(new MapsConfig(options.Value.BrowserApiKey, options.Value.MapId)))
            .RequireAuthorization("CanManageSolicitations")
            .WithApiVersionSet(versionSet)
            .WithTags("Places");

        return app;
    }
}
