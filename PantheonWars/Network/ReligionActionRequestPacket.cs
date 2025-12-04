using System.Collections.Generic;
using ProtoBuf;

namespace PantheonWars.Network;

/// <summary>
///     Client requests an action on a religion (join, leave, kick, invite, etc.)
/// </summary>
[ProtoContract]
public class ReligionActionRequestPacket
{
    public ReligionActionRequestPacket()
    {
    }

    public ReligionActionRequestPacket(string action, string religionUID = "", string targetPlayerUID = "")
    {
        Action = action;
        ReligionUID = religionUID;
        TargetPlayerUID = targetPlayerUID;
    }

    [ProtoMember(1)]
    public string Action { get; set; } = string.Empty; // "join", "leave", "kick", "invite", "ban", "unban"

    [ProtoMember(2)] public string ReligionUID { get; set; } = string.Empty;

    [ProtoMember(3)] public string TargetPlayerUID { get; set; } = string.Empty; // For kick/invite/ban actions

    [ProtoMember(4)]
    public Dictionary<string, object>? Data { get; set; } // Additional data for actions (e.g., ban reason, expiry)
}