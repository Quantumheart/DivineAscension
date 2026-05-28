using System;
using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Client requests to remove a founder-defined custom feast day by id (#422).
/// </summary>
[ProtoContract]
public class RemoveFeastDayRequestPacket
{
    public RemoveFeastDayRequestPacket()
    {
    }

    public RemoveFeastDayRequestPacket(string religionUID, Guid feastId)
    {
        ReligionUID = religionUID;
        FeastId = feastId;
    }

    [ProtoMember(1)] public string ReligionUID { get; set; } = string.Empty;
    [ProtoMember(2)] public Guid FeastId { get; set; }
}
