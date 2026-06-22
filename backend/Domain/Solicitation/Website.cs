using Domain.Kernel;

namespace Domain.Solicitation;

public class Website : Entity
{
    public string Name { get; private set; }
    public string Link { get; private set; }
}