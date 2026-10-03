using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WRMS.Domain.Entities;

namespace WRMS.Infrastructure.Persistence.Configurations;

public class LicenseHistoryConfiguration : IEntityTypeConfiguration<LicenseHistory>
{
    public void Configure(EntityTypeBuilder<LicenseHistory> builder)
    {
        builder.ToTable("LicenseHistories");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Action).HasConversion<string>().HasMaxLength(30);
        builder.Property(h => h.PerformedByName).HasMaxLength(150);
        builder.Property(h => h.Notes).HasMaxLength(1000);

        builder.HasOne(h => h.License)
            .WithMany(l => l.History)
            .HasForeignKey(h => h.LicenseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Weapon)
            .WithMany(w => w.LicenseHistories)
            .HasForeignKey(h => h.WeaponId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
