namespace WRMS.Application.DTOs.Reports;

public enum ReportType
{
    WeaponsRegistered = 0,
    LicensesActiveExpired = 1,
    RegistrationActivity = 2,
    OwnershipTransfers = 3,
    SuspendedRevoked = 4,
    ByLocation = 5,
    ByCategory = 6
}

public class ReportTableDto
{
    public string Title { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}
