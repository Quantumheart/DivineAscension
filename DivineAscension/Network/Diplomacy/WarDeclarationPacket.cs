using ProtoBuf;

namespace DivineAscension.Network.Diplomacy;

/// <summary>
///     Server broadcast when war is declared between two civilizations
/// </summary>
[ProtoContract]
public class WarDeclarationPacket
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public WarDeclarationPacket()
    {
    }

    /// <summary>
    ///     Creates a new war declaration packet
    /// </summary>
    public WarDeclarationPacket(
        string declarerCivId,
        string declarerCivName,
        string targetCivId,
        string targetCivName)
    {
        DeclarerCivId = declarerCivId;
        DeclarerCivName = declarerCivName;
        TargetCivId = targetCivId;
        TargetCivName = targetCivName;
    }

    /// <summary>
    ///     Civilization ID of the war declarer
    /// </summary>
    [ProtoMember(1)]
    public string DeclarerCivId { get; set; } = string.Empty;

    /// <summary>
    ///     Civilization name of the war declarer
    /// </summary>
    [ProtoMember(2)]
    public string DeclarerCivName { get; set; } = string.Empty;

    /// <summary>
    ///     Civilization ID of the war target
    /// </summary>
    [ProtoMember(3)]
    public string TargetCivId { get; set; } = string.Empty;

    /// <summary>
    ///     Civilization name of the war target
    /// </summary>
    [ProtoMember(4)]
    public string TargetCivName { get; set; } = string.Empty;
}
