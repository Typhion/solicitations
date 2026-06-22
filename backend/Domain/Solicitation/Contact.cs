using Domain.Kernel;

namespace Domain.Solicitation;

public class Contact : Entity
{
    public string Name { get; private set; }
    public string PhoneNumber { get; private set; }
    public string Email { get; private set; }
}