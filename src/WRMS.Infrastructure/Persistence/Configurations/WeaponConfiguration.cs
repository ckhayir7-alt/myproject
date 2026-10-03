using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WRMS.Domain.Entities;

namespace WRMS.Infrastructure.Persistence.Configurations;

public class WeaponConfiguration : IEntityTypeConfiguration<Weapon>
{
    public void Configure(EntityTypeBuilder<Weapon> builder)
    {
        builder.ToTable("Weapons");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.RegistrationId).IsRequired().HasMaxLength(30);
        builder.HasIndex(w => w.RegistrationId).IsUnique();

        builder.Property(w => w.Manufacturer).IsRequired().HasMaxLength(150);
        builder.Property(w => w.Model).IsRequired().HasMaxLength(150);
        builder.Property(w => w.SerialNumber).IsRequired().HasMaxLength(100);
        builder.HasIndex(w => w.SerialNumber).IsUnique();

        builder.Property(w => w.Caliber).IsRequired().HasMaxLength(50);
        builder.Property(w => w.RegistrationLocation).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Notes).HasMaxLength(1000);
        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(w => w.Category)
            .WithMany(c => c.Weapons)
            .HasForeignKey(w => w.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Owner)
            .WithMany(o => o.Weapons)
            .HasForeignKey(w => w.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.License)
            .WithOne(l => l.Weapon)
            .HasForeignKey<License>(l => l.WeaponId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(w => !w.IsDeleted);
    }
}
