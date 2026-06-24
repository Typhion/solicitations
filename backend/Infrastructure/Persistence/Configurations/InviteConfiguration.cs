using Domain.Invites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class InviteConfiguration : IEntityTypeConfiguration<Invite>
{
    public void Configure(EntityTypeBuilder<Invite> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(i => i.TokenHash).IsUnique();

        builder.Property(i => i.Role).IsRequired().HasMaxLength(50);
        builder.Property(i => i.Email).HasMaxLength(200);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
    }
}