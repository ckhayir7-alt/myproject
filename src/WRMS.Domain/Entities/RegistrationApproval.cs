using WRMS.Domain.Common;
using WRMS.Domain.Enums;

namespace WRMS.Domain.Entities;

public class RegistrationApproval : BaseEntity
{
    public Guid WeaponId { get; set; }
    public Weapon Weapon { get; set; } = null!;

    public Guid SubmittedById { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public Guid? ReviewedById { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public bool DocumentsVerified { get; set; }
    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Pending;
    public string? RejectionReason { get; set; }
    public string? ReviewNotes { get; set; }
}
