using System.Collections.Generic;
using ProtoBuf;

namespace PantheonWars.Network;

/// <summary>
///     Server sends list of available religions to client
/// </summary>
[ProtoContract]
public class ReligionListResponsePacket
{
    public ReligionListResponsePacket()
    {
    }

    public ReligionListResponsePacket(List<ReligionInfo> religions)
    {
        Religions = religions;
    }

    [ProtoMember(1)] public List<ReligionInfo> Religions { get; set; } = new();

    [ProtoContract]
    public class ReligionInfo
    {
        [ProtoMember(1)] public string ReligionUID { get; set; } = string.Empty;

        [ProtoMember(2)] public string ReligionName { get; set; } = string.Empty;

        [ProtoMember(3)] public int MemberCount { get; set; }

        [ProtoMember(4)] public bool IsPublic { get; set; }

        [ProtoMember(5)] public string FounderUID { get; set; } = string.Empty;

        [ProtoMember(6)] public string Description { get; set; } = string.Empty;
    }
}