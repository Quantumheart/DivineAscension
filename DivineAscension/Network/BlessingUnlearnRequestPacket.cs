using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Client requests to unlearn (respec) a previously unlocked blessing.
/// </summary>
[ProtoContract]
public class BlessingUnlearnRequestPacket
{
    public BlessingUnlearnRequestPacket()
    {
    }

    public BlessingUnlearnRequestPacket(string blessingId)
    {
        BlessingId = blessingId;
    }

    [ProtoMember(1)] public string BlessingId { get; set; } = string.Empty;
}
