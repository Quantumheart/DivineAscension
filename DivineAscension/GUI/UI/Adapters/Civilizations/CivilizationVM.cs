using System.Collections.Generic;

namespace DivineAscension.GUI.UI.Adapters.Civilizations;

/// <summary>
///     View model for civilization adapter pattern.
///     Mirrors CivilizationListResponsePacket.CivilizationInfo structure.
/// </summary>
internal sealed record CivilizationVM(
    string civId,
    string name,
    string founderUID,
    string founderReligionUID,
    int memberCount,
    List<string> memberDeities,
    List<string> memberReligionNames,
    string icon,
    string description
);