using WRMS.Domain.Enums;

namespace WRMS.Application.DTOs.Owners;

public class OwnerListItemDto
{
    public Guid Id { get; set; }
    public string OwnerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public OwnerStatus Status { get; set; }
    public int WeaponCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OwnerDocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class OwnerWeaponSummaryDto
{
    public Guid Id { get; set; }
    public string RegistrationId { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public WeaponStatus Status { get; set; }
}

public class OwnerDetailDto
{
    public Guid Id { get; set; }
    public string OwnerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Occupation { get; set; }
    public OwnerStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<OwnerWeaponSummaryDto> Weapons { get; set; } = new();
    public List<OwnerDocumentDto> Documents { get; set; } = new();
}

public class SaveOwnerRequest
{
    public Guid? Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Occupation { get; set; }
    public string? Notes { get; set; }
}
