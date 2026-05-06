using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Arelia.Web.Services;

public sealed class NavigationHistoryService(NavigationManager navigation) : IDisposable
{
    private string? _currentUri;
    private string? _previousUri;
    private bool _isInitialized;

    public bool CanGoBack => !string.IsNullOrWhiteSpace(_previousUri) && !IsCurrentUri(GetPreviousHref() ?? string.Empty);

    public event Action? OnChanged;

    public void Initialize()
    {
        if (_isInitialized)
            return;

        _currentUri = navigation.Uri;
        navigation.LocationChanged += OnLocationChanged;
        _isInitialized = true;
    }

    public void NavigateBack(string fallbackHref)
    {
        var target = GetPreviousHref();
        if (string.IsNullOrWhiteSpace(target) || IsCurrentUri(target))
            target = fallbackHref;

        navigation.NavigateTo(target);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        if (!string.Equals(_currentUri, args.Location, StringComparison.Ordinal))
            _previousUri = _currentUri;

        _currentUri = args.Location;
        OnChanged?.Invoke();
    }

    private string? GetPreviousHref()
    {
        if (string.IsNullOrWhiteSpace(_previousUri))
            return null;

        var relative = navigation.ToBaseRelativePath(_previousUri);
        return string.IsNullOrWhiteSpace(relative) ? "/" : $"/{relative}";
    }

    private bool IsCurrentUri(string href)
    {
        var absolute = navigation.ToAbsoluteUri(href).ToString();
        return string.Equals(absolute, _currentUri, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (_isInitialized)
            navigation.LocationChanged -= OnLocationChanged;
    }
}
