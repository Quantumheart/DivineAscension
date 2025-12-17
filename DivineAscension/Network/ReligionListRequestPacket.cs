using ProtoBuf;

namespace PantheonWars.Network;

/// <summary>
///     Client requests list of available religions from server
/// </summary>
[ProtoContract]
public class ReligionListRequestPacket
{
    public ReligionListRequestPacket()
    {
    }

    public ReligionListRequestPacket(string filterDeity = "")
    {
        FilterDeity = filterDeity;
    }

    [ProtoMember(1)] public string FilterDeity { get; set; } = string.Empty; // Empty = no filter
}