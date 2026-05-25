using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Founder requests to strike (unlearn) a single inscribed religion blessing, refunding a
///     portion of its prestige cost to the religion's spendable prestige (epic #479, slice 5 — #484).
/// </summary>
[ProtoContract]
public class UnlearnReligionBlessingRequestPacket
{
    public UnlearnReligionBlessingRequestPacket()
    {
    }

    public UnlearnReligionBlessingRequestPacket(string blessingId)
    {
        BlessingId = blessingId;
    }

    [ProtoMember(1)] public string BlessingId { get; set; } = string.Empty;
}
