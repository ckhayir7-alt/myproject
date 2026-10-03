using WRMS.Application.DTOs.Notifications;
using WRMS.Domain.Enums;

namespace WRMS.Application.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetForUserAsync(Guid userId, bool unreadOnly = false, int take = 50, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForRelatedEntityAsync(NotificationType type, string relatedEntityType, Guid relatedEntityId, CancellationToken cancellationToken = default);

    Task CreateAsync(NotificationType type, string title, string message, Guid? userId, string? relatedEntityType, Guid? relatedEntityId, CancellationToken cancellationToken = default);
}
