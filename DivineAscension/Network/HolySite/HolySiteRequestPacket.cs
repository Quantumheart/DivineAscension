using ProtoBuf;

namespace DivineAscension.Network.HolySite;

/// <summary>
/// Client requests holy site data from server.
/// Supports three actions: "list", "detail", "religion_sites".
/// </summary>
[ProtoContract]
public class HolySiteRequestPacket
{
    public HolySiteRequestPacket()
    {
    }

    public HolySiteRequestPacket(string action, string? siteUID = null, string? religionUID = null,
        string? domainFilter = null)
    {
        Action = action;
        SiteUID = siteUID ?? string.Empty;
        ReligionUID = religionUID ?? string.Empty;
        DomainFilter = domainFilter ?? string.Empty;
    }

    /// <summary>
    /// Action to perform: "list", "detail", "religion_sites"
    /// </summary>
    [ProtoMember(1)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Holy site UID for "detail" action
    /// </summary>
    [ProtoMember(2)]
    public string SiteUID { get; set; } = string.Empty;

    /// <summary>
    /// Religion UID for "religion_sites" action
    /// </summary>
    [ProtoMember(3)]
    public string ReligionUID { get; set; } = string.Empty;

    /// <summary>
    /// Optional domain filter for "list" action (empty = all domains)
    /// </summary>
    [ProtoMember(4)]
    public string DomainFilter { get; set; } = string.Empty;
}