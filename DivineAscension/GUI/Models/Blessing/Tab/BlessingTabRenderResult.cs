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
    DeityDomain? requestedActiveDeity = null)
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
}
