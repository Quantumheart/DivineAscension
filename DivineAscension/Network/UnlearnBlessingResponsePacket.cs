using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server responds to an unlearn-blessing request. <see cref="RefundedFavor"/> is the
///     spendable favor credited back on success (0 on failure).
/// </summary>
[ProtoContract]
public class UnlearnBlessingResponsePacket
{
    public UnlearnBlessingResponsePacket()
    {
    }

    public UnlearnBlessingResponsePacket(bool success, string message, string blessingId, int refundedFavor)
    {
        Success = success;
        Message = message;
        BlessingId = blessingId;
        RefundedFavor = refundedFavor;
    }

    [ProtoMember(1)] public bool Success { get; set; }

    [ProtoMember(2)] public string Message { get; set; } = string.Empty;

    [ProtoMember(3)] public string BlessingId { get; set; } = string.Empty;

    [ProtoMember(4)] public int RefundedFavor { get; set; }
}
