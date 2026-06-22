using Domain.Kernel;

namespace Domain.Solicitation;

public class Location : Entity
{
    public string Country { get; private set; }
    public string City { get; private set; }
    public string ZipCode { get; private set; }
    public string Street { get; private set; }
    public string StreetNumber { get; private set; }
}