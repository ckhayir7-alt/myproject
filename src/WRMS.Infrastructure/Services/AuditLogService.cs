using Microsoft.EntityFrameworkCore;
using WRMS.Application.DTOs.Audit;
using WRMS.Application.Interfaces;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AuditLogItemDto>> GetAsync(AuditLogFilterDto filter, int take = 500, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.UserName))
        {
            var term = filter.UserName.Trim();
            query = query.Where(a => a.UserName != null && EF.Functions.Like(a.UserName, $"%{term}%"));
        }

        if (filter.Action.HasValue)
        {
            query = query.Where(a => a.Action == filter.Action.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityType))
        {
            query = query.Where(a => a.EntityType == filter.EntityType);
        }

        if (filter.DateFrom.HasValue)
        {
            query = query.Where(a => a.Timestamp >= filter.DateFrom.Value);
        }

        if (filter.DateTo.HasValue)
        {
            var inclusiveEnd = filter.DateTo.Value.Date.AddDays(1);
            query = query.Where(a => a.Timestamp < inclusiveEnd);
        }

        return await query
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .Select(a => new AuditLogItemDto
            {
                Id = a.Id,
                Timestamp = a.Timestamp,
                UserName = a.UserName,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Details = a.Details,
                IpAddress = a.IpAddress
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetDistinctEntityTypesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs.AsNoTracking().Select(a => a.EntityType).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
    }
}
