using Microsoft.JSInterop;
using MudBlazor;

namespace Arelia.Web.Services;

public sealed record UiSkinOption(
    string Key,
    string Name,
    string Inspiration,
    string Description,
    string CssClass,
    MudTheme Theme);

public sealed class UiSkinService(IJSRuntime jsRuntime, ILogger<UiSkinService> logger)
{
    private const string StorageKey = "arelia_ui_skin";

    public IReadOnlyList<UiSkinOption> Skins { get; } =
    [
        CreateSoftWorkspace(),
        CreateFriendlyOperations(),
        CreateCleanFinance(),
        CreateFocusedDeveloper(),
        CreateMaterialCalm(),
    ];

    public UiSkinOption CurrentSkin { get; private set; } = CreateSoftWorkspace();
    public MudTheme CurrentTheme => CurrentSkin.Theme;

    public event Action? OnSkinChanged;

    public async Task TryInitializeAsync()
    {
        string? storedKey;

        try
        {
            storedKey = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        }
        catch (JSException)
        {
            logger.LogDebug("UI skin could not be read because JavaScript interop is unavailable.");
            return;
        }
        catch (InvalidOperationException)
        {
            logger.LogDebug("UI skin could not be read during prerendering.");
            return;
        }

        if (string.IsNullOrWhiteSpace(storedKey))
        {
            await TryApplyDocumentSkinAsync();
            return;
        }

        if (FindSkin(storedKey) is null)
        {
            logger.LogDebug("Stored UI skin '{SkinKey}' is not available; falling back to the default skin.", storedKey);
            await TryClearStoredSkinAsync();
            await TryApplyDocumentSkinAsync();
            return;
        }

        ApplySkin(storedKey);
        await TryApplyDocumentSkinAsync();
    }

