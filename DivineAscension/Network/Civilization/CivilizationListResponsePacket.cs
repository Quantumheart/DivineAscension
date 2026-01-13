using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network.Civilization;

/// <summary>
///     Server sends list of civilizations to client
/// </summary>
[ProtoContract]
public class CivilizationListResponsePacket
{
    public CivilizationListResponsePacket()
    {
    }

    public CivilizationListResponsePacket(List<CivilizationInfo> civilizations)
    {
        Civilizations = civilizations;
    }

    [ProtoMember(1)] public List<CivilizationInfo> Civilizations { get; set; } = new();

    /// <summary>
    ///     Summary information about a civilization for list display
    /// </summary>
    [ProtoContract]
    public class CivilizationInfo
    {
        [ProtoMember(1)] public string CivId { get; set; } = string.Empty;

        [ProtoMember(2)] public string Name { get; set; } = string.Empty;

        [ProtoMember(3)] public string FounderUID { get; set; } = string.Empty;

        [ProtoMember(4)] public string FounderReligionUID { get; set; } = string.Empty;

        [ProtoMember(5)] public int MemberCount { get; set; }

        [ProtoMember(6)] public List<string> MemberDeities { get; set; } = new(); // Deity names for diversity display

        [ProtoMember(7)] public List<string> MemberReligionNames { get; set; } = new(); // Religion names

        [ProtoMember(8)] public string Icon { get; set; } = "default"; // Icon identifier for the civilization

        [ProtoMember(9)] public string Description { get; set; } = string.Empty; // Civilization description
    }
}