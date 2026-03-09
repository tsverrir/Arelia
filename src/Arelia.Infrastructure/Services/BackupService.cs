using Microsoft.Extensions.Logging;

namespace Arelia.Infrastructure.Services;

/// <summary>
/// Manages SQLite database backups — manual trigger with FIFO retention.
/// </summary>
public class BackupService(ILogger<BackupService> logger)
{
    private const int MaxBackups = 30;

    /// <summary>
    /// Creates a backup of the SQLite database file.
    /// </summary>
    public async Task<string> CreateBackupAsync(string databasePath, string backupDirectory)
    {
        ArgumentNullException.ThrowIfNull(databasePath);
        ArgumentNullException.ThrowIfNull(backupDirectory);

        if (!File.Exists(databasePath))
            throw new FileNotFoundException("Database file not found.", databasePath);

        Directory.CreateDirectory(backupDirectory);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"arelia_backup_{timestamp}.db";
        var backupPath = Path.Combine(backupDirectory, backupFileName);

        await using var source = new FileStream(databasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        await using var dest = new FileStream(backupPath, FileMode.Create, FileAccess.Write);
        await source.CopyToAsync(dest);

        logger.LogInformation("Database backup created: {BackupPath}", backupPath);

        PruneOldBackups(backupDirectory);

        return backupPath;
    }

    /// <summary>
    /// Returns the list of existing backup files, newest first.
    /// </summary>
    public List<BackupInfo> GetBackups(string backupDirectory)
    {
        if (!Directory.Exists(backupDirectory))
            return [];

        return Directory.GetFiles(backupDirectory, "arelia_backup_*.db")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(f => new BackupInfo(f.Name, f.Length, f.CreationTimeUtc))
            .ToList();
    }

    private void PruneOldBackups(string backupDirectory)
    {
        var files = Directory.GetFiles(backupDirectory, "arelia_backup_*.db")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .ToList();

        if (files.Count <= MaxBackups)
            return;

        foreach (var file in files.Skip(MaxBackups))
        {
            try
            {
                file.Delete();
                logger.LogInformation("Pruned old backup: {FileName}", file.Name);
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "Failed to prune backup: {FileName}", file.Name);
            }
        }
    }
}

public record BackupInfo(string FileName, long SizeBytes, DateTime CreatedAtUtc);
