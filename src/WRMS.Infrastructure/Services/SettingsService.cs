using Microsoft.EntityFrameworkCore;
using WRMS.Application.DTOs.Settings;
using WRMS.Application.Interfaces;
using WRMS.Domain.Entities;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public SettingsService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<WeaponCategoryDto>> GetWeaponCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.WeaponCategories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new WeaponCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                WeaponCount = c.Weapons.Count(w => !w.IsDeleted)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsCategoryNameInUseAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await _context.WeaponCategories.AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId), cancellationToken);
    }

    public async Task CreateWeaponCategoryAsync(SaveWeaponCategoryRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var category = new WeaponCategory
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive,
            CreatedById = currentUserId
        };

        _context.WeaponCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, null, AuditAction.Create, "WeaponCategory", category.Id.ToString(),
            $"Created weapon category '{category.Name}'.", null, cancellationToken);
    }

    public async Task UpdateWeaponCategoryAsync(SaveWeaponCategoryRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var category = await _context.WeaponCategories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Weapon category not found.");

        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.IsActive = request.IsActive;
        category.UpdatedById = currentUserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, null, AuditAction.Update, "WeaponCategory", category.Id.ToString(),
            $"Updated weapon category '{category.Name}'.", null, cancellationToken);
    }

    public async Task ToggleWeaponCategoryActiveAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var category = await _context.WeaponCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Weapon category not found.");

        category.IsActive = !category.IsActive;
        category.UpdatedById = currentUserId;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, null, AuditAction.Update, "WeaponCategory", category.Id.ToString(),
            $"Weapon category '{category.Name}' set to {(category.IsActive ? "active" : "inactive")}.", null, cancellationToken);
    }

    public async Task<List<SystemSettingDto>> GetSystemSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SystemSettings
            .AsNoTracking()
            .OrderBy(s => s.Key)
            .Select(s => new SystemSettingDto { Key = s.Key, Value = s.Value, Description = s.Description })
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateSystemSettingAsync(string key, string value, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken)
            ?? throw new KeyNotFoundException("Setting not found.");

        setting.Value = value.Trim();
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedById = currentUserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(currentUserId, null, AuditAction.Update, "SystemSetting", key,
            $"Updated setting '{key}' to '{value}'.", null, cancellationToken);
    }
}
