using System.ComponentModel.DataAnnotations;

namespace WRMS.Web.Models.Licenses;

public class LicenseFormViewModel
{
    public Guid WeaponId { get; set; }
    public string WeaponRegistrationId { get; set; } = string.Empty;
    public string WeaponDescription { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;

    [Display(Name = "License Number")]
    public string LicenseNumber { get; set; } = string.Empty;

    [Display(Name = "Issue Date")]
    [DataType(DataType.Date)]
    public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;

    [Display(Name = "Expiry Date")]
    [DataType(DataType.Date)]
    public DateTime? ExpiryDate { get; set; }

    [Display(Name = "Issued By")]
    public string? IssuedBy { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}

public class RenewLicenseViewModel
{
    public Guid LicenseId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public DateTime CurrentExpiryDate { get; set; }

    [Display(Name = "New Expiry Date")]
    [DataType(DataType.Date)]
    public DateTime? NewExpiryDate { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}
