using ProtoBuf;

namespace DivineAscension.Network.HolySite;

/// <summary>
/// Client sends request to update holy site properties.
/// Supports rename and description editing.
/// </summary>
[ProtoContract]
public class HolySiteUpdateRequestPacket
{
    /// <summary>
    /// Action type: "rename" or "edit_description"
    /// </summary>
    [ProtoMember(1)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// UID of the holy site to update
    /// </summary>
    [ProtoMember(2)]
    public string SiteUID { get; set; } = string.Empty;

    /// <summary>
    /// New value (new name for rename, new description for edit_description)
    /// </summary>
    [ProtoMember(3)]
    public string NewValue { get; set; } = string.Empty;
}
