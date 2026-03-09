using Arelia.Domain.Enums;

namespace Arelia.Application.Authorization;

/// <summary>
/// Attribute to mark a Blazor page or component with a required permission.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute(Permission permission) : Attribute
{
    public Permission Permission { get; } = permission;
}
