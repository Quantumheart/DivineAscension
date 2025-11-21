using System;
using PantheonWars.Models.Enum;
using ProtoBuf;

namespace PantheonWars.Data;

/// <summary>
///     Stores player-specific religion data for persistence.
///     In the religion-only system, players get blessings through their religion,
///     not through personal progression.
/// </summary>
[ProtoContract]
public class PlayerReligionData
{
    /// <summary>
    ///     Creates new player religion data
    /// </summary>
    public PlayerReligionData(string playerUID)
    {
        PlayerUID = playerUID;
    }

    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public PlayerReligionData()
    {
    }

    /// <summary>
    ///     Player's unique identifier
    /// </summary>
    [ProtoMember(1)]
    public string PlayerUID { get; set; } = string.Empty;

    /// <summary>
    ///     UID of the player's current religion (null if not in any religion)
    /// </summary>
    [ProtoMember(2)]
    public string? ReligionUID { get; set; } = null;

    /// <summary>
    ///     Currently active deity (cached from current religion for performance)
    /// </summary>
    [ProtoMember(3)]
    public DeityType ActiveDeity { get; set; } = DeityType.None;

    /// <summary>
    ///     Last time the player switched religions (for cooldown tracking)
    /// </summary>
    [ProtoMember(8)]
    public DateTime? LastReligionSwitch { get; set; } = null;

    /// <summary>
    ///     Data version for migration purposes
    /// </summary>
    [ProtoMember(9)]
    public int DataVersion { get; set; } = 3; // Religion-only format

    /// <summary>
    ///     Checks if player has a religion
    /// </summary>
    public bool HasReligion()
    {
        return !string.IsNullOrEmpty(ReligionUID);
    }
}
