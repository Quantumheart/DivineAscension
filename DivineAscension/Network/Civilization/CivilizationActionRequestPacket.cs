using ProtoBuf;

namespace DivineAscension.Network.Civilization;

/// <summary>
///     Client requests an action on a civilization (create, invite, accept, leave, kick, disband)
/// </summary>
[ProtoContract]
public class CivilizationActionRequestPacket
{
    public CivilizationActionRequestPacket()
    {
    }

    public CivilizationActionRequestPacket(string action, string civId = "", string targetId = "", string name = "",
        string icon = "")
    {
        Action = action;
        CivId = civId;
        TargetId = targetId;
        Name = name;
        Icon = icon;
    }

    [ProtoMember(1)]
    public string Action { get; set; } =
        string.Empty; // "create", "invite", "accept", "leave", "kick", "disband", "updateicon"

    [ProtoMember(2)] public string CivId { get; set; } = string.Empty; // Civilization ID (for most actions)

    [ProtoMember(3)]
    public string TargetId { get; set; } = string.Empty; // Religion ID (for invite/kick) or Invite ID (for accept)

    [ProtoMember(4)] public string Name { get; set; } = string.Empty; // Civilization name (for create action)

    [ProtoMember(5)] public string Icon { get; set; } = string.Empty; // Icon identifier (for create/updateicon actions)
}