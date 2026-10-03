using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WRMS.Application.DTOs.Approvals;
using WRMS.Application.Interfaces;
using WRMS.Infrastructure.Identity;
using WRMS.Web.Extensions;

namespace WRMS.Web.Controllers;

[Authorize(Policy = "RequireOfficerOrAdmin")]
public class ApprovalsController : Controller
{
    private readonly IApprovalService _approvalService;
    private readonly IValidator<ReviewDecisionRequest> _validator;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApprovalsController(
        IApprovalService approvalService,
        IValidator<ReviewDecisionRequest> validator,
        UserManager<ApplicationUser> userManager)
    {
        _approvalService = approvalService;
        _validator = validator;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var pending = await _approvalService.GetPendingAsync(cancellationToken);
        return View(pending);
    }

    public async Task<IActionResult> Review(Guid id, CancellationToken cancellationToken)
    {
        var review = await _approvalService.GetForReviewAsync(id, cancellationToken);
        if (review is null)
        {
            return NotFound();
        }

        return View(review);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(ReviewDecisionRequest request, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var review = await _approvalService.GetForReviewAsync(request.ApprovalId, cancellationToken);
            if (review is null)
            {
                return NotFound();
            }
            ModelState.AddValidationResult(validation);
            return View(nameof(Review), review);
        }

        try
        {
            await _approvalService.DecideAsync(request, GetCurrentUserId(), User.Identity?.Name, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = request.Approve ? "Registration approved." : "Registration rejected.";
        return RedirectToAction(nameof(Index));
    }

    private Guid GetCurrentUserId() => Guid.Parse(_userManager.GetUserId(User)!);
}
