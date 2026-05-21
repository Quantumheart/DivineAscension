using System.Collections.Generic;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.Models.Blessing.Tab;

/// <summary>
///     The result of rendering the blessings tab.
/// </summary>
public readonly struct BlessingTabRenderResult(
    IReadOnlyList<TreeEvent> treeEvents,
    IReadOnlyList<ActionsEvent> actionsEvents,
    string? hoveringBlessingId,
    float renderedHeight,
    DeityDomain? requestedActiveDeity = null,
    float? requestedVowsScrollY = null,
    float? requestedPageScrollY = null,
    IReadOnlyList<InfoEvent>? infoEvents = null)
{
    public IReadOnlyList<TreeEvent> TreeEvents { get; } = treeEvents;
    public IReadOnlyList<ActionsEvent> ActionsEvents { get; } = actionsEvents;
    public string? HoveringBlessingId { get; } = hoveringBlessingId;
    public float RenderedHeight { get; } = renderedHeight;

    /// <summary>
    ///     Non-null when the user clicked a deity tab this frame. Manager swaps active deity
    ///     and resets tree-state on consumption.
    /// </summary>
    public DeityDomain? RequestedActiveDeity { get; } = requestedActiveDeity;

    /// <summary>
    ///     Non-null when the Vows page wheel-scrolled this frame. Manager commits the
    ///     new value to <see cref="State.BlessingTabState.VowsPageScrollY"/>.
    /// </summary>
    public float? RequestedVowsScrollY { get; } = requestedVowsScrollY;

    /// <summary>
    ///     Non-null when the III.ii Blessings page wheel-scrolled this frame. Manager commits
    ///     the new value to <see cref="State.BlessingTabState.BlessingsPageScrollY"/>.
    /// </summary>
    public float? RequestedPageScrollY { get; } = requestedPageScrollY;

    /// <summary>
    ///     Events emitted by the blessing info pane (e.g. Read more toggled).
    /// </summary>
    public IReadOnlyList<InfoEvent> InfoEvents { get; } = infoEvents ?? System.Array.Empty<InfoEvent>();
}
