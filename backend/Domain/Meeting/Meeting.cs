using Domain.Core;

namespace Domain.Meeting;

public class Meeting : Entity
{
    private Meeting() { }

    public Meeting(DateTime scheduledAtUtc, DateTime endsAtUtc, MeetingType type, bool isOnline, string? onlineTool)
    {
        Update(scheduledAtUtc, endsAtUtc, type, isOnline, onlineTool);
    }

    public DateTime ScheduledAtUtc { get; private set; }
    public DateTime EndsAtUtc { get; private set; }
    public MeetingType Type { get; private set; }
    public bool IsOnline { get; private set; }
    public string? OnlineTool { get; private set; }

    public void Update(DateTime scheduledAtUtc, DateTime endsAtUtc, MeetingType type, bool isOnline, string? onlineTool)
    {
        if (endsAtUtc <= scheduledAtUtc)
            throw new DomainException("A meeting must end after it starts.");
        if (isOnline) ArgumentException.ThrowIfNullOrWhiteSpace(onlineTool);

        ScheduledAtUtc = scheduledAtUtc;
        EndsAtUtc = endsAtUtc;
        Type = type;
        IsOnline = isOnline;
        OnlineTool = isOnline ? onlineTool : null;
    }
}
