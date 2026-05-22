using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server responds to founding myth edit request
/// </summary>
[ProtoContract]
public class EditFoundingMythResponsePacket
{
    public EditFoundingMythResponsePacket()
    {
    }

    public EditFoundingMythResponsePacket(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    [ProtoMember(1)] public bool Success { get; set; }

    [ProtoMember(2)] public string Message { get; set; } = string.Empty;
}
