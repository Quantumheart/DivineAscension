using ProtoBuf;

namespace PantheonWars.Network.Civilization;

/// <summary>
///     Server responds to a civilization action request with success/failure
/// </summary>
[ProtoContract]
public class CivilizationActionResponsePacket
{
    public CivilizationActionResponsePacket()
    {
    }

    public CivilizationActionResponsePacket(bool success, string message, string action = "", string civId = "")
    {
        Success = success;
        Message = message;
        Action = action;
        CivId = civId;
    }

    [ProtoMember(1)] public bool Success { get; set; }

    [ProtoMember(2)] public string Message { get; set; } = string.Empty;

    [ProtoMember(3)] public string Action { get; set; } = string.Empty;

    [ProtoMember(4)] public string CivId { get; set; } = string.Empty; // Relevant civilization ID
}