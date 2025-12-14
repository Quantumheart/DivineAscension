using System;
using System.Collections.Generic;
using System.Linq;
using PantheonWars.Data;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace PantheonWars.Systems;

/// <summary>
///     Manages all religions and congregation membership
/// </summary>
public class ReligionManager(ICoreServerAPI sapi) : IReligionManager
{
    private const string DATA_KEY = "pantheonwars_religions";
    private const string INVITE_DATA_KEY = "pantheonwars_religion_invites";
    private readonly Dictionary<string, ReligionData> _religions = new();
    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private ReligionWorldData _inviteData = new();

    /// <summary>
    ///     Initializes the religion manager
    /// </summary>
    public void Initialize()
    {
        _sapi.Logger.Notification("[PantheonWars] Initializing Religion Manager...");

        // Register event handlers
        _sapi.Event.SaveGameLoaded += OnSaveGameLoaded;
        _sapi.Event.GameWorldSave += OnGameWorldSave;

        // Load data immediately in case SaveGameLoaded event already fired
        LoadAllReligions();
        LoadInviteData();

        _sapi.Logger.Notification("[PantheonWars] Religion Manager initialized");
    }

    public void Dispose()
    {
        _sapi.Event.SaveGameLoaded -= OnSaveGameLoaded;
        _sapi.Event.GameWorldSave -= OnGameWorldSave;
    }

    /// <summary>
    ///     Creates a new religion
    /// </summary>
    public ReligionData CreateReligion(string name, DeityType deity, string founderUID, bool isPublic)
    {
        // Generate unique UID
        var religionUID = Guid.NewGuid().ToString();

        // Validate deity type
        if (deity == DeityType.None) throw new ArgumentException("Religion must have a valid deity");

        // Get founder name (player guaranteed to be online during creation)
        var founderPlayer = _sapi.World.PlayerByUid(founderUID);
        var founderName = founderPlayer?.PlayerName ?? founderUID;

        // Create religion data
        var religion = new ReligionData(religionUID, name, deity, founderUID, founderName)
        {
            IsPublic = isPublic,
            Roles = RoleDefaults.CreateDefaultRoles(),
            MemberRoles = new Dictionary<string, string>
            {
                [founderUID] = RoleDefaults.FOUNDER_ROLE_ID
            }
        };

        // Store in dictionary
        _religions[religionUID] = religion;

        _sapi.Logger.Notification(
            $"[PantheonWars] Religion created: {name} (Deity: {deity}, Founder: {founderName}, Public: {isPublic})");

        // Immediately save to prevent data loss if server stops before autosave
        SaveAllReligions();

        return religion;
    }

    /// <summary>
    ///     Adds a member to a religion
    /// </summary>
    public void AddMember(string religionUID, string playerUID)
    {
        if (!_religions.TryGetValue(religionUID, out var religion))
        {
            _sapi.Logger.Error($"[PantheonWars] Cannot add member to non-existent religion: {religionUID}");
            return;
        }

        // Get player name (player guaranteed to be online when joining)
        var player = _sapi.World.PlayerByUid(playerUID);
        var playerName = player?.PlayerName ?? playerUID;

        religion.AddMember(playerUID, playerName);
        _sapi.Logger.Debug(
            $"[PantheonWars] Added player {playerName} ({playerUID}) to religion {religion.ReligionName}");

        // Save immediately to prevent data loss
        Save(religion);
    }

    /// <summary>
    ///     Removes a member from a religion
    /// </summary>
    public void RemoveMember(string religionUID, string playerUID)
    {
        if (!_religions.TryGetValue(religionUID, out var religion))
        {
            _sapi.Logger.Error($"[PantheonWars] Cannot remove member from non-existent religion: {religionUID}");
            return;
        }

        var removed = religion.RemoveMember(playerUID);

        if (removed)
        {
            _sapi.Logger.Debug($"[PantheonWars] Removed player {playerUID} from religion {religion.ReligionName}");

            // Handle founder leaving
            if (religion.IsFounder(playerUID)) HandleFounderLeaving(religion);

            // Delete religion if no members remain
            if (religion.GetMemberCount() == 0)
            {
                _religions.Remove(religionUID);
                _sapi.Logger.Notification(
                    $"[PantheonWars] Religion {religion.ReligionName} disbanded (no members remaining)");
            }

            // Save immediately to prevent data loss
            Save(religion);
        }

        Save(religion);
    }

