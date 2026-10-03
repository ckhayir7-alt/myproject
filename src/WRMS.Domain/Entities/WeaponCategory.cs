using WRMS.Domain.Common;

namespace WRMS.Domain.Entities;

public class WeaponCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Weapon> Weapons { get; set; } = new List<Weapon>();
}
