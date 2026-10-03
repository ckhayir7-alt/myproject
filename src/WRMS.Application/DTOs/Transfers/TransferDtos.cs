using WRMS.Domain.Enums;

namespace WRMS.Application.DTOs.Transfers;

public class TransferListItemDto
{
    public Guid Id { get; set; }
    public Guid WeaponId { get; set; }
    public string WeaponRegistrationId { get; set; } = string.Empty;
    public string WeaponDescription { get; set; } = string.Empty;
    public string PreviousOwnerName { get; set; } = string.Empty;
    public string NewOwnerName { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public TransferApprovalStatus ApprovalStatus { get; set; }
}

public class TransferDetailDto
{
    public Guid Id { get; set; }
    public Guid WeaponId { get; set; }
    public string WeaponRegistrationId { get; set; } = string.Empty;
    public string WeaponDescription { get; set; } = string.Empty;
    public Guid PreviousOwnerId { get; set; }
    public string PreviousOwnerName { get; set; } = string.Empty;
    public Guid NewOwnerId { get; set; }
    public string NewOwnerName { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public TransferApprovalStatus ApprovalStatus { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByName { get; set; }
    public string? RejectionReason { get; set; }
}

public class CreateTransferRequest
{
    public Guid WeaponId { get; set; }
    public Guid NewOwnerId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class DecideTransferRequest
{
    public Guid TransferId { get; set; }
    public bool Approve { get; set; }
    public string? RejectionReason { get; set; }
}
