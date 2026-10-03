using WRMS.Domain.Enums;

namespace WRMS.Application.DTOs.Search;

public class SearchResultItemDto
{
    public Guid WeaponId { get; set; }
    public string RegistrationId { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public WeaponStatus Status { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerCode { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public LicenseStatus? LicenseStatus { get; set; }
}

public class VerificationResultDto
{
    public Guid WeaponId { get; set; }
    public string RegistrationId { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Caliber { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public WeaponStatus WeaponStatus { get; set; }
    public DateTime RegistrationDate { get; set; }

    public string OwnerName { get; set; } = string.Empty;
    public string OwnerCode { get; set; } = string.Empty;
    public string OwnerNationalId { get; set; } = string.Empty;
    public Domain.Enums.OwnerStatus OwnerStatus { get; set; }

    public string? LicenseNumber { get; set; }
    public LicenseStatus? LicenseStatus { get; set; }
    public DateTime? LicenseExpiryDate { get; set; }

    public DateTime VerifiedAtUtc { get; set; } = DateTime.UtcNow;
    public string? VerifiedByName { get; set; }
}
