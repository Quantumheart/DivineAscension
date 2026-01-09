using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.Models.Religion.Table;

/// <summary>
///     Immutable view model for table rendering of religion browse data.
///     Contains all data needed to render the religion table.
/// </summary>
internal readonly record struct ReligionTableViewModel(
    IReadOnlyList<ReligionListResponsePacket.ReligionInfo> Religions,
    bool IsLoading,
    float ScrollY,
    string? SelectedReligionUID,
    float X,
    float Y,
    float Width,
    float Height
);