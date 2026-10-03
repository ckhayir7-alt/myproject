using WRMS.Application.DTOs.Search;
using WRMS.Domain.Enums;

namespace WRMS.Application.Interfaces;

public interface ISearchService
{
    Task<List<SearchResultItemDto>> SearchAsync(string? query, WeaponStatus? status, CancellationToken cancellationToken = default);

    Task<VerificationResultDto?> GetVerificationAsync(Guid weaponId, CancellationToken cancellationToken = default);
}
