using System.ComponentModel.DataAnnotations;

namespace WRMS.Web.Models.Users;

public class ResetPasswordViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    [MinLength(10, ErrorMessage = "Password must be at least 10 characters.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the new password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare(nameof(NewPassword), ErrorMessage = "The confirmation password does not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
