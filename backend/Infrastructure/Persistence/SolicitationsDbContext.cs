using Domain.Solicitation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class SolicitationsDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public SolicitationsDbContext(DbContextOptions<SolicitationsDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Solicitation> Solicitations => Set<Solicitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }
}