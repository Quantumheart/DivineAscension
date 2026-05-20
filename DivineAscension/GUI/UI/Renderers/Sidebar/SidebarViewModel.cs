using System.Collections.Generic;
using DivineAscension.GUI.State;

namespace DivineAscension.GUI.UI.Renderers.Sidebar;

/// <summary>
///     Top-level view model handed to <c>SidebarRenderer</c>. Built by
///     <c>SidebarNavMapper</c> from <c>GuiDialogManager</c> + <c>SidebarState</c>.
/// </summary>
public sealed record SidebarViewModel(
    bool IsCollapsed,
    SidebarNavId CurrentNav,
    IReadOnlyList<SidebarGroupViewModel> Groups
);

/// <summary>
///     One collapsible group inside the sidebar (e.g. "Religion", "Civilization").
///     <paramref name="Key" /> is the persisted identifier used by <c>SidebarState.CollapsedGroups</c>.
/// </summary>
public sealed record SidebarGroupViewModel(
    string Key,
    string Label,
    bool IsCollapsed,
    IReadOnlyList<SidebarItemViewModel> Items
);

/// <summary>
///     One nav row. Renders as label + icon + optional badge. When
///     <see cref="IsDisabled" /> is true, the row is dimmed and shows
///     <see cref="DisabledTooltipKey" /> on hover; clicks are swallowed.
/// </summary>
public sealed record SidebarItemViewModel(
    SidebarNavId Id,
    string Label,
    string IconName,
    int Badge,
    bool IsActive,
    bool IsDisabled,
    string? DisabledTooltipKey
);

/// <summary>
///     Events emitted by <c>SidebarRenderer</c> for the layout coordinator to apply.
/// </summary>
public abstract record SidebarEvent
{
    /// <summary>User clicked an enabled item — switch nav.</summary>
    public sealed record ItemClicked(SidebarNavId Id) : SidebarEvent;

    /// <summary>User clicked a group header chevron — toggle collapse on that key.</summary>
    public sealed record GroupToggled(string Key) : SidebarEvent;

    /// <summary>User clicked the hide-sidebar button — toggle collapsed strip mode.</summary>
    public sealed record SidebarToggled : SidebarEvent;
}
