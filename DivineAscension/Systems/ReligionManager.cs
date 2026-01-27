using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Systems;

/// <summary>
///     Manages all religions and congregation membership
/// </summary>
public class ReligionManager : IReligionManager
{
    private const string DATA_KEY = "divineascension_religions";
    private const string INVITE_DATA_KEY = "divineascension_religion_invites";
    private readonly IEventService _eventService;
    private readonly ILogger _logger;
    private readonly IPersistenceService _persistenceService;
    private readonly ConcurrentDictionary<string, string> _playerToReligionIndex = new();
    private readonly ConcurrentDictionary<string, ReligionData> _religions = new();
    private readonly IWorldService _worldService;
    private ReligionWorldData _inviteData = new();

    public ReligionManager(
        ILogger logger,
        IEventService eventService,
        IPersistenceService persistenceService,
        IWorldService worldService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
    }


    internal IReadOnlyDictionary<string, string> PlayerToReligionIndex => _playerToReligionIndex;

    /// <summary>
    ///     Event fired when a religion is deleted (either manually or automatically)
    /// </summary>
    public event Action<string>? OnReligionDeleted;

    /// <inheritdoc />
    public event Action<string, string>? OnMemberAdded;

    /// <inheritdoc />
    public event Action<string, string>? OnMemberRemoved;

    /// <summary>
    ///     Initializes the religion manager
    /// </summary>
    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Religion Manager...");

        // Register event handlers using wrapper service
        _eventService.OnSaveGameLoaded(OnSaveGameLoaded);
        _eventService.OnGameWorldSave(OnGameWorldSave);

        // Load data immediately in case SaveGameLoaded event already fired
        LoadAllReligions();
        LoadInviteData();
        RebuildPlayerIndex();

