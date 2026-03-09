using Arelia.Application.Interfaces;
using Arelia.Application.Organizations.Queries;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Arelia.Web.Services;

/// <summary>
/// Manages the current tenant context for the Blazor circuit.
/// Selected org is held in-memory per circuit and persisted to localStorage for cross-refresh continuity.
/// </summary>
public class TenantService(
    ITenantContext tenantContext,
    AuthenticationStateProvider authStateProvider,
    IMediator mediator,
    IJSRuntime jsRuntime)
{
    private const string OrgIdKey = "arelia_org_id";
    private const string OrgNameKey = "arelia_org_name";

    private List<OrganizationDto>? _userOrgs;

    public Guid? CurrentOrganizationId => tenantContext.CurrentOrganizationId;
    public string? CurrentOrganizationName { get; private set; }

    public event Action? OnTenantChanged;

    public void SetTenant(Guid organizationId, string organizationName)
    {
        tenantContext.SetOrganization(organizationId);
        CurrentOrganizationName = organizationName;
        _ = PersistAsync(organizationId, organizationName);
        OnTenantChanged?.Invoke();
    }

    /// <summary>
    /// Sets the current user ID, caches the user's organizations, and auto-selects when there is
    /// exactly one. Must be called once per circuit during OnInitializedAsync.
    /// </summary>
    public async Task InitializeAsync()
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId is null)
            return;

        if (tenantContext is Infrastructure.Services.TenantContext tc)
            tc.CurrentUserId = userId;

        if (tenantContext.CurrentOrganizationId is not null)
            return;

        _userOrgs = await mediator.Send(new GetUserOrganizationsQuery(userId));

        if (_userOrgs.Count == 1)
            SetTenant(_userOrgs[0].Id, _userOrgs[0].Name);
    }

    /// <summary>
    /// Reads the previously selected org from localStorage and restores it if still valid.
    /// Must be called after the browser connection is available (OnAfterRenderAsync firstRender).
    /// </summary>
    public async Task TryRestoreFromStorageAsync()
    {
        if (tenantContext.CurrentOrganizationId is not null || _userOrgs is null)
            return;

        try
        {
            var storedIdStr = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", OrgIdKey);
            if (!Guid.TryParse(storedIdStr, out var storedId))
                return;

            var match = _userOrgs.FirstOrDefault(o => o.Id == storedId);
            if (match is not null)
                SetTenant(match.Id, match.Name);
        }
        catch
        {
            // JS interop not available (e.g. during prerendering)
        }
    }

    private async Task PersistAsync(Guid orgId, string orgName)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", OrgIdKey, orgId.ToString());
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", OrgNameKey, orgName);
        }
        catch { }
    }
}
