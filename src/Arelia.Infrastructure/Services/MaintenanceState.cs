namespace Arelia.Infrastructure.Services;

/// <summary>
/// Tracks whether the application is in maintenance mode (e.g. during a backup restore).
/// Registered as a singleton so all requests see the same flag.
/// </summary>
public sealed class MaintenanceState
{
    private volatile bool _isInMaintenance;

    /// <summary>Gets whether the application is currently in maintenance mode.</summary>
    public bool IsInMaintenance => _isInMaintenance;

    /// <summary>Puts the application into maintenance mode.</summary>
    public void Enter() => _isInMaintenance = true;

    /// <summary>Takes the application out of maintenance mode.</summary>
    public void Exit() => _isInMaintenance = false;
}
