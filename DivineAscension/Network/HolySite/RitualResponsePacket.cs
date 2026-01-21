using ProtoBuf;

namespace DivineAscension.Network.HolySite;

/// <summary>
/// Server responds to ritual management requests.
/// </summary>
[ProtoContract]
public class RitualResponsePacket
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    [ProtoMember(1)]
    public bool Success { get; set; }

    /// <summary>
    /// Message to display to the user
    /// </summary>
    [ProtoMember(2)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Updated ritual progress info (if applicable)
    /// </summary>
    [ProtoMember(3)]
    public HolySiteResponsePacket.RitualProgressInfo? RitualProgress { get; set; }
}
