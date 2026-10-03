using WRMS.Application.DTOs.Dashboard;

namespace WRMS.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);

    Task<List<RecentActivityItemDto>> GetRecentActivityAsync(int count = 10, CancellationToken cancellationToken = default);

    Task<List<NameCountDto>> GetWeaponsByCategoryAsync(CancellationToken cancellationToken = default);

    Task<List<NameCountDto>> GetWeaponsByStatusAsync(CancellationToken cancellationToken = default);

    Task<List<TrendPointDto>> GetRegistrationTrendAsync(int months = 6, CancellationToken cancellationToken = default);
}
