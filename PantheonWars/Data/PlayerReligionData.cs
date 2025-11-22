using System;
using ProtoBuf;

namespace PantheonWars.Data;

/// <summary>
///     Stores player-specific religion data for persistence
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
    ///     Last time the player switched religions (for cooldown tracking)
    /// </summary>
    [ProtoMember(3)]
    public DateTime? LastReligionSwitch { get; set; } = null;

    /// <summary>
    ///     Checks if player has a religion
    /// </summary>
    public bool HasReligion()
    {
        return !string.IsNullOrEmpty(ReligionUID);
    }
}