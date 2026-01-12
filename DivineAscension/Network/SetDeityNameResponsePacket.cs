using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server responds to deity name change request
/// </summary>
[ProtoContract]
public class SetDeityNameResponsePacket
{
    public SetDeityNameResponsePacket()
    {
    }

    public SetDeityNameResponsePacket(bool success, string? errorMessage = null, string? newDeityName = null)
    {
        Success = success;
        ErrorMessage = errorMessage;
        NewDeityName = newDeityName;
    }

    [ProtoMember(1)] public bool Success { get; set; }

    [ProtoMember(2)] public string? ErrorMessage { get; set; }

    /// <summary>
    ///     The confirmed new deity name on success
    /// </summary>
    [ProtoMember(3)]
    public string? NewDeityName { get; set; }
}