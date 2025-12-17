using ProtoBuf;

namespace PantheonWars.Network.Civilization;

/// <summary>
///     Client requests list of all civilizations from server
/// </summary>
[ProtoContract]
public class CivilizationListRequestPacket
{
    public CivilizationListRequestPacket()
    {
    }

    public CivilizationListRequestPacket(string filterDeity = "")
    {
        FilterDeity = filterDeity;
    }

    [ProtoMember(1)] public string FilterDeity { get; set; } = string.Empty; // Empty = no filter
}