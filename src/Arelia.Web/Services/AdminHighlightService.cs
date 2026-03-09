namespace Arelia.Web.Services;

/// <summary>
/// Scoped per-circuit service that controls the "highlight missing translations" admin mode.
/// When <see cref="ShowMissingTranslations"/> is true, <see cref="Localization.Localizer"/>
/// wraps any text that falls back to the default culture in a red-outlined span.
/// </summary>
public class AdminHighlightService
{
    public bool ShowMissingTranslations { get; private set; }

    public event Action? OnChanged;

    public void Toggle()
    {
        ShowMissingTranslations = !ShowMissingTranslations;
        OnChanged?.Invoke();
    }
}
