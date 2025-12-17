using ProtoBuf;

namespace DivineAscension.Network.Civilization;

/// <summary>
///     Client requests detailed information about a specific civilization
/// </summary>
[ProtoContract]
public class CivilizationInfoRequestPacket
{
    public CivilizationInfoRequestPacket()
    {
    }

    public CivilizationInfoRequestPacket(string civId)
    {
        CivId = civId;
    }

    [ProtoMember(1)] public string CivId { get; set; } = string.Empty;
}