using WRMS.Domain.Common;
using WRMS.Domain.Enums;

namespace WRMS.Domain.Entities;

public class License : BaseEntity
{
    public Guid WeaponId { get; set; }
    public Weapon Weapon { get; set; } = null!;

    public string LicenseNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public LicenseStatus Status { get; set; } = LicenseStatus.PendingApproval;
    public string? IssuedBy { get; set; }
    public string? Notes { get; set; }

    public ICollection<LicenseHistory> History { get; set; } = new List<LicenseHistory>();

    public LicenseStatus GetEffectiveStatus(DateTime asOfUtc) =>
        Status == LicenseStatus.Active && ExpiryDate < asOfUtc ? LicenseStatus.Expired : Status;
}
