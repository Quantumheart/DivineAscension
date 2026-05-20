using DivineAscension.GUI.State;

namespace DivineAscension.GUI.Events.EdgeBookmarks;

/// <summary>
///     Events emitted by <c>EdgeBookmarkRenderer</c>. The layout coordinator
///     applies these against <c>SidebarState.CurrentNav</c> and the
///     per-destination refresh path used by the sidebar.
/// </summary>
public abstract record EdgeBookmarkEvent
{
    /// <summary>User clicked a bookmark; jump to that section's default nav.</summary>
    public sealed record Jump(SidebarNavId Target) : EdgeBookmarkEvent;
}
