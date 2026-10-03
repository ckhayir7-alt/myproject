using System.ComponentModel.DataAnnotations;
using WRMS.Application.DTOs.Owners;

namespace WRMS.Web.Models.Transfers;

public class TransferFormViewModel
{
    public Guid WeaponId { get; set; }
    public string WeaponRegistrationId { get; set; } = string.Empty;
    public string WeaponDescription { get; set; } = string.Empty;
    public Guid CurrentOwnerId { get; set; }
    public string CurrentOwnerName { get; set; } = string.Empty;

    [Display(Name = "New Owner")]
    public Guid NewOwnerId { get; set; }

    [Display(Name = "Transfer Reason")]
    public string Reason { get; set; } = string.Empty;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    public List<OwnerListItemDto> Owners { get; set; } = new();
}
