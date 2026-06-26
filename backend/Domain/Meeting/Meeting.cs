using Domain.Core;

namespace Domain.Meeting;

public class Meeting : Entity
{
    private Meeting() { }

    public Meeting(DateTime scheduledAtUtc, MeetingType type, bool isOnline, string? onlineTool)
    {
        if (isOnline) ArgumentException.ThrowIfNullOrWhiteSpace(onlineTool);

        ScheduledAtUtc = scheduledAtUtc;
        Type = type;
        IsOnline = isOnline;
        OnlineTool = isOnline ? onlineTool : null;
    }

    public DateTime ScheduledAtUtc { get; private set; }
    public MeetingType Type { get; private set; }
    public bool IsOnline { get; private set; }
    public string? OnlineTool { get; private set; }
}
