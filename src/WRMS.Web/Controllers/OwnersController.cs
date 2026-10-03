using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WRMS.Application.DTOs.Owners;
using WRMS.Application.Interfaces;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Identity;
using WRMS.Web.Extensions;
using WRMS.Web.Models.Owners;

namespace WRMS.Web.Controllers;

[Authorize]
public class OwnersController : Controller
{
    private readonly IOwnerService _ownerService;
    private readonly IFileStorageService _fileStorage;
    private readonly IValidator<SaveOwnerRequest> _validator;
    private readonly UserManager<ApplicationUser> _userManager;

    public OwnersController(
        IOwnerService ownerService,
        IFileStorageService fileStorage,
        IValidator<SaveOwnerRequest> validator,
        UserManager<ApplicationUser> userManager)
    {
        _ownerService = ownerService;
        _fileStorage = fileStorage;
        _validator = validator;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var owners = await _ownerService.GetAllAsync(cancellationToken);
        return View(owners);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var owner = await _ownerService.GetByIdAsync(id, cancellationToken);
        if (owner is null)
        {
            return NotFound();
        }

        return View(owner);
    }

    public IActionResult Create()
    {
        return View(new OwnerFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OwnerFormViewModel model, CancellationToken cancellationToken)
    {
        var request = MapToRequest(model);
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            ModelState.AddValidationResult(validation);
        }

        if (await _ownerService.IsNationalIdInUseAsync(request.NationalId, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.NationalId), "An owner with this National ID already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var id = await _ownerService.CreateAsync(request, GetCurrentUserId(), cancellationToken);
        TempData["SuccessMessage"] = "Owner registered successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var owner = await _ownerService.GetByIdAsync(id, cancellationToken);
        if (owner is null)
        {
            return NotFound();
        }

        return View(new OwnerFormViewModel
        {
            Id = owner.Id,
            FullName = owner.FullName,
            NationalId = owner.NationalId,
            DateOfBirth = owner.DateOfBirth,
            Gender = owner.Gender,
            PhoneNumber = owner.PhoneNumber,
            Address = owner.Address,
            Occupation = owner.Occupation,
            Notes = owner.Notes
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OwnerFormViewModel model, CancellationToken cancellationToken)
    {
        if (!model.Id.HasValue)
        {
            return NotFound();
        }

        var request = MapToRequest(model);
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            ModelState.AddValidationResult(validation);
        }

        if (await _ownerService.IsNationalIdInUseAsync(request.NationalId, model.Id, cancellationToken))
        {
            ModelState.AddModelError(nameof(model.NationalId), "An owner with this National ID already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _ownerService.UpdateAsync(request, GetCurrentUserId(), cancellationToken);
        TempData["SuccessMessage"] = "Owner updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "RequireOfficerOrAdmin")]
    public async Task<IActionResult> SetStatus(Guid id, OwnerStatus status, string? reason, CancellationToken cancellationToken)
    {
        await _ownerService.SetStatusAsync(id, status, reason, GetCurrentUserId(), cancellationToken);
        TempData["SuccessMessage"] = $"Owner status updated to {status}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10_500_000)]
    public async Task<IActionResult> UploadDocument(Guid ownerId, IFormFile file, string? description, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a file to upload.";
            return RedirectToAction(nameof(Details), new { id = ownerId });
        }

        if (!_fileStorage.IsAllowed(file.FileName, file.Length, out var error))
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(Details), new { id = ownerId });
        }

        await using var stream = file.OpenReadStream();
        var stored = await _fileStorage.SaveAsync(stream, file.FileName, cancellationToken);

        await _ownerService.AddDocumentAsync(ownerId, file.FileName, stored.StoredFileName, stored.SizeBytes, file.ContentType, description, GetCurrentUserId(), cancellationToken);

        TempData["SuccessMessage"] = "Document uploaded successfully.";
        return RedirectToAction(nameof(Details), new { id = ownerId });
    }

    public async Task<IActionResult> DownloadDocument(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _ownerService.GetDocumentAsync(documentId, cancellationToken);
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

    private static SaveOwnerRequest MapToRequest(OwnerFormViewModel model) => new()
    {
        Id = model.Id,
        FullName = model.FullName,
        NationalId = model.NationalId,
        DateOfBirth = model.DateOfBirth ?? default,
        Gender = model.Gender,
        PhoneNumber = model.PhoneNumber,
        Address = model.Address,
        Occupation = model.Occupation,
        Notes = model.Notes
    };

    private Guid GetCurrentUserId() => Guid.Parse(_userManager.GetUserId(User)!);
}
