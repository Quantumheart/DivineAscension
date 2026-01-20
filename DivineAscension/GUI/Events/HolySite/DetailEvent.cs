namespace DivineAscension.GUI.Events.HolySite;

/// <summary>
/// Events for the holy sites detail view
/// </summary>
public abstract record DetailEvent
{
    /// <summary>
    /// User clicked the back button to return to browse view
    /// </summary>
    public sealed record BackToBrowseClicked : DetailEvent;

    /// <summary>
    /// User clicked the Mark button to add waypoint to map
    /// </summary>
    public sealed record MarkClicked : DetailEvent;

    /// <summary>
    /// User clicked the rename button/icon
    /// </summary>
    public sealed record RenameClicked : DetailEvent;

    /// <summary>
    /// User typed in the rename input field
    /// </summary>
    public sealed record RenameValueChanged(string NewValue) : DetailEvent;

    /// <summary>
    /// User clicked save on the rename edit
    /// </summary>
    public sealed record RenameSave(string NewName) : DetailEvent;

    /// <summary>
    /// User clicked cancel on the rename edit
    /// </summary>
    public sealed record RenameCancel : DetailEvent;

    /// <summary>
    /// User clicked the edit description button
    /// </summary>
    public sealed record EditDescriptionClicked : DetailEvent;

    /// <summary>
    /// User typed in the description input field
    /// </summary>
    public sealed record DescriptionValueChanged(string NewValue) : DetailEvent;

    /// <summary>
    /// User clicked save on the description edit
    /// </summary>
    public sealed record DescriptionSave(string Description) : DetailEvent;

    /// <summary>
    /// User clicked cancel on the description edit
    /// </summary>
    public sealed record DescriptionCancel : DetailEvent;
}
