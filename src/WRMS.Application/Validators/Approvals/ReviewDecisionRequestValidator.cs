using FluentValidation;
using WRMS.Application.DTOs.Approvals;

namespace WRMS.Application.Validators.Approvals;

public class ReviewDecisionRequestValidator : AbstractValidator<ReviewDecisionRequest>
{
    public ReviewDecisionRequestValidator()
    {
        RuleFor(x => x.ApprovalId).NotEmpty();

        RuleFor(x => x.DocumentsVerified)
            .Equal(true)
            .WithMessage("Documents must be verified before a decision can be recorded.");

        RuleFor(x => x.RejectionReason)
            .NotEmpty()
            .WithMessage("A rejection reason is required when rejecting a registration.")
            .When(x => !x.Approve);

        RuleFor(x => x.RejectionReason).MaximumLength(1000);
        RuleFor(x => x.ReviewNotes).MaximumLength(1000);
    }
}
