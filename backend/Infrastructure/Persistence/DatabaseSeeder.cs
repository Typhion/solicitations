using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal sealed class DatabaseSeeder(
    SolicitationsDbContext context,
    IEnumerable<IDataSeed> seeds)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await context.Database.MigrateAsync(cancellationToken);
        foreach (var seed in seeds)
            await seed.SeedAsync(cancellationToken);
    }
}