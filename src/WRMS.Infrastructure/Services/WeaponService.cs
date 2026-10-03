using Microsoft.EntityFrameworkCore;
using WRMS.Application.DTOs.Weapons;
using WRMS.Application.Interfaces;
using WRMS.Domain.Entities;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.Services;

public class WeaponService : IWeaponService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public WeaponService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<WeaponCategoryOptionDto>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WeaponCategories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new WeaponCategoryOptionDto { Id = c.Id, Name = c.Name })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WeaponListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Weapons
            .AsNoTracking()
            .Include(w => w.Category)
            .Include(w => w.Owner)
            .OrderByDescending(w => w.RegistrationDate)
            .Select(w => new WeaponListItemDto
            {
                Id = w.Id,
                RegistrationId = w.RegistrationId,
                CategoryName = w.Category.Name,
                Manufacturer = w.Manufacturer,
                Model = w.Model,
                SerialNumber = w.SerialNumber,
                OwnerName = w.Owner.FullName,
                OwnerId = w.OwnerId,
                Status = w.Status,
                RegistrationDate = w.RegistrationDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<WeaponDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var weapon = await _context.Weapons
            .AsNoTracking()
            .Include(w => w.Category)
            .Include(w => w.Owner)
            .Include(w => w.License)
            .Include(w => w.Documents)
            .Include(w => w.Approvals)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (weapon is null)
        {
            return null;
        }

        var latestApproval = weapon.Approvals.OrderByDescending(a => a.SubmittedAt).FirstOrDefault();

        return new WeaponDetailDto
        {
            Id = weapon.Id,
            RegistrationId = weapon.RegistrationId,
            CategoryId = weapon.CategoryId,
            CategoryName = weapon.Category.Name,
            Manufacturer = weapon.Manufacturer,
            Model = weapon.Model,
            SerialNumber = weapon.SerialNumber,
            Caliber = weapon.Caliber,
            DateOfManufacture = weapon.DateOfManufacture,
            RegistrationDate = weapon.RegistrationDate,
            Status = weapon.Status,
            RegistrationLocation = weapon.RegistrationLocation,
            Notes = weapon.Notes,
            OwnerId = weapon.OwnerId,
            OwnerName = weapon.Owner.FullName,
            OwnerCode = weapon.Owner.OwnerCode,
            License = weapon.License is null ? null : new WeaponLicenseSummaryDto
            {
                Id = weapon.License.Id,
                LicenseNumber = weapon.License.LicenseNumber,
                IssueDate = weapon.License.IssueDate,
                ExpiryDate = weapon.License.ExpiryDate,
                Status = weapon.License.Status
            },
            Documents = weapon.Documents.Select(d => new WeaponDocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                StoredFileName = d.StoredFileName,
                Description = d.Description,
                FileSizeBytes = d.FileSizeBytes,
                UploadedAt = d.UploadedAt
            }).OrderByDescending(d => d.UploadedAt).ToList(),
            LatestApprovalDecision = latestApproval?.Decision,
            LatestRejectionReason = latestApproval?.RejectionReason
        };
    }

    public async Task<bool> IsSerialNumberInUseAsync(string serialNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Weapons
            .AnyAsync(w => w.SerialNumber == serialNumber && (excludeId == null || w.Id != excludeId), cancellationToken);
    }

    public async Task<Guid> CreateAsync(SaveWeaponRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var weapon = new Weapon
        {
            RegistrationId = await GenerateRegistrationIdAsync(cancellationToken),
            CategoryId = request.CategoryId,
            OwnerId = request.OwnerId,
            Manufacturer = request.Manufacturer.Trim(),
            Model = request.Model.Trim(),
            SerialNumber = request.SerialNumber.Trim(),
            Caliber = request.Caliber.Trim(),
            DateOfManufacture = request.DateOfManufacture,
            RegistrationDate = DateTime.UtcNow,
            RegistrationLocation = request.RegistrationLocation.Trim(),
            Notes = request.Notes?.Trim(),
            Status = WeaponStatus.PendingApproval,
            CreatedById = currentUserId
        };

        _context.Weapons.Add(weapon);

        _context.RegistrationApprovals.Add(new RegistrationApproval
        {
            Weapon = weapon,
            SubmittedById = currentUserId,
            SubmittedAt = DateTime.UtcNow,
            Decision = ApprovalDecision.Pending
        });

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, null, AuditAction.Create, "Weapon", weapon.Id.ToString(),
            $"Submitted weapon registration {weapon.RegistrationId} ({weapon.Manufacturer} {weapon.Model}).", null, cancellationToken);

        return weapon.Id;
    }

    public async Task UpdateAsync(SaveWeaponRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (request.Id is null)
        {
            throw new InvalidOperationException("Weapon Id is required for update.");
        }

        var weapon = await _context.Weapons.FirstOrDefaultAsync(w => w.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Weapon not found.");

        weapon.CategoryId = request.CategoryId;
        weapon.OwnerId = request.OwnerId;
        weapon.Manufacturer = request.Manufacturer.Trim();
        weapon.Model = request.Model.Trim();
        weapon.SerialNumber = request.SerialNumber.Trim();
        weapon.Caliber = request.Caliber.Trim();
        weapon.DateOfManufacture = request.DateOfManufacture;
        weapon.RegistrationLocation = request.RegistrationLocation.Trim();
        weapon.Notes = request.Notes?.Trim();
        weapon.UpdatedById = currentUserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, null, AuditAction.Update, "Weapon", weapon.Id.ToString(),
            $"Updated weapon registration {weapon.RegistrationId}.", null, cancellationToken);
    }

    public async Task SetStatusAsync(Guid id, WeaponStatus status, string? reason, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var weapon = await _context.Weapons.FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Weapon not found.");

        var previousStatus = weapon.Status;
        weapon.Status = status;
        weapon.UpdatedById = currentUserId;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            weapon.Notes = string.IsNullOrWhiteSpace(weapon.Notes) ? reason : $"{weapon.Notes}\n[{DateTime.UtcNow:yyyy-MM-dd}] {reason}";
        }

        await _context.SaveChangesAsync(cancellationToken);

        var action = status switch
        {
            WeaponStatus.Suspended => AuditAction.Suspend,
            WeaponStatus.Revoked => AuditAction.Revoke,
            WeaponStatus.Active => AuditAction.Reinstate,
            _ => AuditAction.Update
        };

        await _auditService.LogAsync(currentUserId, null, action, "Weapon", weapon.Id.ToString(),
            $"Weapon status changed from {previousStatus} to {status}.{(string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason}")}", null, cancellationToken);
    }

    public async Task<Guid> AddDocumentAsync(Guid weaponId, string fileName, string storedFileName, long sizeBytes, string contentType, string? description, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var weaponExists = await _context.Weapons.AnyAsync(w => w.Id == weaponId, cancellationToken);
        if (!weaponExists)
        {
            throw new KeyNotFoundException("Weapon not found.");
        }

        var document = new Document
        {
            WeaponId = weaponId,
            FileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSizeBytes = sizeBytes,
            Description = description,
            UploadedById = currentUserId,
            UploadedAt = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, null, AuditAction.Create, "Document", document.Id.ToString(),
            $"Uploaded document '{fileName}' for weapon {weaponId}.", null, cancellationToken);

        return document.Id;
    }

    public async Task<WeaponDocumentDto?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Where(d => d.Id == documentId && d.WeaponId != null)
            .Select(d => new WeaponDocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                StoredFileName = d.StoredFileName,
                Description = d.Description,
                FileSizeBytes = d.FileSizeBytes,
                UploadedAt = d.UploadedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<string> GenerateRegistrationIdAsync(CancellationToken cancellationToken)
    {
        var count = await _context.Weapons.IgnoreQueryFilters().CountAsync(cancellationToken);
        return $"REG-{count + 1:D6}";
    }
}
