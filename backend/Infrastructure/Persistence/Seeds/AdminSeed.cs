using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Persistence.Seeds;

internal sealed class AdminSeed(
    UserManager<AppUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager) : IDataSeed
{
    private const string Role = "Admin";
    private const string Username = "admin";
    private const string Password = "admin";

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!await roleManager.RoleExistsAsync(Role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(Role));

        if (await userManager.FindByNameAsync(Username) is not null)
            return;

        var admin = new AppUser { UserName = Username, Email = "admin@local" };
        var result = await userManager.CreateAsync(admin, Password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Failed to seed admin: " + string.Join(", ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(admin, Role);
    }
}