using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WRMS.Application.DTOs.Audit;
using WRMS.Application.Interfaces;
using WRMS.Domain.Enums;

namespace WRMS.Web.Controllers;

[Authorize(Policy = "RequireAdmin")]
public class AuditLogsController : Controller
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    public async Task<IActionResult> Index(string? userName, AuditAction? action, string? entityType, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken)
    {
        var filter = new AuditLogFilterDto
        {
            UserName = userName,
            Action = action,
            EntityType = entityType,
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        ViewData["UserName"] = userName;
        ViewData["SelectedAction"] = action;
        ViewData["SelectedEntityType"] = entityType;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");
        ViewData["EntityTypes"] = await _auditLogService.GetDistinctEntityTypesAsync(cancellationToken);

        var logs = await _auditLogService.GetAsync(filter, 500, cancellationToken);
        return View(logs);
    }
}
