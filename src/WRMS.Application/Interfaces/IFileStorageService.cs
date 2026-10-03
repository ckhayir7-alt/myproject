namespace WRMS.Application.Interfaces;

public record StoredFile(string StoredFileName, long SizeBytes);

public interface IFileStorageService
{
    bool IsAllowed(string fileName, long sizeBytes, out string? error);

    Task<StoredFile> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default);

    Task<(Stream Content, string ContentType)?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default);

    void Delete(string storedFileName);
}
