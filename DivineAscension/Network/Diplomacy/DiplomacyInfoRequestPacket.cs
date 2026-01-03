using ProtoBuf;

namespace DivineAscension.Network.Diplomacy;

/// <summary>
///     Client requests diplomacy information for a civilization
/// </summary>
[ProtoContract]
public class DiplomacyInfoRequestPacket
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public DiplomacyInfoRequestPacket()
    {
    }

    /// <summary>
    ///     Creates a new diplomacy info request
    /// </summary>
    public DiplomacyInfoRequestPacket(string civId)
    {
        CivId = civId;
    }

    /// <summary>
    ///     Civilization ID to query diplomacy info for
    /// </summary>
    [ProtoMember(1)]
    public string CivId { get; set; } = string.Empty;
}
