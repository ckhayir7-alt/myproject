using Microsoft.EntityFrameworkCore;
using WRMS.Application.DTOs.Transfers;
using WRMS.Application.Interfaces;
using WRMS.Domain.Entities;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.Services;

public class TransferService : ITransferService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public TransferService(ApplicationDbContext context, IAuditService auditService, INotificationService notificationService)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public async Task<List<TransferListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WeaponTransfers
            .AsNoTracking()
            .Include(t => t.Weapon)
            .Include(t => t.PreviousOwner)
            .Include(t => t.NewOwner)
            .OrderByDescending(t => t.TransferDate)
            .Select(t => new TransferListItemDto
            {
                Id = t.Id,
                WeaponId = t.WeaponId,
                WeaponRegistrationId = t.Weapon.RegistrationId,
                WeaponDescription = $"{t.Weapon.Manufacturer} {t.Weapon.Model}",
                PreviousOwnerName = t.PreviousOwner.FullName,
                NewOwnerName = t.NewOwner.FullName,
                TransferDate = t.TransferDate,
                ApprovalStatus = t.ApprovalStatus
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<TransferDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var transfer = await _context.WeaponTransfers
            .AsNoTracking()
            .Include(t => t.Weapon)
            .Include(t => t.PreviousOwner)
            .Include(t => t.NewOwner)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (transfer is null)
        {
            return null;
        }

        string? approvedByName = null;
        if (transfer.ApprovedById.HasValue)
        {
            var approver = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == transfer.ApprovedById, cancellationToken);
            approvedByName = approver?.FullName;
        }

        return new TransferDetailDto
        {
            Id = transfer.Id,
            WeaponId = transfer.WeaponId,
            WeaponRegistrationId = transfer.Weapon.RegistrationId,
            WeaponDescription = $"{transfer.Weapon.Manufacturer} {transfer.Weapon.Model}",
            PreviousOwnerId = transfer.PreviousOwnerId,
            PreviousOwnerName = transfer.PreviousOwner.FullName,
            NewOwnerId = transfer.NewOwnerId,
            NewOwnerName = transfer.NewOwner.FullName,
            TransferDate = transfer.TransferDate,
            Reason = transfer.Reason,
            Notes = transfer.Notes,
            ApprovalStatus = transfer.ApprovalStatus,
            ApprovedAt = transfer.ApprovedAt,
            ApprovedByName = approvedByName,
            RejectionReason = transfer.RejectionReason
        };
    }

    public async Task<bool> HasPendingTransferAsync(Guid weaponId, CancellationToken cancellationToken = default)
    {
        return await _context.WeaponTransfers.AnyAsync(
            t => t.WeaponId == weaponId && t.ApprovalStatus == TransferApprovalStatus.Pending, cancellationToken);
    }

    public async Task<Guid> CreateAsync(CreateTransferRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var weapon = await _context.Weapons.FirstOrDefaultAsync(w => w.Id == request.WeaponId, cancellationToken)
            ?? throw new KeyNotFoundException("Weapon not found.");

        if (weapon.Status != WeaponStatus.Active)
        {
            throw new InvalidOperationException("Only actively registered weapons can be transferred.");
        }

        if (weapon.OwnerId == request.NewOwnerId)
        {
            throw new InvalidOperationException("The new owner must be different from the current owner.");
        }

        if (await HasPendingTransferAsync(request.WeaponId, cancellationToken))
        {
            throw new InvalidOperationException("A transfer request is already pending for this weapon.");
        }

        var transfer = new WeaponTransfer
        {
            WeaponId = request.WeaponId,
            PreviousOwnerId = weapon.OwnerId,
            NewOwnerId = request.NewOwnerId,
            TransferDate = DateTime.UtcNow,
            Reason = request.Reason.Trim(),
            Notes = request.Notes?.Trim(),
            ApprovalStatus = TransferApprovalStatus.Pending,
            CreatedById = currentUserId
        };

        _context.WeaponTransfers.Add(transfer);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, null, AuditAction.Transfer, "WeaponTransfer", transfer.Id.ToString(),
            $"Requested ownership transfer of weapon {weapon.RegistrationId}.", null, cancellationToken);

        return transfer.Id;
    }

    public async Task DecideAsync(DecideTransferRequest request, Guid currentUserId, string? currentUserName, CancellationToken cancellationToken = default)
    {
        var transfer = await _context.WeaponTransfers
            .Include(t => t.Weapon)
            .FirstOrDefaultAsync(t => t.Id == request.TransferId, cancellationToken)
            ?? throw new KeyNotFoundException("Transfer request not found.");

        if (transfer.ApprovalStatus != TransferApprovalStatus.Pending)
        {
            throw new InvalidOperationException("This transfer request has already been reviewed.");
        }

        transfer.ApprovalStatus = request.Approve ? TransferApprovalStatus.Approved : TransferApprovalStatus.Rejected;
        transfer.ApprovedById = currentUserId;
        transfer.ApprovedAt = DateTime.UtcNow;
        transfer.RejectionReason = request.Approve ? null : request.RejectionReason;

        if (request.Approve)
        {
            transfer.Weapon.OwnerId = transfer.NewOwnerId;
            transfer.Weapon.UpdatedById = currentUserId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, currentUserName,
            request.Approve ? AuditAction.Approve : AuditAction.Reject,
            "WeaponTransfer", transfer.Id.ToString(),
            request.Approve
                ? $"Approved ownership transfer of weapon {transfer.Weapon.RegistrationId}."
                : $"Rejected ownership transfer of weapon {transfer.Weapon.RegistrationId}. Reason: {request.RejectionReason}",
            null, cancellationToken);

        if (transfer.CreatedById.HasValue)
        {
            await _notificationService.CreateAsync(
                NotificationType.RegistrationStatusUpdate,
                request.Approve ? "Ownership transfer approved" : "Ownership transfer rejected",
                request.Approve
                    ? $"Ownership transfer of weapon {transfer.Weapon.RegistrationId} was approved."
                    : $"Ownership transfer of weapon {transfer.Weapon.RegistrationId} was rejected. Reason: {request.RejectionReason}",
                transfer.CreatedById, "WeaponTransfer", transfer.Id, cancellationToken);
        }
    }
}
