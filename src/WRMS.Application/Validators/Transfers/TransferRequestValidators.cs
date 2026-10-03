using FluentValidation;
using WRMS.Application.DTOs.Transfers;

namespace WRMS.Application.Validators.Transfers;

public class CreateTransferRequestValidator : AbstractValidator<CreateTransferRequest>
{
    public CreateTransferRequestValidator()
    {
        RuleFor(x => x.WeaponId).NotEmpty();
        RuleFor(x => x.NewOwnerId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class DecideTransferRequestValidator : AbstractValidator<DecideTransferRequest>
{
    public DecideTransferRequestValidator()
    {
        RuleFor(x => x.TransferId).NotEmpty();
        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .WithMessage("A rejection reason is required when rejecting a transfer.")
            .When(x => !x.Approve);
        RuleFor(x => x.RejectionReason).MaximumLength(1000);
    }
}
