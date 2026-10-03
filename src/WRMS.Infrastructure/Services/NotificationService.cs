using Microsoft.EntityFrameworkCore;
using WRMS.Application.DTOs.Notifications;
using WRMS.Application.Interfaces;
using WRMS.Domain.Entities;
using WRMS.Domain.Enums;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<NotificationDto>> GetForUserAsync(Guid userId, bool unreadOnly = false, int take = 50, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                RelatedEntityType = n.RelatedEntityType,
                RelatedEntityId = n.RelatedEntityId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);
        if (notification is null || notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsForRelatedEntityAsync(NotificationType type, string relatedEntityType, Guid relatedEntityId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications.AnyAsync(
            n => n.Type == type && n.RelatedEntityType == relatedEntityType && n.RelatedEntityId == relatedEntityId,
            cancellationToken);
    }

    public async Task CreateAsync(NotificationType type, string title, string message, Guid? userId, string? relatedEntityType, Guid? relatedEntityId, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Add(new Notification
        {
            Type = type,
            Title = title,
            Message = message,
            UserId = userId,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
