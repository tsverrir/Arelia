using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Arelia.Infrastructure.Services;

/// <summary>
/// Manages SQLite database backups — manual trigger with FIFO retention.
/// </summary>
public partial class BackupService(ILogger<BackupService> logger)
{
    private const int MaxBackups = 30;

    /// <summary>Prefix used for all backup filenames.</summary>
    private const string BackupPrefix = "arelia_backup_";

    /// <summary>
    /// Matches filenames like <c>arelia_backup_20260310_123456_v20260310112654_Name.db</c>.
    /// Group 1 = backup timestamp, Group 2 = migration id.
    /// </summary>
    [GeneratedRegex(@"^arelia_backup_(\d{8}_\d{6})_v(.+)\.db$")]
    private static partial Regex VersionedFileNameRegex();

    /// <summary>
    /// Returns the last applied EF Core migration id for the given <paramref name="context"/>,
    /// or <c>null</c> if no migrations have been applied.
    /// </summary>
    public static async Task<string?> GetCurrentDatabaseVersionAsync(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var applied = await context.Database.GetAppliedMigrationsAsync();
        return applied.LastOrDefault();
    }

    /// <summary>
    /// Creates a backup of the SQLite database file.
    /// The database version (last applied migration) is embedded in the filename.
    /// </summary>
    public async Task<string> CreateBackupAsync(string databasePath, string backupDirectory, string? databaseVersion)
    {
        ArgumentNullException.ThrowIfNull(databasePath);
        ArgumentNullException.ThrowIfNull(backupDirectory);

        if (!File.Exists(databasePath))
            throw new FileNotFoundException("Database file not found.", databasePath);

        Directory.CreateDirectory(backupDirectory);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var versionSuffix = string.IsNullOrWhiteSpace(databaseVersion) ? "" : $"_v{databaseVersion}";
        var backupFileName = $"{BackupPrefix}{timestamp}{versionSuffix}.db";
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
    /// Backups whose embedded version does not match <paramref name="currentDatabaseVersion"/>
    /// are marked as invalid.
    /// </summary>
    public List<BackupInfo> GetBackups(string backupDirectory, string? currentDatabaseVersion)
    {
        if (!Directory.Exists(backupDirectory))
            return [];

        return Directory.GetFiles(backupDirectory, $"{BackupPrefix}*.db")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(f => ToBackupInfo(f, currentDatabaseVersion))
            .ToList();
    }

    /// <summary>
    /// Restores a backup by replacing the active database file.
    /// The caller is responsible for entering maintenance mode and restarting afterwards.
    /// </summary>
    public async Task RestoreBackupAsync(string databasePath, string backupDirectory, string backupFileName)
    {
        ArgumentNullException.ThrowIfNull(databasePath);
        ArgumentNullException.ThrowIfNull(backupDirectory);
        ArgumentNullException.ThrowIfNull(backupFileName);

        // Prevent path traversal by ensuring the filename has no directory components
        if (backupFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            backupFileName.Contains("..") ||
            Path.GetFileName(backupFileName) != backupFileName)
        {
            throw new ArgumentException("Invalid backup filename.", nameof(backupFileName));
        }

        var backupPath = Path.GetFullPath(Path.Combine(backupDirectory, backupFileName));
        if (!backupPath.StartsWith(Path.GetFullPath(backupDirectory), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Access to the specified backup path is denied.");

        if (!File.Exists(backupPath))
            throw new FileNotFoundException("Backup file not found.", backupPath);

        await using var source = new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var dest = new FileStream(databasePath, FileMode.Create, FileAccess.Write);
        await source.CopyToAsync(dest);

        logger.LogInformation("Database restored from backup: {BackupPath}", backupPath);
    }

    /// <summary>
    /// Deletes all backups whose database version does not match <paramref name="currentDatabaseVersion"/>.
    /// Returns the number of deleted files.
    /// </summary>
    public int DeleteInvalidBackups(string backupDirectory, string? currentDatabaseVersion)
    {
        var backups = GetBackups(backupDirectory, currentDatabaseVersion);
        var invalid = backups.Where(b => !b.IsValid).ToList();
        var deleted = 0;

        foreach (var backup in invalid)
        {
            var fullPath = Path.Combine(backupDirectory, backup.FileName);
            try
            {
                File.Delete(fullPath);
                deleted++;
                logger.LogInformation("Deleted invalid backup: {FileName}", backup.FileName);
            }
            catch (IOException ex)
            {
                logger.LogWarning(ex, "Failed to delete invalid backup: {FileName}", backup.FileName);
            }
        }

        return deleted;
    }

    private static BackupInfo ToBackupInfo(FileInfo file, string? currentDatabaseVersion)
    {
        var match = VersionedFileNameRegex().Match(file.Name);
        string? embeddedVersion = match.Success ? match.Groups[2].Value : null;

        // A backup is valid when its embedded version matches the current DB version.
        // Backups without a version (legacy) are always considered invalid.
        var isValid = embeddedVersion is not null
                      && string.Equals(embeddedVersion, currentDatabaseVersion, StringComparison.Ordinal);

        return new BackupInfo(file.Name, file.Length, file.CreationTimeUtc, embeddedVersion, isValid);
    }

    private void PruneOldBackups(string backupDirectory)
    {
        var files = Directory.GetFiles(backupDirectory, $"{BackupPrefix}*.db")
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

/// <param name="FileName">Backup file name.</param>
/// <param name="SizeBytes">File size in bytes.</param>
/// <param name="CreatedAtUtc">UTC creation timestamp.</param>
/// <param name="DatabaseVersion">The EF Core migration id embedded in the filename, or null for legacy backups.</param>
/// <param name="IsValid">Whether this backup matches the current database version.</param>
public record BackupInfo(string FileName, long SizeBytes, DateTime CreatedAtUtc, string? DatabaseVersion, bool IsValid);
