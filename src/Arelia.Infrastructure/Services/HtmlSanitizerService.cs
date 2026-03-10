using Arelia.Application.Interfaces;
using Ganss.Xss;

namespace Arelia.Infrastructure.Services;

public class HtmlSanitizerService : IHtmlSanitizerService
{
    private static readonly HtmlSanitizer Sanitizer = BuildSanitizer();

    public string Sanitize(string html) => Sanitizer.Sanitize(html);

    private static HtmlSanitizer BuildSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[]
        {
            "p", "strong", "em", "u", "h1", "h2", "h3",
            "ul", "ol", "li", "a", "br",
            "table", "thead", "tbody", "tr", "th", "td",
            "span", "div", "blockquote"
        })
        {
            sanitizer.AllowedTags.Add(tag);
        }

        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.Add("href");
        sanitizer.AllowedAttributes.Add("class");
        sanitizer.AllowedAttributes.Add("style");

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("mailto");

        return sanitizer;
    }
}
