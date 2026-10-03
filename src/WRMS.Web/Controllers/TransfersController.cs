using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WRMS.Application.DTOs.Transfers;
using WRMS.Application.Interfaces;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Identity;
using WRMS.Web.Extensions;
using WRMS.Web.Models.Transfers;

namespace WRMS.Web.Controllers;

[Authorize]
public class TransfersController : Controller
{
    private readonly ITransferService _transferService;
    private readonly IWeaponService _weaponService;
    private readonly IOwnerService _ownerService;
    private readonly IValidator<CreateTransferRequest> _createValidator;
    private readonly IValidator<DecideTransferRequest> _decideValidator;
    private readonly UserManager<ApplicationUser> _userManager;

    public TransfersController(
        ITransferService transferService,
        IWeaponService weaponService,
        IOwnerService ownerService,
        IValidator<CreateTransferRequest> createValidator,
        IValidator<DecideTransferRequest> decideValidator,
        UserManager<ApplicationUser> userManager)
    {
        _transferService = transferService;
        _weaponService = weaponService;
        _ownerService = ownerService;
        _createValidator = createValidator;
        _decideValidator = decideValidator;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var transfers = await _transferService.GetAllAsync(cancellationToken);
        return View(transfers);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var transfer = await _transferService.GetByIdAsync(id, cancellationToken);
        if (transfer is null)
        {
            return NotFound();
        }

        return View(transfer);
    }

    public async Task<IActionResult> Create(Guid weaponId, CancellationToken cancellationToken)
    {
        var weapon = await _weaponService.GetByIdAsync(weaponId, cancellationToken);
        if (weapon is null)
        {
            return NotFound();
        }

        if (weapon.Status != WeaponStatus.Active)
        {
            TempData["ErrorMessage"] = "Only actively registered weapons can be transferred.";
            return RedirectToAction("Details", "Weapons", new { id = weaponId });
        }

        if (await _transferService.HasPendingTransferAsync(weaponId, cancellationToken))
        {
            TempData["ErrorMessage"] = "A transfer request is already pending for this weapon.";
            return RedirectToAction("Details", "Weapons", new { id = weaponId });
        }

        var owners = await _ownerService.GetAllAsync(cancellationToken);

        return View(new TransferFormViewModel
        {
            WeaponId = weapon.Id,
            WeaponRegistrationId = weapon.RegistrationId,
            WeaponDescription = $"{weapon.Manufacturer} {weapon.Model}",
            CurrentOwnerId = weapon.OwnerId,
            CurrentOwnerName = weapon.OwnerName,
            Owners = owners.Where(o => o.Id != weapon.OwnerId).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransferFormViewModel form, CancellationToken cancellationToken)
    {
        var request = new CreateTransferRequest
        {
            WeaponId = form.WeaponId,
            NewOwnerId = form.NewOwnerId,
            Reason = form.Reason,
            Notes = form.Notes
        };

        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            ModelState.AddValidationResult(validation);
        }

        if (!ModelState.IsValid)
        {
            var weapon = await _weaponService.GetByIdAsync(form.WeaponId, cancellationToken);
            form.WeaponRegistrationId = weapon?.RegistrationId ?? string.Empty;
            form.WeaponDescription = weapon is null ? string.Empty : $"{weapon.Manufacturer} {weapon.Model}";
            form.CurrentOwnerName = weapon?.OwnerName ?? string.Empty;
            form.Owners = (await _ownerService.GetAllAsync(cancellationToken)).Where(o => o.Id != form.CurrentOwnerId).ToList();
            return View(form);
        }

        try
        {
            var id = await _transferService.CreateAsync(request, GetCurrentUserId(), cancellationToken);
            TempData["SuccessMessage"] = "Transfer request submitted and is pending approval.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Details", "Weapons", new { id = form.WeaponId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireOfficerOrAdmin")]
    public async Task<IActionResult> Decide(DecideTransferRequest request, CancellationToken cancellationToken)
    {
        var validation = await _decideValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id = request.TransferId });
        }

        try
        {
            await _transferService.DecideAsync(request, GetCurrentUserId(), User.Identity?.Name, cancellationToken);
            TempData["SuccessMessage"] = request.Approve ? "Transfer approved." : "Transfer rejected.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = request.TransferId });
    }

    private Guid GetCurrentUserId() => Guid.Parse(_userManager.GetUserId(User)!);
}
