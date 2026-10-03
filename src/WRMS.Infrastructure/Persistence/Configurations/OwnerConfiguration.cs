using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WRMS.Domain.Entities;

namespace WRMS.Infrastructure.Persistence.Configurations;

public class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("Owners");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OwnerCode).IsRequired().HasMaxLength(30);
        builder.HasIndex(o => o.OwnerCode).IsUnique();

        builder.Property(o => o.FullName).IsRequired().HasMaxLength(150);
        builder.Property(o => o.NationalId).IsRequired().HasMaxLength(50);
        builder.HasIndex(o => o.NationalId).IsUnique();

        builder.Property(o => o.PhoneNumber).IsRequired().HasMaxLength(30);
        builder.Property(o => o.Address).IsRequired().HasMaxLength(300);
        builder.Property(o => o.Occupation).HasMaxLength(100);
        builder.Property(o => o.Notes).HasMaxLength(1000);

        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}
