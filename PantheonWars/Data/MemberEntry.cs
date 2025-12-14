using System;
using ProtoBuf;

namespace PantheonWars.Data;

/// <summary>
///     Represents a member entry in a religion with cached player information
/// </summary>
[ProtoContract]
public class MemberEntry
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public MemberEntry()
    {
    }

    /// <summary>
    ///     Creates a new member entry
    /// </summary>
    public MemberEntry(string playerUID, string playerName)
    {
        PlayerUID = playerUID;
        PlayerName = playerName;
        JoinDate = DateTime.UtcNow;
        LastNameUpdate = DateTime.UtcNow;
    }

    /// <summary>
    ///     The UID of the player
    /// </summary>
    [ProtoMember(1)]
    public string PlayerUID { get; set; } = string.Empty;

    /// <summary>
    ///     The cached player name (updated opportunistically when player is online)
    /// </summary>
    [ProtoMember(2)]
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    ///     When the player joined the religion
    /// </summary>
    [ProtoMember(3)]
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     When the player name was last updated
    /// </summary>
    [ProtoMember(4)]
    public DateTime LastNameUpdate { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Updates the cached player name if it has changed
    /// </summary>
    public void UpdateName(string newName)
    {
        if (PlayerName != newName)
        {
            PlayerName = newName;
            LastNameUpdate = DateTime.UtcNow;
        }
    }
}