using ProtoBuf;

namespace DivineAscension.Network.HolySite;

/// <summary>
/// Client sends request to manage rituals at holy sites.
/// Supports starting and canceling rituals.
/// </summary>
[ProtoContract]
public class RitualRequestPacket
{
    /// <summary>
    /// Action type: "start" or "cancel"
    /// </summary>
    [ProtoMember(1)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// UID of the holy site
    /// </summary>
    [ProtoMember(2)]
    public string SiteUID { get; set; } = string.Empty;

    /// <summary>
    /// Target tier for ritual (2 or 3) - used for "start" action
    /// </summary>
    [ProtoMember(3)]
    public int TargetTier { get; set; }
}
