using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
///     Stores religion-specific data for persistence.
///     This class is thread-safe for concurrent access.
/// </summary>
[ProtoContract]
public class ReligionData
{
    // Thread-safety: Lazy lock initialization (handles ProtoBuf deserialization)
    [ProtoIgnore] private object? _lock;
    [ProtoIgnore] private object Lock => _lock ??= new object();
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
        _memberUIDs = new List<string> { founderUID }; // Founder is first member
        _members = new Dictionary<string, MemberEntry>
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
    ///     Backing field for member UIDs (serialized)
    /// </summary>
    [ProtoMember(5)]
    private List<string> _memberUIDs = new();

    /// <summary>
    ///     Ordered list of member player UIDs (founder is always first).
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyList<string> MemberUIDs
    {
        get
        {
            lock (Lock)
            {
                return _memberUIDs.ToList();
            }
        }
    }

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
    ///     Backing field for unlocked blessings (serialized)
    /// </summary>
    [ProtoMember(10)]
    private Dictionary<string, bool> _unlockedBlessings = new();

    /// <summary>
    ///     Dictionary of unlocked religion blessings.
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, bool> UnlockedBlessings
    {
        get
        {
            lock (Lock)
            {
                return new Dictionary<string, bool>(_unlockedBlessings);
            }
        }
    }

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
    ///     Backing field for banned players (serialized)
    /// </summary>
    [ProtoMember(13)]
    private Dictionary<string, BanEntry> _bannedPlayers = new();

    /// <summary>
    ///     Dictionary of banned players.
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, BanEntry> BannedPlayers
    {
        get
        {
            lock (Lock)
            {
                return new Dictionary<string, BanEntry>(_bannedPlayers);
            }
        }
    }

    /// <summary>
    ///     Backing field for roles (serialized)
    /// </summary>
    [ProtoMember(14)]
    private Dictionary<string, RoleData> _roles = new();

    /// <summary>
    ///     Dictionary of roles in the religion.
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, RoleData> Roles
    {
        get
        {
            lock (Lock)
            {
                return new Dictionary<string, RoleData>(_roles);
            }
        }
    }

    /// <summary>
    ///     Sets or updates a role in the religion.
    ///     Thread-safe.
    /// </summary>
    public void SetRole(string roleId, RoleData role)
    {
        lock (Lock)
        {
            _roles[roleId] = role;
        }
    }

    /// <summary>
    ///     Removes a role from the religion.
    ///     Thread-safe.
    /// </summary>
    public bool RemoveRole(string roleId)
    {
        lock (Lock)
        {
            return _roles.Remove(roleId);
        }
    }

    /// <summary>
    ///     Backing field for member roles (serialized)
    /// </summary>
    [ProtoMember(15)]
    private Dictionary<string, string> _memberRoles = new();

    /// <summary>
    ///     A dictionary of roles for the religion. Keys are player UIDs, values are the role IDs.
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, string> MemberRoles
    {
        get
        {
            lock (Lock)
            {
                return new Dictionary<string, string>(_memberRoles);
            }
        }
    }

    /// <summary>
    ///     Assigns a role to a member.
    ///     Thread-safe.
    /// </summary>
    public void AssignMemberRole(string playerUID, string roleId)
    {
        lock (Lock)
        {
            _memberRoles[playerUID] = roleId;
        }
    }

    /// <summary>
    ///     Removes a member's role assignment.
    ///     Thread-safe.
    /// </summary>
    public bool RemoveMemberRole(string playerUID)
    {
        lock (Lock)
        {
            return _memberRoles.Remove(playerUID);
        }
    }

    /// <summary>
    ///     Initializes the roles dictionary (for use during construction only).
    ///     Thread-safe.
    /// </summary>
    internal void InitializeRoles(Dictionary<string, RoleData> roles)
    {
        lock (Lock)
        {
            _roles = roles ?? new Dictionary<string, RoleData>();
        }
    }

    /// <summary>
    ///     Initializes the member roles dictionary (for use during construction only).
    ///     Thread-safe.
    /// </summary>
    internal void InitializeMemberRoles(Dictionary<string, string> memberRoles)
    {
        lock (Lock)
        {
            _memberRoles = memberRoles ?? new Dictionary<string, string>();
        }
    }

