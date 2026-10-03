using Microsoft.EntityFrameworkCore;
using WRMS.Application.DTOs.Dashboard;
using WRMS.Application.Interfaces;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expirySoonThreshold = now.AddDays(30);
        var thirtyDaysAgo = now.AddDays(-30);

        return new DashboardStatsDto
        {
            TotalWeapons = await _context.Weapons.CountAsync(cancellationToken),
            ActiveLicenses = await _context.Licenses.CountAsync(l => l.Status == LicenseStatus.Active && l.ExpiryDate >= now, cancellationToken),
            ExpiredLicenses = await _context.Licenses.CountAsync(l => l.Status == LicenseStatus.Expired || (l.Status == LicenseStatus.Active && l.ExpiryDate < now), cancellationToken),
            PendingRegistrations = await _context.Weapons.CountAsync(w => w.Status == WeaponStatus.PendingApproval, cancellationToken),
            SuspendedOrRevokedWeapons = await _context.Weapons.CountAsync(w => w.Status == WeaponStatus.Suspended || w.Status == WeaponStatus.Revoked, cancellationToken),
            TotalOwners = await _context.Owners.CountAsync(cancellationToken),
            PendingTransfers = await _context.WeaponTransfers.CountAsync(t => t.ApprovalStatus == TransferApprovalStatus.Pending, cancellationToken),
            LicensesExpiringSoon = await _context.Licenses.CountAsync(l => l.Status == LicenseStatus.Active && l.ExpiryDate >= now && l.ExpiryDate <= expirySoonThreshold, cancellationToken),
            NewWeaponsLast30Days = await _context.Weapons.CountAsync(w => w.CreatedAt >= thirtyDaysAgo, cancellationToken),
            NewOwnersLast30Days = await _context.Owners.CountAsync(o => o.CreatedAt >= thirtyDaysAgo, cancellationToken),
            NewLicensesLast30Days = await _context.Licenses.CountAsync(l => l.CreatedAt >= thirtyDaysAgo, cancellationToken)
        };
    }

    public async Task<List<RecentActivityItemDto>> GetRecentActivityAsync(int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .Select(a => new RecentActivityItemDto
            {
                Timestamp = a.Timestamp,
                Action = a.Action.ToString(),
                EntityType = a.EntityType,
                Details = a.Details,
                UserName = a.UserName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NameCountDto>> GetWeaponsByCategoryAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Weapons
            .AsNoTracking()
            .GroupBy(w => w.Category.Name)
            .Select(g => new NameCountDto { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NameCountDto>> GetWeaponsByStatusAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Weapons
            .AsNoTracking()
            .GroupBy(w => w.Status)
            .Select(g => new NameCountDto { Name = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<TrendPointDto>> GetRegistrationTrendAsync(int months = 6, CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow.Date.AddMonths(-(months - 1));
        start = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var raw = await _context.Weapons
            .AsNoTracking()
            .Where(w => w.RegistrationDate >= start)
            .Select(w => new { w.RegistrationDate.Year, w.RegistrationDate.Month })
            .ToListAsync(cancellationToken);

        var grouped = raw
            .GroupBy(x => new { x.Year, x.Month })
            .ToDictionary(g => (g.Key.Year, g.Key.Month), g => g.Count());

        var points = new List<TrendPointDto>();
        for (var i = 0; i < months; i++)
        {
            var month = start.AddMonths(i);
            grouped.TryGetValue((month.Year, month.Month), out var count);
            points.Add(new TrendPointDto { Label = month.ToString("MMM yyyy"), Count = count });
        }

        return points;
    }
}
