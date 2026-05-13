// Copyright (c) 2026 JBT Marel. All rights reserved.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Arelia.Domain.Enums;

namespace Arelia.Web.Extensions;

/// <summary>
/// Extension methods for the <see cref="Permission"/> enum.
/// </summary>
public static class PermissionExtensions
{
    /// <summary>
    /// Returns the human-readable display name for a permission
    /// (from the <see cref="DisplayAttribute"/> if present, otherwise the enum value name).
    /// </summary>
    public static string GetDisplayName(this Permission permission) =>
        permission.GetType()
            .GetField(permission.ToString())
            ?.GetCustomAttribute<DisplayAttribute>()
            ?.Name
        ?? permission.ToString();
}
