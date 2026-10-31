using Application.Places;
using Domain.Solicitation;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Places;

/// <summary>
/// Keeps verified addresses within Google's terms and verifies old ones. It runs at startup and then
/// every 12 hours, and only when a server key is configured.
/// <list type="number">
/// <item><b>Refresh</b>: Google allows caching coordinates for at most 30 days, so coordinates older than
/// <see cref="GoogleMapsSettings.RefreshAfterDays"/> are re-fetched by place id. A place that no longer
/// exists turns the address back into free text, which drops its coordinates.</item>
/// <item><b>Backfill</b>: addresses saved before verification existed are looked up <b>once</b>. An EU
/// match gets a place id and coordinates and keeps the text the user typed. A miss stays free text.
/// Either way the attempt is stamped so it isn't retried.</item>
/// </list>
/// If Google is unreachable, the run stops and resumes on the next pass.
/// </summary>
internal sealed class LocationMaintenanceService(
    IServiceScopeFactory scopes,
    IOptions<GoogleMapsSettings> options,
    TimeProvider clock,
    ILogger<LocationMaintenanceService> logger) : BackgroundService
{
    public const string BackfillAttemptedAtColumn = "LocationBackfillAttemptedAt";

    private static readonly TimeSpan Interval = TimeSpan.FromHours(12);
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(200); // be gentle with the quota
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!options.Value.IsConfigured) return;

        using var timer = new PeriodicTimer(Interval, clock);
        do
        {
            try
            {
                await RefreshStaleAsync(ct);
                await BackfillAsync(ct);
            }
            catch (PlacesUnavailableException)
            {
                logger.LogWarning("Location maintenance paused: Google Places unavailable; retrying next run");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                // Never take the API down over maintenance.
                logger.LogError(ex, "Location maintenance failed");
            }
        } while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task RefreshStaleAsync(CancellationToken ct)
    {
        var cutoff = clock.GetUtcNow().UtcDateTime.AddDays(-options.Value.RefreshAfterDays);
        int refreshed = 0, dropped = 0;

        await ForEachBatchAsync(
            db => db.Solicitations.Where(s => s.Location.PlaceId != null && s.Location.VerifiedAtUtc < cutoff),
            async (s, places, _) =>
            {
                var place = await places.GetPlaceAsync(s.Location.PlaceId!, sessionToken: null, ct);
                var l = s.Location;
                var location = place is not null
                    ? l.Refreshed(place.Latitude, place.Longitude, clock.GetUtcNow().UtcDateTime)
                    : Location.Unverified(l.Country, l.City, l.ZipCode, l.Street, l.StreetNumber);
                s.UpdateDetails(s.JobName, location, s.Website, s.Contact);
                if (place is not null) refreshed++; else dropped++;
            }, ct);

        if (refreshed + dropped > 0)
            logger.LogInformation("Location refresh: {Refreshed} refreshed, {Dropped} no longer found (now free text)",
                refreshed, dropped);
    }

    private async Task BackfillAsync(CancellationToken ct)
    {
        int verified = 0, unmatched = 0;

        await ForEachBatchAsync(
            db => db.Solicitations.Where(s => s.Location.PlaceId == null
                                              && EF.Property<DateTime?>(s, BackfillAttemptedAtColumn) == null),
            async (s, places, db) =>
            {
                var l = s.Location;
                var place = await places.FindAddressAsync(
                    new AddressQuery(l.Country, l.City, l.ZipCode, l.Street, l.StreetNumber), ct);

                if (place is not null && EuCountries.Contains(place.CountryCode))
                {
                    s.UpdateDetails(s.JobName,
                        Location.Verified(l.Country, l.City, l.ZipCode, l.Street, l.StreetNumber,
                            place.PlaceId, place.Latitude, place.Longitude, clock.GetUtcNow().UtcDateTime),
                        s.Website, s.Contact);
                    verified++;
                }
                else unmatched++;

                db.Entry(s).Property(BackfillAttemptedAtColumn).CurrentValue = clock.GetUtcNow().UtcDateTime;
            }, ct);

        if (verified + unmatched > 0)
            logger.LogInformation("Location backfill: {Verified} verified, {Unmatched} left as free text",
                verified, unmatched);
    }

    /// <summary>
    /// Processes matching rows in batches, saving after each batch. Every handled row must stop
    /// matching the query (refreshed, verified or stamped), otherwise the loop would never end.
    /// When Google fails mid-batch, the rows handled so far are saved and the exception propagates.
    /// </summary>
    private async Task ForEachBatchAsync(
        Func<SolicitationsDbContext, IQueryable<Domain.Solicitation.Solicitation>> query,
        Func<Domain.Solicitation.Solicitation, IPlacesClient, SolicitationsDbContext, Task> handle,
        CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SolicitationsDbContext>();
            var places = scope.ServiceProvider.GetRequiredService<IPlacesClient>();

            var batch = await query(db).OrderBy(s => s.Id).Take(BatchSize).ToListAsync(ct);
            if (batch.Count == 0) return;

            try
            {
                foreach (var s in batch)
                {
                    await handle(s, places, db);
                    await Task.Delay(Delay, clock, ct);
                }
            }
            finally
            {
                await db.SaveChangesAsync(CancellationToken.None);
            }
        }
    }
}
