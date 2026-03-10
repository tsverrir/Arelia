namespace Arelia.Application.Interfaces;

public interface IHtmlSanitizerService
{
    /// <summary>Sanitizes HTML, stripping unsafe tags and attributes.</summary>
    string Sanitize(string html);
}
