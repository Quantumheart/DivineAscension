using System.Collections.Generic;
using DivineAscension.GUI.Events.Civilization;

namespace DivineAscension.GUI.Models.HolySite.Table;

/// <summary>
///     Immutable result from holy site table rendering containing events and state.
/// </summary>
internal readonly record struct HolySiteTableRenderResult(
    IReadOnlyList<ListEvent> Events,
    float RenderedHeight
);
