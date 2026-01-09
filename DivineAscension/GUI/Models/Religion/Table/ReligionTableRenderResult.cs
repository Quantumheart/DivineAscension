using System.Collections.Generic;
using DivineAscension.GUI.Events.Religion;

namespace DivineAscension.GUI.Models.Religion.Table;

/// <summary>
///     Immutable result from table rendering containing events and state.
/// </summary>
internal readonly record struct ReligionTableRenderResult(
    IReadOnlyList<ListEvent> Events,
    float RenderedHeight
);