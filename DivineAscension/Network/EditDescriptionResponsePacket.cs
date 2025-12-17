using ProtoBuf;

namespace PantheonWars.Network;

/// <summary>
///     Server responds to description edit request
/// </summary>
[ProtoContract]
public class EditDescriptionResponsePacket
{
    public EditDescriptionResponsePacket()
    {
    }

    public EditDescriptionResponsePacket(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    [ProtoMember(1)] public bool Success { get; set; }

    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
}