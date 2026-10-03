using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using WRMS.Application.Interfaces;
using WRMS.Infrastructure.Persistence;

namespace WRMS.Infrastructure.Services;

public class BackupService : IBackupService
{
    private readonly ApplicationDbContext _context;

    public BackupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> GenerateDataSnapshotAsync(CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();

        var owners = await _context.Owners.AsNoTracking().OrderBy(o => o.OwnerCode).ToListAsync(cancellationToken);
        var ownersSheet = workbook.Worksheets.Add("Owners");
        WriteHeader(ownersSheet, "OwnerCode", "FullName", "NationalId", "PhoneNumber", "Address", "Status", "CreatedAt");
        for (var i = 0; i < owners.Count; i++)
        {
            var o = owners[i];
            var row = i + 2;
            ownersSheet.Cell(row, 1).Value = o.OwnerCode;
            ownersSheet.Cell(row, 2).Value = o.FullName;
            ownersSheet.Cell(row, 3).Value = o.NationalId;
            ownersSheet.Cell(row, 4).Value = o.PhoneNumber;
            ownersSheet.Cell(row, 5).Value = o.Address;
            ownersSheet.Cell(row, 6).Value = o.Status.ToString();
            ownersSheet.Cell(row, 7).Value = o.CreatedAt;
        }
        ownersSheet.Columns().AdjustToContents();

        var weapons = await _context.Weapons.AsNoTracking().Include(w => w.Category).Include(w => w.Owner).OrderBy(w => w.RegistrationId).ToListAsync(cancellationToken);
        var weaponsSheet = workbook.Worksheets.Add("Weapons");
        WriteHeader(weaponsSheet, "RegistrationId", "Category", "Manufacturer", "Model", "SerialNumber", "Owner", "Status", "RegistrationDate");
        for (var i = 0; i < weapons.Count; i++)
        {
            var w = weapons[i];
            var row = i + 2;
            weaponsSheet.Cell(row, 1).Value = w.RegistrationId;
            weaponsSheet.Cell(row, 2).Value = w.Category.Name;
            weaponsSheet.Cell(row, 3).Value = w.Manufacturer;
            weaponsSheet.Cell(row, 4).Value = w.Model;
            weaponsSheet.Cell(row, 5).Value = w.SerialNumber;
            weaponsSheet.Cell(row, 6).Value = w.Owner.FullName;
            weaponsSheet.Cell(row, 7).Value = w.Status.ToString();
            weaponsSheet.Cell(row, 8).Value = w.RegistrationDate;
        }
        weaponsSheet.Columns().AdjustToContents();

        var licenses = await _context.Licenses.AsNoTracking().Include(l => l.Weapon).OrderBy(l => l.LicenseNumber).ToListAsync(cancellationToken);
        var licensesSheet = workbook.Worksheets.Add("Licenses");
        WriteHeader(licensesSheet, "LicenseNumber", "Weapon", "IssueDate", "ExpiryDate", "Status");
        for (var i = 0; i < licenses.Count; i++)
        {
            var l = licenses[i];
            var row = i + 2;
            licensesSheet.Cell(row, 1).Value = l.LicenseNumber;
            licensesSheet.Cell(row, 2).Value = l.Weapon.RegistrationId;
            licensesSheet.Cell(row, 3).Value = l.IssueDate;
            licensesSheet.Cell(row, 4).Value = l.ExpiryDate;
            licensesSheet.Cell(row, 5).Value = l.Status.ToString();
        }
        licensesSheet.Columns().AdjustToContents();

        var transfers = await _context.WeaponTransfers.AsNoTracking()
            .Include(t => t.Weapon).Include(t => t.PreviousOwner).Include(t => t.NewOwner)
            .OrderByDescending(t => t.TransferDate).ToListAsync(cancellationToken);
        var transfersSheet = workbook.Worksheets.Add("Transfers");
        WriteHeader(transfersSheet, "Weapon", "PreviousOwner", "NewOwner", "TransferDate", "Status");
        for (var i = 0; i < transfers.Count; i++)
        {
            var t = transfers[i];
            var row = i + 2;
            transfersSheet.Cell(row, 1).Value = t.Weapon.RegistrationId;
            transfersSheet.Cell(row, 2).Value = t.PreviousOwner.FullName;
            transfersSheet.Cell(row, 3).Value = t.NewOwner.FullName;
            transfersSheet.Cell(row, 4).Value = t.TransferDate;
            transfersSheet.Cell(row, 5).Value = t.ApprovalStatus.ToString();
        }
        transfersSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteHeader(IXLWorksheet sheet, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(11, 37, 69);
            cell.Style.Font.FontColor = XLColor.White;
        }
    }
}
