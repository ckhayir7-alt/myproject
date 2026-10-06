using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WRMS.Domain.Common;
using WRMS.Domain.Entities;
using WRMS.Infrastructure.Identity;

namespace WRMS.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<WeaponCategory> WeaponCategories => Set<WeaponCategory>();
    public DbSet<Weapon> Weapons => Set<Weapon>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<LicenseHistory> LicenseHistories => Set<LicenseHistory>();
    public DbSet<RegistrationApproval> RegistrationApprovals => Set<RegistrationApproval>();
    public DbSet<WeaponTransfer> WeaponTransfers => Set<WeaponTransfer>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        if (Database.IsNpgsql())
        {
            // PostgreSQL's "timestamp with time zone" only accepts UTC values, but dates bound
            // from forms or built with DateTime.Today arrive as Unspecified/Local.
            configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        }
    }

    private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(
            value => value.Kind == DateTimeKind.Utc ? value
                : value.Kind == DateTimeKind.Local ? value.ToUniversalTime()
                : DateTime.SpecifyKind(value, DateTimeKind.Utc),
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
        {
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        if (Database.IsNpgsql())
        {
            // PostgreSQL folds unquoted identifiers to lower case, so the column names must be quoted.
            builder.Entity<Document>().ToTable(t => t.HasCheckConstraint(
                "CK_Documents_SingleOwnerLink",
                "(CASE WHEN \"WeaponId\" IS NOT NULL THEN 1 ELSE 0 END + " +
                "CASE WHEN \"OwnerId\" IS NOT NULL THEN 1 ELSE 0 END + " +
                "CASE WHEN \"WeaponTransferId\" IS NOT NULL THEN 1 ELSE 0 END) = 1"));
        }

        // Rename default Identity tables to a consistent, professional schema.
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditAndSoftDeleteConventions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDeleteConventions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditAndSoftDeleteConventions()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    break;
                case EntityState.Deleted when entry.Entity is SoftDeletableEntity softDeletable:
                    entry.State = EntityState.Modified;
                    softDeletable.IsDeleted = true;
                    softDeletable.DeletedAt = utcNow;
                    break;
            }
        }
    }
}
