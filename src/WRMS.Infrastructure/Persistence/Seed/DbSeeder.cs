using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WRMS.Domain.Entities;
using WRMS.Infrastructure.Identity;

namespace WRMS.Infrastructure.Persistence.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        ILogger logger)
    {
        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager, configuration, logger);
        await SeedWeaponCategoriesAsync(context);
        await SeedSystemSettingsAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roleDescriptions = new Dictionary<string, string>
        {
            [Roles.Admin] = "Full system access: user management, settings, and all registry operations.",
            [Roles.Officer] = "Reviews and approves registrations, licenses, and ownership transfers.",
            [Roles.AuthorizedStaff] = "Data entry and record management without approval authority."
        };

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName, roleDescriptions[roleName]));
            }
        }
    }

    private static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var adminEmail = configuration["SeedAdmin:Email"] ?? "admin@wrms.local";

        if (await userManager.FindByEmailAsync(adminEmail) is not null)
        {
            return;
        }

        var configuredPassword = configuration["SeedAdmin:Password"];
        var password = string.IsNullOrWhiteSpace(configuredPassword)
            ? GenerateRandomPassword()
            : configuredPassword;

        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = "System Administrator",
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to seed default admin user: {Errors}", errors);
            return;
        }

        await userManager.AddToRoleAsync(adminUser, Roles.Admin);

        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            logger.LogWarning(
                "==================================================================\n" +
                "A default administrator account was created because no SeedAdmin:Password was configured.\n" +
                "  Email:    {Email}\n" +
                "  Password: {Password}\n" +
                "Sign in and change this password immediately, or set SeedAdmin:Password via user-secrets/environment variables before first run in any shared environment.\n" +
                "==================================================================",
                adminEmail, password);
        }
    }

    private static string GenerateRandomPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*";
        const string all = upper + lower + digits + special;

        Span<char> passwordChars = stackalloc char[16];
        passwordChars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        passwordChars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        passwordChars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        passwordChars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        for (var i = 4; i < passwordChars.Length; i++)
        {
            passwordChars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        // Shuffle so the guaranteed character classes aren't always in the first four positions.
        for (var i = passwordChars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (passwordChars[i], passwordChars[j]) = (passwordChars[j], passwordChars[i]);
        }

        return new string(passwordChars);
    }

    private static async Task SeedWeaponCategoriesAsync(ApplicationDbContext context)
    {
        if (context.WeaponCategories.Any())
        {
            return;
        }

        var categories = new[]
        {
            "Handgun / Pistol",
            "Revolver",
            "Rifle",
            "Shotgun",
            "Sub-Machine Gun",
            "Carbine",
            "Other / Specialized"
        };

        foreach (var name in categories)
        {
            context.WeaponCategories.Add(new WeaponCategory { Name = name, IsActive = true });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedSystemSettingsAsync(ApplicationDbContext context)
    {
        if (context.SystemSettings.Any())
        {
            return;
        }

        var defaults = new (string Key, string Value, string Description)[]
        {
            ("Agency.Name", "National Firearms Registry Authority", "Displayed on reports and printed documents."),
            ("License.DefaultValidityMonths", "24", "Default license validity period in months when issuing a new license."),
            ("License.ExpiryAlertDaysBefore", "30", "How many days before expiry to raise a license expiry alert."),
            ("Registration.RequireDocumentVerificationBeforeApproval", "true", "Officers must mark documents verified before approving a registration.")
        };

        foreach (var (key, value, description) in defaults)
        {
            context.SystemSettings.Add(new SystemSetting { Key = key, Value = value, Description = description });
        }

        await context.SaveChangesAsync();
    }
}
