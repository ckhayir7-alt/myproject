using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

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
}
