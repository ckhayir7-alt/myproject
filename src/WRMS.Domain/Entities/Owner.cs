using WRMS.Domain.Common;
using WRMS.Domain.Enums;

namespace WRMS.Domain.Entities;

public class Owner : SoftDeletableEntity
{
    public string OwnerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Occupation { get; set; }
    public OwnerStatus Status { get; set; } = OwnerStatus.Active;
    public string? Notes { get; set; }

    public ICollection<Weapon> Weapons { get; set; } = new List<Weapon>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<WeaponTransfer> TransfersOut { get; set; } = new List<WeaponTransfer>();
    public ICollection<WeaponTransfer> TransfersIn { get; set; } = new List<WeaponTransfer>();
}
