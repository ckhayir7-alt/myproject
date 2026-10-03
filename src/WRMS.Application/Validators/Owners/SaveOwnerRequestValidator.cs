using FluentValidation;
using WRMS.Application.DTOs.Owners;

namespace WRMS.Application.Validators.Owners;

public class SaveOwnerRequestValidator : AbstractValidator<SaveOwnerRequest>
{
    public SaveOwnerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.NationalId).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Occupation).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .Must(dob => dob <= DateTime.UtcNow.AddYears(-18)).WithMessage("Owner must be at least 18 years old.")
            .Must(dob => dob >= DateTime.UtcNow.AddYears(-120)).WithMessage("Enter a valid date of birth.");
    }
}
