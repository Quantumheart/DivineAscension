namespace DivineAscension.GUI.Events.Blessing;

public abstract record InfoEvent
{
    /// <summary>
    ///     User clicked the "Read more ▾" / "Read less ▴" toggle on the selected blessing's
    ///     description block.
    /// </summary>
    public sealed record DescriptionExpansionToggled : InfoEvent;
}