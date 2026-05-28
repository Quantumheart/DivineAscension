using System;

namespace DivineAscension.GUI.State.Religion;

/// <summary>
///     Per-pane state for the Sacred Calendar editor (#422). Lives on
///     <see cref="InfoState"/> alongside the other read-mostly religion state.
/// </summary>
public class SacredCalendarState
{
    public bool AddDialogOpen { get; set; }
    public string AddName { get; set; } = string.Empty;
    public int AddMonth { get; set; } = 1;
    public int AddDay { get; set; } = 1;

    /// <summary>The Guid of the feast pending removal-confirmation, or null.</summary>
    public Guid? RemoveConfirmFeastId { get; set; }
    public string? RemoveConfirmFeastName { get; set; }

    /// <summary>Last error message from the server, surfaced as a banner.</summary>
    public string? LastErrorMessage { get; set; }

    public void Reset()
    {
        AddDialogOpen = false;
        AddName = string.Empty;
        AddMonth = 1;
        AddDay = 1;
        RemoveConfirmFeastId = null;
        RemoveConfirmFeastName = null;
        LastErrorMessage = null;
    }
}
