using FluentValidation;
using WRMS.Application.DTOs.Weapons;

namespace WRMS.Application.Validators.Weapons;

public class SaveWeaponRequestValidator : AbstractValidator<SaveWeaponRequest>
{
    public SaveWeaponRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Select a weapon category.");
        RuleFor(x => x.OwnerId).NotEmpty().WithMessage("Select an owner.");
        RuleFor(x => x.Manufacturer).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(150);
        RuleFor(x => x.SerialNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Caliber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RegistrationLocation).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.DateOfManufacture)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Date of manufacture cannot be in the future.")
            .When(x => x.DateOfManufacture.HasValue);
    }
}
