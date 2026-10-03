using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WRMS.Domain.Entities;

namespace WRMS.Infrastructure.Persistence.Configurations;

public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> builder)
    {
        builder.ToTable("Licenses");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.LicenseNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(l => l.LicenseNumber).IsUnique();
        builder.Property(l => l.IssuedBy).HasMaxLength(150);
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(30);
    }
}
