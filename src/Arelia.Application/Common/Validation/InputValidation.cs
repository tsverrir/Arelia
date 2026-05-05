using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Arelia.Application.Common.Validation;

public static class InputValidation
{
    private static readonly EmailAddressAttribute EmailAddressAttribute = new();

    public static bool IsValidEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) || EmailAddressAttribute.IsValid(email.Trim());

    public static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool HasTextContent(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return false;

        var withoutTags = Regex.Replace(html, "<.*?>", string.Empty);
        var decoded = System.Net.WebUtility.HtmlDecode(withoutTags)
            .Replace("\u00a0", " ", StringComparison.Ordinal);

        return !string.IsNullOrWhiteSpace(decoded);
    }
}
