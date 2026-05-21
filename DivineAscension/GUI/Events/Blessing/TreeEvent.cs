namespace DivineAscension.GUI.Events.Blessing;

public abstract record TreeEvent
{
    /// <summary>
    ///     Emitted by <see cref="UI.Renderers.Blessing.BlessingTreeRenderer"/> whenever the
    ///     tree panel's scroll position changes. The hosting page (III.ii Blessings or
    ///     I.iii Vows of the Order) translates this kind-neutral event into the page-specific
    ///     <see cref="PlayerTreeScrollChanged"/> or <see cref="ReligionTreeScrollChanged"/>
    ///     variant before forwarding it to <see cref="Managers.BlessingStateManager"/>.
    /// </summary>
    public sealed record ScrollChanged(float ScrollX, float ScrollY) : TreeEvent;

    /// <summary>Player-side scroll variant emitted by the III.ii Blessings host page.</summary>
    public sealed record PlayerTreeScrollChanged(float ScrollX, float ScrollY) : TreeEvent;

    /// <summary>Religion-side scroll variant emitted by the I.iii Vows of the Order host page.</summary>
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