using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WRMS.Domain.Entities;

namespace WRMS.Infrastructure.Persistence.Configurations;

public class WeaponCategoryConfiguration : IEntityTypeConfiguration<WeaponCategory>
{
    public void Configure(EntityTypeBuilder<WeaponCategory> builder)
    {
        builder.ToTable("WeaponCategories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => c.Name).IsUnique();
        builder.Property(c => c.Description).HasMaxLength(500);
    }
}
