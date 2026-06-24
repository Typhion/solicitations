using Application.Common;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence.Seeds;

internal sealed class RolesSeed(RoleManager<IdentityRole<Guid>> roleManager) : IDataSeed
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (var role in Roles.Assignable)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
    }
}
