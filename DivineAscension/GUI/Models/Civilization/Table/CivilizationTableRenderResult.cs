using System.Collections.Generic;
using DivineAscension.GUI.Events.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Table;

/// <summary>
///     Result from rendering civilization table.
///     Contains events emitted during rendering and final height.
/// </summary>
internal readonly record struct CivilizationTableRenderResult(
    List<ListEvent> Events,
    float Height);