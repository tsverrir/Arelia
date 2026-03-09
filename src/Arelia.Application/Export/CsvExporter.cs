using System.Globalization;
using System.Text;

namespace Arelia.Application.Export;

/// <summary>
/// Generates CSV content with European formatting (semicolon delimiter, comma decimal separator, UTF-8 BOM).
/// </summary>
public static class CsvExporter
{
    private const char Delimiter = ';';
    private static readonly CultureInfo DanishCulture = new("da-DK");

    /// <summary>
    /// Exports a list of objects to a CSV byte array.
    /// </summary>
    public static byte[] Export<T>(IEnumerable<T> items, params (string Header, Func<T, object?> Selector)[] columns)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine(string.Join(Delimiter, columns.Select(c => Escape(c.Header))));

        // Rows
        foreach (var item in items)
        {
            var values = columns.Select(c => FormatValue(c.Selector(item)));
            sb.AppendLine(string.Join(Delimiter, values));
        }

        // UTF-8 with BOM for Excel compatibility
        var preamble = Encoding.UTF8.GetPreamble();
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + content.Length];
        preamble.CopyTo(result, 0);
        content.CopyTo(result, preamble.Length);

        return result;
    }

    private static string FormatValue(object? value) => value switch
    {
        null => "",
        decimal d => Escape(d.ToString("N2", DanishCulture)),
        double d => Escape(d.ToString("N2", DanishCulture)),
        DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        bool b => b ? "Ja" : "Nej",
        _ => Escape(value.ToString() ?? ""),
    };

    private static string Escape(string value)
    {
        if (value.Contains(Delimiter) || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
