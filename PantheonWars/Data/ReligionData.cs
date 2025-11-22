using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace PantheonWars.Data;

/// <summary>
///     Stores religion-specific data for persistence
/// </summary>
[ProtoContract]
public class ReligionData
{
    /// <summary>
    ///     Creates a new religion with the specified parameters
    /// </summary>
    public ReligionData(string religionUID, string religionName, string founderUID)
    {
        ReligionUID = religionUID;
        ReligionName = religionName;
        FounderUID = founderUID;
        MemberUIDs = new List<string> { founderUID }; // Founder is first member
        CreationDate = DateTime.UtcNow;
    }

    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public ReligionData()
    {
    }

    /// <summary>
    ///     Unique identifier for this religion
    /// </summary>
    [ProtoMember(1)]
    public string ReligionUID { get; set; } = string.Empty;

    /// <summary>
    ///     Display name of the religion (e.g., "Knights of the Sun")
    /// </summary>
    [ProtoMember(2)]
    public string ReligionName { get; set; } = string.Empty;

    /// <summary>
    ///     Player UID of the religion founder
    /// </summary>
    [ProtoMember(3)]
    public string FounderUID { get; set; } = string.Empty;

    /// <summary>
    ///     Ordered list of member player UIDs (founder is always first)
    /// </summary>
    [ProtoMember(4)]
    public List<string> MemberUIDs { get; set; } = new();

    /// <summary>
    ///     When the religion was created
    /// </summary>
    [ProtoMember(5)]
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Whether this is a public religion (anyone can join) or private (invite-only)
    /// </summary>
    [ProtoMember(6)]
    public bool IsPublic { get; set; } = true;

    /// <summary>
    ///     Religion description or manifesto set by the founder
    /// </summary>
    [ProtoMember(7)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Dictionary of banned players
    ///     Key: player UID, Value: ban entry with details
    /// </summary>
    [ProtoMember(8)]
    public Dictionary<string, BanEntry> BannedPlayers { get; set; } = new();

    /// <summary>
    ///     Adds a member to the religion
    /// </summary>
    public void AddMember(string playerUID)
    {
        if (!MemberUIDs.Contains(playerUID)) MemberUIDs.Add(playerUID);
    }

    /// <summary>
    ///     Removes a member from the religion
    /// </summary>
    public bool RemoveMember(string playerUID)
    {
        return MemberUIDs.Remove(playerUID);
    }

    /// <summary>
    ///     Checks if a player is a member of this religion
    /// </summary>
    public bool IsMember(string playerUID)
    {
        return MemberUIDs.Contains(playerUID);
    }

    /// <summary>
    ///     Checks if a player is the founder
    /// </summary>
    public bool IsFounder(string playerUID)
    {
        return FounderUID == playerUID;
    }

    /// <summary>
    ///     Gets the member count
    /// </summary>
    public int GetMemberCount()
    {
        return MemberUIDs.Count;
    }

    /// <summary>
    ///     Adds a banned player to the religion's ban list
    /// </summary>
    public void AddBannedPlayer(string playerUID, BanEntry entry)
    {
        BannedPlayers[playerUID] = entry;
    }

    /// <summary>
    ///     Removes a banned player from the religion's ban list
    /// </summary>
    public bool RemoveBannedPlayer(string playerUID)
    {
        return BannedPlayers.Remove(playerUID);
    }

    /// <summary>
    ///     Checks if a player is banned from this religion (including expired bans)
    /// </summary>
    public bool IsBanned(string playerUID)
    {
        if (!BannedPlayers.TryGetValue(playerUID, out var banEntry))
            return false;

        // Check if ban has expired
        if (banEntry.ExpiresAt.HasValue && banEntry.ExpiresAt.Value <= DateTime.UtcNow)
            return false;

        return true;
    }

    /// <summary>
    ///     Gets the ban entry for a specific player
    /// </summary>
    public BanEntry? GetBannedPlayer(string playerUID)
    {
        if (BannedPlayers.TryGetValue(playerUID, out var banEntry))
        {
            // Check if expired
            if (banEntry.ExpiresAt.HasValue && banEntry.ExpiresAt.Value <= DateTime.UtcNow)
                return null;

            return banEntry;
        }

        return null;
    }

    /// <summary>
    ///     Gets all active (non-expired) bans
    /// </summary>
    public List<BanEntry> GetActiveBans()
    {
        CleanupExpiredBans();
        return BannedPlayers.Values.ToList();
    }

    /// <summary>
    ///     Removes expired bans from the ban list
    /// </summary>
    public void CleanupExpiredBans()
    {
        var now = DateTime.UtcNow;
        var expiredBans = BannedPlayers
            .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var playerUID in expiredBans) BannedPlayers.Remove(playerUID);
    }
}

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