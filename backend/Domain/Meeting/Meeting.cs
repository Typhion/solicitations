using Domain.Core;

namespace Domain.Meeting;

public class Meeting : Entity
{
    public DateTime DateTime { get; private set; }
    public bool IsOnline { get; private set; }
    public string OnlineTool { get; private set; } = null!;
    public Solicitation.Solicitation Solicitation { get; private set; } = null!;
    public MeetingType Type { get; private set; }
}