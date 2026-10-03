using Microsoft.EntityFrameworkCore;
using WRMS.Application.Interfaces;
using WRMS.Domain.Entities;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        Guid? userId,
        string? userName,
        AuditAction action,
        string entityType,
        string? entityId = null,
        string? details = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (userName is null && userId.HasValue)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
            userName = user?.FullName ?? user?.Email;
        }

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
