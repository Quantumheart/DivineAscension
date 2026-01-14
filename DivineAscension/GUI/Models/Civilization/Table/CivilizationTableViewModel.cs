using System.Collections.Generic;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.Models.Civilization.Table;

/// <summary>
///     View model for civilization table rendering with variable-height rows.
///     Contains data needed to render the scrollable table with wrapped descriptions.
/// </summary>
internal readonly record struct CivilizationTableViewModel(
    IReadOnlyList<CivilizationListResponsePacket.CivilizationInfo> Civilizations,
    bool IsLoading,
    float ScrollY,
    string? SelectedCivId,
    float X,
    float Y,
    float Width,
    float Height);