using Arelia.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.JSInterop;
using System.Globalization;
using System.Security.Claims;

namespace Arelia.Web.Services;

/// <summary>
/// Manages the active UI culture for the Blazor circuit.
/// Culture is persisted in a browser cookie read by RequestLocalizationMiddleware.
/// Call <see cref="TryInitializeAsync"/> once in OnAfterRenderAsync(firstRender).
/// </summary>
public class CultureService(
    IMediator mediator,
    AuthenticationStateProvider authStateProvider,
    TenantService tenantService,
    NavigationManager navigation,
    IJSRuntime jsRuntime)
{
    private const string CultureCookieName = ".AspNetCore.Culture";

    public static readonly string[] SupportedCultures = ["en", "da", "is"];

    public string CurrentCultureName =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    /// <summary>
    /// Reads the user's language preference from the DB and, if it differs from the
    /// current cookie culture, sets the cookie and reloads the page.
    /// </summary>
    public async Task TryInitializeAsync()
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return;

        var effectiveCulture = await mediator.Send(
            new GetUserLanguagePreferenceQuery(userId, tenantService.CurrentOrganizationId));

        if (!string.Equals(effectiveCulture, CurrentCultureName, StringComparison.OrdinalIgnoreCase))
            await ApplyCultureAsync(effectiveCulture);
    }

    /// <summary>
    /// Persists <paramref name="cultureName"/> as the user's preference via cookie and reloads.
    /// </summary>
    public async Task SetCultureAsync(string userId, string cultureName)
    {
        await mediator.Send(
            new Application.Users.Commands.SetUserLanguagePreferenceCommand(userId, cultureName));

        await ApplyCultureAsync(cultureName);
    }

    private async Task ApplyCultureAsync(string cultureName)
    {
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(cultureName));
        try
        {
            await jsRuntime.InvokeVoidAsync(
                "document.cookie = arguments[0]",
                $"{CultureCookieName}={cookieValue};path=/;samesite=lax");
        }
        catch
        {
            // JS not available during prerender — skip
            return;
        }

        navigation.NavigateTo(navigation.Uri, forceLoad: true);
    }
}
