using WRMS.Application.DTOs.Settings;

namespace WRMS.Application.Interfaces;

public interface ISettingsService
{
    Task<List<WeaponCategoryDto>> GetWeaponCategoriesAsync(CancellationToken cancellationToken = default);

    Task<bool> IsCategoryNameInUseAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task CreateWeaponCategoryAsync(SaveWeaponCategoryRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task UpdateWeaponCategoryAsync(SaveWeaponCategoryRequest request, Guid currentUserId, CancellationToken cancellationToken = default);

    Task ToggleWeaponCategoryActiveAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);

    Task<List<SystemSettingDto>> GetSystemSettingsAsync(CancellationToken cancellationToken = default);

    Task UpdateSystemSettingAsync(string key, string value, Guid currentUserId, CancellationToken cancellationToken = default);
}