    /// <summary>
    ///     Backing field for member entries (serialized)
    /// </summary>
    [ProtoMember(16)]
    private Dictionary<string, MemberEntry> _members = new();

    /// <summary>
    ///     Dictionary of member entries with cached player names.
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, MemberEntry> Members
    {
        get
        {
            lock (Lock)
            {
                return new Dictionary<string, MemberEntry>(_members);
            }
        }
    }

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
    ///     Backing field for activity log (serialized)
    /// </summary>
    [ProtoMember(19)]
    private List<ActivityLogEntry> _activityLog = new();

    /// <summary>
    ///     Recent activity log entries (last 100 entries, FIFO).
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyList<ActivityLogEntry> ActivityLog
    {
        get
        {
            lock (Lock)
            {
                return _activityLog.ToList();
            }
        }
    }

    /// <summary>
    ///     Adds an activity log entry (thread-safe).
    ///     Maintains FIFO with max entries limit.
    /// </summary>
    public void AddActivityEntry(ActivityLogEntry entry, int maxEntries = 100)
    {
        lock (Lock)
        {
            _activityLog.Insert(0, entry);
            if (_activityLog.Count > maxEntries)
            {
                _activityLog.RemoveRange(maxEntries, _activityLog.Count - maxEntries);
            }
        }
    }

    /// <summary>
    ///     Gets recent activity entries (thread-safe).
    /// </summary>
    public List<ActivityLogEntry> GetRecentActivity(int limit)
    {
        lock (Lock)
        {
            return _activityLog.Take(limit).ToList();
        }
    }

    /// <summary>
    ///     Clears the activity log.
    ///     Thread-safe.
    /// </summary>
    public void ClearActivityLog()
    {
        lock (Lock)
        {
            _activityLog.Clear();
        }
    }

    /// <summary>
    ///     Accumulated fractional prestige (not yet awarded).
    ///     Enables true 1:1 favor-to-prestige conversion for fractional favor amounts.
    /// </summary>
    [ProtoMember(20)]
    public float AccumulatedFractionalPrestige { get; set; }

    /// <summary>
    ///     Adds a member to the religion with player name.
    ///     Thread-safe.
    /// </summary>
    public void AddMember(string playerUID, string playerName)
    {
        lock (Lock)
        {
            if (!_memberUIDs.Contains(playerUID))
                _memberUIDs.Add(playerUID);

            if (!_members.ContainsKey(playerUID))
                _members[playerUID] = new MemberEntry(playerUID, playerName);
        }
    }

    /// <summary>
    ///     Removes a member from the religion.
    ///     Thread-safe.
    /// </summary>
    public bool RemoveMember(string playerUID)
    {
        lock (Lock)
        {
            _memberRoles.Remove(playerUID);
            _members.Remove(playerUID);
            return _memberUIDs.Remove(playerUID);
        }
    }

    /// <summary>
    ///     Moves a member to the first position in the member list (used for founder transfer).
    ///     Thread-safe.
    /// </summary>
    public void MoveToFirstMember(string playerUID)
    {
        lock (Lock)
        {
            if (_memberUIDs.Remove(playerUID))
            {
                _memberUIDs.Insert(0, playerUID);
            }
        }
    }

    /// <summary>
    ///     Checks if a player is a member of this religion.
    ///     Thread-safe.
    /// </summary>
    public bool IsMember(string playerUID)
    {
        lock (Lock)
        {
            return _memberUIDs.Contains(playerUID);
        }
    }

    /// <summary>
    ///     Checks if a player is the founder
    /// </summary>
    public bool IsFounder(string playerUID)
    {
        return FounderUID == playerUID;
    }

