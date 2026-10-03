using WRMS.Application.DTOs.Audit;

namespace WRMS.Application.Interfaces;

public interface IAuditLogService
{
    Task<List<AuditLogItemDto>> GetAsync(AuditLogFilterDto filter, int take = 500, CancellationToken cancellationToken = default);

    Task<List<string>> GetDistinctEntityTypesAsync(CancellationToken cancellationToken = default);
}