    /// <summary>
    ///     Gets the religion a player belongs to
    /// </summary>
    public ReligionData? GetPlayerReligion(string playerUID)
    {
        return _religions.Values.FirstOrDefault(r => r.IsMember(playerUID));
    }

    /// <summary>
    ///     Gets a religion by UID
    /// </summary>
    public ReligionData? GetReligion(string religionUID)
    {
        return _religions.GetValueOrDefault(religionUID);
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
    public DeityType GetPlayerActiveDeity(string playerUID)
    {
        var religion = GetPlayerReligion(playerUID);
        return religion?.Deity ?? DeityType.None;
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
            _sapi.Logger.Error($"[PantheonWars] Cannot invite to non-existent religion: {religionUID}");
            return false;
        }

        // Validate inviter is a member
        if (!religion.IsMember(inviterUID))
        {
            _sapi.Logger.Warning($"[PantheonWars] Player {inviterUID} cannot invite to religion they're not in");
            return false;
        }

        // Check if invite already exists
        if (_inviteData.HasPendingInvite(religionUID, playerUID))
        {
            _sapi.Logger.Warning("[PantheonWars] Invite already sent to this player");
            return false;
        }

        // Create structured invite with 7-day expiration
        var inviteId = Guid.NewGuid().ToString();
        var invite = new ReligionInvite(inviteId, religionUID, playerUID, DateTime.UtcNow);
        _inviteData.AddInvite(invite);

        _sapi.Logger.Debug(
            $"[PantheonWars] Created invitation: InviteId={inviteId}, PlayerUID={playerUID}, ReligionUID={religionUID} ({religion.ReligionName})");

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
            _sapi.Logger.Warning($"[PantheonWars] Invalid or expired invite: {inviteId}");
            return (false, string.Empty, "Invalid or expired invite");
        }

        if (invite.PlayerUID != playerUID)
        {
            _sapi.Logger.Warning($"[PantheonWars] Player {playerUID} cannot accept invite for {invite.PlayerUID}");
            return (false, string.Empty, "Player cannot accept invite");
        }

        // Check if player can join
        if (HasReligion(playerUID))
        {
            _sapi.Logger.Warning($"[PantheonWars] Player {playerUID} already has a religion");
            return (false, string.Empty, "Player has already has a religion");
        }

        var religion = GetReligion(invite.ReligionId);
        if (religion == null)
        {
            _sapi.Logger.Warning($"[PantheonWars] Religion {invite.ReligionId} no longer exists");
            return (false, string.Empty, "No religion");
        }

        if (religion.IsBanned(playerUID))
        {
            _sapi.Logger.Warning($"[PantheonWars] Player {playerUID} is banned from religion {religion.ReligionName}");
            return (false, string.Empty, "Player is banned from religion");
        }

        // Join religion
        AddMember(invite.ReligionId, playerUID);
        var religionId = invite.ReligionId;
        // Remove invite
        _inviteData.RemoveInvite(inviteId);
        SaveInviteData();

