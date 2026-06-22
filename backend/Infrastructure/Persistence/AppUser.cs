using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence;

public class AppUser : IdentityUser<Guid>
{
    public AppUser()
    {
    }
}