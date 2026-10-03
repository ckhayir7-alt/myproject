using System.ComponentModel.DataAnnotations;
using WRMS.Application.DTOs.Owners;
using WRMS.Application.DTOs.Weapons;

namespace WRMS.Web.Models.Weapons;

public class WeaponFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Category")]
    public Guid CategoryId { get; set; }

    [Display(Name = "Owner")]
    public Guid OwnerId { get; set; }

    [Display(Name = "Manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    [Display(Name = "Model")]
    public string Model { get; set; } = string.Empty;

    [Display(Name = "Serial Number")]
    public string SerialNumber { get; set; } = string.Empty;

    [Display(Name = "Caliber")]
    public string Caliber { get; set; } = string.Empty;

    [Display(Name = "Date of Manufacture")]
    [DataType(DataType.Date)]
    public DateTime? DateOfManufacture { get; set; }

    [Display(Name = "Registration Location")]
    public string RegistrationLocation { get; set; } = string.Empty;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public List<WeaponCategoryOptionDto> Categories { get; set; } = new();
    public List<OwnerListItemDto> Owners { get; set; } = new();

    public bool IsEdit => Id.HasValue;
}
