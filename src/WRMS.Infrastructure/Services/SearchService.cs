using Microsoft.EntityFrameworkCore;
using WRMS.Application.DTOs.Search;
using WRMS.Application.Interfaces;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _context;

    public SearchService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SearchResultItemDto>> SearchAsync(string? query, WeaponStatus? status, CancellationToken cancellationToken = default)
    {
        var weapons = _context.Weapons
            .AsNoTracking()
            .Include(w => w.Category)
            .Include(w => w.Owner)
            .Include(w => w.License)
            .AsQueryable();

        if (status.HasValue)
        {
            weapons = weapons.Where(w => w.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            weapons = weapons.Where(w =>
                EF.Functions.Like(w.RegistrationId, $"%{term}%") ||
                EF.Functions.Like(w.SerialNumber, $"%{term}%") ||
                EF.Functions.Like(w.Manufacturer, $"%{term}%") ||
                EF.Functions.Like(w.Model, $"%{term}%") ||
                EF.Functions.Like(w.Owner.FullName, $"%{term}%") ||
                EF.Functions.Like(w.Owner.OwnerCode, $"%{term}%") ||
                EF.Functions.Like(w.Owner.NationalId, $"%{term}%") ||
                (w.License != null && EF.Functions.Like(w.License.LicenseNumber, $"%{term}%")));
        }

        return await weapons
            .OrderByDescending(w => w.RegistrationDate)
            .Take(200)
            .Select(w => new SearchResultItemDto
            {
                WeaponId = w.Id,
                RegistrationId = w.RegistrationId,
                SerialNumber = w.SerialNumber,
                Manufacturer = w.Manufacturer,
                Model = w.Model,
                CategoryName = w.Category.Name,
                Status = w.Status,
                OwnerId = w.OwnerId,
                OwnerName = w.Owner.FullName,
                OwnerCode = w.Owner.OwnerCode,
                LicenseNumber = w.License != null ? w.License.LicenseNumber : null,
                LicenseStatus = w.License != null ? w.License.Status : null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<VerificationResultDto?> GetVerificationAsync(Guid weaponId, CancellationToken cancellationToken = default)
    {
        var weapon = await _context.Weapons
            .AsNoTracking()
            .Include(w => w.Category)
            .Include(w => w.Owner)
            .Include(w => w.License)
            .FirstOrDefaultAsync(w => w.Id == weaponId, cancellationToken);

        if (weapon is null)
        {
            return null;
        }

        return new VerificationResultDto
        {
            WeaponId = weapon.Id,
            RegistrationId = weapon.RegistrationId,
            SerialNumber = weapon.SerialNumber,
            Manufacturer = weapon.Manufacturer,
            Model = weapon.Model,
            Caliber = weapon.Caliber,
            CategoryName = weapon.Category.Name,
            WeaponStatus = weapon.Status,
            RegistrationDate = weapon.RegistrationDate,
            OwnerName = weapon.Owner.FullName,
            OwnerCode = weapon.Owner.OwnerCode,
            OwnerNationalId = weapon.Owner.NationalId,
            OwnerStatus = weapon.Owner.Status,
            LicenseNumber = weapon.License?.LicenseNumber,
            LicenseStatus = weapon.License?.GetEffectiveStatus(DateTime.UtcNow),
            LicenseExpiryDate = weapon.License?.ExpiryDate
        };
    }
}
