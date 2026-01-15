using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
///     Stores religion-specific data for persistence
/// </summary>
[ProtoContract]
public class ReligionData
{
    /// <summary>
    ///     Creates a new religion with the specified parameters
    /// </summary>
    public ReligionData(string religionUID, string religionName, DeityDomain domain, string deityName,
        string founderUID, string founderName)
    {
        ReligionUID = religionUID;
        ReligionName = religionName;
        Domain = domain;
        DeityName = deityName;
        FounderUID = founderUID;
        FounderName = founderName;
        MemberUIDs = new List<string> { founderUID }; // Founder is first member
        Members = new Dictionary<string, MemberEntry>
        {
            [founderUID] = new(founderUID, founderName)
        };
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
    ///     Display name of the religion (e.g., "Knights of the Forge")
    /// </summary>
    [ProtoMember(2)]
    public string ReligionName { get; set; } = string.Empty;

    /// <summary>
    ///     The deity this religion serves (permanent, cannot be changed)
    /// </summary>
    [ProtoMember(3)]
    public DeityDomain Domain { get; set; } = DeityDomain.None;

    /// <summary>
    ///     Player UID of the religion founder
    /// </summary>
    [ProtoMember(4)]
    public string FounderUID { get; set; } = string.Empty;

    /// <summary>
    ///     Ordered list of member player UIDs (founder is always first)
    /// </summary>
    [ProtoMember(5)]
    public List<string> MemberUIDs { get; set; } = new();

    /// <summary>
    ///     Current prestige rank of the religion
    /// </summary>
    [ProtoMember(6)]
    public PrestigeRank PrestigeRank { get; set; } = PrestigeRank.Fledgling;

    /// <summary>
    ///     Current prestige points
    /// </summary>
    [ProtoMember(7)]
    public int Prestige { get; set; }

    /// <summary>
    ///     Total prestige earned (lifetime stat, used for ranking)
    /// </summary>
    [ProtoMember(8)]
    public int TotalPrestige { get; set; }

    /// <summary>
    ///     When the religion was created
    /// </summary>
    [ProtoMember(9)]
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Dictionary of unlocked religion blessings
    ///     Key: blessing ID, Value: unlock status (true if unlocked)
    /// </summary>
    [ProtoMember(10)]
    public Dictionary<string, bool> UnlockedBlessings { get; set; } = new();

    /// <summary>
    ///     Whether this is a public religion (anyone can join) or private (invite-only)
    /// </summary>
    [ProtoMember(11)]
    public bool IsPublic { get; set; } = true;

    /// <summary>
    ///     Religion description or manifesto set by the founder
    /// </summary>
    [ProtoMember(12)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Dictionary of banned players
    ///     Key: player UID, Value: ban entry with details
    /// </summary>
    [ProtoMember(13)]
    public Dictionary<string, BanEntry> BannedPlayers { get; set; } = new();

    /// <summary>
    ///     Dictionary of roles in the religion
    /// </summary>
    [ProtoMember(14)]
    public Dictionary<string, RoleData> Roles { get; set; } = new();

    /// <summary>
    ///     A dictionary of roles for the religion. Keys are player UIDs, values are the role IDs.
    /// </summary>
    [ProtoMember(15)]
    public Dictionary<string, string> MemberRoles { get; set; } = new();

    /// <summary>
    ///     Dictionary of member entries with cached player names
    ///     Key: player UID, Value: member entry with name and join date
    /// </summary>
    [ProtoMember(16)]
    public Dictionary<string, MemberEntry> Members { get; set; } = new();

    /// <summary>
    ///     Cached founder name for quick access
    /// </summary>
    [ProtoMember(17)]
    public string FounderName { get; set; } = string.Empty;

    /// <summary>
    ///     The custom name of the deity this religion worships (required).
    ///     This allows religions with the same domain to have uniquely named deities.
    /// </summary>
    [ProtoMember(18)]
    public string DeityName { get; set; } = string.Empty;

    /// <summary>
    ///     Recent activity log entries (last 100 entries, FIFO).
    ///     Stores favor/prestige awards from member actions.
    /// </summary>
    [ProtoMember(19)]
    public List<ActivityLogEntry> ActivityLog { get; set; } = new();

    /// <summary>
    ///     Accumulated fractional prestige (not yet awarded).
    ///     Enables true 1:1 favor-to-prestige conversion for fractional favor amounts.
    /// </summary>
    [ProtoMember(20)]
    public float AccumulatedFractionalPrestige { get; set; }

    /// <summary>
    ///     Adds a member to the religion with player name
    /// </summary>
    public void AddMember(string playerUID, string playerName)
    {
        if (!MemberUIDs.Contains(playerUID))
            MemberUIDs.Add(playerUID);

        if (!Members.ContainsKey(playerUID))
            Members[playerUID] = new MemberEntry(playerUID, playerName);
    }

    /// <summary>
    ///     Removes a member from the religion
    /// </summary>
    public bool RemoveMember(string playerUID)
    {
        MemberRoles.Remove(playerUID);
        Members.Remove(playerUID);
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
    ///     Adds fractional prestige and updates statistics when accumulated amount >= 1.
    ///     Enables true 1:1 favor-to-prestige conversion for fractional favor amounts.
    /// </summary>
    public void AddFractionalPrestige(float amount)
    {
        if (amount > 0)
        {
            AccumulatedFractionalPrestige += amount;

            // Award integer prestige when we have accumulated >= 1.0
            if (AccumulatedFractionalPrestige >= 1.0f)
            {
                var prestigeToAward = (int)AccumulatedFractionalPrestige;
                AccumulatedFractionalPrestige -= prestigeToAward; // Keep the fractional remainder

                Prestige += prestigeToAward;
                TotalPrestige += prestigeToAward;
            }
        }
    }

    /// <summary>
    ///     Gets the cached player name for a member (fallback to UID if not found)
    /// </summary>
    public string GetMemberName(string playerUID)
    {
        // Special case for founder - use FounderName as fallback
        if (playerUID == FounderUID && !string.IsNullOrEmpty(FounderName))
        {
            if (Members.TryGetValue(playerUID, out var founderEntry) && !string.IsNullOrEmpty(founderEntry.PlayerName))
                return founderEntry.PlayerName;
            return FounderName;
        }

        // For non-founders
        return Members.TryGetValue(playerUID, out var entry) && !string.IsNullOrEmpty(entry.PlayerName)
            ? entry.PlayerName
            : playerUID;
    }

    /// <summary>
    ///     Updates the cached player name if the member exists
    /// </summary>
    public void UpdateMemberName(string playerUID, string playerName)
    {
        if (Members.TryGetValue(playerUID, out var entry))
            entry.UpdateName(playerName);
    }

    /// <summary>
    ///     Updates the founder name and the founder's member entry
    /// </summary>
    public void UpdateFounderName(string founderName)
    {
        FounderName = founderName;
        UpdateMemberName(FounderUID, founderName);
    }

    /// <summary>
    ///     Updates the prestige rank based on total prestige earned
    /// </summary>
    public void UpdatePrestigeRank()
    {
        PrestigeRank = TotalPrestige switch
        {
            >= 50000 => PrestigeRank.Mythic,
            >= 25000 => PrestigeRank.Legendary,
            >= 10000 => PrestigeRank.Renowned,
            >= 2500 => PrestigeRank.Established,
            _ => PrestigeRank.Fledgling
        };
    }

    /// <summary>
    ///     Adds prestige and updates statistics
    /// </summary>
    public void AddPrestige(int amount)
    {
        if (amount > 0)
        {
            Prestige += amount;
            TotalPrestige += amount;
            UpdatePrestigeRank();
        }
    }

    /// <summary>
    ///     Unlocks a blessing for this religion
    /// </summary>
    public void UnlockBlessing(string blessingId)
    {
        UnlockedBlessings[blessingId] = true;
    }

    /// <summary>
    ///     Checks if a blessing is unlocked
    /// </summary>
    public bool IsBlessingUnlocked(string blessingId)
    {
        return UnlockedBlessings.TryGetValue(blessingId, out var unlocked) && unlocked;
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

    // Get player's role
    public string GetPlayerRole(string playerUID)
    {
        if (MemberRoles.TryGetValue(playerUID, out var roleUID))
            return roleUID;

        return RoleDefaults.MEMBER_ROLE_ID; // Fallback
    }

// Get role data
    public RoleData? GetRole(string roleUID)
    {
        return Roles.TryGetValue(roleUID, out var role) ? role : null;
    }

// Check if player has a specific permission
    public bool HasPermission(string playerUID, string permission)
    {
        var roleUID = GetPlayerRole(playerUID);
        var role = GetRole(roleUID);

        if (role == null)
            return false;

        return role.HasPermission(permission);
    }

// Check if player can assign a specific role
    public bool CanAssignRole(string assignerUID, string targetRoleUID)
    {
        // SYSTEM can always assign roles (for automated assignments like join/invite)
        if (assignerUID == "SYSTEM")
        {
            // Cannot assign Founder role (must use transfer)
            if (targetRoleUID == RoleDefaults.FOUNDER_ROLE_ID)
                return false;

            // Role must exist
            if (!Roles.ContainsKey(targetRoleUID))
                return false;

            return true;
        }

        // Must have MANAGE_ROLES permission
        if (!HasPermission(assignerUID, RolePermissions.MANAGE_ROLES))
            return false;

        // Cannot assign Founder role (must use transfer)
        if (targetRoleUID == RoleDefaults.FOUNDER_ROLE_ID)
            return false;

        // Role must exist
        if (!Roles.ContainsKey(targetRoleUID))
            return false;

        return true;
    }

// Get list of roles a player can assign
    public List<RoleData> GetAssignableRoles(string playerUID)
    {
        var assignable = new List<RoleData>();

        if (!HasPermission(playerUID, RolePermissions.MANAGE_ROLES))
            return assignable;

        foreach (var role in Roles.Values)
        {
            // Cannot assign Founder role
            if (role.RoleUID == RoleDefaults.FOUNDER_ROLE_ID)
                continue;

            assignable.Add(role);
        }

        return assignable;
    }

// Get role by name (case-insensitive)
    public RoleData? GetRoleByName(string roleName)
    {
        foreach (var role in Roles.Values)
            if (role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase))
                return role;

        return null;
    }

// Check if role name is taken
    public bool IsRoleNameTaken(string roleName, string? excludeRoleUID = null)
    {
        foreach (var role in Roles.Values)
        {
            if (role.RoleUID == excludeRoleUID)
                continue;

            if (role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

// Get members with a specific role
    public List<string> GetMembersWithRole(string roleUID)
    {
        var members = new List<string>();

        foreach (var kvp in MemberRoles)
            if (kvp.Value == roleUID)
                members.Add(kvp.Key);

        return members;
    }

// Count members per role
    public Dictionary<string, int> GetRoleMemberCounts()
    {
        var counts = new Dictionary<string, int>();

        foreach (var role in Roles.Keys) counts[role] = 0;

        foreach (var roleUID in MemberRoles.Values)
            if (counts.ContainsKey(roleUID))
                counts[roleUID]++;
            else
                counts[roleUID] = 1;

        return counts;
    }
}

/// <summary>
///     Represents an invitation for a player to join a religion
/// </summary>
[ProtoContract]
public class ReligionInvite
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public ReligionInvite()
    {
    }

    /// <summary>
    ///     Creates a new religion invite
    /// </summary>
    public ReligionInvite(string inviteId, string religionId, string playerUID, DateTime sentDate)
    {
        InviteId = inviteId;
        ReligionId = religionId;
        PlayerUID = playerUID;
        SentDate = sentDate;
        ExpiresDate = sentDate.AddDays(7);
    }

    /// <summary>
    ///     Unique identifier for the invite
    /// </summary>
    [ProtoMember(1)]
    public string InviteId { get; set; } = string.Empty;

    /// <summary>
    ///     Religion ID this invite is for
    /// </summary>
    [ProtoMember(2)]
    public string ReligionId { get; set; } = string.Empty;

    /// <summary>
    ///     Player UID being invited
    /// </summary>
    [ProtoMember(3)]
    public string PlayerUID { get; set; } = string.Empty;

    /// <summary>
    ///     When the invite was sent
    /// </summary>
    [ProtoMember(4)]
    public DateTime SentDate { get; set; }

    /// <summary>
    ///     When the invite expires (7 days from sent date)
    /// </summary>
    [ProtoMember(5)]
    public DateTime ExpiresDate { get; set; }

    /// <summary>
    ///     Checks if the invite is still valid (not expired)
    /// </summary>
    public bool IsValid => DateTime.UtcNow < ExpiresDate;
}