using WRMS.Application.DTOs.Dashboard;

namespace WRMS.Web.Models.Dashboard;

public class DashboardViewModel
{
    public DashboardStatsDto Stats { get; set; } = new();
    public List<RecentActivityItemDto> RecentActivity { get; set; } = new();
    public List<NameCountDto> WeaponsByCategory { get; set; } = new();
    public List<NameCountDto> WeaponsByStatus { get; set; } = new();
    public List<TrendPointDto> RegistrationTrend { get; set; } = new();
}
