namespace Domain.Core;

public class Entity
{
    public Entity()
    {
        Id = Guid.NewGuid();
    }
    
    public Guid Id { get; protected set; }
}