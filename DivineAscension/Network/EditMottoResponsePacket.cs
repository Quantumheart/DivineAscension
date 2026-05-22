using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server responds to motto edit request
/// </summary>
[ProtoContract]
public class EditMottoResponsePacket
{
    public EditMottoResponsePacket()
    {
    }

    public EditMottoResponsePacket(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    [ProtoMember(1)] public bool Success { get; set; }

    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
}
