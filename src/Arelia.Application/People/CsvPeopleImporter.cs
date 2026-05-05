using Arelia.Application.Common.Validation;

namespace Arelia.Application.People;

/// <summary>
/// Parses a people CSV stream into a list of <see cref="PersonImportRow"/> records.
/// </summary>
/// <remarks>
/// Expected CSV format (header row required, all columns except FirstName and LastName are optional):
/// <code>
/// FirstName;LastName;Email;Phone;VoiceGroup;Notes
/// </code>
/// Both semicolons (;) and commas (,) are accepted as delimiters — the delimiter is
/// auto-detected from the header row.
/// The VoiceGroup column accepts: None, Soprano, Alto, Tenor, Bass, Other (case-insensitive).
/// Rows with a missing or blank FirstName or LastName are included but marked with
/// <see cref="PersonImportRow.HasError"/> = true so the user can correct them before committing.
/// </remarks>
public static class CsvPeopleImporter
{
    private static readonly string[] KnownHeaders =
        ["firstname", "lastname", "email", "phone", "voicegroup", "notes"];

    /// <summary>
    /// Parses the given stream and returns one <see cref="PersonImportRow"/> per data row.
    /// </summary>
    public static async Task<List<PersonImportRow>> ParseAsync(
        Stream csvStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(csvStream);

        using var reader = new StreamReader(csvStream, leaveOpen: true);

        var headerLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(headerLine))
            return [];

        var delimiter = DetectDelimiter(headerLine);
        var columnIndex = BuildColumnIndex(headerLine, delimiter);

        var rows = new List<PersonImportRow>();

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cells = SplitLine(line, delimiter);
            rows.Add(ParseRow(cells, columnIndex));
        }

        return rows;
    }

    private static char DetectDelimiter(string headerLine) =>
        headerLine.Contains(';') ? ';' : ',';

    private static Dictionary<string, int> BuildColumnIndex(string headerLine, char delimiter)
    {
        var headers = SplitLine(headerLine, delimiter);
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            var name = headers[i].Trim().Trim('"');
            if (!string.IsNullOrWhiteSpace(name))
                index.TryAdd(name, i);
        }
        return index;
    }

    private static PersonImportRow ParseRow(string[] cells, Dictionary<string, int> idx)
    {
        string? Get(string col) =>
            idx.TryGetValue(col, out var i) && i < cells.Length
                ? NormalizeCell(cells[i])
                : null;

        return new PersonImportRow
        {
            FirstName = Get("FirstName") ?? string.Empty,
            LastName  = Get("LastName")  ?? string.Empty,
            Email     = Get("Email"),
            Phone     = Get("Phone"),
            VoiceGroupName = ParseVoiceGroupName(Get("VoiceGroup")),
            Notes     = Get("Notes"),
        };
    }

    private static string? NormalizeCell(string raw)
    {
        raw = raw.Trim();
        if (raw.StartsWith('"') && raw.EndsWith('"') && raw.Length >= 2)
            raw = raw[1..^1].Replace("\"\"", "\"");
        return string.IsNullOrWhiteSpace(raw) ? null : raw;
    }

    /// <summary>
    /// Splits a CSV line respecting double-quoted fields that may contain the delimiter.
    /// </summary>
    private static string[] SplitLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check for escaped quote ("")
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == delimiter)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return [.. fields];
    }

    private static string? ParseVoiceGroupName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Represents a single row from the people import CSV, editable by the user before commit.</summary>
public class PersonImportRow
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public string? Email     { get; set; }
    public string? Phone     { get; set; }
    /// <summary>Raw voice group name from the CSV; resolved to a <see cref="VoiceGroupId"/> in the review UI.</summary>
    public string? VoiceGroupName { get; set; }
    /// <summary>Resolved voice group ID, set in the import review UI.</summary>
    public Guid? VoiceGroupId { get; set; }
    public string? Notes     { get; set; }

    /// <summary>True when the row is missing a required field and cannot be committed as-is.</summary>
    public bool HasError =>
        string.IsNullOrWhiteSpace(FirstName) ||
        string.IsNullOrWhiteSpace(LastName) ||
        !InputValidation.IsValidEmail(Email);
}
