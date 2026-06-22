namespace Infrastructure.Persistence;

internal interface IDataSeed
{
    Task SeedAsync(CancellationToken cancellationToken);
}