using Domain.Kernel;

namespace Domain.Meeting;

public class Meeting : Entity
{
    public DateTime DateTime { get; private set; }
    public bool IsOnline { get; private set; }
    public string OnlineTool { get; private set; }
    public Solicitation.Solicitation Solicitation { get; private set; }
    public MeetingType Type { get; private set; }
}