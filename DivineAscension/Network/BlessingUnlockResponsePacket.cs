using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server responds to blessing unlock request
/// </summary>
[ProtoContract]
public class BlessingUnlockResponsePacket
{
    public BlessingUnlockResponsePacket()
    {
    }

    public BlessingUnlockResponsePacket(bool success, string message, string blessingId)
    {
        Success = success;
        Message = message;
        BlessingId = blessingId;
    }

    [ProtoMember(1)] public bool Success { get; set; }

    [ProtoMember(2)] public string Message { get; set; } = string.Empty;

    [ProtoMember(3)] public string BlessingId { get; set; } = string.Empty;
}