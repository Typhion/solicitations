using Domain.Solicitation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class SolicitationConfiguration : IEntityTypeConfiguration<Solicitation>
{
    public void Configure(EntityTypeBuilder<Solicitation> builder)
    {
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.OwnerId).IsRequired();
        builder.HasIndex(s => s.OwnerId);

        builder.Property(s => s.JobName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(50);
        
        builder.OwnsOne(s => s.Location, b =>
        {
            b.Property(p => p.Country).IsRequired().HasMaxLength(100);
            b.Property(p => p.City).IsRequired().HasMaxLength(100);
            b.Property(p => p.ZipCode).IsRequired().HasMaxLength(20);
            b.Property(p => p.Street).IsRequired().HasMaxLength(200);
            b.Property(p => p.StreetNumber).IsRequired().HasMaxLength(20);
        });
        builder.OwnsOne(s => s.Website, b =>
        {
            b.Property(p => p.Name).IsRequired().HasMaxLength(100);
            b.Property(p => p.Link).IsRequired().HasMaxLength(500);
        });
        builder.OwnsOne(s => s.Contact, b =>
        {
            b.Property(p => p.Name).IsRequired().HasMaxLength(100);
            b.Property(p => p.PhoneNumber).IsRequired().HasMaxLength(50);
            b.Property(p => p.Email).IsRequired().HasMaxLength(200);
        });
        
        builder.Navigation(s => s.Location).IsRequired();
        builder.Navigation(s => s.Website).IsRequired();
        builder.Navigation(s => s.Contact).IsRequired();
        
        builder.HasMany(s => s.Meetings)
            .WithOne()
            .HasForeignKey("SolicitationId")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Metadata.FindNavigation(nameof(Solicitation.Meetings))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}