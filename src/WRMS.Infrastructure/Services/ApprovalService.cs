using Microsoft.EntityFrameworkCore;
using WRMS.Application.DTOs.Approvals;
using WRMS.Application.Interfaces;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.Services;

public class ApprovalService : IApprovalService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;

    public ApprovalService(ApplicationDbContext context, IAuditService auditService, INotificationService notificationService)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
    }

    public async Task<List<PendingApprovalListItemDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var query =
            from a in _context.RegistrationApprovals
            where a.Decision == ApprovalDecision.Pending
            join u in _context.Users on a.SubmittedById equals u.Id into submitters
            from submitter in submitters.DefaultIfEmpty()
            orderby a.SubmittedAt
            select new PendingApprovalListItemDto
            {
                ApprovalId = a.Id,
                WeaponId = a.WeaponId,
                RegistrationId = a.Weapon.RegistrationId,
                Manufacturer = a.Weapon.Manufacturer,
                Model = a.Weapon.Model,
                OwnerName = a.Weapon.Owner.FullName,
                SubmittedAt = a.SubmittedAt,
                SubmittedByName = submitter != null ? submitter.FullName : null
            };

        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<ApprovalReviewDto?> GetForReviewAsync(Guid approvalId, CancellationToken cancellationToken = default)
    {
        var approval = await _context.RegistrationApprovals
            .AsNoTracking()
            .Include(a => a.Weapon).ThenInclude(w => w.Category)
            .Include(a => a.Weapon).ThenInclude(w => w.Owner)
            .Include(a => a.Weapon).ThenInclude(w => w.Documents)
            .FirstOrDefaultAsync(a => a.Id == approvalId, cancellationToken);

        if (approval is null)
        {
            return null;
        }

        var submitter = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == approval.SubmittedById, cancellationToken);

        return new ApprovalReviewDto
        {
            ApprovalId = approval.Id,
            WeaponId = approval.WeaponId,
            RegistrationId = approval.Weapon.RegistrationId,
            CategoryName = approval.Weapon.Category.Name,
            Manufacturer = approval.Weapon.Manufacturer,
            Model = approval.Weapon.Model,
            SerialNumber = approval.Weapon.SerialNumber,
            Caliber = approval.Weapon.Caliber,
            RegistrationLocation = approval.Weapon.RegistrationLocation,
            Notes = approval.Weapon.Notes,
            OwnerId = approval.Weapon.OwnerId,
            OwnerName = approval.Weapon.Owner.FullName,
            OwnerCode = approval.Weapon.Owner.OwnerCode,
            SubmittedAt = approval.SubmittedAt,
            SubmittedByName = submitter?.FullName,
            DocumentsVerified = approval.DocumentsVerified,
            Documents = approval.Weapon.Documents.Select(d => new ApprovalDocumentDto { Id = d.Id, FileName = d.FileName }).ToList()
        };
    }

    public async Task DecideAsync(ReviewDecisionRequest request, Guid currentUserId, string? currentUserName, CancellationToken cancellationToken = default)
    {
        var approval = await _context.RegistrationApprovals
            .Include(a => a.Weapon)
            .FirstOrDefaultAsync(a => a.Id == request.ApprovalId, cancellationToken)
            ?? throw new KeyNotFoundException("Approval request not found.");

        if (approval.Decision != ApprovalDecision.Pending)
        {
            throw new InvalidOperationException("This registration has already been reviewed.");
        }

        approval.DocumentsVerified = request.DocumentsVerified;
        approval.ReviewedById = currentUserId;
        approval.ReviewedAt = DateTime.UtcNow;
        approval.ReviewNotes = request.ReviewNotes;
        approval.Decision = request.Approve ? ApprovalDecision.Approved : ApprovalDecision.Rejected;
        approval.RejectionReason = request.Approve ? null : request.RejectionReason;

        approval.Weapon.Status = request.Approve ? WeaponStatus.Active : WeaponStatus.Rejected;
        approval.Weapon.UpdatedById = currentUserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, currentUserName,
            request.Approve ? AuditAction.Approve : AuditAction.Reject,
            "Weapon", approval.WeaponId.ToString(),
            request.Approve
                ? $"Approved registration {approval.Weapon.RegistrationId}."
                : $"Rejected registration {approval.Weapon.RegistrationId}. Reason: {request.RejectionReason}",
            null, cancellationToken);

        await _notificationService.CreateAsync(
            NotificationType.RegistrationStatusUpdate,
            request.Approve ? "Registration approved" : "Registration rejected",
            request.Approve
                ? $"Weapon registration {approval.Weapon.RegistrationId} was approved and is now active."
                : $"Weapon registration {approval.Weapon.RegistrationId} was rejected. Reason: {request.RejectionReason}",
            approval.SubmittedById, "Weapon", approval.WeaponId, cancellationToken);
    }
}
