using MigraDocCore.DocumentObjectModel;
using MigraDocCore.Rendering;
using WRMS.Application.DTOs.Reports;
using WRMS.Application.Interfaces;

namespace WRMS.Infrastructure.Services;

public class PdfExportService : IPdfExportService
{
    public byte[] Generate(ReportTableDto report)
    {
        var document = new Document();
        document.Info.Title = report.Title;

        var section = document.AddSection();
        section.PageSetup.Orientation = Orientation.Landscape;
        section.PageSetup.TopMargin = "1.5cm";
        section.PageSetup.LeftMargin = "1.5cm";
        section.PageSetup.RightMargin = "1.5cm";

        var title = section.AddParagraph(report.Title);
        title.Format.Font.Size = 16;
        title.Format.Font.Bold = true;
        title.Format.SpaceAfter = "0.2cm";

        var subtitle = section.AddParagraph($"Weapon Registration & Management System — Generated {report.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC");
        subtitle.Format.Font.Size = 9;
        subtitle.Format.Font.Color = Colors.Gray;
        subtitle.Format.SpaceAfter = "0.5cm";

        var table = section.AddTable();
        table.Borders.Width = 0.5;
        table.Borders.Color = Colors.LightGray;

        foreach (var _ in report.Columns)
        {
            table.AddColumn();
        }

        var headerRow = table.AddRow();
        headerRow.Shading.Color = new Color(11, 37, 69);
        headerRow.Format.Font.Color = Colors.White;
        headerRow.Format.Font.Bold = true;
        headerRow.Format.Font.Size = 9;
        for (var i = 0; i < report.Columns.Count; i++)
        {
            headerRow.Cells[i].AddParagraph(report.Columns[i]);
        }

        foreach (var row in report.Rows)
        {
            var dataRow = table.AddRow();
            dataRow.Format.Font.Size = 8.5;
            for (var i = 0; i < row.Count; i++)
            {
                dataRow.Cells[i].AddParagraph(row[i]);
            }
        }

        if (report.Rows.Count == 0)
        {
            var emptyRow = table.AddRow();
            emptyRow.Cells[0].AddParagraph("No records found.");
        }

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();

        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, false);
        return stream.ToArray();
    }
}
