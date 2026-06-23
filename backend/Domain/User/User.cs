using Domain.Core;

namespace Domain.User;

public class User : Entity
{
    public User()
    {
        Username = default!;
        Email = default!;
    }

    public User(Guid id, string username, string email) : this(username, email)
    {
        Id = id;
    }

    public User(string username, string email)
    {
        Username = username;
        Email = email;
    }

    public string Username { get; private set; }
    public string Email { get; private set; }
}