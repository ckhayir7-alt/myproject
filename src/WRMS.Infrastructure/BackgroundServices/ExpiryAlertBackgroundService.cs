using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WRMS.Application.Interfaces;
using WRMS.Domain.Entities;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Identity;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.BackgroundServices;

public class ExpiryAlertBackgroundService : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpiryAlertBackgroundService> _logger;

    public ExpiryAlertBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ExpiryAlertBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expiry alert background pass failed.");
            }

            try
            {
                await Task.Delay(RunInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Shutting down.
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var recipientIds = await GetAlertRecipientIdsAsync(context, cancellationToken);
        if (recipientIds.Count == 0)
        {
            return;
        }

        await ExpireOverdueLicensesAsync(context, notificationService, recipientIds, cancellationToken);
        await NotifyLicensesExpiringSoonAsync(context, notificationService, recipientIds, cancellationToken);
        await NotifyPendingApprovalsAsync(context, notificationService, recipientIds, cancellationToken);
    }

    private static async Task<List<Guid>> GetAlertRecipientIdsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var adminAndOfficerRoleIds = await context.Roles
            .Where(r => r.Name == Roles.Admin || r.Name == Roles.Officer)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        return await context.UserRoles
            .Where(ur => adminAndOfficerRoleIds.Contains(ur.RoleId))
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private static async Task ExpireOverdueLicensesAsync(
        ApplicationDbContext context, INotificationService notificationService, List<Guid> recipientIds, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var overdue = await context.Licenses
            .Include(l => l.Weapon)
            .Where(l => l.Status == LicenseStatus.Active && l.ExpiryDate < now)
            .ToListAsync(cancellationToken);

        foreach (var license in overdue)
        {
            license.Status = LicenseStatus.Expired;

            context.LicenseHistories.Add(new LicenseHistory
            {
                LicenseId = license.Id,
                WeaponId = license.WeaponId,
                Action = LicenseHistoryAction.Expired,
                ActionDate = now,
                PerformedByName = "System",
                Notes = "Automatically marked as expired."
            });

            foreach (var userId in recipientIds)
            {
                await notificationService.CreateAsync(
                    NotificationType.LicenseExpiry,
                    "License expired",
                    $"License {license.LicenseNumber} for weapon {license.Weapon.RegistrationId} has expired.",
                    userId, "License", license.Id, cancellationToken);
            }
        }

        if (overdue.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task NotifyLicensesExpiringSoonAsync(
        ApplicationDbContext context, INotificationService notificationService, List<Guid> recipientIds, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var threshold = now.AddDays(30);

        var expiringSoon = await context.Licenses
            .Include(l => l.Weapon)
            .Where(l => l.Status == LicenseStatus.Active && l.ExpiryDate >= now && l.ExpiryDate <= threshold)
            .ToListAsync(cancellationToken);

        foreach (var license in expiringSoon)
        {
            if (await notificationService.ExistsForRelatedEntityAsync(NotificationType.LicenseExpiry, "License", license.Id, cancellationToken))
            {
                continue;
            }

            var daysLeft = (int)Math.Ceiling((license.ExpiryDate - now).TotalDays);
            foreach (var userId in recipientIds)
            {
                await notificationService.CreateAsync(
                    NotificationType.LicenseExpiry,
                    "License expiring soon",
                    $"License {license.LicenseNumber} for weapon {license.Weapon.RegistrationId} expires in {daysLeft} day(s).",
                    userId, "License", license.Id, cancellationToken);
            }
        }
    }

    private static async Task NotifyPendingApprovalsAsync(
        ApplicationDbContext context, INotificationService notificationService, List<Guid> recipientIds, CancellationToken cancellationToken)
    {
        var staleThreshold = DateTime.UtcNow.AddHours(-24);

        var stalePending = await context.RegistrationApprovals
            .Include(a => a.Weapon)
            .Where(a => a.Decision == ApprovalDecision.Pending && a.SubmittedAt <= staleThreshold)
            .ToListAsync(cancellationToken);

        foreach (var approval in stalePending)
        {
            if (await notificationService.ExistsForRelatedEntityAsync(NotificationType.PendingApproval, "RegistrationApproval", approval.Id, cancellationToken))
            {
                continue;
            }

            foreach (var userId in recipientIds)
            {
                await notificationService.CreateAsync(
                    NotificationType.PendingApproval,
                    "Registration awaiting review",
                    $"Weapon registration {approval.Weapon.RegistrationId} has been pending review for over 24 hours.",
                    userId, "RegistrationApproval", approval.Id, cancellationToken);
            }
        }
    }
}
