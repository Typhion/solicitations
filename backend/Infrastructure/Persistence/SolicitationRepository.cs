using Application.Meetings;
using Application.Solicitations;
using Domain.Solicitation;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal sealed class SolicitationRepository(SolicitationsDbContext context) : ISolicitationRepository
{
    public async Task<IReadOnlyList<Solicitation>> ListAsync(Guid ownerId, int skip, int take, CancellationToken ct)
        => await context.Solicitations.AsNoTracking()
            .Include(s => s.Meetings)
            .Where(s => s.OwnerId == ownerId)
            .OrderBy(s => s.Id)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    // Meeting has no navigation back to its solicitation, so flatten from the aggregate root.
    public async Task<IReadOnlyList<CalendarMeetingDto>> ListMeetingsAsync(
        Guid ownerId, DateTime fromUtc, DateTime toUtc, Guid? solicitationId, CancellationToken ct)
        => await context.Solicitations.AsNoTracking()
            .Where(s => s.OwnerId == ownerId && (solicitationId == null || s.Id == solicitationId))
            .SelectMany(
                // overlap, not just start, so a meeting crossing the range edge still shows
                s => s.Meetings.Where(m => m.ScheduledAtUtc < toUtc && m.EndsAtUtc > fromUtc),
                (s, m) => new { s, m })
            // order before projecting: EF can't translate members of a constructor-projected record
            .OrderBy(x => x.m.ScheduledAtUtc)
            .Select(x => new CalendarMeetingDto(
                x.m.Id, x.s.Id, x.s.JobName, x.s.Website.Name, x.s.Status,
                x.m.ScheduledAtUtc, x.m.EndsAtUtc, x.m.Type, x.m.IsOnline, x.m.OnlineTool, x.s.Location.City))
            .ToListAsync(ct);

    public Task<int> CountAsync(Guid ownerId, CancellationToken ct)
        => context.Solicitations.CountAsync(s => s.OwnerId == ownerId, ct);

    public Task<Solicitation?> GetByIdAsync(Guid id, Guid ownerId, CancellationToken ct)
        => context.Solicitations.Include(s => s.Meetings)
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId, ct);

    public void Add(Solicitation solicitation) => context.Solicitations.Add(solicitation);

    public void Remove(Solicitation solicitation) => context.Solicitations.Remove(solicitation);

    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
}