using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WRMS.Domain.Entities;

namespace WRMS.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName).IsRequired().HasMaxLength(260);
        builder.Property(d => d.StoredFileName).IsRequired().HasMaxLength(260);
        builder.Property(d => d.ContentType).IsRequired().HasMaxLength(150);
        builder.Property(d => d.Description).HasMaxLength(500);

        builder.HasOne(d => d.Weapon)
            .WithMany(w => w.Documents)
            .HasForeignKey(d => d.WeaponId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Owner)
            .WithMany(o => o.Documents)
            .HasForeignKey(d => d.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.WeaponTransfer)
            .WithMany(t => t.Documents)
            .HasForeignKey(d => d.WeaponTransferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Documents_SingleOwnerLink",
            "(CASE WHEN \"WeaponId\" IS NOT NULL THEN 1 ELSE 0 END + " +
            "CASE WHEN \"OwnerId\" IS NOT NULL THEN 1 ELSE 0 END + " +
            "CASE WHEN \"WeaponTransferId\" IS NOT NULL THEN 1 ELSE 0 END) = 1"));
    }
}
