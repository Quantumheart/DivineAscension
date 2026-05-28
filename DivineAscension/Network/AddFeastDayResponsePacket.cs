using System;
using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server response to an add-custom-feast request (#422). On success
///     returns the created feast so the client can echo it into the local
///     view without waiting for a full PlayerReligionInfoResponse refresh.
/// </summary>
[ProtoContract]
public class AddFeastDayResponsePacket
{
    public AddFeastDayResponsePacket()
    {
    }

    [ProtoMember(1)] public bool Success { get; set; }

    /// <summary>Mirror of <see cref="DivineAscension.Models.Enum.FeastDayErrorCode"/>.</summary>
    [ProtoMember(2)] public int ErrorCode { get; set; }

    [ProtoMember(3)] public Guid FeastId { get; set; }
    [ProtoMember(4)] public string Name { get; set; } = string.Empty;
    [ProtoMember(5)] public int Month { get; set; }
    [ProtoMember(6)] public int Day { get; set; }
}
