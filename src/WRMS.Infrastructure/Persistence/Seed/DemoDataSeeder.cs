using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WRMS.Domain.Entities;
using WRMS.Domain.Enums;

namespace WRMS.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds a small set of illustrative sample records so the application is not empty on first run
/// in a development environment. Never runs outside Development (see Program.cs) and is a no-op
/// once any owner records already exist, so it will not duplicate or overwrite real data.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await context.Owners.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        var categories = await context.WeaponCategories.ToListAsync(cancellationToken);
        if (categories.Count == 0)
        {
            return;
        }

        var owners = new[]
        {
            new Owner
            {
                OwnerCode = "OWN-000001", FullName = "Michael Anderson", NationalId = "ID-DEMO-1001",
                DateOfBirth = new DateTime(1982, 4, 12), Gender = Gender.Male, PhoneNumber = "+1-555-0101",
                Address = "12 Liberty Street, Rivertown", Occupation = "Security Consultant", Status = OwnerStatus.Active
            },
            new Owner
            {
                OwnerCode = "OWN-000002", FullName = "Sarah Chen", NationalId = "ID-DEMO-1002",
                DateOfBirth = new DateTime(1990, 9, 3), Gender = Gender.Female, PhoneNumber = "+1-555-0102",
                Address = "48 Harbor Road, Rivertown", Occupation = "Firearms Instructor", Status = OwnerStatus.Active
            },
            new Owner
            {
                OwnerCode = "OWN-000003", FullName = "David Okafor", NationalId = "ID-DEMO-1003",
                DateOfBirth = new DateTime(1975, 1, 22), Gender = Gender.Male, PhoneNumber = "+1-555-0103",
                Address = "7 Northgate Ave, Millbrook", Occupation = "Rancher", Status = OwnerStatus.Active
            }
        };
        context.Owners.AddRange(owners);
        await context.SaveChangesAsync(cancellationToken);

        var handgun = categories.FirstOrDefault(c => c.Name.Contains("Handgun")) ?? categories[0];
        var rifle = categories.FirstOrDefault(c => c.Name.Contains("Rifle")) ?? categories[0];
        var shotgun = categories.FirstOrDefault(c => c.Name.Contains("Shotgun")) ?? categories[0];

        var weapons = new[]
        {
            new Weapon
            {
                RegistrationId = "REG-DEMO01", CategoryId = handgun.Id, OwnerId = owners[0].Id,
                Manufacturer = "Smith & Wesson", Model = "M&P Shield", SerialNumber = "SW-DEMO-0001",
                Caliber = "9mm", DateOfManufacture = new DateTime(2019, 5, 1), RegistrationDate = DateTime.UtcNow.AddMonths(-10),
                RegistrationLocation = "Rivertown Regional Office", Status = WeaponStatus.Active
            },
            new Weapon
            {
                RegistrationId = "REG-DEMO02", CategoryId = rifle.Id, OwnerId = owners[1].Id,
                Manufacturer = "Ruger", Model = "10/22", SerialNumber = "RG-DEMO-0002",
                Caliber = ".22 LR", DateOfManufacture = new DateTime(2021, 2, 15), RegistrationDate = DateTime.UtcNow.AddMonths(-6),
                RegistrationLocation = "Rivertown Regional Office", Status = WeaponStatus.Active
            },
            new Weapon
            {
                RegistrationId = "REG-DEMO03", CategoryId = shotgun.Id, OwnerId = owners[2].Id,
                Manufacturer = "Mossberg", Model = "500", SerialNumber = "MB-DEMO-0003",
                Caliber = "12 gauge", DateOfManufacture = new DateTime(2017, 8, 20), RegistrationDate = DateTime.UtcNow.AddMonths(-14),
                RegistrationLocation = "Millbrook District Office", Status = WeaponStatus.Suspended,
                Notes = "Suspended pending review of a reported incident."
            },
            new Weapon
            {
                RegistrationId = "REG-DEMO04", CategoryId = handgun.Id, OwnerId = owners[1].Id,
                Manufacturer = "Glock", Model = "17", SerialNumber = "GL-DEMO-0004",
                Caliber = "9mm", DateOfManufacture = new DateTime(2023, 3, 10), RegistrationDate = DateTime.UtcNow.AddDays(-5),
                RegistrationLocation = "Rivertown Regional Office", Status = WeaponStatus.PendingApproval
            }
        };
        context.Weapons.AddRange(weapons);

        foreach (var weapon in weapons)
        {
            context.RegistrationApprovals.Add(new RegistrationApproval
            {
                Weapon = weapon,
                SubmittedAt = weapon.RegistrationDate,
                Decision = weapon.Status == WeaponStatus.PendingApproval ? ApprovalDecision.Pending : ApprovalDecision.Approved,
                ReviewedAt = weapon.Status == WeaponStatus.PendingApproval ? null : weapon.RegistrationDate.AddHours(6),
                DocumentsVerified = weapon.Status != WeaponStatus.PendingApproval
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        var activeWeapons = weapons.Where(w => w.Status is WeaponStatus.Active or WeaponStatus.Suspended).ToArray();
        var licenseNumbers = new[] { "LIC-DEMO01", "LIC-DEMO02", "LIC-DEMO03" };
        // One license expiring soon, one comfortably valid, one already past expiry (illustrates each dashboard bucket).
        var expiryDaysFromNow = new[] { 20, 500, -30 };
        var issueDaysAgo = new[] { 700, 230, 760 };

        for (var i = 0; i < activeWeapons.Length; i++)
        {
            var license = new License
            {
                WeaponId = activeWeapons[i].Id,
                LicenseNumber = licenseNumbers[i],
                IssueDate = DateTime.UtcNow.AddDays(-issueDaysAgo[i]),
                ExpiryDate = DateTime.UtcNow.AddDays(expiryDaysFromNow[i]),
                Status = LicenseStatus.Active,
                IssuedBy = "Central Licensing Bureau"
            };
            context.Licenses.Add(license);
            context.LicenseHistories.Add(new LicenseHistory
            {
                License = license,
                WeaponId = activeWeapons[i].Id,
                Action = LicenseHistoryAction.Issued,
                ActionDate = license.IssueDate,
                NewExpiryDate = license.ExpiryDate,
                PerformedByName = "System (demo data)"
            });
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded demonstration data: {OwnerCount} owners, {WeaponCount} weapons.", owners.Length, weapons.Length);
    }
}
