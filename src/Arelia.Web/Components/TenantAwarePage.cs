using Arelia.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Arelia.Web.Components;

/// <summary>
/// Base class for pages that need to reload their data when the active tenant changes.
/// Inherit from this class and override <see cref="OnTenantChangedAsync"/> to re-fetch data.
/// Call <see cref="RegisterTenantChangeHandler"/> at the start of your OnInitializedAsync.
/// </summary>
public abstract class TenantAwarePage : ComponentBase, IDisposable
{
    [Inject] protected TenantService TenantService { get; set; } = null!;

    /// <summary>
    /// Called on the UI thread whenever the active tenant changes after the initial render.
    /// Override to reload page data and call StateHasChanged if needed.
    /// </summary>
    protected virtual Task OnTenantChangedAsync() => Task.CompletedTask;

    /// <summary>
    /// Registers the tenant-change handler. Must be called in the page's OnInitializedAsync.
    /// </summary>
    protected void RegisterTenantChangeHandler()
    {
        TenantService.OnTenantChanged += HandleTenantChanged;
    }

    private void HandleTenantChanged()
    {
        _ = InvokeAsync(async () =>
        {
            await OnTenantChangedAsync();
            StateHasChanged();
        });
    }

    public virtual void Dispose()
    {
        TenantService.OnTenantChanged -= HandleTenantChanged;
    }
}