        _logger.Notification("[DivineAscension] Religion Manager initialized");
    }

    public void Dispose()
    {
        _eventService.UnsubscribeSaveGameLoaded(OnSaveGameLoaded);
        _eventService.UnsubscribeGameWorldSave(OnGameWorldSave);
    }

    /// <summary>
    ///     Creates a new religion
    /// </summary>
    public ReligionData CreateReligion(string name, DeityDomain domain, string deityName, string founderUID,
        bool isPublic)
    {
        // Generate unique UID
        var religionUID = Guid.NewGuid().ToString();

        // Validate domain
        if (domain == DeityDomain.None) throw new ArgumentException("Religion must have a valid domain");

        // Validate deity name
        if (string.IsNullOrWhiteSpace(deityName))
            throw new ArgumentException("Religion must have a deity name");

        // Get founder name (player guaranteed to be online during creation)
        var founderPlayer = _worldService.GetPlayerByUID(founderUID);
        var founderName = !string.IsNullOrEmpty(founderPlayer?.PlayerName)
            ? founderPlayer.PlayerName
            : founderUID;

        // Create religion data
        var religion = new ReligionData(religionUID, name, domain, deityName, founderUID, founderName)
        {
            IsPublic = isPublic
        };
        // Initialize roles using thread-safe methods
        religion.InitializeRoles(RoleDefaults.CreateDefaultRoles());
        religion.InitializeMemberRoles(new Dictionary<string, string>
        {
            [founderUID] = RoleDefaults.FOUNDER_ROLE_ID
        });

        // Store in dictionary (thread-safe)
        _religions[religionUID] = religion;
        _playerToReligionIndex.TryAdd(founderUID, religionUID);

        _logger.Notification(
            $"[DivineAscension] Religion created: {name} (Domain: {domain}, Deity: {deityName}, Founder: {founderName}, Public: {isPublic})");

        // Immediately save to prevent data loss if server stops before autosave
        SaveAllReligions();

        return religion;
    }

    /// <summary>
    ///     Sets the deity name for a religion
    /// </summary>
    public bool SetDeityName(string religionUID, string deityName, out string error)
    {
        error = string.Empty;

        if (!_religions.TryGetValue(religionUID, out var religion))
        {
            error = "Religion not found";
            return false;
        }

        // Validate deity name
        if (string.IsNullOrWhiteSpace(deityName))
        {
            error = "Deity name cannot be empty";
            return false;
        }

        var trimmedName = deityName.Trim();

        if (trimmedName.Length < 2)
        {
            error = "Deity name must be at least 2 characters";
            return false;
        }

        if (trimmedName.Length > 48)
        {
            error = "Deity name cannot exceed 48 characters";
            return false;
        }

        // Validate allowed characters: letters, spaces, apostrophes, hyphens
        if (!Regex.IsMatch(trimmedName, @"^[\p{L}\s'\-]+$"))
        {
            error = "Deity name can only contain letters, spaces, apostrophes, and hyphens";
            return false;
        }

        // Check for profanity
        if (ProfanityFilterService.Instance.ContainsProfanity(trimmedName))
        {
            error = "Deity name contains inappropriate language";
            return false;
        }

        religion.DeityName = trimmedName;
        SaveAllReligions();

        _logger.Notification(
            $"[DivineAscension] Deity name updated for {religion.ReligionName}: {trimmedName}");

        return true;
    }

    /// <summary>
    ///     Adds a member to a religion
    /// </summary>
    public void AddMember(string religionUID, string playerUID)
    {
        if (!_religions.TryGetValue(religionUID, out var religion))
        {
            _logger.Error($"[DivineAscension] Cannot add member to non-existent religion: {religionUID}");
            return;
        }

        // Check if already a member (idempotency - prevents double-adding)
        if (religion.IsMember(playerUID))
        {
            _logger.Debug(
                $"[DivineAscension] Player {playerUID} already in religion {religion.ReligionName} - skipping add");
            return;
        }

        // Get player name (player guaranteed to be online when joining)
        var player = _worldService.GetPlayerByUID(playerUID);
        var playerName = player?.PlayerName ?? playerUID;

        religion.AddMember(playerUID, playerName);
        _playerToReligionIndex[playerUID] = religionUID; // Use indexer to avoid duplicate key exception
        _logger.Debug(
            $"[DivineAscension] Added player {playerName} ({playerUID}) to religion {religion.ReligionName}");

        // Save immediately to prevent data loss
        Save(religion);

        // Fire member added event
        OnMemberAdded?.Invoke(religionUID, playerUID);
    }

    /// <summary>
    ///     Removes a member from a religion
    /// </summary>
    public void RemoveMember(string religionUID, string playerUID)
    {
        if (!_religions.TryGetValue(religionUID, out var religion))
        {
            _logger.Error($"[DivineAscension] Cannot remove member from non-existent religion: {religionUID}");
            return;
        }

        var removed = religion.RemoveMember(playerUID);

        if (removed)
        {
            _playerToReligionIndex.TryRemove(playerUID, out _);
            _logger.Debug($"[DivineAscension] Removed player {playerUID} from religion {religion.ReligionName}");

            // Fire member removed event before potential religion deletion
            OnMemberRemoved?.Invoke(religionUID, playerUID);

            // Handle founder leaving
            if (religion.IsFounder(playerUID)) HandleFounderLeaving(religion);

            // Delete religion if no members remain
            if (religion.GetMemberCount() == 0)
            {
                _religions.TryRemove(religionUID, out _);
                _logger.Notification(
                    $"[DivineAscension] Religion {religion.ReligionName} disbanded (no members remaining)");

                // Notify subscribers that religion was deleted
                OnReligionDeleted?.Invoke(religionUID);
            }
        }

        SaveAllReligions();
    }

    /// <summary>
    ///     Gets the religion a player belongs to
    /// </summary>
    public ReligionData? GetPlayerReligion(string playerId)
    {
        if (_playerToReligionIndex.TryGetValue(playerId, out var religionId))
            return _religions.GetValueOrDefault(religionId);
        return null;
    }

    /// <summary>
    ///     Gets the religion ID a player belongs to (O(1) lookup)
    /// </summary>
    public string? GetPlayerReligionId(string playerId)
    {
        return _playerToReligionIndex.TryGetValue(playerId, out var religionId) ? religionId : null;
    }

    /// <summary>
    ///     Gets a religion by UID
    /// </summary>
    public ReligionData? GetReligion(string? religionUID)
    {
        return _religions!.GetValueOrDefault(religionUID);
    }

    /// <summary>
    ///     Gets a religion by name
    /// </summary>
    public ReligionData? GetReligionByName(string name)
    {
        return _religions.Values.FirstOrDefault(r =>
            r.ReligionName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Gets the active deity for a player
    /// </summary>
    public DeityDomain GetPlayerActiveDeityDomain(string playerId)
    {
        var religion = GetPlayerReligion(playerId);
        return religion?.Domain ?? DeityDomain.None;
    }

    /// <summary>
    ///     Checks if a player can join a religion
    /// </summary>
    public bool CanJoinReligion(string religionUID, string playerUID)
    {
        if (!_religions.TryGetValue(religionUID, out var religion)) return false;

        // Check if already a member
        if (religion.IsMember(playerUID)) return false;

        // Check if player is banned
        if (IsBanned(religionUID, playerUID)) return false;

        // Check if public or has invitation
        if (religion.IsPublic) return true;

        return HasInvitation(playerUID, religionUID);
    }

    /// <summary>
    ///     Invites a player to a religion
    /// </summary>
    public bool InvitePlayer(string religionUID, string playerUID, string inviterUID)
    {
        if (!_religions.TryGetValue(religionUID, out var religion))
        {
            _logger.Error($"[DivineAscension] Cannot invite to non-existent religion: {religionUID}");
            return false;
        }

        // Validate inviter is a member
        if (!religion.IsMember(inviterUID))
        {
            _logger.Warning($"[DivineAscension] Player {inviterUID} cannot invite to religion they're not in");
            return false;
        }

        // Check if invite already exists
        if (_inviteData.HasPendingInvite(religionUID, playerUID))
        {
            _logger.Warning("[DivineAscension] Invite already sent to this player");
            return false;
        }

        // Create structured invite with 7-day expiration
        var inviteId = Guid.NewGuid().ToString();
        var invite = new ReligionInvite(inviteId, religionUID, playerUID, DateTime.UtcNow);
        _inviteData.AddInvite(invite);

        _logger.Debug(
            $"[DivineAscension] Created invitation: InviteId={inviteId}, PlayerUID={playerUID}, ReligionUID={religionUID} ({religion.ReligionName})");

        // Save immediately to prevent data loss
        SaveInviteData();

        return true;
    }

    /// <summary>
    ///     Checks if a player has an invitation to a religion
    /// </summary>
    public bool HasInvitation(string playerUID, string religionUID)
    {
        return _inviteData.HasPendingInvite(religionUID, playerUID);
    }

    /// <summary>
    ///     Removes an invitation (called after accepting or declining)
    /// </summary>
    public void RemoveInvitation(string playerUID, string religionUID)
    {
        var invite = _inviteData.PendingInvites.FirstOrDefault(i =>
            i.PlayerUID == playerUID && i.ReligionId == religionUID);
        if (invite != null)
        {
            _inviteData.RemoveInvite(invite.InviteId);
            SaveInviteData();
        }
    }

    /// <summary>
    ///     Gets all invitations for a player
    /// </summary>
    public List<ReligionInvite> GetPlayerInvitations(string playerUID)
    {
        _inviteData ??= new ReligionWorldData();
        _inviteData.CleanupExpired();
        return _inviteData.GetInvitesForPlayer(playerUID);
    }

    /// <summary>
    ///     Accepts a religion invite
    /// </summary>
    public (bool, string, string) AcceptInvite(string inviteId, string playerUID)
    {
        var invite = _inviteData.GetInvite(inviteId);
        if (invite == null || !invite.IsValid)
        {
            _logger.Warning($"[DivineAscension] Invalid or expired invite: {inviteId}");
            return (false, string.Empty, "Invalid or expired invite");
        }

        if (invite.PlayerUID != playerUID)
        {
            _logger.Warning($"[DivineAscension] Player {playerUID} cannot accept invite for {invite.PlayerUID}");
            return (false, string.Empty, "Player cannot accept invite");
        }

        // Check if player can join
        if (HasReligion(playerUID))
        {
            _logger.Warning($"[DivineAscension] Player {playerUID} already has a religion");
            return (false, string.Empty, "Player has already has a religion");
        }

        var religion = GetReligion(invite.ReligionId);
        if (religion == null)
        {
            _logger.Warning($"[DivineAscension] Religion {invite.ReligionId} no longer exists");
            return (false, string.Empty, "No religion");
        }

        if (religion.IsBanned(playerUID))
        {
            _logger.Warning(
                $"[DivineAscension] Player {playerUID} is banned from religion {religion.ReligionName}");
            return (false, string.Empty, "Player is banned from religion");
        }

        // Join religion
        AddMember(invite.ReligionId, playerUID);
        var religionId = invite.ReligionId;
        // Remove invite
        _inviteData.RemoveInvite(inviteId);
        SaveInviteData();

        _logger.Notification($"[DivineAscension] Player {playerUID} accepted invite to {religion.ReligionName}");
        return (true, religionId, string.Empty);
    }

    /// <summary>
    ///     Declines a religion invite
    /// </summary>
    public bool DeclineInvite(string inviteId, string playerUID)
    {
        var invite = _inviteData.GetInvite(inviteId);
        if (invite == null)
        {
            _logger.Warning($"[DivineAscension] Invite not found: {inviteId}");
            return false;
        }

        if (invite.PlayerUID != playerUID)
        {
            _logger.Warning($"[DivineAscension] Player {playerUID} cannot decline invite for {invite.PlayerUID}");
            return false;
        }

        // Remove invite
        _inviteData.RemoveInvite(inviteId);
        SaveInviteData();

        _logger.Debug($"[DivineAscension] Player {playerUID} declined invite {inviteId}");
        return true;
    }

    /// <summary>
    ///     Checks if a player has a religion
    /// </summary>
    public bool HasReligion(string playerUID)
    {
        return GetPlayerReligion(playerUID) != null;
    }

    /// <summary>
    ///     Gets all religions
    /// </summary>
    public List<ReligionData> GetAllReligions()
    {
        return _religions.Values.ToList();
    }

    /// <summary>
    ///     Gets religions by domain
    /// </summary>
    public List<ReligionData> GetReligionsByDomain(DeityDomain domain)
    {
        return _religions.Values.Where(r => r.Domain == domain).ToList();
    }

    /// <summary>
    ///     Deletes a religion (founder only).
    ///     Removes all members from the index and deletes the religion data.
    /// </summary>
    public bool DeleteReligion(string religionUID, string requesterUID)
    {
        if (!_religions.TryGetValue(religionUID, out var religion))
        {
            _logger.Error($"[DivineAscension] Cannot delete non-existent religion: {religionUID}");
            return false;
        }

        // Only founder can delete
        if (!religion.IsFounder(requesterUID))
        {
            _logger.Warning(
                $"[DivineAscension] Player {requesterUID} is not founder of {religion.ReligionName}, cannot delete");
            return false;
        }

        var memberCount = religion.GetMemberCount();
        _logger.Notification(
            $"[DivineAscension] Deleting religion {religion.ReligionName} with {memberCount} member(s)...");

        // Remove all members from the index before deleting religion (thread-safe)
        var removedCount = 0;
        foreach (var memberUID in religion.MemberUIDs.ToList())
        {
            if (_playerToReligionIndex.TryRemove(memberUID, out _))
            {
                removedCount++;
                _logger.Debug($"[DivineAscension] Removed player {memberUID} from index during religion deletion");
            }
        }

        _logger.Notification(
            $"[DivineAscension] Removed {removedCount} member(s) from index for religion {religion.ReligionName}");

        // Verify all members removed
        if (removedCount != memberCount)
        {
            _logger.Warning(
                $"[DivineAscension] Member count mismatch: expected {memberCount}, removed {removedCount}");
        }

        // Delete the religion (thread-safe)
        _religions.TryRemove(religionUID, out _);
        SaveAllReligions();

        _logger.Notification($"[DivineAscension] Religion {religion.ReligionName} disbanded by founder");

        // Notify subscribers that religion was deleted (CivilizationManager cleans up alliances)
        OnReligionDeleted?.Invoke(religionUID);

        return true;
    }

    /// <summary>
    ///     Bans a player from a religion
    /// </summary>
    public bool BanPlayer(string religionUID, string playerUID, string bannedByUID, string reason = "",
        int? expiryDays = null)
    {
        if (!_religions.TryGetValue(religionUID, out var religion))
        {
            _logger.Error($"[DivineAscension] Cannot ban player from non-existent religion: {religionUID}");
            return false;
        }

        // Get player name from cached Members dictionary before removing them
        var playerName = religion.GetMemberName(playerUID);

        var banEntry = new BanEntry(
            playerUID,
            bannedByUID,
            reason,
            expiryDays.HasValue ? DateTime.UtcNow.AddDays(expiryDays.Value) : null
        )
        {
            PlayerName = playerName // Cache the player name for display when offline
        };

        religion.AddBannedPlayer(playerUID, banEntry);
        // Use RemoveMember which handles all collections (Members, MemberUIDs, MemberRoles) thread-safely
        religion.RemoveMember(playerUID);

        // Remove from player-to-religion index since they're no longer a member (thread-safe)
        _playerToReligionIndex.TryRemove(playerUID, out _);

        Save(religion);

        var expiryText = expiryDays.HasValue ? $" for {expiryDays} days" : " permanently";
        _logger.Notification(
            $"[DivineAscension] Player {playerUID} banned from {religion.ReligionName}{expiryText}. Reason: {reason}");

        return true;
    }

    /// <summary>
    ///     Unbans a player from a religion
    /// </summary>
    public bool UnbanPlayer(string religionUID, string playerUID)
    {
        if (!_religions.TryGetValue(religionUID, out var religion))
        {
            _logger.Error($"[DivineAscension] Cannot unban player from non-existent religion: {religionUID}");
            return false;
        }

        var removed = religion.RemoveBannedPlayer(playerUID);
        Save(religion);

        if (removed)
            _logger.Notification($"[DivineAscension] Player {playerUID} unbanned from {religion.ReligionName}");

        return removed;
    }

    /// <summary>
    ///     Checks if a player is banned from a religion
    /// </summary>
    public bool IsBanned(string religionUID, string playerUID)
    {
        if (!_religions.TryGetValue(religionUID, out var religion)) return false;

        religion.CleanupExpiredBans();
        return religion.IsBanned(playerUID);
    }

    /// <summary>
    ///     Gets the ban details for a player
    /// </summary>
    public BanEntry? GetBanDetails(string religionUID, string playerUID)
    {
        if (!_religions.TryGetValue(religionUID, out var religion)) return null;

        religion.CleanupExpiredBans();
        return religion.GetBannedPlayer(playerUID);
    }

    /// <summary>
    ///     Gets all banned players for a religion
    /// </summary>
    public List<BanEntry> GetBannedPlayers(string religionUID)
    {
        if (!_religions.TryGetValue(religionUID, out var religion)) return new List<BanEntry>();

        religion.CleanupExpiredBans();
        return religion.GetActiveBans();
    }

    /// <summary>
    ///     Manually triggers a save of all religion data
    /// </summary>
    public void TriggerSave()
    {
        SaveAllReligions();
    }

    /// <summary>
    ///     Validates membership consistency for a single player.
    ///     Checks if the player-to-religion index matches the actual membership in religion data.
    /// </summary>
    /// <param name="playerUID">Player UID to validate</param>
    /// <returns>Tuple: (isConsistent, issues description)</returns>
    public (bool IsConsistent, string Issues) ValidateMembershipConsistency(string playerUID)
    {
        var inIndex = _playerToReligionIndex.TryGetValue(playerUID, out var indexReligionUID);
        ReligionData? religionInIndex =
            inIndex && indexReligionUID != null ? _religions.GetValueOrDefault(indexReligionUID) : null;

        // Find which religion(s) actually have this player as a member
        var actualReligions = _religions.Values.Where(r => r.IsMember(playerUID)).ToList();

        // Case 1: Player in multiple religions (should be impossible, but check anyway)
        if (actualReligions.Count > 1)
        {
            var religionNames = string.Join(", ", actualReligions.Select(r => r.ReligionName));
            return (false,
                $"CRITICAL: Player in multiple religions: {religionNames}. This should never happen!");
        }

        // Case 2: Player in one religion
        if (actualReligions.Count == 1)
        {
            var actualReligion = actualReligions[0];

            // Check if index matches
            if (!inIndex)
            {
                return (false,
                    $"INCONSISTENT: Player is member of '{actualReligion.ReligionName}' " +
                    $"({actualReligion.ReligionUID}) but NOT in index");
            }

            if (indexReligionUID != actualReligion.ReligionUID)
            {
                return (false,
                    $"INCONSISTENT: Index points to '{indexReligionUID}' but player is member of " +
                    $"'{actualReligion.ReligionName}' ({actualReligion.ReligionUID})");
            }

            // Consistent
            return (true, "Consistent: Player in religion and index matches");
        }

        // Case 3: Player not in any religion
        if (actualReligions.Count == 0)
        {
            if (inIndex)
            {
                var indexedReligionName = religionInIndex?.ReligionName ?? indexReligionUID!;
                return (false,
                    $"INCONSISTENT: Index points to '{indexedReligionName}' but player is not a member of any religion");
            }

            // Consistent - not in any religion
            return (true, "Consistent: Player not in any religion");
        }

        return (true, "Unknown state");
    }

    /// <summary>
    ///     Repairs membership inconsistency for a single player.
    ///     Uses religion membership lists as the source of truth and updates the index accordingly.
    /// </summary>
    /// <param name="playerUID">Player UID to repair</param>
    /// <returns>True if repair was successful, false otherwise</returns>
    public bool RepairMembershipConsistency(string playerUID)
    {
        try
        {
            // Find which religion(s) actually have this player as a member
            var actualReligions = _religions.Values.Where(r => r.IsMember(playerUID)).ToList();

            // Remove player from index first (thread-safe)
            _playerToReligionIndex.TryRemove(playerUID, out _);

            // If player is in exactly one religion, add them to index
            if (actualReligions.Count == 1)
            {
                var religion = actualReligions[0];
                _playerToReligionIndex[playerUID] = religion.ReligionUID;
                _logger.Debug(
                    $"[DivineAscension] Repaired index: Added {playerUID} -> {religion.ReligionName}");
                return true;
            }

            // If player is in no religions, index removal is sufficient
            if (actualReligions.Count == 0)
            {
                _logger.Debug($"[DivineAscension] Repaired index: Removed {playerUID} (no religion)");
                return true;
            }

            // If player is in multiple religions (critical error), we can't auto-repair
            if (actualReligions.Count > 1)
            {
                _logger.Error(
                    $"[DivineAscension] Cannot auto-repair: Player {playerUID} is in multiple religions");
                return false;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error repairing membership for {playerUID}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Handles the founder leaving the religion
    /// </summary>
    private void HandleFounderLeaving(ReligionData religion)
    {
        // If there are other members, transfer founder to next member
        if (religion.GetMemberCount() > 0)
        {
            var newFounderUID = religion.MemberUIDs[0];
            religion.FounderUID = newFounderUID;

            // Update founder name and role
            var newFounderName = religion.GetMemberName(newFounderUID);
            religion.UpdateFounderName(newFounderName);
            religion.AssignMemberRole(newFounderUID, RoleDefaults.FOUNDER_ROLE_ID);

            _logger.Notification(
                $"[DivineAscension] Religion {religion.ReligionName} founder transferred to {newFounderName}");
        }
    }

    /// <summary>
    /// Rebuilds the internal player-to-religion index based on the current data in the religion manager.
    /// This index maps player unique IDs (UIDs) to the corresponding religion UIDs they are members of.
    /// The method iterates through all registered religions and their members to populate the index.
    /// Handles data corruption gracefully by detecting players in multiple religions.
    /// </summary>
    internal void RebuildPlayerIndex()
    {
        _playerToReligionIndex.Clear();
        var corruptionDetected = false;

        // Take snapshot of religions for thread-safe iteration
        foreach (var religion in _religions.ToList())
        {
            // Take snapshot of member UIDs for thread-safe iteration
            foreach (var userId in religion.Value.MemberUIDs.ToList())
            {
                // Check if player already exists in index (data corruption scenario)
                // Use TryAdd for thread-safe atomic check-and-add
                if (!_playerToReligionIndex.TryAdd(userId, religion.Key))
                {
                    corruptionDetected = true;
                    var existingReligionId = _playerToReligionIndex[userId];
                    _religions.TryGetValue(existingReligionId, out var existingReligion);

                    _logger.Error(
                        $"[DivineAscension] CRITICAL DATA CORRUPTION: Player {userId} is in multiple religions: " +
                        $"'{existingReligion?.ReligionName}' ({existingReligionId}) and " +
                        $"'{religion.Value.ReligionName}' ({religion.Key}). " +
                        $"Using first religion as authority. Run '/religion admin repair {userId}' to fix.");

                    // Use first religion found as authority, skip duplicate
                }
            }
        }

        if (corruptionDetected)
        {
            _logger.Warning(
                $"[DivineAscension] Index rebuilt with {_playerToReligionIndex.Count} entries, " +
                "but data corruption was detected. Validation will attempt auto-repair.");
        }
        else
        {
            _logger.Debug(
                $"[DivineAscension] Rebuilt player-to-religion index with {_playerToReligionIndex.Count} entries");
        }
    }


    /// <summary>
    ///     Validates all memberships to ensure consistency between the player-to-religion index
    ///     and the actual membership lists in each religion.
    /// </summary>
    /// <returns>Validation summary with counts of total, consistent, repaired, and failed players</returns>
    public (int Total, int Consistent, int Repaired, int Failed) ValidateAllMemberships()
    {
        var totalPlayers = 0;
        var consistentPlayers = 0;
        var repairedPlayers = 0;
        var failedPlayers = 0;

        // Build a set of all unique player UIDs from both sources
        var allPlayerUIDs = new HashSet<string>();

        // Add players from the index (thread-safe snapshot)
        foreach (var playerUID in _playerToReligionIndex.Keys.ToList())
        {
            allPlayerUIDs.Add(playerUID);
        }

        // Add players from all religion member lists (thread-safe snapshots)
        foreach (var religion in _religions.Values.ToList())
        {
            foreach (var memberUID in religion.MemberUIDs.ToList())
            {
                allPlayerUIDs.Add(memberUID);
            }
        }

        _logger.Notification(
            $"[DivineAscension] Starting membership validation for {allPlayerUIDs.Count} players...");

        // Validate each player
        foreach (var playerUID in allPlayerUIDs)
        {
            totalPlayers++;
            var (isConsistent, issues) = ValidateMembershipConsistency(playerUID);

            if (isConsistent)
            {
                consistentPlayers++;
            }
            else
            {
                _logger.Warning($"[DivineAscension] Player {playerUID}: {issues}");

                // Attempt to repair by rebuilding index from authoritative religion data
                var wasRepaired = RepairMembershipConsistency(playerUID);
                if (wasRepaired)
                {
                    repairedPlayers++;
                    _logger.Notification($"[DivineAscension] Successfully repaired membership for {playerUID}");
                }
                else
                {
                    failedPlayers++;
                    _logger.Error($"[DivineAscension] Failed to repair membership for {playerUID}");
                }
            }
        }

        // Log summary
        _logger.Notification(
            $"[DivineAscension] Membership validation complete: " +
            $"Total={totalPlayers}, Consistent={consistentPlayers}, Repaired={repairedPlayers}, Failed={failedPlayers}");

        return (totalPlayers, consistentPlayers, repairedPlayers, failedPlayers);
    }

    #region Persistence

    public void OnSaveGameLoaded()
    {
        LoadAllReligions();
        LoadInviteData();
    }

    public void OnGameWorldSave()
    {
        SaveAllReligions();
        SaveInviteData();
    }

    /// <summary>
    ///     Migrates existing religions that have empty DeityName fields.
    ///     Called on world load after religions are loaded from save data.
    ///     Sets DeityName to the domain name (e.g., "Craft", "Wild").
    /// </summary>
    public HashSet<string> MigrateEmptyDeityNames()
    {
        var migratedUIDs = new HashSet<string>();

        // Take snapshot for thread-safe iteration
        foreach (var religion in _religions.Values.ToList())
        {
            if (string.IsNullOrEmpty(religion.DeityName))
            {
                var domainName = religion.Domain.ToString();
                religion.DeityName = domainName;
                migratedUIDs.Add(religion.ReligionUID);
                _logger.Notification(
                    $"[DivineAscension] Migrated deity name for {religion.ReligionName}: '{domainName}'");
            }
        }

        if (migratedUIDs.Count > 0)
        {
            SaveAllReligions();
            _logger.Notification(
                $"[DivineAscension] Migrated {migratedUIDs.Count} religion(s) with empty deity names");
        }

        return migratedUIDs;
    }

    public void Save(ReligionData religionData)
    {
        try
        {
            _religions[religionData.ReligionUID] = religionData;
            SaveAllReligions();
            _logger.Debug($"[DivineAscension] Saved the {religionData.ReligionName} religion");
        }

        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Failed to save the religion: {ex.Message}");
        }
    }

    /// <summary>
    ///     Loads all religions from world storage
    /// </summary>
    private void LoadAllReligions()
    {
        try
        {
            var religionsList = _persistenceService.Load<List<ReligionData>>(DATA_KEY);
            if (religionsList != null)
            {
                _religions.Clear();
                foreach (var religion in religionsList) _religions[religion.ReligionUID] = religion;

                // Migrate religions without DeityName (pre-v3.3.0 data)
                MigrateReligionsWithoutDeityName();

                _logger.Notification($"[DivineAscension] Loaded {_religions.Count} religions");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Failed to load religions: {ex.Message}");
        }
    }

    /// <summary>
    ///     Migrates religions that don't have a DeityName (created before deity naming was added)
    ///     Sets a default deity name based on the domain (e.g., "Craft", "Wild")
    /// </summary>
    private void MigrateReligionsWithoutDeityName()
    {
        var migratedCount = 0;
        // Take snapshot for thread-safe iteration
        foreach (var religion in _religions.Values.ToList())
        {
            if (string.IsNullOrWhiteSpace(religion.DeityName))
            {
                // Set default deity name to domain name
                religion.DeityName = religion.Domain.ToString();
                migratedCount++;
            }
        }

        if (migratedCount > 0)
        {
            _logger.Notification(
                $"[DivineAscension] Migrated {migratedCount} religion(s) with default deity names");
            SaveAllReligions(); // Persist the migration
        }
    }

    /// <summary>
    ///     Saves all religions to world storage
    /// </summary>
    private void SaveAllReligions()
    {
        try
        {
            var religionsList = _religions.Values.ToList();
            _persistenceService.Save(DATA_KEY, religionsList);
            _logger.Debug($"[DivineAscension] Saved {religionsList.Count} religions");
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Failed to save religions: {ex.Message}");
        }
    }


    /// <summary>
    ///     Loads religion invite data from world storage
    /// </summary>
    private void LoadInviteData()
    {
        try
        {
            var loadedData = _persistenceService.Load<ReligionWorldData>(INVITE_DATA_KEY);
            if (loadedData != null)
            {
                _inviteData = loadedData;
                _inviteData.CleanupExpired();
                _logger.Notification(
                    $"[DivineAscension] Loaded {_inviteData.PendingInvites.Count} religion invites");

                // Log details of each invitation for debugging
                foreach (var invite in _inviteData.PendingInvites)
                {
                    var religion = GetReligion(invite.ReligionId);
                    _logger.Debug(
                        $"[DivineAscension]   - Player {invite.PlayerUID} invited to {religion?.ReligionName ?? invite.ReligionId} (expires {invite.ExpiresDate})");
                }
            }
            else
            {
                _logger.Notification("[DivineAscension] No invitation data found in save game");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Failed to load religion invites: {ex.Message}");
            _inviteData = new ReligionWorldData();
        }
    }

    /// <summary>
    ///     Saves religion invite data to world storage
    /// </summary>
    private void SaveInviteData()
    {
        try
        {
            _inviteData.CleanupExpired();
            _persistenceService.Save(INVITE_DATA_KEY, _inviteData);
            _logger.Debug($"[DivineAscension] Saved {_inviteData.PendingInvites.Count} religion invites");
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Failed to save religion invites: {ex.Message}");
        }
    }

    #endregion
}