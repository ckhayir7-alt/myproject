using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using WRMS.Application.Interfaces;
using WRMS.Infrastructure.BackgroundServices;
using WRMS.Infrastructure.Identity;
using WRMS.Infrastructure.Persistence;
using WRMS.Infrastructure.Services;

namespace WRMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            // Each provider has its own migration history, kept in a separate assembly.
            if (UsesPostgres(configuration, connectionString))
            {
                options.UseNpgsql(NormalizePostgresConnectionString(connectionString), npgsql =>
                    npgsql.MigrationsAssembly("WRMS.Migrations.PostgreSql"));
            }
            else
            {
                options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("WRMS.Migrations.SqlServer"));
            }
        });

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationClaimsPrincipalFactory>();

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IPdfExportService, PdfExportService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IOwnerService, OwnerService>();
        services.AddScoped<IWeaponService, WeaponService>();
        services.AddScoped<ILicenseService, LicenseService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<ITransferService, TransferService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddHostedService<ExpiryAlertBackgroundService>();

        return services;
    }

    /// <summary>
    /// Chooses the database provider. An explicit <c>Database:Provider</c> setting
    /// ("SqlServer" or "PostgreSql") wins; otherwise the connection string decides.
    /// </summary>
    private static bool UsesPostgres(IConfiguration configuration, string connectionString)
    {
        var provider = configuration["Database:Provider"];
        if (!string.IsNullOrWhiteSpace(provider))
        {
            if (provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase) ||
                provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            throw new InvalidOperationException(
                $"Database:Provider '{provider}' is not supported. Use 'SqlServer' or 'PostgreSql'.");
        }

        var trimmed = connectionString.TrimStart();
        if (trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Keywords that Npgsql understands but SQL Server does not.
        var keywords = trimmed
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2)[0].Trim());
        return keywords.Any(keyword =>
            keyword.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
            keyword.Equals("Port", StringComparison.OrdinalIgnoreCase) ||
            keyword.Equals("Username", StringComparison.OrdinalIgnoreCase) ||
            keyword.Equals("SSL Mode", StringComparison.OrdinalIgnoreCase) ||
            keyword.Equals("SslMode", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePostgresConnectionString(string connectionString)
    {
        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return connectionString;
        }

        var credentials = uri.UserInfo.Split(':', 2);
        var database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));
        if (credentials.Length != 2 || string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' must include a PostgreSQL username, password, and database.");
        }

        var requireChannelBinding = uri.Query
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Any(parameter => parameter.Equals("channel_binding=require", StringComparison.OrdinalIgnoreCase));

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = database,
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            SslMode = SslMode.Require,
            ChannelBinding = requireChannelBinding ? ChannelBinding.Require : ChannelBinding.Prefer
        }.ConnectionString;
    }
}
