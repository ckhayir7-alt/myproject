namespace WRMS.Application.Interfaces;

public interface IBackupService
{
    Task<byte[]> GenerateDataSnapshotAsync(CancellationToken cancellationToken = default);
}
