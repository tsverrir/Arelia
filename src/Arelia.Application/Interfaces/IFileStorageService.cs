namespace Arelia.Application.Interfaces;

public interface IFileStorageService
{
    /// <summary>Saves content to storage and returns the relative file path.</summary>
    Task<string> SaveAsync(Stream content, string fileName, string contentType,
        string subDirectory, CancellationToken ct = default);

    /// <summary>Opens a read stream for the given relative file path.</summary>
    Task<Stream> ReadAsync(string relativePath, CancellationToken ct = default);

    /// <summary>Deletes the file at the given relative file path. No-op if not found.</summary>
    Task DeleteAsync(string relativePath, CancellationToken ct = default);

    /// <summary>Returns the size in bytes for the given relative file path, or null if not found.</summary>
    Task<long?> GetFileSizeAsync(string relativePath, CancellationToken ct = default);
}
