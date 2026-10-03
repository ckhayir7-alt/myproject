using Microsoft.EntityFrameworkCore;
using WRMS.Application.DTOs.Owners;
using WRMS.Application.Interfaces;
using WRMS.Domain.Entities;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.Services;

public class OwnerService : IOwnerService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public OwnerService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<OwnerListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Owners
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OwnerListItemDto
            {
                Id = o.Id,
                OwnerCode = o.OwnerCode,
                FullName = o.FullName,
                NationalId = o.NationalId,
                PhoneNumber = o.PhoneNumber,
                Status = o.Status,
                WeaponCount = o.Weapons.Count(w => !w.IsDeleted),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<OwnerDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var owner = await _context.Owners
            .AsNoTracking()
            .Include(o => o.Weapons)
            .Include(o => o.Documents)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (owner is null)
        {
            return null;
        }

        return new OwnerDetailDto
        {
            Id = owner.Id,
            OwnerCode = owner.OwnerCode,
            FullName = owner.FullName,
            NationalId = owner.NationalId,
            DateOfBirth = owner.DateOfBirth,
            Gender = owner.Gender,
            PhoneNumber = owner.PhoneNumber,
            Address = owner.Address,
            Occupation = owner.Occupation,
            Status = owner.Status,
            Notes = owner.Notes,
            CreatedAt = owner.CreatedAt,
            UpdatedAt = owner.UpdatedAt,
            Weapons = owner.Weapons.Where(w => !w.IsDeleted).Select(w => new OwnerWeaponSummaryDto
            {
                Id = w.Id,
                RegistrationId = w.RegistrationId,
                Manufacturer = w.Manufacturer,
                Model = w.Model,
                Status = w.Status
            }).ToList(),
            Documents = owner.Documents.Select(d => new OwnerDocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                StoredFileName = d.StoredFileName,
                Description = d.Description,
                FileSizeBytes = d.FileSizeBytes,
                UploadedAt = d.UploadedAt
            }).OrderByDescending(d => d.UploadedAt).ToList()
        };
    }

    public async Task<bool> IsNationalIdInUseAsync(string nationalId, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Owners
            .AnyAsync(o => o.NationalId == nationalId && (excludeId == null || o.Id != excludeId), cancellationToken);
    }

    public async Task<Guid> CreateAsync(SaveOwnerRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var owner = new Owner
        {
            OwnerCode = await GenerateOwnerCodeAsync(cancellationToken),
            FullName = request.FullName.Trim(),
            NationalId = request.NationalId.Trim(),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            PhoneNumber = request.PhoneNumber.Trim(),
            Address = request.Address.Trim(),
            Occupation = request.Occupation?.Trim(),
            Notes = request.Notes?.Trim(),
            Status = OwnerStatus.Active,
            CreatedById = currentUserId
        };

        _context.Owners.Add(owner);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, null, AuditAction.Create, "Owner", owner.Id.ToString(),
            $"Registered owner {owner.FullName} ({owner.OwnerCode}).", null, cancellationToken);

        return owner.Id;
    }

    public async Task UpdateAsync(SaveOwnerRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (request.Id is null)
        {
            throw new InvalidOperationException("Owner Id is required for update.");
        }

        var owner = await _context.Owners.FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Owner not found.");

        owner.FullName = request.FullName.Trim();
        owner.NationalId = request.NationalId.Trim();
        owner.DateOfBirth = request.DateOfBirth;
        owner.Gender = request.Gender;
        owner.PhoneNumber = request.PhoneNumber.Trim();
        owner.Address = request.Address.Trim();
        owner.Occupation = request.Occupation?.Trim();
        owner.Notes = request.Notes?.Trim();
        owner.UpdatedById = currentUserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, null, AuditAction.Update, "Owner", owner.Id.ToString(),
            $"Updated owner {owner.FullName} ({owner.OwnerCode}).", null, cancellationToken);
    }

    public async Task SetStatusAsync(Guid id, OwnerStatus status, string? reason, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var owner = await _context.Owners.FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Owner not found.");

        var previousStatus = owner.Status;
        owner.Status = status;
        owner.UpdatedById = currentUserId;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            owner.Notes = string.IsNullOrWhiteSpace(owner.Notes) ? reason : $"{owner.Notes}\n[{DateTime.UtcNow:yyyy-MM-dd}] {reason}";
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, null, AuditAction.Update, "Owner", owner.Id.ToString(),
            $"Owner status changed from {previousStatus} to {status}.{(string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason}")}", null, cancellationToken);
    }

    public async Task<Guid> AddDocumentAsync(Guid ownerId, string fileName, string storedFileName, long sizeBytes, string contentType, string? description, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var ownerExists = await _context.Owners.AnyAsync(o => o.Id == ownerId, cancellationToken);
        if (!ownerExists)
        {
            throw new KeyNotFoundException("Owner not found.");
        }

        var document = new Document
        {
            OwnerId = ownerId,
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
            $"Uploaded document '{fileName}' for owner {ownerId}.", null, cancellationToken);

        return document.Id;
    }

    public async Task<OwnerDocumentDto?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Where(d => d.Id == documentId && d.OwnerId != null)
            .Select(d => new OwnerDocumentDto
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

    private async Task<string> GenerateOwnerCodeAsync(CancellationToken cancellationToken)
    {
        var count = await _context.Owners.CountAsync(cancellationToken);
        return $"OWN-{count + 1:D6}";
    }
}
