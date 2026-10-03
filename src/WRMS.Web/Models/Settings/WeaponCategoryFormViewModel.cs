using System.ComponentModel.DataAnnotations;

namespace WRMS.Web.Models.Settings;

public class WeaponCategoryFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}
