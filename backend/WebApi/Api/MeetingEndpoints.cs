using Application.Meetings;
using Asp.Versioning.Builder;

namespace WebApi.Api;

public static class MeetingEndpoints
{
    public static IEndpointRouteBuilder MapMeetingEndpoints(this IEndpointRouteBuilder app, ApiVersionSet versionSet)
    {
        var group = app.MapGroup("/api/meetings")
            .RequireAuthorization("CanManageSolicitations")
            .WithApiVersionSet(versionSet)
            .WithTags("Meetings");

        // GET /api/meetings?from=&to=&solicitationId= — all of the user's meetings in [from, to), for the calendar.
        group.MapGet("/", async ([AsParameters] MeetingCalendarQuery query, MeetingCalendarService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(query, ct)))
            .AddEndpointFilter<ValidationFilter<MeetingCalendarQuery>>();

        return app;
    }
}
