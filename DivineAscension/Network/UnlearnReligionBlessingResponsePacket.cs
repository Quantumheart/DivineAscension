using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server responds to a strike-religion-blessing request. <see cref="RefundedPrestige"/> is the
///     spendable prestige credited back to the religion on success (0 on failure).
///     <see cref="StruckBlessingIds"/> is the full cascade removed (target + dependents) so the
///     client can re-lock every node (epic #479, slice 5 — #484).
/// </summary>
[ProtoContract]
public class UnlearnReligionBlessingResponsePacket
{
    public UnlearnReligionBlessingResponsePacket()
    {
    }

    public UnlearnReligionBlessingResponsePacket(bool success, string message, string blessingId, int refundedPrestige,
        List<string>? struckBlessingIds = null)
    {
        Success = success;
        Message = message;
        BlessingId = blessingId;
        RefundedPrestige = refundedPrestige;
        StruckBlessingIds = struckBlessingIds ?? new List<string>();
    }

    [ProtoMember(1)] public bool Success { get; set; }

    [ProtoMember(2)] public string Message { get; set; } = string.Empty;

    [ProtoMember(3)] public string BlessingId { get; set; } = string.Empty;

    [ProtoMember(4)] public int RefundedPrestige { get; set; }

    [ProtoMember(5)] public List<string> StruckBlessingIds { get; set; } = new();
}
