using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Client requests to edit religion motto/creed
/// </summary>
[ProtoContract]
public class EditMottoRequestPacket
{
    public EditMottoRequestPacket()
    {
    }

    public EditMottoRequestPacket(string religionUID, string motto)
    {
        ReligionUID = religionUID;
        Motto = motto;
    }

    [ProtoMember(1)] public string ReligionUID { get; set; } = string.Empty;

    [ProtoMember(2)] public string Motto { get; set; } = string.Empty;
}
