using Domain.Core;

namespace Domain.Solicitation;

public class Solicitation : Entity
{
    private Solicitation() { }
    
    public Solicitation(string jobName, Location location, Website website, Contact contact)
    {
        UpdateDetails(jobName, location, website, contact);
        Status = SolicitationStatus.Draft;   // invariant: new solicitations start as Draft
    }
    
    public string JobName { get; private set; } = null!;
    public Location Location { get; private set; } = null!;
    public Website Website { get; private set; } = null!;
    public Contact Contact { get; private set; } = null!;
    public SolicitationStatus Status { get; private set; }
    // public ICollection<Meeting.Meeting> Meetings { get; private set; }
    
    public void UpdateDetails(string jobName, Location location, Website website, Contact contact)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(website);
        ArgumentNullException.ThrowIfNull(contact);

        JobName = jobName;
        Location = location;
        Website = website;
        Contact = contact;
    }

    public void ChangeStatus(SolicitationStatus status) => Status = status;
}