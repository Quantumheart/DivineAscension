using System.Collections.Generic;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.Models.Blessing.Tab;

/// <summary>
///     The result of rendering the blessings tab.
/// </summary>
public readonly struct BlessingTabRenderResult(
    IReadOnlyList<TreeEvent> treeEvents,
    string? hoveringBlessingId,
    float renderedHeight,
    DeityDomain? requestedActiveDeity = null,
    float? requestedVowsScrollY = null,
    float? requestedPageScrollY = null)
{
    public IReadOnlyList<TreeEvent> TreeEvents { get; } = treeEvents;
    public string? HoveringBlessingId { get; } = hoveringBlessingId;
    public float RenderedHeight { get; } = renderedHeight;

    /// <summary>
    ///     Non-null when the user clicked a deity tab this frame. Manager swaps active deity
    ///     and resets tree-state on consumption.
    /// </summary>
    public DeityDomain? RequestedActiveDeity { get; } = requestedActiveDeity;

    /// <summary>
    ///     Non-null when the Vows page wheel-scrolled this frame.
    /// </summary>
    public float? RequestedVowsScrollY { get; } = requestedVowsScrollY;

    /// <summary>
    ///     Non-null when the III.ii Blessings page wheel-scrolled this frame.
    /// </summary>
    public float? RequestedPageScrollY { get; } = requestedPageScrollY;
}
