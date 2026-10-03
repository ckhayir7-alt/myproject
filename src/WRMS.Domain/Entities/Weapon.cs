using WRMS.Domain.Common;
using WRMS.Domain.Enums;

namespace WRMS.Domain.Entities;

public class Weapon : SoftDeletableEntity
{
    public string RegistrationId { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public WeaponCategory Category { get; set; } = null!;

    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Caliber { get; set; } = string.Empty;
    public DateTime? DateOfManufacture { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public WeaponStatus Status { get; set; } = WeaponStatus.PendingApproval;
    public string RegistrationLocation { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public Guid OwnerId { get; set; }
    public Owner Owner { get; set; } = null!;

    public License? License { get; set; }
    public ICollection<LicenseHistory> LicenseHistories { get; set; } = new List<LicenseHistory>();
    public ICollection<RegistrationApproval> Approvals { get; set; } = new List<RegistrationApproval>();
    public ICollection<WeaponTransfer> Transfers { get; set; } = new List<WeaponTransfer>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
