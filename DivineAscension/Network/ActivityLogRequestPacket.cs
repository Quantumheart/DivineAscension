using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Client requests activity log for their current religion
/// </summary>
[ProtoContract]
public class ActivityLogRequestPacket
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public ActivityLogRequestPacket()
    {
    }

    /// <summary>
    ///     Religion UID to fetch activity log for
    /// </summary>
    [ProtoMember(1)]
    public string ReligionUID { get; set; } = string.Empty;

    /// <summary>
    ///     Maximum number of entries to return (default: 50)
    /// </summary>
    [ProtoMember(2)]
    public int Limit { get; set; } = 50;
}