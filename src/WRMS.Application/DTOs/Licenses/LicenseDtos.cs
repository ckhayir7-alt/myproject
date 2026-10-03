using WRMS.Domain.Enums;

namespace WRMS.Application.DTOs.Licenses;

public class LicenseListItemDto
{
    public Guid Id { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public Guid WeaponId { get; set; }
    public string WeaponRegistrationId { get; set; } = string.Empty;
    public string WeaponDescription { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public LicenseStatus Status { get; set; }
    public int DaysUntilExpiry { get; set; }
}

public class LicenseHistoryItemDto
{
    public LicenseHistoryAction Action { get; set; }
    public DateTime ActionDate { get; set; }
    public DateTime? PreviousExpiryDate { get; set; }
    public DateTime? NewExpiryDate { get; set; }
    public string? PerformedByName { get; set; }
    public string? Notes { get; set; }
}

public class LicenseDetailDto
{
    public Guid Id { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public LicenseStatus Status { get; set; }
    public string? IssuedBy { get; set; }
    public string? Notes { get; set; }

    public Guid WeaponId { get; set; }
    public string WeaponRegistrationId { get; set; } = string.Empty;
    public string WeaponDescription { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;

    public List<LicenseHistoryItemDto> History { get; set; } = new();
}

public class IssueLicenseRequest
{
    public Guid WeaponId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string? IssuedBy { get; set; }
    public string? Notes { get; set; }
}

public class RenewLicenseRequest
{
    public Guid LicenseId { get; set; }
    public DateTime NewExpiryDate { get; set; }
    public string? Notes { get; set; }
}
