using WRMS.Domain.Common;

namespace WRMS.Domain.Entities;

public class Document : BaseEntity
{
    public Guid? WeaponId { get; set; }
    public Weapon? Weapon { get; set; }

    public Guid? OwnerId { get; set; }
    public Owner? Owner { get; set; }

    public Guid? WeaponTransferId { get; set; }
    public WeaponTransfer? WeaponTransfer { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Description { get; set; }

    public Guid UploadedById { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
