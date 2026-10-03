using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WRMS.Application.Interfaces;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Identity;
using WRMS.Web.Models.Account;

namespace WRMS.Web.Controllers;

public class AccountController : Controller
{
    private const long MaxProfilePictureBytes = 2 * 1024 * 1024;
    private static readonly string[] AllowedProfilePictureExtensions = { ".jpg", ".jpeg", ".png" };

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService,
        IFileStorageService fileStorage,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (_signInManager.IsSignedIn(User))
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user is not null && !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account has been deactivated. Contact your system administrator.");
            await _auditService.LogAsync(user.Id, user.Email, AuditAction.LoginFailed, "User", user.Id.ToString(), "Account deactivated.", ipAddress);
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user = await _userManager.FindByEmailAsync(model.Email);
            if (user is not null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                await _auditService.LogAsync(user.Id, user.Email, AuditAction.Login, "User", user.Id.ToString(), null, ipAddress);
            }

            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Account locked out: {Email}", model.Email);
            await _auditService.LogAsync(user?.Id, model.Email, AuditAction.LoginFailed, "User", user?.Id.ToString(), "Account locked out after repeated failed attempts.", ipAddress);
            ModelState.AddModelError(string.Empty, "This account is locked due to multiple failed login attempts. Try again later.");
            return View(model);
        }

        await _auditService.LogAsync(user?.Id, model.Email, AuditAction.LoginFailed, "User", user?.Id.ToString(), "Invalid credentials.", ipAddress);
        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        var email = User.Identity?.Name;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _signInManager.SignOutAsync();

        if (Guid.TryParse(userId, out var userGuid))
        {
            await _auditService.LogAsync(userGuid, email, AuditAction.Logout, "User", userId, null, ipAddress);
        }

        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        await _auditService.LogAsync(user.Id, user.Email, AuditAction.Update, "User", user.Id.ToString(), "Password changed by user.", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["SuccessMessage"] = "Your password has been changed successfully.";
        return RedirectToAction(nameof(ChangePassword));
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        var roles = await _userManager.GetRolesAsync(user);

        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList(),
            HasProfilePicture = !string.IsNullOrEmpty(user.ProfilePictureFileName)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxProfilePictureBytes + 50_000)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile? file)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (file is null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select an image to upload.";
            return RedirectToAction(nameof(Profile));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedProfilePictureExtensions.Contains(extension) || file.Length > MaxProfilePictureBytes)
        {
            TempData["ErrorMessage"] = "Please upload a JPG or PNG image up to 2 MB.";
            return RedirectToAction(nameof(Profile));
        }

        var previousFileName = user.ProfilePictureFileName;

        await using (var stream = file.OpenReadStream())
        {
            var stored = await _fileStorage.SaveAsync(stream, file.FileName);
            user.ProfilePictureFileName = stored.StoredFileName;
        }

        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrEmpty(previousFileName))
        {
            _fileStorage.Delete(previousFileName);
        }

        await _signInManager.RefreshSignInAsync(user);
        await _auditService.LogAsync(user.Id, user.Email, AuditAction.Update, "User", user.Id.ToString(),
            "Updated profile picture.", HttpContext.Connection.RemoteIpAddress?.ToString());

        TempData["SuccessMessage"] = "Profile picture updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProfilePicture()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return RedirectToAction(nameof(Login));
        }

        if (!string.IsNullOrEmpty(user.ProfilePictureFileName))
        {
            _fileStorage.Delete(user.ProfilePictureFileName);
            user.ProfilePictureFileName = null;
            await _userManager.UpdateAsync(user);
            await _signInManager.RefreshSignInAsync(user);
            await _auditService.LogAsync(user.Id, user.Email, AuditAction.Update, "User", user.Id.ToString(),
                "Removed profile picture.", HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        TempData["SuccessMessage"] = "Profile picture removed.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public async Task<IActionResult> ProfilePicture(Guid? userId)
    {
        var targetUserId = userId ?? (Guid.TryParse(_userManager.GetUserId(User), out var currentId) ? currentId : (Guid?)null);
        if (targetUserId is null)
        {
            return NotFound();
        }

        var user = await _userManager.FindByIdAsync(targetUserId.Value.ToString());
        if (user is null || string.IsNullOrEmpty(user.ProfilePictureFileName))
        {
            return NotFound();
        }

        var file = await _fileStorage.OpenReadAsync(user.ProfilePictureFileName);
        if (file is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "private, max-age=86400";
        return File(file.Value.Content, file.Value.ContentType);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
