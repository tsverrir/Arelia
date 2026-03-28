using Arelia.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Arelia.Infrastructure.Services;

public class DiskFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public DiskFileStorageService(IConfiguration configuration)
    {
        _basePath = Path.GetFullPath(configuration["FileStorage:BasePath"] ?? "./uploads");
    }

    /// <summary>Resolves and validates that the path stays within the base directory to prevent path traversal.</summary>
    private string SafeResolvePath(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));
        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Access to the specified path is denied.");
        return fullPath;
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType,
        string subDirectory, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        var storedName = $"{Guid.NewGuid()}{ext}";
        var relPath = Path.Combine(subDirectory, storedName);
        var fullPath = SafeResolvePath(relPath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(file, ct);

        return relPath;
    }

    public Task<Stream> ReadAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = SafeResolvePath(relativePath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = SafeResolvePath(relativePath);
        try { File.Delete(fullPath); } catch (FileNotFoundException) { }
        return Task.CompletedTask;
    }

    public Task<long?> GetFileSizeAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = SafeResolvePath(relativePath);
        var info = new FileInfo(fullPath);
        long? size = info.Exists ? info.Length : null;
        return Task.FromResult(size);
    }
}
