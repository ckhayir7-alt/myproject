using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WRMS.Application.DTOs.Settings;
using WRMS.Application.Interfaces;
using WRMS.Infrastructure.Identity;
using WRMS.Web.Models.Settings;

namespace WRMS.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
public class SettingsController : Controller
{
    private readonly ISettingsService _settingsService;
    private readonly IBackupService _backupService;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SettingsController(
        ISettingsService settingsService,
        IBackupService backupService,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager)
    {
        _settingsService = settingsService;
        _backupService = backupService;
        _auditService = auditService;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Categories(CancellationToken cancellationToken)
    {
        var categories = await _settingsService.GetWeaponCategoriesAsync(cancellationToken);
        return View(categories);
    }

    public IActionResult CreateCategory()
    {
        return View(new WeaponCategoryFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(WeaponCategoryFormViewModel form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Name))
        {
            ModelState.AddModelError(nameof(form.Name), "Name is required.");
        }
        else if (await _settingsService.IsCategoryNameInUseAsync(form.Name, null, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.Name), "A category with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        await _settingsService.CreateWeaponCategoryAsync(new SaveWeaponCategoryRequest
        {
            Name = form.Name,
            Description = form.Description,
            IsActive = form.IsActive
        }, GetCurrentUserId(), cancellationToken);

        TempData["SuccessMessage"] = "Weapon category created successfully.";
        return RedirectToAction(nameof(Categories));
    }

    public async Task<IActionResult> EditCategory(Guid id, CancellationToken cancellationToken)
    {
        var categories = await _settingsService.GetWeaponCategoriesAsync(cancellationToken);
        var category = categories.FirstOrDefault(c => c.Id == id);
        if (category is null)
        {
            return NotFound();
        }

        return View(new WeaponCategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(WeaponCategoryFormViewModel form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.Name))
        {
            ModelState.AddModelError(nameof(form.Name), "Name is required.");
        }
        else if (await _settingsService.IsCategoryNameInUseAsync(form.Name, form.Id, cancellationToken))
        {
            ModelState.AddModelError(nameof(form.Name), "A category with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        await _settingsService.UpdateWeaponCategoryAsync(new SaveWeaponCategoryRequest
        {
            Id = form.Id,
            Name = form.Name,
            Description = form.Description,
            IsActive = form.IsActive
        }, GetCurrentUserId(), cancellationToken);

        TempData["SuccessMessage"] = "Weapon category updated successfully.";
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategoryActive(Guid id, CancellationToken cancellationToken)
    {
        await _settingsService.ToggleWeaponCategoryActiveAsync(id, GetCurrentUserId(), cancellationToken);
        return RedirectToAction(nameof(Categories));
    }

    public async Task<IActionResult> General(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetSystemSettingsAsync(cancellationToken);
        return View(settings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> General(Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        foreach (var (key, value) in values)
        {
            await _settingsService.UpdateSystemSettingAsync(key, value, GetCurrentUserId(), cancellationToken);
        }

        TempData["SuccessMessage"] = "System settings updated successfully.";
        return RedirectToAction(nameof(General));
    }

    public IActionResult DataManagement()
    {
        return View();
    }

    public async Task<IActionResult> ExportSnapshot(CancellationToken cancellationToken)
    {
        var bytes = await _backupService.GenerateDataSnapshotAsync(cancellationToken);
        await _auditService.LogAsync(GetCurrentUserId(), User.Identity?.Name, Domain.Enums.AuditAction.Export, "DataSnapshot", null,
            "Exported full data snapshot for backup purposes.", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"wrms-data-snapshot-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx");
    }

    private Guid GetCurrentUserId() => Guid.Parse(_userManager.GetUserId(User)!);
}
