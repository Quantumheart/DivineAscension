using ProtoBuf;

namespace PantheonWars.Network;

/// <summary>
///     Server responds to religion creation request
/// </summary>
[ProtoContract]
public class CreateReligionResponsePacket
{
    public CreateReligionResponsePacket()
    {
    }

    public CreateReligionResponsePacket(bool success, string message, string religionUID = "")
    {
        Success = success;
        Message = message;
        ReligionUID = religionUID;
    }

    [ProtoMember(1)] public bool Success { get; set; }

    [ProtoMember(2)] public string Message { get; set; } = string.Empty;

    [ProtoMember(3)] public string ReligionUID { get; set; } = string.Empty;
}