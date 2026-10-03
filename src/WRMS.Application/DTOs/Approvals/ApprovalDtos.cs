using WRMS.Domain.Enums;

namespace WRMS.Application.DTOs.Approvals;

public class PendingApprovalListItemDto
{
    public Guid ApprovalId { get; set; }
    public Guid WeaponId { get; set; }
    public string RegistrationId { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string? SubmittedByName { get; set; }
}

public class ApprovalDocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
}

public class ApprovalReviewDto
{
    public Guid ApprovalId { get; set; }
    public Guid WeaponId { get; set; }
    public string RegistrationId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Caliber { get; set; } = string.Empty;
    public string RegistrationLocation { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerCode { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string? SubmittedByName { get; set; }
    public bool DocumentsVerified { get; set; }
    public List<ApprovalDocumentDto> Documents { get; set; } = new();
}

public class ReviewDecisionRequest
{
    public Guid ApprovalId { get; set; }
    public bool DocumentsVerified { get; set; }
    public bool Approve { get; set; }
    public string? RejectionReason { get; set; }
    public string? ReviewNotes { get; set; }
}
