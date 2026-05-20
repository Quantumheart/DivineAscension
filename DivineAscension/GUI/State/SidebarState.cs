using System.Collections.Generic;

namespace DivineAscension.GUI.State;

/// <summary>
///     Runtime state for the sidebar nav surface. Persisted bits live in
///     <c>UiPrefs</c>; this object holds the live, in-session view.
/// </summary>
public class SidebarState
{
    public bool IsCollapsed { get; set; }

    public Dictionary<string, bool> CollapsedGroups { get; } = new();

    public SidebarNavId CurrentNav { get; set; } = SidebarNavId.ReligionInfo;

    public void Reset()
    {
        IsCollapsed = false;
        CollapsedGroups.Clear();
        CurrentNav = SidebarNavId.ReligionInfo;
    }
}
