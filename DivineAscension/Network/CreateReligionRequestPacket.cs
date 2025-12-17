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

    public CreateReligionRequestPacket(string religionName, string deity, bool isPublic)
    {
        ReligionName = religionName;
        Deity = deity;
        IsPublic = isPublic;
    }

    [ProtoMember(1)] public string ReligionName { get; set; } = string.Empty;

    [ProtoMember(2)] public string Deity { get; set; } = string.Empty;

    [ProtoMember(3)] public bool IsPublic { get; set; }
}