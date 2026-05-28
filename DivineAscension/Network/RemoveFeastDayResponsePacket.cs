using System;
using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server response to a remove-custom-feast request (#422).
/// </summary>
[ProtoContract]
public class RemoveFeastDayResponsePacket
{
    public RemoveFeastDayResponsePacket()
    {
    }

    [ProtoMember(1)] public bool Success { get; set; }

    /// <summary>Mirror of <see cref="DivineAscension.Models.Enum.FeastDayErrorCode"/>.</summary>
    [ProtoMember(2)] public int ErrorCode { get; set; }

    [ProtoMember(3)] public Guid FeastId { get; set; }
}
