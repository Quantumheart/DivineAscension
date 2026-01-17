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
    // Lock objects for thread safety (NOT serialized)
    [ProtoIgnore] private readonly object _memberLock = new();
    [ProtoIgnore] private readonly object _blessingLock = new();
    [ProtoIgnore] private readonly object _banLock = new();
    [ProtoIgnore] private readonly object _roleLock = new();
    [ProtoIgnore] private readonly object _activityLock = new();
    [ProtoIgnore] private readonly object _prestigeLock = new();

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
    ///     Ordered list of member player UIDs (founder is always first) - PRIVATE backing field for serialization
    /// </summary>
    [ProtoMember(5)]
    private List<string> _memberUIDs = new();

    /// <summary>
    ///     Read-only snapshot of member UIDs (thread-safe)
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyList<string> MemberUIDs
    {
        get
        {
            lock (_memberLock)
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
    ///     Dictionary of unlocked religion blessings - PRIVATE backing field for serialization
    ///     Key: blessing ID, Value: unlock status (true if unlocked)
    /// </summary>
    [ProtoMember(10)]
    private Dictionary<string, bool> _unlockedBlessings = new();

    /// <summary>
    ///     Read-only snapshot of unlocked blessings (thread-safe)
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, bool> UnlockedBlessings
    {
        get
        {
            lock (_blessingLock)
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
    ///     Dictionary of banned players - PRIVATE backing field for serialization
    ///     Key: player UID, Value: ban entry with details
    /// </summary>
    [ProtoMember(13)]
    private Dictionary<string, BanEntry> _bannedPlayers = new();

    /// <summary>
    ///     Read-only snapshot of banned players (thread-safe)
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, BanEntry> BannedPlayers
    {
        get
        {
            lock (_banLock)
            {
                return new Dictionary<string, BanEntry>(_bannedPlayers);
            }
        }
    }

    /// <summary>
    ///     Dictionary of roles in the religion - PRIVATE backing field for serialization
    /// </summary>
    [ProtoMember(14)]
    private Dictionary<string, RoleData> _roles = new();

    /// <summary>
    ///     Read-only snapshot of roles (thread-safe)
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, RoleData> Roles
    {
        get
        {
            lock (_roleLock)
            {
                return new Dictionary<string, RoleData>(_roles);
            }
        }
    }

    /// <summary>
    ///     A dictionary of roles for the religion - PRIVATE backing field for serialization
    ///     Keys are player UIDs, values are the role IDs.
    /// </summary>
    [ProtoMember(15)]
    private Dictionary<string, string> _memberRoles = new();

    /// <summary>
    ///     Read-only snapshot of member roles (thread-safe)
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, string> MemberRoles
    {
        get
        {
            lock (_memberLock)
            {
                return new Dictionary<string, string>(_memberRoles);
            }
        }
    }

    /// <summary>
    ///     Dictionary of member entries with cached player names - PRIVATE backing field for serialization
    ///     Key: player UID, Value: member entry with name and join date
    /// </summary>
    [ProtoMember(16)]
    private Dictionary<string, MemberEntry> _members = new();

    /// <summary>
    ///     Read-only snapshot of members (thread-safe)
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyDictionary<string, MemberEntry> Members
    {
        get
        {
            lock (_memberLock)
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
    ///     Recent activity log entries (last 100 entries, FIFO) - PRIVATE backing field for serialization
    ///     Stores favor/prestige awards from member actions.
    /// </summary>
    [ProtoMember(19)]
    private List<ActivityLogEntry> _activityLog = new();

    /// <summary>
    ///     Read-only snapshot of activity log (thread-safe)
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyList<ActivityLogEntry> ActivityLog
    {
        get
        {
            lock (_activityLock)
            {
                return _activityLog.ToList();
            }
        }
    }

    /// <summary>
    ///     Accumulated fractional prestige (not yet awarded).
    ///     Enables true 1:1 favor-to-prestige conversion for fractional favor amounts.
    /// </summary>
    [ProtoMember(20)]
    public float AccumulatedFractionalPrestige { get; set; }

    /// <summary>
    ///     Adds a member to the religion with player name (thread-safe)
    /// </summary>
    public void AddMember(string playerUID, string playerName)
    {
        lock (_memberLock)
        {
            if (!_memberUIDs.Contains(playerUID))
                _memberUIDs.Add(playerUID);

            if (!_members.ContainsKey(playerUID))
                _members[playerUID] = new MemberEntry(playerUID, playerName);
        }
    }

    /// <summary>
    ///     Removes a member from the religion (thread-safe)
    /// </summary>
    public bool RemoveMember(string playerUID)
    {
        lock (_memberLock)
        {
            _memberRoles.Remove(playerUID);
            _members.Remove(playerUID);
            return _memberUIDs.Remove(playerUID);
        }
    }

    /// <summary>
    ///     Checks if a player is a member of this religion (thread-safe)
    /// </summary>
    public bool IsMember(string playerUID)
    {
        lock (_memberLock)
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
    ///     Gets the member count (thread-safe)
    /// </summary>
    public int GetMemberCount()
    {
        lock (_memberLock)
        {
            return _memberUIDs.Count;
        }
    }

    /// <summary>
    ///     Adds fractional prestige and updates statistics when accumulated amount >= 1 (thread-safe).
    ///     Enables true 1:1 favor-to-prestige conversion for fractional favor amounts.
    /// </summary>
    public void AddFractionalPrestige(float amount)
    {
        if (amount > 0)
        {
            lock (_prestigeLock)
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
    }

    /// <summary>
    ///     Gets the cached player name for a member (fallback to UID if not found) (thread-safe)
    /// </summary>
    public string GetMemberName(string playerUID)
    {
        lock (_memberLock)
        {
            // Special case for founder - use FounderName as fallback
            if (playerUID == FounderUID && !string.IsNullOrEmpty(FounderName))
            {
                if (_members.TryGetValue(playerUID, out var founderEntry) && !string.IsNullOrEmpty(founderEntry.PlayerName))
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
    ///     Updates the cached player name if the member exists (thread-safe)
    /// </summary>
    public void UpdateMemberName(string playerUID, string playerName)
    {
        lock (_memberLock)
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
    ///     Updates the prestige rank based on total prestige earned (thread-safe)
    /// </summary>
    public void UpdatePrestigeRank()
    {
        lock (_prestigeLock)
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
    }

    /// <summary>
    ///     Adds prestige and updates statistics (thread-safe)
    /// </summary>
    public void AddPrestige(int amount)
    {
        if (amount > 0)
        {
            lock (_prestigeLock)
            {
                Prestige += amount;
                TotalPrestige += amount;

                // Update rank inline to avoid nested lock
                PrestigeRank = TotalPrestige switch
                {
                    >= 50000 => PrestigeRank.Mythic,
                    >= 25000 => PrestigeRank.Legendary,
                    >= 10000 => PrestigeRank.Renowned,
                    >= 2500 => PrestigeRank.Established,
                    _ => PrestigeRank.Fledgling
                };
            }
        }
    }

    /// <summary>
    ///     Unlocks a blessing for this religion (thread-safe)
    /// </summary>
    public void UnlockBlessing(string blessingId)
    {
        lock (_blessingLock)
        {
            _unlockedBlessings[blessingId] = true;
        }
    }

    /// <summary>
    ///     Checks if a blessing is unlocked (thread-safe)
    /// </summary>
    public bool IsBlessingUnlocked(string blessingId)
    {
        lock (_blessingLock)
        {
            return _unlockedBlessings.TryGetValue(blessingId, out var unlocked) && unlocked;
        }
    }

    /// <summary>
    ///     Adds a banned player to the religion's ban list (thread-safe)
    /// </summary>
    public void AddBannedPlayer(string playerUID, BanEntry entry)
    {
        lock (_banLock)
        {
            _bannedPlayers[playerUID] = entry;
        }
    }

    /// <summary>
    ///     Removes a banned player from the religion's ban list (thread-safe)
    /// </summary>
    public bool RemoveBannedPlayer(string playerUID)
    {
        lock (_banLock)
        {
            return _bannedPlayers.Remove(playerUID);
        }
    }

    /// <summary>
    ///     Checks if a player is banned from this religion (including expired bans) (thread-safe)
    /// </summary>
    public bool IsBanned(string playerUID)
    {
        lock (_banLock)
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
    ///     Gets the ban entry for a specific player (thread-safe)
    /// </summary>
    public BanEntry? GetBannedPlayer(string playerUID)
    {
        lock (_banLock)
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
    ///     Gets all active (non-expired) bans (thread-safe)
    /// </summary>
    public List<BanEntry> GetActiveBans()
    {
        lock (_banLock)
        {
            var now = DateTime.UtcNow;
            var expiredBans = _bannedPlayers
                .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var playerUID in expiredBans)
                _bannedPlayers.Remove(playerUID);

            return _bannedPlayers.Values.ToList();
        }
    }

    /// <summary>
    ///     Removes expired bans from the ban list (thread-safe)
    /// </summary>
    public void CleanupExpiredBans()
    {
        lock (_banLock)
        {
            var now = DateTime.UtcNow;
            var expiredBans = _bannedPlayers
                .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var playerUID in expiredBans)
                _bannedPlayers.Remove(playerUID);
        }
    }

    /// <summary>
    ///     Get player's role (thread-safe)
    /// </summary>
    public string GetPlayerRole(string playerUID)
    {
        lock (_memberLock)
        {
            if (_memberRoles.TryGetValue(playerUID, out var roleUID))
                return roleUID;

            return RoleDefaults.MEMBER_ROLE_ID; // Fallback
        }
    }

    /// <summary>
    ///     Get role data (thread-safe)
    /// </summary>
    public RoleData? GetRole(string roleUID)
    {
        lock (_roleLock)
        {
            return _roles.TryGetValue(roleUID, out var role) ? role : null;
        }
    }

    /// <summary>
    ///     Check if player has a specific permission (thread-safe)
    /// </summary>
    public bool HasPermission(string playerUID, string permission)
    {
        // Get role UID first
        string roleUID;
        lock (_memberLock)
        {
            if (!_memberRoles.TryGetValue(playerUID, out var tempRoleUID))
                roleUID = RoleDefaults.MEMBER_ROLE_ID;
            else
                roleUID = tempRoleUID;
        }

        // Then get role data
        RoleData? role;
        lock (_roleLock)
        {
            role = _roles.TryGetValue(roleUID, out var tempRole) ? tempRole : null;
        }

        if (role == null)
            return false;

        return role.HasPermission(permission);
    }

    /// <summary>
    ///     Check if player can assign a specific role (thread-safe)
    /// </summary>
    public bool CanAssignRole(string assignerUID, string targetRoleUID)
    {
        // SYSTEM can always assign roles (for automated assignments like join/invite)
        if (assignerUID == "SYSTEM")
        {
            // Cannot assign Founder role (must use transfer)
            if (targetRoleUID == RoleDefaults.FOUNDER_ROLE_ID)
                return false;

            // Role must exist
            lock (_roleLock)
            {
                if (!_roles.ContainsKey(targetRoleUID))
                    return false;
            }

            return true;
        }

        // Must have MANAGE_ROLES permission
        if (!HasPermission(assignerUID, RolePermissions.MANAGE_ROLES))
            return false;

        // Cannot assign Founder role (must use transfer)
        if (targetRoleUID == RoleDefaults.FOUNDER_ROLE_ID)
            return false;

        // Role must exist
        lock (_roleLock)
        {
            if (!_roles.ContainsKey(targetRoleUID))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Get list of roles a player can assign (thread-safe)
    /// </summary>
    public List<RoleData> GetAssignableRoles(string playerUID)
    {
        var assignable = new List<RoleData>();

        if (!HasPermission(playerUID, RolePermissions.MANAGE_ROLES))
            return assignable;

        lock (_roleLock)
        {
            foreach (var role in _roles.Values)
            {
                // Cannot assign Founder role
                if (role.RoleUID == RoleDefaults.FOUNDER_ROLE_ID)
                    continue;

                assignable.Add(role);
            }
        }

        return assignable;
    }

    /// <summary>
    ///     Get role by name (case-insensitive) (thread-safe)
    /// </summary>
    public RoleData? GetRoleByName(string roleName)
    {
        lock (_roleLock)
        {
            foreach (var role in _roles.Values)
                if (role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase))
                    return role;

            return null;
        }
    }

    /// <summary>
    ///     Check if role name is taken (thread-safe)
    /// </summary>
    public bool IsRoleNameTaken(string roleName, string? excludeRoleUID = null)
    {
        lock (_roleLock)
        {
            foreach (var role in _roles.Values)
            {
                if (role.RoleUID == excludeRoleUID)
                    continue;

                if (role.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    ///     Get members with a specific role (thread-safe)
    /// </summary>
    public List<string> GetMembersWithRole(string roleUID)
    {
        lock (_memberLock)
        {
            var members = new List<string>();

            foreach (var kvp in _memberRoles)
                if (kvp.Value == roleUID)
                    members.Add(kvp.Key);

            return members;
        }
    }

    /// <summary>
    ///     Count members per role (thread-safe)
    /// </summary>
    public Dictionary<string, int> GetRoleMemberCounts()
    {
        var counts = new Dictionary<string, int>();

        // Acquire locks in consistent order to avoid deadlocks
        lock (_roleLock)
        {
            foreach (var role in _roles.Keys)
                counts[role] = 0;
        }

        lock (_memberLock)
        {
            foreach (var roleUID in _memberRoles.Values)
                if (counts.ContainsKey(roleUID))
                    counts[roleUID]++;
                else
                    counts[roleUID] = 1;
        }

        return counts;
    }

    // ==================== Thread-Safe Mutation Methods ====================

    // MemberUIDs helpers
    /// <summary>
    ///     Adds a member UID to the list (thread-safe)
    /// </summary>
    public void AddMemberUID(string uid)
    {
        lock (_memberLock)
        {
            if (!_memberUIDs.Contains(uid))
            {
                _memberUIDs.Add(uid);
            }
        }
    }

    /// <summary>
    ///     Removes a member UID from the list (thread-safe)
    /// </summary>
    public bool RemoveMemberUID(string uid)
    {
        lock (_memberLock)
        {
            return _memberUIDs.Remove(uid);
        }
    }

    /// <summary>
    ///     Checks if a member UID exists (thread-safe)
    /// </summary>
    public bool HasMemberUID(string uid)
    {
        lock (_memberLock)
        {
            return _memberUIDs.Contains(uid);
        }
    }

    // MemberRoles helpers
    /// <summary>
    ///     Sets a member's role (thread-safe)
    /// </summary>
    public void SetMemberRole(string playerUID, string roleUID)
    {
        lock (_memberLock)
        {
            _memberRoles[playerUID] = roleUID;
        }
    }

    /// <summary>
    ///     Removes a member's role assignment (thread-safe)
    /// </summary>
    public bool RemoveMemberRole(string playerUID)
    {
        lock (_memberLock)
        {
            return _memberRoles.Remove(playerUID);
        }
    }

    // Members helpers
    /// <summary>
    ///     Gets a member entry (thread-safe)
    /// </summary>
    public MemberEntry? GetMemberEntry(string playerUID)
    {
        lock (_memberLock)
        {
            return _members.TryGetValue(playerUID, out var entry) ? entry : null;
        }
    }

    // Roles helpers
    /// <summary>
    ///     Adds or updates a role (thread-safe)
    /// </summary>
    public void SetRole(string roleUID, RoleData role)
    {
        lock (_roleLock)
        {
            _roles[roleUID] = role;
        }
    }

    /// <summary>
    ///     Removes a role (thread-safe)
    /// </summary>
    public bool RemoveRole(string roleUID)
    {
        lock (_roleLock)
        {
            return _roles.Remove(roleUID);
        }
    }

    /// <summary>
    ///     Gets all role UIDs (thread-safe)
    /// </summary>
    public List<string> GetAllRoleUIDs()
    {
        lock (_roleLock)
        {
            return _roles.Keys.ToList();
        }
    }

    // ActivityLog helpers
    /// <summary>
    ///     Adds an activity log entry atomically with FIFO eviction (thread-safe)
    /// </summary>
    public void AddActivityEntry(ActivityLogEntry entry, int maxEntries = 100)
    {
        lock (_activityLock)
        {
            _activityLog.Insert(0, entry);
            if (_activityLog.Count > maxEntries)
            {
                _activityLog.RemoveRange(maxEntries, _activityLog.Count - maxEntries);
            }
        }
    }

    /// <summary>
    ///     Clears all activity log entries (thread-safe)
    /// </summary>
    public void ClearActivityLog()
    {
        lock (_activityLock)
        {
            _activityLog.Clear();
        }
    }

    /// <summary>
    ///     Gets the activity log count (thread-safe)
    /// </summary>
    public int GetActivityLogCount()
    {
        lock (_activityLock)
        {
            return _activityLog.Count;
        }
    }

    // Prestige helpers
    /// <summary>
    ///     Gets current prestige (thread-safe)
    /// </summary>
    public int GetPrestige()
    {
        lock (_prestigeLock)
        {
            return Prestige;
        }
    }

    /// <summary>
    ///     Gets total prestige (thread-safe)
    /// </summary>
    public int GetTotalPrestige()
    {
        lock (_prestigeLock)
        {
            return TotalPrestige;
        }
    }

    /// <summary>
    ///     Gets prestige rank (thread-safe)
    /// </summary>
    public PrestigeRank GetPrestigeRank()
    {
        lock (_prestigeLock)
        {
            return PrestigeRank;
        }
    }

    /// <summary>
    ///     Gets accumulated fractional prestige (thread-safe)
    /// </summary>
    public float GetAccumulatedFractionalPrestige()
    {
        lock (_prestigeLock)
        {
            return AccumulatedFractionalPrestige;
        }
    }

    // Blessing helpers
    /// <summary>
    ///     Gets count of unlocked blessings (thread-safe)
    /// </summary>
    public int GetUnlockedBlessingCount()
    {
        lock (_blessingLock)
        {
            return _unlockedBlessings.Count(kvp => kvp.Value);
        }
    }

    /// <summary>
    ///     Gets all unlocked blessing IDs (thread-safe)
    /// </summary>
    public List<string> GetUnlockedBlessingIds()
    {
        lock (_blessingLock)
        {
            return _unlockedBlessings.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
        }
    }

    // Ban helpers
    /// <summary>
    ///     Gets count of banned players (thread-safe)
    /// </summary>
    public int GetBannedPlayerCount()
    {
        lock (_banLock)
        {
            return _bannedPlayers.Count;
        }
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