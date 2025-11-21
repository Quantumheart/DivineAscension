using ProtoBuf;

namespace PantheonWars.Network;

/// <summary>
///     Client requests to create a new religion
/// </summary>
[ProtoContract]
public class CreateReligionRequestPacket
{
    public CreateReligionRequestPacket()
    {
    }

    public CreateReligionRequestPacket(string religionName, bool isPublic)
    {
        ReligionName = religionName;
        IsPublic = isPublic;
    }

    [ProtoMember(1)] public string ReligionName { get; set; } = string.Empty;

    [ProtoMember(2)] public bool IsPublic { get; set; }
}