    public async Task SetSkinAsync(string key)
    {
        if (!ApplySkin(key))
            return;

        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, CurrentSkin.Key);
            await jsRuntime.InvokeVoidAsync("areliaInterop.setDocumentSkin", CurrentSkin.Key);
        }
        catch (JSException)
        {
            logger.LogDebug("UI skin was changed in-memory but could not be persisted because JavaScript interop is unavailable.");
            return;
        }
        catch (InvalidOperationException)
        {
            logger.LogDebug("UI skin was changed in-memory but could not be persisted during prerendering.");
            return;
        }
    }

    private async Task TryClearStoredSkinAsync()
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
        }
        catch (JSException)
        {
            logger.LogDebug("Stored UI skin could not be cleared because JavaScript interop is unavailable.");
        }
        catch (InvalidOperationException)
        {
            logger.LogDebug("Stored UI skin could not be cleared during prerendering.");
        }
    }

    private async Task TryApplyDocumentSkinAsync()
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("areliaInterop.setDocumentSkin", CurrentSkin.Key);
        }
        catch (JSException)
        {
            logger.LogDebug("Document skin attribute could not be applied because JavaScript interop is unavailable.");
        }
        catch (InvalidOperationException)
        {
            logger.LogDebug("Document skin attribute could not be applied during prerendering.");
        }
    }

    private UiSkinOption? FindSkin(string key) =>
        Skins.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));

    private bool ApplySkin(string key)
    {
        var selected = FindSkin(key);
        if (selected is null || selected.Key == CurrentSkin.Key)
            return false;

        CurrentSkin = selected;
        OnSkinChanged?.Invoke();
        return true;
    }

    private static MudTheme CreateTheme(
        string primary,
        string secondary,
        string tertiary,
        string background,
        string backgroundGray,
        string surface,
        string drawerBackground,
        string drawerText,
        string appbarBackground,
        string appbarText,
        string textPrimary,
        string textSecondary,
        string lines,
        string tableHover,
        string defaultBorderRadius,
        string info,
        string success,
        string warning,
        string error) =>
        new()
        {
            PaletteLight = new PaletteLight
            {
                Primary = primary,
                PrimaryContrastText = "#ffffff",
                Secondary = secondary,
                SecondaryContrastText = "#ffffff",
                Tertiary = tertiary,
                TertiaryContrastText = "#ffffff",
                Background = background,
                BackgroundGray = backgroundGray,
                Surface = surface,
                DrawerBackground = drawerBackground,
                DrawerText = drawerText,
                DrawerIcon = drawerText,
                AppbarBackground = appbarBackground,
                AppbarText = appbarText,
                TextPrimary = textPrimary,
                TextSecondary = textSecondary,
                LinesDefault = lines,
                LinesInputs = lines,
                TableLines = lines,
                TableHover = tableHover,
                TableStriped = backgroundGray,
                Divider = lines,
                DividerLight = lines,
                Info = info,
                Success = success,
                Warning = warning,
                Error = error,
                Dark = textPrimary,
                ActionDefault = textSecondary,
                HoverOpacity = 0.06,
                BorderOpacity = 0.16,
            },
            Typography = CreateTypography(),
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = defaultBorderRadius,
                AppbarHeight = "72px",
                DrawerWidthLeft = "284px",
            },
        };

    private static Typography CreateTypography()
    {
        var fontFamily = new[] { "Inter", "Roboto", "system-ui", "-apple-system", "BlinkMacSystemFont", "Segoe UI", "sans-serif" };

        return new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = fontFamily,
                FontSize = "0.96rem",
                LineHeight = "1.6",
                LetterSpacing = "-0.006em",
            },
            H1 = new H1Typography { FontFamily = fontFamily, FontSize = "2.4rem", FontWeight = "700", LineHeight = "1.14", LetterSpacing = "-0.035em" },
            H2 = new H2Typography { FontFamily = fontFamily, FontSize = "2rem", FontWeight = "700", LineHeight = "1.18", LetterSpacing = "-0.03em" },
            H3 = new H3Typography { FontFamily = fontFamily, FontSize = "1.72rem", FontWeight = "700", LineHeight = "1.22", LetterSpacing = "-0.026em" },
            H4 = new H4Typography { FontFamily = fontFamily, FontSize = "1.48rem", FontWeight = "650", LineHeight = "1.28", LetterSpacing = "-0.02em" },
            H5 = new H5Typography { FontFamily = fontFamily, FontSize = "1.24rem", FontWeight = "650", LineHeight = "1.34", LetterSpacing = "-0.014em" },
            H6 = new H6Typography { FontFamily = fontFamily, FontSize = "1.08rem", FontWeight = "650", LineHeight = "1.38", LetterSpacing = "-0.01em" },
            Subtitle1 = new Subtitle1Typography { FontFamily = fontFamily, FontSize = "0.98rem", FontWeight = "600", LineHeight = "1.5" },
            Subtitle2 = new Subtitle2Typography { FontFamily = fontFamily, FontSize = "0.9rem", FontWeight = "600", LineHeight = "1.45" },
            Body1 = new Body1Typography { FontFamily = fontFamily, FontSize = "0.96rem", LineHeight = "1.6" },
            Body2 = new Body2Typography { FontFamily = fontFamily, FontSize = "0.88rem", LineHeight = "1.56" },
            Button = new ButtonTypography { FontFamily = fontFamily, FontSize = "0.86rem", FontWeight = "650", LetterSpacing = "0.005em", TextTransform = "none" },
            Caption = new CaptionTypography { FontFamily = fontFamily, FontSize = "0.76rem", LineHeight = "1.45", LetterSpacing = "0.01em" },
            Overline = new OverlineTypography { FontFamily = fontFamily, FontSize = "0.72rem", FontWeight = "700", LetterSpacing = "0.08em" },
        };
    }

    private static UiSkinOption CreateSoftWorkspace() =>
        new(
            "soft-workspace",
            "Soft Workspace",
            "Calm productivity",
            "Warm neutrals, gentle borders, and document-like readability.",
            "skin-soft-workspace",
            CreateTheme("#6f6256", "#8b735f", "#b98962", "#f7f3ec", "#efe8df", "#fffdf9", "#fbf7f0", "#5d544c", "#fffaf2", "#3f3832", "#342f2a", "#6a6057", "#ded4c7", "rgba(111, 98, 86, 0.08)", "18px", "#55718a", "#2f765f", "#aa7138", "#b24d55"));

    private static UiSkinOption CreateFriendlyOperations() =>
        new(
            "friendly-operations",
            "Friendly Operations",
            "Approachable consumer flows",
            "Airy cards, warm accents, and friendly member-facing energy.",
            "skin-friendly-operations",
            CreateTheme("#e05b63", "#2f8f83", "#f2a65a", "#fff7f5", "#fce9e4", "#ffffff", "#fff3ef", "#59423f", "#fff4f0", "#4d3734", "#332826", "#71524f", "#efd5ce", "rgba(224, 91, 99, 0.09)", "22px", "#287c8f", "#2f8f83", "#c27a23", "#c94f5d"));

    private static UiSkinOption CreateCleanFinance() =>
        new(
            "clean-finance",
            "Clean Finance",
            "Polished business dashboards",
            "Cool blues, refined surfaces, and precise data hierarchy.",
            "skin-clean-finance",
            CreateTheme("#635bff", "#00a3ff", "#7c3aed", "#f6f8ff", "#edf1ff", "#ffffff", "#f7f8ff", "#3d4566", "#fbfcff", "#222945", "#171b2d", "#5d647f", "#dbe1f5", "rgba(99, 91, 255, 0.08)", "16px", "#2563eb", "#14805e", "#b26a00", "#c2414b"));

    private static UiSkinOption CreateFocusedDeveloper() =>
        new(
            "focused-developer",
            "Focused Developer",
            "Practical information design",
            "Neutral grays, crisp focus states, and readable dense workflows.",
            "skin-focused-developer",
            CreateTheme("#0969da", "#8250df", "#1f883d", "#f6f8fa", "#eef2f6", "#ffffff", "#ffffff", "#3f4b5f", "#ffffff", "#24292f", "#24292f", "#57606a", "#d0d7de", "rgba(9, 105, 218, 0.07)", "12px", "#0969da", "#1f883d", "#9a6700", "#cf222e"));

    private static UiSkinOption CreateMaterialCalm() =>
        new(
            "material-calm",
            "Material Calm",
            "Familiar accessible components",
            "Balanced color roles, clear states, and predictable interaction feedback.",
            "skin-material-calm",
            CreateTheme("#1a73e8", "#018786", "#6750a4", "#f8fafd", "#edf2f7", "#ffffff", "#f6f9fc", "#3c4858", "#ffffff", "#263238", "#202124", "#5f6368", "#d8dee9", "rgba(26, 115, 232, 0.08)", "20px", "#1a73e8", "#188038", "#b06000", "#d93025"));
}
