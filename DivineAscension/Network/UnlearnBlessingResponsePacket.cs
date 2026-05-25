using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server responds to an unlearn-blessing request. <see cref="RefundedFavor"/> is the
///     spendable favor credited back on success (0 on failure). <see cref="StruckBlessingIds"/>
///     is the full cascade removed (target + dependents) so the client can re-lock every node (#460).
/// </summary>
[ProtoContract]
public class UnlearnBlessingResponsePacket
{
    public UnlearnBlessingResponsePacket()
    {
    }

    public UnlearnBlessingResponsePacket(bool success, string message, string blessingId, int refundedFavor,
        List<string>? struckBlessingIds = null)
    {
        Success = success;
        Message = message;
        BlessingId = blessingId;
        RefundedFavor = refundedFavor;
        StruckBlessingIds = struckBlessingIds ?? new List<string>();
    }

    [ProtoMember(1)] public bool Success { get; set; }

    [ProtoMember(2)] public string Message { get; set; } = string.Empty;

    [ProtoMember(3)] public string BlessingId { get; set; } = string.Empty;

    [ProtoMember(4)] public int RefundedFavor { get; set; }

    [ProtoMember(5)] public List<string> StruckBlessingIds { get; set; } = new();
}
