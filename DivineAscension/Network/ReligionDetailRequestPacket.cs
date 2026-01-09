using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Client requests detailed information about a specific religion
/// </summary>
[ProtoContract]
public class ReligionDetailRequestPacket
{
    [ProtoMember(1)] public string ReligionUID { get; set; } = string.Empty;
}