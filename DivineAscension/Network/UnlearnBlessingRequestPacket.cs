using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Client requests to unlearn (remove) a single owned personal blessing, refunding a
///     portion of its favor cost to spendable favor (epic #425, slice 1 — #459).
/// </summary>
[ProtoContract]
public class UnlearnBlessingRequestPacket
{
    public UnlearnBlessingRequestPacket()
    {
    }

    public UnlearnBlessingRequestPacket(string blessingId)
    {
        BlessingId = blessingId;
    }

    [ProtoMember(1)] public string BlessingId { get; set; } = string.Empty;
}
