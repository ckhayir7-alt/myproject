using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WRMS.Application.DTOs.Licenses;
using WRMS.Application.Interfaces;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Identity;
using WRMS.Web.Extensions;
using WRMS.Web.Models.Licenses;

namespace WRMS.Web.Controllers;

[Authorize]
public class LicensesController : Controller
{
    private readonly ILicenseService _licenseService;
    private readonly IWeaponService _weaponService;
    private readonly IValidator<IssueLicenseRequest> _issueValidator;
    private readonly IValidator<RenewLicenseRequest> _renewValidator;
    private readonly UserManager<ApplicationUser> _userManager;

    public LicensesController(
        ILicenseService licenseService,
        IWeaponService weaponService,
        IValidator<IssueLicenseRequest> issueValidator,
        IValidator<RenewLicenseRequest> renewValidator,
        UserManager<ApplicationUser> userManager)
    {
        _licenseService = licenseService;
        _weaponService = weaponService;
        _issueValidator = issueValidator;
        _renewValidator = renewValidator;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var licenses = await _licenseService.GetAllAsync(cancellationToken);
        return View(licenses);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var license = await _licenseService.GetByIdAsync(id, cancellationToken);
        if (license is null)
        {
            return NotFound();
        }

        return View(license);
    }

    [Authorize(Policy = "RequireOfficerOrAdmin")]
    public async Task<IActionResult> Create(Guid weaponId, CancellationToken cancellationToken)
    {
        var weapon = await _weaponService.GetByIdAsync(weaponId, cancellationToken);
        if (weapon is null)
        {
            return NotFound();
        }

        if (weapon.Status != WeaponStatus.Active)
        {
            TempData["ErrorMessage"] = "A license can only be issued for an actively registered weapon.";
            return RedirectToAction("Details", "Weapons", new { id = weaponId });
        }

        if (await _licenseService.WeaponHasLicenseAsync(weaponId, cancellationToken))
        {
            TempData["ErrorMessage"] = "This weapon already has a license on record.";
            return RedirectToAction("Details", "Weapons", new { id = weaponId });
        }

        return View(new LicenseFormViewModel
        {
            WeaponId = weapon.Id,
            WeaponRegistrationId = weapon.RegistrationId,
            WeaponDescription = $"{weapon.Manufacturer} {weapon.Model}",
            OwnerName = weapon.OwnerName
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireOfficerOrAdmin")]
    public async Task<IActionResult> Create(LicenseFormViewModel form, CancellationToken cancellationToken)
    {
        var request = new IssueLicenseRequest
        {
            WeaponId = form.WeaponId,
            LicenseNumber = form.LicenseNumber,
            IssueDate = form.IssueDate,
            ExpiryDate = form.ExpiryDate ?? default,
            IssuedBy = form.IssuedBy,
            Notes = form.Notes
        };

        var validation = await _issueValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            ModelState.AddValidationResult(validation);
        }

        if (await _licenseService.IsLicenseNumberInUseAsync(request.LicenseNumber, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.LicenseNumber), "This license number is already in use.");
        }

        if (!ModelState.IsValid)
        {
            var weapon = await _weaponService.GetByIdAsync(form.WeaponId, cancellationToken);
            form.WeaponRegistrationId = weapon?.RegistrationId ?? string.Empty;
            form.WeaponDescription = weapon is null ? string.Empty : $"{weapon.Manufacturer} {weapon.Model}";
            form.OwnerName = weapon?.OwnerName ?? string.Empty;
            return View(form);
        }

        try
        {
            var id = await _licenseService.IssueAsync(request, GetCurrentUserId(), User.Identity?.Name, cancellationToken);
            TempData["SuccessMessage"] = "License issued successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction("Details", "Weapons", new { id = form.WeaponId });
        }
    }

    [Authorize(Policy = "RequireOfficerOrAdmin")]
    public async Task<IActionResult> Renew(Guid id, CancellationToken cancellationToken)
    {
        var license = await _licenseService.GetByIdAsync(id, cancellationToken);
        if (license is null)
        {
            return NotFound();
        }

        return View(new RenewLicenseViewModel
        {
            LicenseId = license.Id,
            LicenseNumber = license.LicenseNumber,
            CurrentExpiryDate = license.ExpiryDate
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireOfficerOrAdmin")]
    public async Task<IActionResult> Renew(RenewLicenseViewModel form, CancellationToken cancellationToken)
    {
        var request = new RenewLicenseRequest
        {
            LicenseId = form.LicenseId,
            NewExpiryDate = form.NewExpiryDate ?? default,
            Notes = form.Notes
        };

        var validation = await _renewValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            ModelState.AddValidationResult(validation);
            return View(form);
        }

        await _licenseService.RenewAsync(request, GetCurrentUserId(), User.Identity?.Name, cancellationToken);
        TempData["SuccessMessage"] = "License renewed successfully.";
        return RedirectToAction(nameof(Details), new { id = form.LicenseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireOfficerOrAdmin")]
    public async Task<IActionResult> SetStatus(Guid id, LicenseStatus status, string? reason, CancellationToken cancellationToken)
    {
        await _licenseService.SetStatusAsync(id, status, reason, GetCurrentUserId(), User.Identity?.Name, cancellationToken);
        TempData["SuccessMessage"] = $"License status updated to {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private Guid GetCurrentUserId() => Guid.Parse(_userManager.GetUserId(User)!);
}
