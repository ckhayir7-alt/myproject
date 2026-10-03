using WRMS.Application.DTOs.Licenses;
using WRMS.Domain.Enums;

namespace WRMS.Application.Interfaces;

public interface ILicenseService
{
    Task<List<LicenseListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<LicenseDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> IsLicenseNumberInUseAsync(string licenseNumber, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<bool> WeaponHasLicenseAsync(Guid weaponId, CancellationToken cancellationToken = default);

    Task<Guid> IssueAsync(IssueLicenseRequest request, Guid currentUserId, string? currentUserName, CancellationToken cancellationToken = default);

    Task RenewAsync(RenewLicenseRequest request, Guid currentUserId, string? currentUserName, CancellationToken cancellationToken = default);

    Task SetStatusAsync(Guid licenseId, LicenseStatus status, string? reason, Guid currentUserId, string? currentUserName, CancellationToken cancellationToken = default);
}
