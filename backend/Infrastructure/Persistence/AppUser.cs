using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence;

public class AppUser : IdentityUser<Guid>
{
    public AppUser()
    {
    }

    /// <summary>Preferred UI language (see <c>Application.Common.Languages</c>); null = not chosen yet.</summary>
    public string? Language { get; set; }
}