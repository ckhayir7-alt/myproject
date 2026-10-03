using WRMS.Domain.Enums;

namespace WRMS.Application.DTOs.Audit;

public class AuditLogItemDto
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? UserName { get; set; }
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}

public class AuditLogFilterDto
{
    public string? UserName { get; set; }
    public AuditAction? Action { get; set; }
    public string? EntityType { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}
