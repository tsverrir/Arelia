using Microsoft.AspNetCore.Components;

namespace Arelia.Web.Components.Shared;

/// <summary>
/// Defines a column for use with <see cref="ColumnChooser{TItem}"/>.
/// </summary>
public record ColumnDefinition<TItem>(
    string Key,
    string Title,
    Func<TItem, object?> ValueSelector,
    bool Visible = true,
    Func<TItem, object>? SortBy = null);
