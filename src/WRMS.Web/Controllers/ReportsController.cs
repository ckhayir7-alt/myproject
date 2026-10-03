using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WRMS.Application.DTOs.Reports;
using WRMS.Application.Interfaces;
using WRMS.Domain.Enums;

namespace WRMS.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IExcelExportService _excelExportService;
    private readonly IAuditService _auditService;

    public ReportsController(
        IReportService reportService,
        IPdfExportService pdfExportService,
        IExcelExportService excelExportService,
        IAuditService auditService)
    {
        _reportService = reportService;
        _pdfExportService = pdfExportService;
        _excelExportService = excelExportService;
        _auditService = auditService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> View(ReportType type, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetReportAsync(type, dateFrom, dateTo, cancellationToken);
        ViewData["ReportType"] = type;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");
        return View(report);
    }

    public async Task<IActionResult> ExportPdf(ReportType type, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetReportAsync(type, dateFrom, dateTo, cancellationToken);
        var bytes = _pdfExportService.Generate(report);
        await LogExportAsync(type, "PDF", cancellationToken);
        return File(bytes, "application/pdf", $"{type}-{DateTime.UtcNow:yyyyMMdd}.pdf");
    }

    public async Task<IActionResult> ExportExcel(ReportType type, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetReportAsync(type, dateFrom, dateTo, cancellationToken);
        var bytes = _excelExportService.Generate(report);
        await LogExportAsync(type, "Excel", cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{type}-{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    private async Task LogExportAsync(ReportType type, string format, CancellationToken cancellationToken)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = Guid.TryParse(userIdString, out var id) ? id : null;
        await _auditService.LogAsync(userId, User.Identity?.Name, AuditAction.Export, "Report", type.ToString(),
            $"Exported {type} report as {format}.", HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);
    }
}
