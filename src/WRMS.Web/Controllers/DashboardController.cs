using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WRMS.Application.Interfaces;
using WRMS.Web.Models.Dashboard;

namespace WRMS.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new DashboardViewModel
        {
            Stats = await _dashboardService.GetStatsAsync(cancellationToken),
            RecentActivity = await _dashboardService.GetRecentActivityAsync(10, cancellationToken),
            WeaponsByCategory = await _dashboardService.GetWeaponsByCategoryAsync(cancellationToken),
            WeaponsByStatus = await _dashboardService.GetWeaponsByStatusAsync(cancellationToken),
            RegistrationTrend = await _dashboardService.GetRegistrationTrendAsync(6, cancellationToken)
        };

        return View(model);
    }
}
