using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Hosting;
using WRMS.Application.Interfaces;

namespace WRMS.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    private readonly string _basePath;
    private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    public FileStorageService(IHostEnvironment environment)
    {
        _basePath = Path.Combine(environment.ContentRootPath, "App_Data", "Uploads");
        Directory.CreateDirectory(_basePath);
    }

    public bool IsAllowed(string fileName, long sizeBytes, out string? error)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            error = "Only PDF, JPG, and PNG files are allowed.";
            return false;
        }

        if (sizeBytes <= 0 || sizeBytes > MaxFileSizeBytes)
        {
            error = "File size must be between 1 byte and 10 MB.";
            return false;
        }

        error = null;
        return true;
    }

    public async Task<StoredFile> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_basePath, storedFileName);

        await using var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(fileStream, cancellationToken);

        return new StoredFile(storedFileName, fileStream.Length);
    }

    public Task<(Stream Content, string ContentType)?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(storedFileName);
        if (fullPath is null || !File.Exists(fullPath))
        {
            return Task.FromResult<(Stream, string)?>(null);
        }

        if (!_contentTypeProvider.TryGetContentType(fullPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<(Stream, string)?>((stream, contentType));
    }

    public void Delete(string storedFileName)
    {
        var fullPath = ResolveSafePath(storedFileName);
        if (fullPath is not null && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private string? ResolveSafePath(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName) || storedFileName.Contains("..") ||
            Path.IsPathRooted(storedFileName) || storedFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        return Path.Combine(_basePath, storedFileName);
    }
}
