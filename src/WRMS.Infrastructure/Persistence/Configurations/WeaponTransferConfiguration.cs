using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WRMS.Domain.Entities;

namespace WRMS.Infrastructure.Persistence.Configurations;

public class WeaponTransferConfiguration : IEntityTypeConfiguration<WeaponTransfer>
{
    public void Configure(EntityTypeBuilder<WeaponTransfer> builder)
    {
        builder.ToTable("WeaponTransfers");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Reason).IsRequired().HasMaxLength(500);
        builder.Property(t => t.ApprovalStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.RejectionReason).HasMaxLength(1000);
        builder.Property(t => t.Notes).HasMaxLength(1000);

        builder.HasOne(t => t.Weapon)
            .WithMany(w => w.Transfers)
            .HasForeignKey(t => t.WeaponId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.PreviousOwner)
            .WithMany(o => o.TransfersOut)
            .HasForeignKey(t => t.PreviousOwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.NewOwner)
            .WithMany(o => o.TransfersIn)
            .HasForeignKey(t => t.NewOwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
