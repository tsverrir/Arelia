using Arelia.Web.Resources;
using Arelia.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using System.Globalization;
using System.Net;

namespace Arelia.Web.Localization;

/// <inheritdoc cref="ILocalizer"/>
public class Localizer : ILocalizer
{
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly AdminHighlightService _highlight;

    // Keys present in the current culture-specific resource (not the fallback).
    // Lazily populated once per culture name per instance.
    private string? _cachedCultureName;
    private HashSet<string>? _cultureKeys;

    public Localizer(IStringLocalizer<SharedResource> localizer, AdminHighlightService highlight)
    {
        _localizer = localizer;
        _highlight = highlight;
    }

    public string this[string key] => _localizer[key];

    public string this[string key, params object[] args] => _localizer[key, args];

    public MarkupString H(string key) =>
        Markup(_localizer[key], key);

    public MarkupString H(string key, params object[] args) =>
        Markup(_localizer[key, args], key);

    private MarkupString Markup(LocalizedString result, string key)
    {
        var text = WebUtility.HtmlEncode(result.Value);

        if (_highlight.ShowMissingTranslations && !IsDefaultCulture() && !HasKeyInCurrentCulture(key))
            return new MarkupString($"<span class=\"missing-translation\">{text}</span>");

        return new MarkupString(text);
    }

    private static bool IsDefaultCulture() =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            .StartsWith("en", StringComparison.OrdinalIgnoreCase);

    private bool HasKeyInCurrentCulture(string key)
    {
        var cultureName = CultureInfo.CurrentUICulture.Name;

        if (_cachedCultureName != cultureName)
        {
            _cultureKeys = [.._localizer.GetAllStrings(includeParentCultures: false).Select(s => s.Name)];
            _cachedCultureName = cultureName;
        }

        return _cultureKeys!.Contains(key);
    }
}
