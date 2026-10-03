namespace WRMS.Application.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TotalWeapons { get; set; }
    public int ActiveLicenses { get; set; }
    public int ExpiredLicenses { get; set; }
    public int PendingRegistrations { get; set; }
    public int SuspendedOrRevokedWeapons { get; set; }
    public int TotalOwners { get; set; }
    public int PendingTransfers { get; set; }
    public int LicensesExpiringSoon { get; set; }

    /// <summary>Real (not estimated) counts of records created in the last 30 days, used for dashboard trend context.</summary>
    public int NewWeaponsLast30Days { get; set; }
    public int NewOwnersLast30Days { get; set; }
    public int NewLicensesLast30Days { get; set; }
}

public class RecentActivityItemDto
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? UserName { get; set; }
}

public class NameCountDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TrendPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
