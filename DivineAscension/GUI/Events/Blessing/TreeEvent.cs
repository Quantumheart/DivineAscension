namespace DivineAscension.GUI.Events.Blessing;

public abstract record TreeEvent
{
    /// <summary>
    ///     Emitted when tree scroll position changes
    /// </summary>
    public sealed record PlayerTreeScrollChanged(float ScrollX, float ScrollY) : TreeEvent;

    public sealed record ReligionTreeScrollChanged(float ScrollX, float ScrollY) : TreeEvent;

    /// <summary>
    ///     Emitted when a blessing node is clicked
    /// </summary>
    public sealed record Selected(string BlessingId) : TreeEvent;

    /// <summary>
    ///     Emitted when mouse hovers over a blessing
    /// </summary>
    public sealed record Hovered(string? BlessingId) : TreeEvent;
}