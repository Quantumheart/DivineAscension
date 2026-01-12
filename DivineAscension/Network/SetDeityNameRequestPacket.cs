using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Client requests to change the deity name for their religion
/// </summary>
[ProtoContract]
public class SetDeityNameRequestPacket
{
    public SetDeityNameRequestPacket()
    {
    }

    public SetDeityNameRequestPacket(string religionUID, string newDeityName)
    {
        ReligionUID = religionUID;
        NewDeityName = newDeityName;
    }

    [ProtoMember(1)] public string ReligionUID { get; set; } = string.Empty;

    [ProtoMember(2)] public string NewDeityName { get; set; } = string.Empty;
}