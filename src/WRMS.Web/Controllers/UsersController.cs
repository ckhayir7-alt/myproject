using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WRMS.Application.Interfaces;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Identity;
using WRMS.Web.Models.Users;

namespace WRMS.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IAuditService _auditService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.OrderBy(u => u.FullName).ToList();
        var items = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Roles = roles.ToList(),
                IsActive = user.IsActive,
                IsLockedOut = await _userManager.IsLockedOutAsync(user),
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt
            });
        }

        return View(items);
    }

    public async Task<IActionResult> Create()
    {
        var model = new UserFormViewModel
        {
            AvailableRoles = await GetAvailableRolesAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "Password is required when creating a new user.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetAvailableRolesAsync();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,
            FullName = model.FullName,
            IsActive = model.IsActive
        };

        var result = await _userManager.CreateAsync(user, model.Password!);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.AvailableRoles = await GetAvailableRolesAsync();
            return View(model);
        }

        await _userManager.AddToRolesAsync(user, model.SelectedRoles);
        await _auditService.LogAsync(GetCurrentUserId(), User.Identity?.Name, AuditAction.Create, "User", user.Id.ToString(),
            $"Created user {user.Email} with roles: {string.Join(", ", model.SelectedRoles)}.", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["SuccessMessage"] = $"User '{user.FullName}' was created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var model = new UserFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            SelectedRoles = roles.ToList(),
            AvailableRoles = await GetAvailableRolesAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserFormViewModel model)
    {
        if (!model.Id.HasValue)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(model.Password) && model.Password.Length < 10)
        {
            ModelState.AddModelError(nameof(model.Password), "Password must be at least 10 characters.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetAvailableRolesAsync();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id.Value.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var isSelf = user.Id.ToString() == _userManager.GetUserId(User);

        if (isSelf && !model.IsActive)
        {
            ModelState.AddModelError(string.Empty, "You cannot deactivate your own account.");
            model.AvailableRoles = await GetAvailableRolesAsync();
            return View(model);
        }

        if (currentRoles.Contains(Roles.Admin) && !model.SelectedRoles.Contains(Roles.Admin))
        {
            var adminCount = await _userManager.GetUsersInRoleAsync(Roles.Admin);
            if (adminCount.Count <= 1)
            {
                ModelState.AddModelError(string.Empty, "At least one administrator account must remain in the Admin role.");
                model.AvailableRoles = await GetAvailableRolesAsync();
                return View(model);
            }
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.IsActive = model.IsActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.AvailableRoles = await GetAvailableRolesAsync();
            return View(model);
        }

        var rolesToRemove = currentRoles.Except(model.SelectedRoles).ToList();
        var rolesToAdd = model.SelectedRoles.Except(currentRoles).ToList();
        if (rolesToRemove.Count > 0)
        {
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
        }
        if (rolesToAdd.Count > 0)
        {
            await _userManager.AddToRolesAsync(user, rolesToAdd);
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, model.Password);
        }

        await _userManager.UpdateSecurityStampAsync(user);

        await _auditService.LogAsync(GetCurrentUserId(), User.Identity?.Name, AuditAction.Update, "User", user.Id.ToString(),
            $"Updated user {user.Email}.", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["SuccessMessage"] = $"User '{user.FullName}' was updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        return View(new ResetPasswordViewModel
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.UserId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        await _userManager.UpdateSecurityStampAsync(user);
        await _auditService.LogAsync(GetCurrentUserId(), User.Identity?.Name, AuditAction.Update, "User", user.Id.ToString(),
            $"Administrator reset password for {user.Email}.", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["SuccessMessage"] = $"Password for '{user.FullName}' has been reset.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
        {
            return NotFound();
        }

        if (user.Id.ToString() == _userManager.GetUserId(User))
        {
            TempData["ErrorMessage"] = "You cannot deactivate your own account.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);

        await _auditService.LogAsync(GetCurrentUserId(), User.Identity?.Name,
            user.IsActive ? AuditAction.Update : AuditAction.Suspend, "User", user.Id.ToString(),
            user.IsActive ? $"Reactivated user {user.Email}." : $"Deactivated user {user.Email}.",
            HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["SuccessMessage"] = $"User '{user.FullName}' is now {(user.IsActive ? "active" : "inactive")}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<string>> GetAvailableRolesAsync()
    {
        return await Task.FromResult(_roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList());
    }

    private Guid? GetCurrentUserId()
    {
        return Guid.TryParse(_userManager.GetUserId(User), out var id) ? id : null;
    }
}
