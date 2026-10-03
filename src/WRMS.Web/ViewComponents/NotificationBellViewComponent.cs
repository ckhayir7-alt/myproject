using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WRMS.Application.DTOs.Notifications;
using WRMS.Application.Interfaces;
using WRMS.Infrastructure.Identity;

namespace WRMS.Web.ViewComponents;

public class NotificationBellViewModel
{
    public int UnreadCount { get; set; }
    public List<NotificationDto> Recent { get; set; } = new();
}

public class NotificationBellViewComponent : ViewComponent
{
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationBellViewComponent(INotificationService notificationService, UserManager<ApplicationUser> userManager)
    {
        _notificationService = notificationService;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (HttpContext.User.Identity?.IsAuthenticated != true)
        {
            return View(new NotificationBellViewModel());
        }

        var userIdString = _userManager.GetUserId(HttpContext.User);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return View(new NotificationBellViewModel());
        }

        var model = new NotificationBellViewModel
        {
            UnreadCount = await _notificationService.GetUnreadCountAsync(userId),
            Recent = await _notificationService.GetForUserAsync(userId, unreadOnly: false, take: 5)
        };

        return View(model);
    }
}
