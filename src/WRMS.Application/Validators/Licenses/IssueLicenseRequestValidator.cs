using FluentValidation;
using WRMS.Application.DTOs.Licenses;

namespace WRMS.Application.Validators.Licenses;

public class IssueLicenseRequestValidator : AbstractValidator<IssueLicenseRequest>
{
    public IssueLicenseRequestValidator()
    {
        RuleFor(x => x.WeaponId).NotEmpty();
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.IssuedBy).MaximumLength(150);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.ExpiryDate)
            .GreaterThan(x => x.IssueDate)
            .WithMessage("Expiry date must be after the issue date.");
    }
}

public class RenewLicenseRequestValidator : AbstractValidator<RenewLicenseRequest>
{
    public RenewLicenseRequestValidator()
    {
        RuleFor(x => x.LicenseId).NotEmpty();
        RuleFor(x => x.NewExpiryDate).GreaterThan(DateTime.UtcNow.Date).WithMessage("New expiry date must be in the future.");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
