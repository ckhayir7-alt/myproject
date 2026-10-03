using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WRMS.Application.DTOs.Weapons;
using WRMS.Application.Interfaces;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Identity;
using WRMS.Web.Extensions;
using WRMS.Web.Models.Weapons;

namespace WRMS.Web.Controllers;

[Authorize]
public class WeaponsController : Controller
{
    private readonly IWeaponService _weaponService;
    private readonly IOwnerService _ownerService;
    private readonly IFileStorageService _fileStorage;
    private readonly IValidator<SaveWeaponRequest> _validator;
    private readonly UserManager<ApplicationUser> _userManager;

    public WeaponsController(
        IWeaponService weaponService,
        IOwnerService ownerService,
        IFileStorageService fileStorage,
        IValidator<SaveWeaponRequest> validator,
        UserManager<ApplicationUser> userManager)
    {
        _weaponService = weaponService;
        _ownerService = ownerService;
        _fileStorage = fileStorage;
        _validator = validator;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var weapons = await _weaponService.GetAllAsync(cancellationToken);
        return View(weapons);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var weapon = await _weaponService.GetByIdAsync(id, cancellationToken);
        if (weapon is null)
        {
            return NotFound();
        }

        return View(weapon);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new WeaponFormViewModel
        {
            Categories = await _weaponService.GetActiveCategoriesAsync(cancellationToken),
            Owners = await _ownerService.GetAllAsync(cancellationToken)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WeaponFormViewModel form, CancellationToken cancellationToken)
    {
        var request = MapToRequest(form);
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            ModelState.AddValidationResult(validation);
        }

        if (await _weaponService.IsSerialNumberInUseAsync(request.SerialNumber, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.SerialNumber), "A weapon with this serial number is already registered.");
        }

        if (!ModelState.IsValid)
        {
            form.Categories = await _weaponService.GetActiveCategoriesAsync(cancellationToken);
            form.Owners = await _ownerService.GetAllAsync(cancellationToken);
            return View(form);
        }

        var id = await _weaponService.CreateAsync(request, GetCurrentUserId(), cancellationToken);
        TempData["SuccessMessage"] = "Weapon registration submitted and is pending approval.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var weapon = await _weaponService.GetByIdAsync(id, cancellationToken);
        if (weapon is null)
        {
            return NotFound();
        }

        return View(new WeaponFormViewModel
        {
            Id = weapon.Id,
            CategoryId = weapon.CategoryId,
            OwnerId = weapon.OwnerId,
            Manufacturer = weapon.Manufacturer,
            Model = weapon.Model,
            SerialNumber = weapon.SerialNumber,
            Caliber = weapon.Caliber,
            DateOfManufacture = weapon.DateOfManufacture,
            RegistrationLocation = weapon.RegistrationLocation,
            Notes = weapon.Notes,
            Categories = await _weaponService.GetActiveCategoriesAsync(cancellationToken),
            Owners = await _ownerService.GetAllAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(WeaponFormViewModel form, CancellationToken cancellationToken)
    {
        if (!form.Id.HasValue)
        {
            return NotFound();
        }

        var request = MapToRequest(form);
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            ModelState.AddValidationResult(validation);
        }

        if (await _weaponService.IsSerialNumberInUseAsync(request.SerialNumber, form.Id, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.SerialNumber), "A weapon with this serial number is already registered.");
        }

        if (!ModelState.IsValid)
        {
            form.Categories = await _weaponService.GetActiveCategoriesAsync(cancellationToken);
            form.Owners = await _ownerService.GetAllAsync(cancellationToken);
            return View(form);
        }

        await _weaponService.UpdateAsync(request, GetCurrentUserId(), cancellationToken);
        TempData["SuccessMessage"] = "Weapon registration updated successfully.";
        return RedirectToAction(nameof(Details), new { id = form.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireOfficerOrAdmin")]
    public async Task<IActionResult> SetStatus(Guid id, WeaponStatus status, string? reason, CancellationToken cancellationToken)
    {
        await _weaponService.SetStatusAsync(id, status, reason, GetCurrentUserId(), cancellationToken);
        TempData["SuccessMessage"] = $"Weapon status updated to {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10_500_000)]
    public async Task<IActionResult> UploadDocument(Guid weaponId, IFormFile file, string? description, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a file to upload.";
            return RedirectToAction(nameof(Details), new { id = weaponId });
        }

        if (!_fileStorage.IsAllowed(file.FileName, file.Length, out var error))
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(Details), new { id = weaponId });
        }

        await using var stream = file.OpenReadStream();
        var stored = await _fileStorage.SaveAsync(stream, file.FileName, cancellationToken);

        await _weaponService.AddDocumentAsync(weaponId, file.FileName, stored.StoredFileName, stored.SizeBytes, file.ContentType, description, GetCurrentUserId(), cancellationToken);

        TempData["SuccessMessage"] = "Document uploaded successfully.";
        return RedirectToAction(nameof(Details), new { id = weaponId });
    }

    public async Task<IActionResult> DownloadDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _weaponService.GetDocumentAsync(documentId, cancellationToken);
        if (document is null)
        {
            return NotFound();
        }

        var file = await _fileStorage.OpenReadAsync(document.StoredFileName, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        return File(file.Value.Content, file.Value.ContentType, document.FileName);
    }

    private static SaveWeaponRequest MapToRequest(WeaponFormViewModel model) => new()
    {
        Id = model.Id,
        CategoryId = model.CategoryId,
        OwnerId = model.OwnerId,
        Manufacturer = model.Manufacturer,
        Model = model.Model,
        SerialNumber = model.SerialNumber,
        Caliber = model.Caliber,
        DateOfManufacture = model.DateOfManufacture,
        RegistrationLocation = model.RegistrationLocation,
        Notes = model.Notes
    };

    private Guid GetCurrentUserId() => Guid.Parse(_userManager.GetUserId(User)!);
}
