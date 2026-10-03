using WRMS.Domain.Common;
using WRMS.Domain.Enums;

namespace WRMS.Domain.Entities;

public class WeaponTransfer : BaseEntity
{
    public Guid WeaponId { get; set; }
    public Weapon Weapon { get; set; } = null!;

    public Guid PreviousOwnerId { get; set; }
    public Owner PreviousOwner { get; set; } = null!;

    public Guid NewOwnerId { get; set; }
    public Owner NewOwner { get; set; } = null!;

    public DateTime TransferDate { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;
    public TransferApprovalStatus ApprovalStatus { get; set; } = TransferApprovalStatus.Pending;

    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }

    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
