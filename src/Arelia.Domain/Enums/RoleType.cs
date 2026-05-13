// Copyright (c) 2026 JBT Marel. All rights reserved.
namespace Arelia.Domain.Enums;

/// <summary>
/// Identifies the nature of a Role within an organization.
/// </summary>
public enum RoleType
{
    /// <summary>Full access. Permissions are hard-coded; RolePermission rows are ignored.</summary>
    Admin,

    /// <summary>Pre-seeded with default management permissions. Org-editable.</summary>
    Board,

    /// <summary>Pre-seeded with default member permissions. Org-editable.</summary>
    Member,

    /// <summary>Org-defined role. Starts with no permissions; freely assigned.</summary>
    Custom,
}
