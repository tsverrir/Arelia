using Arelia.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Arelia.Infrastructure.Services;

public class DiskFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public DiskFileStorageService(IConfiguration configuration)
    {
        _basePath = configuration["FileStorage:BasePath"] ?? "./uploads";
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType,
        string subDirectory, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        var storedName = $"{Guid.NewGuid()}{ext}";
        var directory = Path.Combine(_basePath, subDirectory);
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, storedName);
        await using var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(file, ct);

        return Path.Combine(subDirectory, storedName);
    }

    public Task<Stream> ReadAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, relativePath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, relativePath);
        try { File.Delete(fullPath); } catch (FileNotFoundException) { }
        return Task.CompletedTask;
    }

    public Task<long?> GetFileSizeAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_basePath, relativePath);
        var info = new FileInfo(fullPath);
        long? size = info.Exists ? info.Length : null;
        return Task.FromResult(size);
    }
}
