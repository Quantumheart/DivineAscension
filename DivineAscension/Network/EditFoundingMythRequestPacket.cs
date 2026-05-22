using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Client requests to edit religion founding myth
/// </summary>
[ProtoContract]
public class EditFoundingMythRequestPacket
{
    public EditFoundingMythRequestPacket()
    {
    }

    public EditFoundingMythRequestPacket(string religionUID, string foundingMyth)
    {
        ReligionUID = religionUID;
        FoundingMyth = foundingMyth;
    }

    [ProtoMember(1)] public string ReligionUID { get; set; } = string.Empty;

    [ProtoMember(2)] public string FoundingMyth { get; set; } = string.Empty;
}
