using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WRMS.Domain.Entities;

namespace WRMS.Infrastructure.Persistence.Configurations;

public class RegistrationApprovalConfiguration : IEntityTypeConfiguration<RegistrationApproval>
{
    public void Configure(EntityTypeBuilder<RegistrationApproval> builder)
    {
        builder.ToTable("RegistrationApprovals");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Decision).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.RejectionReason).HasMaxLength(1000);
        builder.Property(a => a.ReviewNotes).HasMaxLength(1000);

        builder.HasOne(a => a.Weapon)
            .WithMany(w => w.Approvals)
            .HasForeignKey(a => a.WeaponId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
