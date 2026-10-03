using ClosedXML.Excel;
using WRMS.Application.DTOs.Reports;
using WRMS.Application.Interfaces;

namespace WRMS.Infrastructure.Services;

public class ExcelExportService : IExcelExportService
{
    public byte[] Generate(ReportTableDto report)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Report");

        sheet.Cell(1, 1).Value = report.Title;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;

        sheet.Cell(2, 1).Value = $"Generated {report.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC";
        sheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        const int headerRowIndex = 4;
        for (var c = 0; c < report.Columns.Count; c++)
        {
            var cell = sheet.Cell(headerRowIndex, c + 1);
            cell.Value = report.Columns[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(11, 37, 69);
            cell.Style.Font.FontColor = XLColor.White;
        }

        for (var r = 0; r < report.Rows.Count; r++)
        {
            for (var c = 0; c < report.Rows[r].Count; c++)
            {
                sheet.Cell(headerRowIndex + 1 + r, c + 1).Value = report.Rows[r][c];
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