    /// <summary>
    ///     Gets the member count.
    ///     Thread-safe.
    /// </summary>
    public int GetMemberCount()
    {
        lock (Lock)
        {
            return _memberUIDs.Count;
        }
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
    ///     Gets the cached player name for a member (fallback to UID if not found).
    ///     Thread-safe.
    /// </summary>
    public string GetMemberName(string playerUID)
    {
        lock (Lock)
        {
            // Special case for founder - use FounderName as fallback
            if (playerUID == FounderUID && !string.IsNullOrEmpty(FounderName))
            {
                if (_members.TryGetValue(playerUID, out var founderEntry) &&
                    !string.IsNullOrEmpty(founderEntry.PlayerName))
                    return founderEntry.PlayerName;
                return FounderName;
            }

            // For non-founders
            return _members.TryGetValue(playerUID, out var entry) && !string.IsNullOrEmpty(entry.PlayerName)
                ? entry.PlayerName
                : playerUID;
        }
    }

    /// <summary>
    ///     Updates the cached player name if the member exists.
    ///     Thread-safe.
    /// </summary>
    public void UpdateMemberName(string playerUID, string playerName)
    {
        lock (Lock)
        {
            if (_members.TryGetValue(playerUID, out var entry))
                entry.UpdateName(playerName);
        }
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
    ///     Unlocks a blessing for this religion.
    ///     Thread-safe.
    /// </summary>
    public void UnlockBlessing(string blessingId)
    {
        lock (Lock)
        {
            _unlockedBlessings[blessingId] = true;
        }
    }

    /// <summary>
    ///     Locks (removes) a blessing for this religion.
    ///     Thread-safe.
    /// </summary>
    public bool LockBlessing(string blessingId)
    {
        lock (Lock)
        {
            return _unlockedBlessings.Remove(blessingId);
        }
    }

    /// <summary>
    ///     Checks if a blessing is unlocked.
    ///     Thread-safe.
    /// </summary>
    public bool IsBlessingUnlocked(string blessingId)
    {
        lock (Lock)
        {
            return _unlockedBlessings.TryGetValue(blessingId, out var unlocked) && unlocked;
        }
    }

    /// <summary>
    ///     Adds a banned player to the religion's ban list.
    ///     Thread-safe.
    /// </summary>
    public void AddBannedPlayer(string playerUID, BanEntry entry)
    {
        lock (Lock)
        {
            _bannedPlayers[playerUID] = entry;
        }
    }

    /// <summary>
    ///     Removes a banned player from the religion's ban list.
    ///     Thread-safe.
    /// </summary>
    public bool RemoveBannedPlayer(string playerUID)
    {
        lock (Lock)
        {
            return _bannedPlayers.Remove(playerUID);
        }
    }

    /// <summary>
    ///     Checks if a player is banned from this religion (including expired bans).
    ///     Thread-safe.
    /// </summary>
    public bool IsBanned(string playerUID)
    {
        lock (Lock)
        {
            if (!_bannedPlayers.TryGetValue(playerUID, out var banEntry))
                return false;

            // Check if ban has expired
            if (banEntry.ExpiresAt.HasValue && banEntry.ExpiresAt.Value <= DateTime.UtcNow)
                return false;

            return true;
        }
    }

    /// <summary>
    ///     Gets the ban entry for a specific player.
    ///     Thread-safe.
    /// </summary>
    public BanEntry? GetBannedPlayer(string playerUID)
    {
        lock (Lock)
        {
            if (_bannedPlayers.TryGetValue(playerUID, out var banEntry))
            {
                // Check if expired
                if (banEntry.ExpiresAt.HasValue && banEntry.ExpiresAt.Value <= DateTime.UtcNow)
                    return null;

                return banEntry;
            }

            return null;
        }
    }

    /// <summary>
    ///     Gets all active (non-expired) bans.
    ///     Thread-safe.
    /// </summary>
    public List<BanEntry> GetActiveBans()
    {
        lock (Lock)
        {
            CleanupExpiredBansInternal();
            return _bannedPlayers.Values.ToList();
        }
    }

    /// <summary>
    ///     Removes expired bans from the ban list.
    ///     Thread-safe.
    /// </summary>
    public void CleanupExpiredBans()
    {
        lock (Lock)
        {
            CleanupExpiredBansInternal();
        }
    }

    /// <summary>
    ///     Internal implementation for cleanup (call within lock).
    /// </summary>
    private void CleanupExpiredBansInternal()
    {
        var now = DateTime.UtcNow;
        var expiredBans = _bannedPlayers
            .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var playerUID in expiredBans) _bannedPlayers.Remove(playerUID);
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