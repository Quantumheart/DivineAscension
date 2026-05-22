using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Client requests to create a new religion
/// </summary>
[ProtoContract]
public class CreateReligionRequestPacket
{
    public CreateReligionRequestPacket()
    {
    }

    public CreateReligionRequestPacket(string religionName, string domain, string deityName, bool isPublic,
        string motto = "")
    {
        ReligionName = religionName;
        Domain = domain;
        DeityName = deityName;
        IsPublic = isPublic;
        Motto = motto;
    }

    [ProtoMember(1)] public string ReligionName { get; set; } = string.Empty;

    /// <summary>
    ///     The domain for the religion (Craft, Wild, Harvest, Stone)
    /// </summary>
    [ProtoMember(2)]
    public string Domain { get; set; } = string.Empty;

    [ProtoMember(3)] public bool IsPublic { get; set; }

    /// <summary>
    ///     The custom name for the deity this religion worships
    /// </summary>
    [ProtoMember(4)]
    public string DeityName { get; set; } = string.Empty;

    /// <summary>
    ///     Optional motto/creed (~80 chars).
    /// </summary>
    [ProtoMember(5)]
    public string Motto { get; set; } = string.Empty;
}