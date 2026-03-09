using Arelia.Application.Export;
using FluentAssertions;
using System.Text;

namespace Arelia.Application.Tests.Export;

public class CsvExporterTests
{
    private record TestRow(string Name, decimal Amount, DateTime Date);

    [Fact]
    public void WhenExportingThenOutputHasBom()
    {
        var data = new[] { new TestRow("Test", 100m, new DateTime(2025, 1, 15)) };

        var bytes = CsvExporter.Export(data,
            ("Name", r => r.Name),
            ("Amount", r => r.Amount),
            ("Date", r => r.Date));

        var bom = Encoding.UTF8.GetPreamble();
        bytes.Take(bom.Length).Should().BeEquivalentTo(bom);
    }

    [Fact]
    public void WhenExportingThenSemicolonDelimiterUsed()
    {
        var data = new[] { new TestRow("Test", 100m, new DateTime(2025, 1, 15)) };

        var bytes = CsvExporter.Export(data,
            ("Name", r => r.Name),
            ("Amount", r => r.Amount),
            ("Date", r => r.Date));

        var content = Encoding.UTF8.GetString(bytes);
        content.Should().Contain("Name;Amount;Date");
    }

    [Fact]
    public void WhenExportingDecimalThenCommaDecimalSeparator()
    {
        var data = new[] { new TestRow("Test", 1234.56m, new DateTime(2025, 1, 15)) };

        var bytes = CsvExporter.Export(data,
            ("Name", r => r.Name),
            ("Amount", r => r.Amount));

        var content = Encoding.UTF8.GetString(bytes);
        // Danish format: 1.234,56
        content.Should().Contain("1.234,56");
    }

    [Fact]
    public void WhenExportingDateThenIsoFormat()
    {
        var data = new[] { new TestRow("Test", 0m, new DateTime(2025, 3, 15)) };

        var bytes = CsvExporter.Export(data,
            ("Date", r => r.Date));

        var content = Encoding.UTF8.GetString(bytes);
        content.Should().Contain("2025-03-15");
    }

    [Fact]
    public void WhenValueContainsSemicolonThenQuoted()
    {
        var data = new[] { new TestRow("Test; value", 0m, DateTime.MinValue) };

        var bytes = CsvExporter.Export(data,
            ("Name", r => r.Name));

        var content = Encoding.UTF8.GetString(bytes);
        content.Should().Contain("\"Test; value\"");
    }
}
