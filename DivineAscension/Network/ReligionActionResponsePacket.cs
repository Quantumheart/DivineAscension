using ProtoBuf;

namespace PantheonWars.Network;

/// <summary>
///     Server responds to a religion action request with success/failure
/// </summary>
[ProtoContract]
public class ReligionActionResponsePacket
{
    public ReligionActionResponsePacket()
    {
    }

    public ReligionActionResponsePacket(bool success, string message, string action = "")
    {
        Success = success;
        Message = message;
        Action = action;
    }

    [ProtoMember(1)] public bool Success { get; set; }

    [ProtoMember(2)] public string Message { get; set; } = string.Empty;

    [ProtoMember(3)] public string Action { get; set; } = string.Empty;
}