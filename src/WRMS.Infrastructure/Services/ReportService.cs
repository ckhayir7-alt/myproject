using Microsoft.EntityFrameworkCore;
using WRMS.Application.DTOs.Reports;
using WRMS.Application.Interfaces;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReportTableDto> GetReportAsync(ReportType type, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default)
    {
        return type switch
        {
            ReportType.WeaponsRegistered => await BuildWeaponsRegisteredAsync(dateFrom, dateTo, cancellationToken),
            ReportType.LicensesActiveExpired => await BuildLicensesAsync(cancellationToken),
            ReportType.RegistrationActivity => await BuildRegistrationActivityAsync(dateFrom, dateTo, cancellationToken),
            ReportType.OwnershipTransfers => await BuildTransfersAsync(dateFrom, dateTo, cancellationToken),
            ReportType.SuspendedRevoked => await BuildSuspendedRevokedAsync(cancellationToken),
            ReportType.ByLocation => await BuildByLocationAsync(cancellationToken),
            ReportType.ByCategory => await BuildByCategoryAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    private async Task<ReportTableDto> BuildWeaponsRegisteredAsync(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var query = _context.Weapons.AsNoTracking().Include(w => w.Category).Include(w => w.Owner).AsQueryable();
        if (from.HasValue) query = query.Where(w => w.RegistrationDate >= from.Value);
        if (to.HasValue) query = query.Where(w => w.RegistrationDate < to.Value.Date.AddDays(1));

        var weapons = await query.OrderByDescending(w => w.RegistrationDate).ToListAsync(ct);

        return new ReportTableDto
        {
            Title = "Registered Weapons Report",
            Columns = new() { "Registration ID", "Category", "Manufacturer", "Model", "Serial Number", "Owner", "Location", "Status", "Registered" },
            Rows = weapons.Select(w => new List<string>
            {
                w.RegistrationId, w.Category.Name, w.Manufacturer, w.Model, w.SerialNumber,
                w.Owner.FullName, w.RegistrationLocation, w.Status.ToString(), w.RegistrationDate.ToString("yyyy-MM-dd")
            }).ToList()
        };
    }

    private async Task<ReportTableDto> BuildLicensesAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var licenses = await _context.Licenses.AsNoTracking().Include(l => l.Weapon).ThenInclude(w => w.Owner).ToListAsync(ct);

        return new ReportTableDto
        {
            Title = "Active and Expired Licenses Report",
            Columns = new() { "License Number", "Weapon", "Owner", "Issue Date", "Expiry Date", "Status" },
            Rows = licenses
                .Where(l => l.GetEffectiveStatus(now) is LicenseStatus.Active or LicenseStatus.Expired)
                .OrderBy(l => l.ExpiryDate)
                .Select(l => new List<string>
                {
                    l.LicenseNumber, $"{l.Weapon.RegistrationId} ({l.Weapon.Manufacturer} {l.Weapon.Model})",
                    l.Weapon.Owner.FullName, l.IssueDate.ToString("yyyy-MM-dd"), l.ExpiryDate.ToString("yyyy-MM-dd"),
                    l.GetEffectiveStatus(now).ToString()
                }).ToList()
        };
    }

    private async Task<ReportTableDto> BuildRegistrationActivityAsync(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var query = _context.AuditLogs.AsNoTracking().Where(a => a.EntityType == "Weapon");
        if (from.HasValue) query = query.Where(a => a.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Timestamp < to.Value.Date.AddDays(1));

        var logs = await query.OrderByDescending(a => a.Timestamp).Take(1000).ToListAsync(ct);

        return new ReportTableDto
        {
            Title = "Registration Activity Report",
            Columns = new() { "Timestamp", "Action", "User", "Details" },
            Rows = logs.Select(a => new List<string>
            {
                a.Timestamp.ToString("yyyy-MM-dd HH:mm"), a.Action.ToString(), a.UserName ?? "-", a.Details ?? "-"
            }).ToList()
        };
    }

    private async Task<ReportTableDto> BuildTransfersAsync(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var query = _context.WeaponTransfers.AsNoTracking()
            .Include(t => t.Weapon).Include(t => t.PreviousOwner).Include(t => t.NewOwner).AsQueryable();
        if (from.HasValue) query = query.Where(t => t.TransferDate >= from.Value);
        if (to.HasValue) query = query.Where(t => t.TransferDate < to.Value.Date.AddDays(1));

        var transfers = await query.OrderByDescending(t => t.TransferDate).ToListAsync(ct);

        return new ReportTableDto
        {
            Title = "Ownership Transfer Report",
            Columns = new() { "Weapon", "Previous Owner", "New Owner", "Date", "Status", "Reason" },
            Rows = transfers.Select(t => new List<string>
            {
                t.Weapon.RegistrationId, t.PreviousOwner.FullName, t.NewOwner.FullName,
                t.TransferDate.ToString("yyyy-MM-dd"), t.ApprovalStatus.ToString(), t.Reason
            }).ToList()
        };
    }

    private async Task<ReportTableDto> BuildSuspendedRevokedAsync(CancellationToken ct)
    {
        var weapons = await _context.Weapons.AsNoTracking()
            .Include(w => w.Category).Include(w => w.Owner)
            .Where(w => w.Status == WeaponStatus.Suspended || w.Status == WeaponStatus.Revoked)
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync(ct);

        return new ReportTableDto
        {
            Title = "Suspended and Revoked Registrations Report",
            Columns = new() { "Registration ID", "Category", "Manufacturer", "Model", "Owner", "Status", "Notes" },
            Rows = weapons.Select(w => new List<string>
            {
                w.RegistrationId, w.Category.Name, w.Manufacturer, w.Model, w.Owner.FullName, w.Status.ToString(), w.Notes ?? "-"
            }).ToList()
        };
    }

    private async Task<ReportTableDto> BuildByLocationAsync(CancellationToken ct)
    {
        var grouped = await _context.Weapons.AsNoTracking()
            .GroupBy(w => w.RegistrationLocation)
            .Select(g => new { Location = g.Key, Total = g.Count(), Active = g.Count(w => w.Status == WeaponStatus.Active) })
            .OrderByDescending(g => g.Total)
            .ToListAsync(ct);

        return new ReportTableDto
        {
            Title = "Registrations by Location",
            Columns = new() { "Location", "Total Weapons", "Active" },
            Rows = grouped.Select(g => new List<string> { g.Location, g.Total.ToString(), g.Active.ToString() }).ToList()
        };
    }

    private async Task<ReportTableDto> BuildByCategoryAsync(CancellationToken ct)
    {
        var grouped = await _context.Weapons.AsNoTracking()
            .Include(w => w.Category)
            .GroupBy(w => w.Category.Name)
            .Select(g => new { Category = g.Key, Total = g.Count(), Active = g.Count(w => w.Status == WeaponStatus.Active) })
            .OrderByDescending(g => g.Total)
            .ToListAsync(ct);

        return new ReportTableDto
        {
            Title = "Registrations by Weapon Category",
            Columns = new() { "Category", "Total Weapons", "Active" },
            Rows = grouped.Select(g => new List<string> { g.Category, g.Total.ToString(), g.Active.ToString() }).ToList()
        };
    }
}
