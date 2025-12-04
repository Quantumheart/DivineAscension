using ProtoBuf;

namespace PantheonWars.Network.Civilization;

/// <summary>
///     Client requests an action on a civilization (create, invite, accept, leave, kick, disband)
/// </summary>
[ProtoContract]
public class CivilizationActionRequestPacket
{
    public CivilizationActionRequestPacket()
    {
    }

    public CivilizationActionRequestPacket(string action, string civId = "", string targetId = "", string name = "")
    {
        Action = action;
        CivId = civId;
        TargetId = targetId;
        Name = name;
    }

    [ProtoMember(1)]
    public string Action { get; set; } = string.Empty; // "create", "invite", "accept", "leave", "kick", "disband"

    [ProtoMember(2)] public string CivId { get; set; } = string.Empty; // Civilization ID (for most actions)

    [ProtoMember(3)]
    public string TargetId { get; set; } = string.Empty; // Religion ID (for invite/kick) or Invite ID (for accept)

    [ProtoMember(4)] public string Name { get; set; } = string.Empty; // Civilization name (for create action)
}