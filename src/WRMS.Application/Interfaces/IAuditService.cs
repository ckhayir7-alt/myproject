using WRMS.Domain.Enums;

namespace WRMS.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        Guid? userId,
        string? userName,
        AuditAction action,
        string entityType,
        string? entityId = null,
        string? details = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);
}
