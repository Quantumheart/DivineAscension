using ProtoBuf;

namespace DivineAscension.Network.HolySite;

/// <summary>
/// Server sends update result back to client.
/// </summary>
[ProtoContract]
public class HolySiteUpdateResponsePacket
{
    /// <summary>
    /// Whether the update succeeded
    /// </summary>
    [ProtoMember(1)]
    public bool Success { get; set; }

    /// <summary>
    /// Success or error message
    /// </summary>
    [ProtoMember(2)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// UID of the updated site
    /// </summary>
    [ProtoMember(3)]
    public string SiteUID { get; set; } = string.Empty;

    /// <summary>
    /// The updated value (for success cases, used to update local state)
    /// </summary>
    [ProtoMember(4)]
    public string UpdatedValue { get; set; } = string.Empty;
}
