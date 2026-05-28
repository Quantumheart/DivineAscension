using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Client requests to add a founder-defined custom feast day (#422).
/// </summary>
[ProtoContract]
public class AddFeastDayRequestPacket
{
    public AddFeastDayRequestPacket()
    {
    }

    public AddFeastDayRequestPacket(string religionUID, string name, int month, int day)
    {
        ReligionUID = religionUID;
        Name = name;
        Month = month;
        Day = day;
    }

    [ProtoMember(1)] public string ReligionUID { get; set; } = string.Empty;
    [ProtoMember(2)] public string Name { get; set; } = string.Empty;
    [ProtoMember(3)] public int Month { get; set; }
    [ProtoMember(4)] public int Day { get; set; }
}
