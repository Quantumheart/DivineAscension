namespace PantheonWars.GUI.Events;

public abstract record BlessingTreeEvent
{
    /// <summary>
    ///     Emitted when tree scroll position changes
    /// </summary>
    public sealed record PlayerTreeScrollChanged(float ScrollX, float ScrollY) : BlessingTreeEvent;

    public sealed record ReligionTreeScrollChanged(float ScrollX, float ScrollY) : BlessingTreeEvent;

    /// <summary>
    ///     Emitted when a blessing node is clicked
    /// </summary>
    public sealed record BlessingSelected(string BlessingId) : BlessingTreeEvent;

    /// <summary>
    ///     Emitted when mouse hovers over a blessing
    /// </summary>
    public sealed record BlessingHovered(string? BlessingId) : BlessingTreeEvent;
}