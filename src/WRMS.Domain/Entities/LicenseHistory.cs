using WRMS.Domain.Common;
using WRMS.Domain.Enums;

namespace WRMS.Domain.Entities;

public class LicenseHistory : BaseEntity
{
    public Guid LicenseId { get; set; }
    public License License { get; set; } = null!;

    public Guid WeaponId { get; set; }
    public Weapon Weapon { get; set; } = null!;

    public LicenseHistoryAction Action { get; set; }
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    public DateTime? PreviousExpiryDate { get; set; }
    public DateTime? NewExpiryDate { get; set; }
    public string? PerformedByName { get; set; }
    public string? Notes { get; set; }
}
