using Microsoft.EntityFrameworkCore;
using WRMS.Application.DTOs.Licenses;
using WRMS.Application.Interfaces;
using WRMS.Domain.Entities;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.Services;

public class LicenseService : ILicenseService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public LicenseService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<LicenseListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var licenses = await _context.Licenses
            .AsNoTracking()
            .Include(l => l.Weapon).ThenInclude(w => w.Owner)
            .OrderByDescending(l => l.IssueDate)
            .ToListAsync(cancellationToken);

        return licenses.Select(l => new LicenseListItemDto
        {
            Id = l.Id,
            LicenseNumber = l.LicenseNumber,
            WeaponId = l.WeaponId,
            WeaponRegistrationId = l.Weapon.RegistrationId,
            WeaponDescription = $"{l.Weapon.Manufacturer} {l.Weapon.Model}",
            OwnerName = l.Weapon.Owner.FullName,
            IssueDate = l.IssueDate,
            ExpiryDate = l.ExpiryDate,
            Status = l.GetEffectiveStatus(now),
            DaysUntilExpiry = (int)Math.Ceiling((l.ExpiryDate - now).TotalDays)
        }).ToList();
    }

    public async Task<LicenseDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var license = await _context.Licenses
            .AsNoTracking()
            .Include(l => l.Weapon).ThenInclude(w => w.Owner)
            .Include(l => l.History)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (license is null)
        {
            return null;
        }

        return new LicenseDetailDto
        {
            Id = license.Id,
            LicenseNumber = license.LicenseNumber,
            IssueDate = license.IssueDate,
            ExpiryDate = license.ExpiryDate,
            Status = license.GetEffectiveStatus(DateTime.UtcNow),
            IssuedBy = license.IssuedBy,
            Notes = license.Notes,
            WeaponId = license.WeaponId,
            WeaponRegistrationId = license.Weapon.RegistrationId,
            WeaponDescription = $"{license.Weapon.Manufacturer} {license.Weapon.Model}",
            OwnerId = license.Weapon.OwnerId,
            OwnerName = license.Weapon.Owner.FullName,
            History = license.History
                .OrderByDescending(h => h.ActionDate)
                .Select(h => new LicenseHistoryItemDto
                {
                    Action = h.Action,
                    ActionDate = h.ActionDate,
                    PreviousExpiryDate = h.PreviousExpiryDate,
                    NewExpiryDate = h.NewExpiryDate,
                    PerformedByName = h.PerformedByName,
                    Notes = h.Notes
                }).ToList()
        };
    }

    public async Task<bool> IsLicenseNumberInUseAsync(string licenseNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Licenses.AnyAsync(l => l.LicenseNumber == licenseNumber && (excludeId == null || l.Id != excludeId), cancellationToken);
    }

    public async Task<bool> WeaponHasLicenseAsync(Guid weaponId, CancellationToken cancellationToken = default)
    {
        return await _context.Licenses.AnyAsync(l => l.WeaponId == weaponId, cancellationToken);
    }

    public async Task<Guid> IssueAsync(IssueLicenseRequest request, Guid currentUserId, string? currentUserName, CancellationToken cancellationToken = default)
    {
        var weapon = await _context.Weapons.FirstOrDefaultAsync(w => w.Id == request.WeaponId, cancellationToken)
            ?? throw new KeyNotFoundException("Weapon not found.");

        if (weapon.Status != WeaponStatus.Active)
        {
            throw new InvalidOperationException("A license can only be issued for an actively registered weapon.");
        }

        if (await WeaponHasLicenseAsync(request.WeaponId, cancellationToken))
        {
            throw new InvalidOperationException("This weapon already has a license on record.");
        }

        var license = new License
        {
            WeaponId = request.WeaponId,
            LicenseNumber = request.LicenseNumber.Trim(),
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            Status = LicenseStatus.Active,
            IssuedBy = request.IssuedBy?.Trim(),
            Notes = request.Notes?.Trim(),
            CreatedById = currentUserId
        };

        _context.Licenses.Add(license);

        _context.LicenseHistories.Add(new LicenseHistory
        {
            License = license,
            WeaponId = request.WeaponId,
            Action = LicenseHistoryAction.Issued,
            ActionDate = DateTime.UtcNow,
            NewExpiryDate = request.ExpiryDate,
            PerformedByName = currentUserName,
            Notes = "License issued."
        });

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, currentUserName, AuditAction.Create, "License", license.Id.ToString(),
            $"Issued license {license.LicenseNumber} for weapon {weapon.RegistrationId}.", null, cancellationToken);

        return license.Id;
    }

    public async Task RenewAsync(RenewLicenseRequest request, Guid currentUserId, string? currentUserName, CancellationToken cancellationToken = default)
    {
        var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == request.LicenseId, cancellationToken)
            ?? throw new KeyNotFoundException("License not found.");

        var previousExpiry = license.ExpiryDate;
        license.ExpiryDate = request.NewExpiryDate;
        license.Status = LicenseStatus.Active;
        license.UpdatedById = currentUserId;

        _context.LicenseHistories.Add(new LicenseHistory
        {
            LicenseId = license.Id,
            WeaponId = license.WeaponId,
            Action = LicenseHistoryAction.Renewed,
            ActionDate = DateTime.UtcNow,
            PreviousExpiryDate = previousExpiry,
            NewExpiryDate = request.NewExpiryDate,
            PerformedByName = currentUserName,
            Notes = request.Notes
        });

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, currentUserName, AuditAction.Renew, "License", license.Id.ToString(),
            $"Renewed license {license.LicenseNumber} until {request.NewExpiryDate:yyyy-MM-dd}.", null, cancellationToken);
    }

    public async Task SetStatusAsync(Guid licenseId, LicenseStatus status, string? reason, Guid currentUserId, string? currentUserName, CancellationToken cancellationToken = default)
    {
        var license = await _context.Licenses.FirstOrDefaultAsync(l => l.Id == licenseId, cancellationToken)
            ?? throw new KeyNotFoundException("License not found.");

        var previousStatus = license.Status;
        license.Status = status;
        license.UpdatedById = currentUserId;

        var action = status switch
        {
            LicenseStatus.Suspended => LicenseHistoryAction.Suspended,
            LicenseStatus.Revoked => LicenseHistoryAction.Revoked,
            LicenseStatus.Active => LicenseHistoryAction.Reinstated,
            _ => LicenseHistoryAction.Issued
        };

        _context.LicenseHistories.Add(new LicenseHistory
        {
            LicenseId = license.Id,
            WeaponId = license.WeaponId,
            Action = action,
            ActionDate = DateTime.UtcNow,
            PerformedByName = currentUserName,
            Notes = reason
        });

        await _context.SaveChangesAsync(cancellationToken);

        var auditAction = status switch
        {
            LicenseStatus.Suspended => AuditAction.Suspend,
            LicenseStatus.Revoked => AuditAction.Revoke,
            LicenseStatus.Active => AuditAction.Reinstate,
            _ => AuditAction.Update
        };

        await _auditService.LogAsync(currentUserId, currentUserName, auditAction, "License", license.Id.ToString(),
            $"License status changed from {previousStatus} to {status}.{(string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason}")}", null, cancellationToken);
    }
}
