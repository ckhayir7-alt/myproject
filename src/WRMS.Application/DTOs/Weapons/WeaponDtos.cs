using WRMS.Domain.Enums;

namespace WRMS.Application.DTOs.Weapons;

public class WeaponCategoryOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class WeaponListItemDto
{
    public Guid Id { get; set; }
    public string RegistrationId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public WeaponStatus Status { get; set; }
    public DateTime RegistrationDate { get; set; }
}

public class WeaponDocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class WeaponLicenseSummaryDto
{
    public Guid Id { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public LicenseStatus Status { get; set; }
}

public class WeaponDetailDto
{
    public Guid Id { get; set; }
    public string RegistrationId { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Caliber { get; set; } = string.Empty;
    public DateTime? DateOfManufacture { get; set; }
    public DateTime RegistrationDate { get; set; }
    public WeaponStatus Status { get; set; }
    public string RegistrationLocation { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerCode { get; set; } = string.Empty;

    public WeaponLicenseSummaryDto? License { get; set; }
    public List<WeaponDocumentDto> Documents { get; set; } = new();

    public ApprovalDecision? LatestApprovalDecision { get; set; }
    public string? LatestRejectionReason { get; set; }
}

public class SaveWeaponRequest
{
    public Guid? Id { get; set; }
    public Guid CategoryId { get; set; }
    public Guid OwnerId { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Caliber { get; set; } = string.Empty;
    public DateTime? DateOfManufacture { get; set; }
    public string RegistrationLocation { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
