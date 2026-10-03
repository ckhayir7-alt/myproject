using WRMS.Application.DTOs.Reports;

namespace WRMS.Application.Interfaces;

public interface IReportService
{
    Task<ReportTableDto> GetReportAsync(ReportType type, DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default);
}

public interface IPdfExportService
{
    byte[] Generate(ReportTableDto report);
}

public interface IExcelExportService
{
    byte[] Generate(ReportTableDto report);
}
