using Domain.Kernel;

namespace Domain.Solicitation;

public class Solicitation : Entity
{
    public string JobName { get; private set; }
    public Location Location { get; private set; }
    public Website Website { get; private set; }
    public Contact Contact { get; private set; }
    public SolicitationStatus Status { get; private set; }
    public ICollection<Meeting.Meeting> Meetings { get; private set; }
}