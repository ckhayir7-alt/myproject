using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WRMS.Application.Interfaces;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Identity;

namespace WRMS.Web.Controllers;

[Authorize]
public class SearchController : Controller
{
    private readonly ISearchService _searchService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SearchController(ISearchService searchService, UserManager<ApplicationUser> userManager)
    {
        _searchService = searchService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? q, WeaponStatus? status, CancellationToken cancellationToken)
    {
        ViewData["Query"] = q;
        ViewData["SelectedStatus"] = status;

        var results = string.IsNullOrWhiteSpace(q) && status is null
            ? new List<Application.DTOs.Search.SearchResultItemDto>()
            : await _searchService.SearchAsync(q, status, cancellationToken);

        return View(results);
    }

    public async Task<IActionResult> Verify(Guid weaponId, CancellationToken cancellationToken)
    {
        var result = await _searchService.GetVerificationAsync(weaponId, cancellationToken);
        if (result is null)
        {
            return NotFound();
        }

        result.VerifiedByName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name;
        return View(result);
    }
}
