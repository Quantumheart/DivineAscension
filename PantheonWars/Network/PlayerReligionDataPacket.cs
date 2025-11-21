using ProtoBuf;

namespace PantheonWars.Network;

[ProtoContract]
public class PlayerReligionDataPacket
{
    public PlayerReligionDataPacket()
    {
    }

    public PlayerReligionDataPacket(string religionName)
    {
        ReligionName = religionName;
    }

    [ProtoMember(1)] public string ReligionName { get; set; } = string.Empty;
}