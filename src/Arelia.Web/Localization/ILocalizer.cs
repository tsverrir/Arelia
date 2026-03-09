using Microsoft.AspNetCore.Components;

namespace Arelia.Web.Localization;

/// <summary>
/// Thin localizer wrapper over <c>IStringLocalizer&lt;SharedResource&gt;</c>.
/// Inject as <c>ILocalizer</c> in Razor components.
/// </summary>
public interface ILocalizer
{
    /// <summary>Returns the localised string for <paramref name="key"/>.</summary>
    string this[string key] { get; }

    /// <summary>Returns the localised string formatted with <paramref name="args"/>.</summary>
    string this[string key, params object[] args] { get; }

    /// <summary>
    /// Returns the localised string as <see cref="MarkupString"/>.
    /// When the admin highlight mode is active and the key is missing in the current
    /// non-default culture, the text is wrapped in a red-outlined span.
    /// </summary>
    MarkupString H(string key);

    /// <summary>
    /// Returns a formatted localised string as <see cref="MarkupString"/>.
    /// Applies the same highlight logic as <see cref="H(string)"/>.
    /// </summary>
    MarkupString H(string key, params object[] args);
}
