using System.Collections.Generic;
using PantheonWars.GUI.Events.Blessing;

namespace PantheonWars.GUI.Models.Blessing.Tab;

/// <summary>
///     The result of rendering the blessings tab.
/// </summary>
public readonly struct BlessingTabRenderResult(
    IReadOnlyList<TreeEvent> treeEvents,
    IReadOnlyList<ActionsEvent> actionsEvents,
    string? hoveringBlessingId,
    float renderedHeight)
{
    public IReadOnlyList<TreeEvent> TreeEvents { get; } = treeEvents;
    public IReadOnlyList<ActionsEvent> ActionsEvents { get; } = actionsEvents;
    public string? HoveringBlessingId { get; } = hoveringBlessingId;
    public float RenderedHeight { get; } = renderedHeight;
}