        _sapi.Logger.Notification($"[PantheonWars] Player {playerUID} accepted invite to {religion.ReligionName}");
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
            _sapi.Logger.Warning($"[PantheonWars] Invite not found: {inviteId}");
            return false;
        }

        if (invite.PlayerUID != playerUID)
        {
            _sapi.Logger.Warning($"[PantheonWars] Player {playerUID} cannot decline invite for {invite.PlayerUID}");
            return false;
        }

        // Remove invite
        _inviteData.RemoveInvite(inviteId);
        SaveInviteData();

        _sapi.Logger.Debug($"[PantheonWars] Player {playerUID} declined invite {inviteId}");
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
    ///     Gets religions by deity
    /// </summary>
    public List<ReligionData> GetReligionsByDeity(DeityType deity)
    {
        return _religions.Values.Where(r => r.Deity == deity).ToList();
    }

    /// <summary>
    ///     Deletes a religion (founder only)
    /// </summary>
    public bool DeleteReligion(string religionUID, string requesterUID)
    {
        if (!_religions.TryGetValue(religionUID, out var religion)) return false;

        // Only founder can delete
        if (!religion.IsFounder(requesterUID)) return false;

        _religions.Remove(religionUID);
        _sapi.Logger.Notification($"[PantheonWars] Religion {religion.ReligionName} disbanded by founder");
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
            _sapi.Logger.Error($"[PantheonWars] Cannot ban player from non-existent religion: {religionUID}");
            return false;
        }

        var banEntry = new BanEntry(
            playerUID,
            bannedByUID,
            reason,
            expiryDays.HasValue ? DateTime.UtcNow.AddDays(expiryDays.Value) : null
        );

        religion.AddBannedPlayer(playerUID, banEntry);
        religion.Members.Remove(playerUID);
        religion.MemberUIDs.Remove(playerUID);
        Save(religion);

        var expiryText = expiryDays.HasValue ? $" for {expiryDays} days" : " permanently";
        _sapi.Logger.Notification(
            $"[PantheonWars] Player {playerUID} banned from {religion.ReligionName}{expiryText}. Reason: {reason}");

        return true;
    }

    /// <summary>
    ///     Unbans a player from a religion
    /// </summary>
    public bool UnbanPlayer(string religionUID, string playerUID)
    {
        if (!_religions.TryGetValue(religionUID, out var religion))
        {
            _sapi.Logger.Error($"[PantheonWars] Cannot unban player from non-existent religion: {religionUID}");
            return false;
        }

        var removed = religion.RemoveBannedPlayer(playerUID);
        Save(religion);

        if (removed)
            _sapi.Logger.Notification($"[PantheonWars] Player {playerUID} unbanned from {religion.ReligionName}");

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
            religion.MemberRoles[newFounderUID] = RoleDefaults.FOUNDER_ROLE_ID;

            _sapi.Logger.Notification(
                $"[PantheonWars] Religion {religion.ReligionName} founder transferred to {newFounderName}");
        }
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

    public void Save(ReligionData religionData)
    {
        try
        {
            _religions[religionData.ReligionUID] = religionData;
            SaveAllReligions();
            _sapi.Logger.Debug($"[PantheonWars] Saved the {religionData.ReligionName} religion");
        }

        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Failed to save the religion: {ex.Message}");
        }
    }

    /// <summary>
    ///     Loads all religions from world storage
    /// </summary>
    private void LoadAllReligions()
    {
        try
        {
            var data = _sapi.WorldManager.SaveGame.GetData(DATA_KEY);
            if (data != null)
            {
                var religionsList = SerializerUtil.Deserialize<List<ReligionData>>(data);
                if (religionsList != null)
                {
                    _religions.Clear();
                    foreach (var religion in religionsList) _religions[religion.ReligionUID] = religion;
                    _sapi.Logger.Notification($"[PantheonWars] Loaded {_religions.Count} religions");
                }
            }
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Failed to load religions: {ex.Message}");
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
            var data = SerializerUtil.Serialize(religionsList);
            _sapi.WorldManager.SaveGame.StoreData(DATA_KEY, data);
            _sapi.Logger.Debug($"[PantheonWars] Saved {religionsList.Count} religions");
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Failed to save religions: {ex.Message}");
        }
    }


    /// <summary>
    ///     Loads religion invite data from world storage
    /// </summary>
    private void LoadInviteData()
    {
        try
        {
            var data = _sapi.WorldManager.SaveGame.GetData(INVITE_DATA_KEY);
            if (data != null)
            {
                _inviteData = SerializerUtil.Deserialize<ReligionWorldData>(data) ?? new ReligionWorldData();
                _inviteData.CleanupExpired();
                _sapi.Logger.Notification($"[PantheonWars] Loaded {_inviteData.PendingInvites.Count} religion invites");

                // Log details of each invitation for debugging
                foreach (var invite in _inviteData.PendingInvites)
                {
                    var religion = GetReligion(invite.ReligionId);
                    _sapi.Logger.Debug(
                        $"[PantheonWars]   - Player {invite.PlayerUID} invited to {religion?.ReligionName ?? invite.ReligionId} (expires {invite.ExpiresDate})");
                }
            }
            else
            {
                _sapi.Logger.Notification("[PantheonWars] No invitation data found in save game");
            }
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Failed to load religion invites: {ex.Message}");
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
            var data = SerializerUtil.Serialize(_inviteData);
            _sapi.WorldManager.SaveGame.StoreData(INVITE_DATA_KEY, data);
            _sapi.Logger.Debug($"[PantheonWars] Saved {_inviteData.PendingInvites.Count} religion invites");
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Failed to save religion invites: {ex.Message}");
        }
    }

    #endregion
}