using System.Collections.Generic;
using PantheonWars.GUI.Events;

namespace PantheonWars.GUI.Models.Blessing.Tab;

/// <summary>
///     The result of rendering the blessings tab.
/// </summary>
public readonly struct BlessingTabRenderResult(
    IReadOnlyList<BlessingTreeEvent> treeEvents,
    IReadOnlyList<BlessingActionsEvent> actionsEvents,
    string? hoveringBlessingId,
    float renderedHeight)
{
    public IReadOnlyList<BlessingTreeEvent> TreeEvents { get; } = treeEvents;
    public IReadOnlyList<BlessingActionsEvent> ActionsEvents { get; } = actionsEvents;
    public string? HoveringBlessingId { get; } = hoveringBlessingId;
    public float RenderedHeight { get; } = renderedHeight;
}