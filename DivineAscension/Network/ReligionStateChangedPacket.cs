using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server notification to client that their religion state has changed
///     (e.g., religion disbanded, kicked from religion, etc.)
///     Client should request fresh blessing data after receiving this
/// </summary>
[ProtoContract]
public class ReligionStateChangedPacket
{
    /// <summary>
    ///     Reason for the state change
    /// </summary>
    [ProtoMember(1)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    ///     Whether the player still has a religion
    /// </summary>
    [ProtoMember(2)]
    public bool HasReligion { get; set; }
}