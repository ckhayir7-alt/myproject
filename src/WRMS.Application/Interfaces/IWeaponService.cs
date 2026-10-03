using WRMS.Application.DTOs.Weapons;
using WRMS.Domain.Enums;

namespace WRMS.Application.Interfaces;

public interface IWeaponService
{
    Task<List<WeaponCategoryOptionDto>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);

    Task<List<WeaponListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<WeaponDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> IsSerialNumberInUseAsync(string serialNumber, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(SaveWeaponRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task UpdateAsync(SaveWeaponRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task SetStatusAsync(Guid id, WeaponStatus status, string? reason, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<Guid> AddDocumentAsync(Guid weaponId, string fileName, string storedFileName, long sizeBytes, string contentType, string? description, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<WeaponDocumentDto?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
