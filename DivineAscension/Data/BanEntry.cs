using System;
using ProtoBuf;

namespace PantheonWars.Data;

/// <summary>
///     Represents a ban entry for a player banned from a religion
/// </summary>
[ProtoContract]
public class BanEntry
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public BanEntry()
    {
    }

    /// <summary>
    ///     Creates a new ban entry
    /// </summary>
    public BanEntry(string playerUID, string bannedByUID, string reason = "", DateTime? expiresAt = null)
    {
        PlayerUID = playerUID;
        BannedByUID = bannedByUID;
        Reason = reason;
        BannedAt = DateTime.UtcNow;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    ///     The UID of the banned player
    /// </summary>
    [ProtoMember(1)]
    public string PlayerUID { get; set; } = string.Empty;

    /// <summary>
    ///     Reason for the ban
    /// </summary>
    [ProtoMember(2)]
    public string Reason { get; set; } = "No reason provided";

    /// <summary>
    ///     When the ban was issued
    /// </summary>
    [ProtoMember(3)]
    public DateTime BannedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     When the ban expires (null = permanent)
    /// </summary>
    [ProtoMember(4)]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     UID of the player who issued the ban (typically the founder)
    /// </summary>
    [ProtoMember(5)]
    public string BannedByUID { get; set; } = string.Empty;
}