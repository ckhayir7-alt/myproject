using System.ComponentModel.DataAnnotations;
using WRMS.Domain.Enums;

namespace WRMS.Web.Models.Owners;

public class OwnerFormViewModel
{
    public Guid? Id { get; set; }

    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "National ID")]
    public string NationalId { get; set; } = string.Empty;

    [Display(Name = "Date of Birth")]
    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "Gender")]
    public Gender Gender { get; set; }

    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "Occupation")]
    public string? Occupation { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public bool IsEdit => Id.HasValue;
}
