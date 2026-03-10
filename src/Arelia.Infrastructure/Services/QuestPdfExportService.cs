using Arelia.Application.Documents.Queries;
using Arelia.Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.RegularExpressions;

namespace Arelia.Infrastructure.Services;

public class QuestPdfExportService : IPdfExportService
{
    static QuestPdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> ExportDocumentAsync(DocumentDetailDto document, CancellationToken ct = default)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);

                page.Content().Column(col =>
                {
                    RenderHtmlContent(col, document.ContentHtml);
                });

                page.Footer().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().AlignCenter().Text(text =>
                    {
                        text.Span("Page ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Medium);
                        text.Span(" / ").FontSize(9).FontColor(Colors.Grey.Medium);
                        text.TotalPages().FontSize(9).FontColor(Colors.Grey.Medium);
                    });
                });
            });
        }).GeneratePdf();

        return Task.FromResult(bytes);
    }

    private static void RenderHtmlContent(ColumnDescriptor col, string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return;

        var blocks = ParseHtmlBlocks(html);
        foreach (var block in blocks)
        {
            switch (block.Type)
            {
                case HtmlBlockType.H1:
                    col.Item().PaddingTop(12).PaddingBottom(4)
                        .Text(block.Text).FontSize(18).Bold();
                    break;
                case HtmlBlockType.H2:
                    col.Item().PaddingTop(10).PaddingBottom(4)
                        .Text(block.Text).FontSize(15).Bold();
                    break;
                case HtmlBlockType.H3:
                    col.Item().PaddingTop(8).PaddingBottom(2)
                        .Text(block.Text).FontSize(13).Bold();
                    break;
                case HtmlBlockType.Paragraph:
                    if (!string.IsNullOrWhiteSpace(block.Text))
                    {
                        col.Item().PaddingBottom(6)
                            .Text(block.Text).FontSize(11);
                    }
                    break;
                case HtmlBlockType.ListItem:
                    col.Item().PaddingLeft(16).PaddingBottom(2).Row(row =>
                    {
                        row.ConstantItem(12).Text("•").FontSize(11);
                        row.RelativeItem().Text(block.Text).FontSize(11);
                    });
                    break;
            }
        }
    }

    private static List<HtmlBlock> ParseHtmlBlocks(string html)
    {
        var blocks = new List<HtmlBlock>();

        // Normalise line endings
        html = html.Replace("\r\n", "\n").Replace("\r", "\n");

        // Extract block-level elements
        var blockPattern = new Regex(
            @"<(h[1-3]|p|li|ul|ol|div|blockquote)\b[^>]*>(.*?)</\1>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        int lastIndex = 0;
        foreach (Match match in blockPattern.Matches(html))
        {
            // Any text before this match
            if (match.Index > lastIndex)
            {
                var preText = StripTags(html[lastIndex..match.Index]);
                if (!string.IsNullOrWhiteSpace(preText))
                    blocks.Add(new HtmlBlock(HtmlBlockType.Paragraph, preText.Trim()));
            }

            var tag = match.Groups[1].Value.ToLowerInvariant();
            var inner = StripTags(match.Groups[2].Value).Trim();

            var blockType = tag switch
            {
                "h1" => HtmlBlockType.H1,
                "h2" => HtmlBlockType.H2,
                "h3" => HtmlBlockType.H3,
                "li" => HtmlBlockType.ListItem,
                _ => HtmlBlockType.Paragraph
            };

            // Skip ul/ol wrappers — their li children are already captured
            if (tag is not "ul" and not "ol" && !string.IsNullOrWhiteSpace(inner))
                blocks.Add(new HtmlBlock(blockType, inner));

            lastIndex = match.Index + match.Length;
        }

        // Remaining text
        if (lastIndex < html.Length)
        {
            var tail = StripTags(html[lastIndex..]);
            if (!string.IsNullOrWhiteSpace(tail))
                blocks.Add(new HtmlBlock(HtmlBlockType.Paragraph, tail.Trim()));
        }

        return blocks;
    }

    private static string StripTags(string input)
    {
        var stripped = Regex.Replace(input, "<[^>]+>", string.Empty);
        // Decode common HTML entities
        return stripped
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&nbsp;", " ")
            .Replace("&quot;", "\"")
            .Replace("&#39;", "'")
            .Trim();
    }

    private enum HtmlBlockType { Paragraph, H1, H2, H3, ListItem }

    private record HtmlBlock(HtmlBlockType Type, string Text);
}
