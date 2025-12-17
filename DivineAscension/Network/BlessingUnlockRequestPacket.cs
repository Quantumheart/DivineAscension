using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Client requests to unlock a blessing
/// </summary>
[ProtoContract]
public class BlessingUnlockRequestPacket
{
    public BlessingUnlockRequestPacket()
    {
    }

    public BlessingUnlockRequestPacket(string blessingId)
    {
        BlessingId = blessingId;
    }

    [ProtoMember(1)] public string BlessingId { get; set; } = string.Empty;
}