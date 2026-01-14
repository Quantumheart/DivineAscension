namespace DivineAscension.GUI.Events.Civilization;

/// <summary>
///     Events emitted from civilization list rendering
/// </summary>
internal abstract record ListEvent
{
    /// <summary>
    ///     User clicked on a civilization item (card or table row)
    /// </summary>
    internal sealed record ItemClicked(string CivId, float NewScrollY) : ListEvent;

    /// <summary>
    ///     User scrolled the list
    /// </summary>
    internal sealed record ScrollChanged(float NewScrollY) : ListEvent;
}