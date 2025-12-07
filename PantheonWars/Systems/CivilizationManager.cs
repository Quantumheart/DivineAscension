using System;
using System.Collections.Generic;
using System.Linq;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace PantheonWars.Systems;

/// <summary>
///     Manages civilizations - alliances of 1-4 religions with different deities
/// </summary>
public class CivilizationManager
{
    private const string DATA_KEY = "pantheonwars_civilizations";
    private const int MIN_RELIGIONS = 1;
    private const int MAX_RELIGIONS = 4;
    private const int COOLDOWN_DAYS = 7;
    private const int INVITE_EXPIRY_DAYS = 7;
    private readonly DeityRegistry _deityRegistry;
    private readonly ReligionManager _religionManager;

    private readonly ICoreServerAPI _sapi;
    private CivilizationWorldData _data;

    public CivilizationManager(ICoreServerAPI sapi, ReligionManager religionManager, DeityRegistry deityRegistry)
    {
        _sapi = sapi;
        _religionManager = religionManager;
        _deityRegistry = deityRegistry;
        _data = new CivilizationWorldData();
    }

    /// <summary>
    ///     Initializes the civilization manager
    /// </summary>
    public void Initialize()
    {
        _sapi.Logger.Notification("[PantheonWars] Initializing Civilization Manager...");

        // Register event handlers
        _sapi.Event.SaveGameLoaded += OnSaveGameLoaded;
        _sapi.Event.GameWorldSave += OnGameWorldSave;

        _sapi.Logger.Notification("[PantheonWars] Civilization Manager initialized");
    }

    #region Civilization CRUD

    /// <summary>
    ///     Creates a new civilization
    /// </summary>
    /// <param name="name">Name of the civilization</param>
    /// <param name="founderUID">Player UID of the founder</param>
    /// <param name="founderReligionId">Religion ID of the founder</param>
    /// <returns>The created civilization, or null if creation failed</returns>
    public Civilization? CreateCivilization(string name, string founderUID, string founderReligionId)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(name))
            {
                _sapi.Logger.Warning("[PantheonWars] Cannot create civilization with empty name");
                return null;
            }

            if (name.Length < 3 || name.Length > 32)
            {
                _sapi.Logger.Warning("[PantheonWars] Civilization name must be 3-32 characters");
                return null;
            }

            // Check if name already exists
            if (_data.Civilizations.Values.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                _sapi.Logger.Warning($"[PantheonWars] Civilization name '{name}' already exists");
                return null;
            }

            // Validate founder's religion exists
            var founderReligion = _religionManager.GetReligion(founderReligionId);
            if (founderReligion == null)
            {
                _sapi.Logger.Warning($"[PantheonWars] Founder religion '{founderReligionId}' not found");
                return null;
            }

            // Check if founder is the religion founder
            if (founderReligion.FounderUID != founderUID)
            {
                _sapi.Logger.Warning("[PantheonWars] Only religion founders can create civilizations");
                return null;
            }

            // Check if religion is already in a civilization
            if (_data.GetCivilizationByReligion(founderReligionId) != null)
            {
                _sapi.Logger.Warning(
                    $"[PantheonWars] Religion '{founderReligion.ReligionName}' is already in a civilization");
                return null;
            }

            // Create civilization
            var civId = Guid.NewGuid().ToString();
            var civ = new Civilization(civId, name, founderUID, founderReligionId)
            {
                MemberCount = founderReligion.MemberUIDs.Count
            };

            _data.AddCivilization(civ);

            _sapi.Logger.Notification($"[PantheonWars] Civilization '{name}' created by {founderUID}");
            return civ;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Error creating civilization: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Invites a religion to join a civilization
    /// </summary>
    public bool InviteReligion(string civId, string religionId, string inviterUID)
    {
        try
        {
            var civ = _data.Civilizations.GetValueOrDefault(civId);
            if (civ == null)
            {
                _sapi.Logger.Warning($"[PantheonWars] Civilization '{civId}' not found");
                return false;
            }

            // Check if inviter is the civilization founder
            if (civ.FounderUID != inviterUID)
            {
                _sapi.Logger.Warning("[PantheonWars] Only civilization founder can invite religions");
                return false;
            }

            // Check if civilization is full
            if (civ.MemberReligionIds.Count >= MAX_RELIGIONS)
            {
                _sapi.Logger.Warning("[PantheonWars] Civilization is full (max 4 religions)");
                return false;
            }

            // Validate target religion exists
            var targetReligion = _religionManager.GetReligion(religionId);
            if (targetReligion == null)
            {
                _sapi.Logger.Warning($"[PantheonWars] Target religion '{religionId}' not found");
                return false;
            }

            // Check if religion is already a member
            if (civ.HasReligion(religionId))
            {
                _sapi.Logger.Warning($"[PantheonWars] Religion '{targetReligion.ReligionName}' is already a member");
                return false;
            }

            // Check if religion is already in another civilization
            if (_data.GetCivilizationByReligion(religionId) != null)
            {
                _sapi.Logger.Warning(
                    $"[PantheonWars] Religion '{targetReligion.ReligionName}' is already in a civilization");
                return false;
            }

            // Check if invite already exists
            if (_data.HasPendingInvite(civId, religionId))
            {
                _sapi.Logger.Warning("[PantheonWars] Invite already sent to this religion");
                return false;
            }

            // Check deity diversity (no duplicate deities)
            var civDeities = GetCivDeityTypes(civId);
            if (civDeities.Contains(targetReligion.Deity))
            {
                _sapi.Logger.Warning($"[PantheonWars] Civilization already has a {targetReligion.Deity} religion");
                return false;
            }

            // Create invite
            var inviteId = Guid.NewGuid().ToString();
            var invite = new CivilizationInvite(inviteId, civId, religionId, DateTime.UtcNow);
            _data.AddInvite(invite);

            _sapi.Logger.Notification(
                $"[PantheonWars] Invited religion '{targetReligion.ReligionName}' to civilization '{civ.Name}'");
            return true;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Error inviting religion: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Accepts an invitation to join a civilization
    /// </summary>
    public bool AcceptInvite(string inviteId, string accepterUID)
    {
        try
        {
            var invite = _data.GetInvite(inviteId);
            if (invite == null || !invite.IsValid)
            {
                _sapi.Logger.Warning("[PantheonWars] Invite not found or expired");
                return false;
            }

            var civ = _data.Civilizations.GetValueOrDefault(invite.CivId);
            if (civ == null)
            {
                _sapi.Logger.Warning($"[PantheonWars] Civilization '{invite.CivId}' not found");
                _data.RemoveInvite(inviteId);
                return false;
            }

            var religion = _religionManager.GetReligion(invite.ReligionId);
            if (religion == null)
            {
                _sapi.Logger.Warning($"[PantheonWars] Religion '{invite.ReligionId}' not found");
                _data.RemoveInvite(inviteId);
                return false;
            }

            // Check if accepter is the religion founder
            if (religion.FounderUID != accepterUID)
            {
                _sapi.Logger.Warning("[PantheonWars] Only religion founder can accept civilization invites");
                return false;
            }

            // Check if civilization still has space
            if (civ.MemberReligionIds.Count >= MAX_RELIGIONS)
            {
                _sapi.Logger.Warning("[PantheonWars] Civilization is now full");
                _data.RemoveInvite(inviteId);
                return false;
            }

            // Add religion to civilization
            if (!_data.AddReligionToCivilization(invite.CivId, invite.ReligionId))
            {
                _sapi.Logger.Error("[PantheonWars] Failed to add religion to civilization");
                return false;
            }

            // Update member count
            civ.MemberCount += religion.MemberUIDs.Count;

            // Remove invite
            _data.RemoveInvite(inviteId);

            _sapi.Logger.Notification(
                $"[PantheonWars] Religion '{religion.ReligionName}' joined civilization '{civ.Name}'");
            return true;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Error accepting invite: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     A religion leaves a civilization voluntarily
    /// </summary>
    public bool LeaveReligion(string religionId, string requesterUID)
    {
        try
        {
            var civ = _data.GetCivilizationByReligion(religionId);
            if (civ == null)
            {
                _sapi.Logger.Warning("[PantheonWars] Religion is not in a civilization");
                return false;
            }

            var religion = _religionManager.GetReligion(religionId);
            if (religion == null)
            {
                _sapi.Logger.Warning($"[PantheonWars] Religion '{religionId}' not found");
                return false;
            }

            // Check if requester is the religion founder
            if (religion.FounderUID != requesterUID)
            {
                _sapi.Logger.Warning("[PantheonWars] Only religion founder can leave civilization");
                return false;
            }

            // If this is the civilization founder's religion, disband instead
            if (civ.FounderUID == requesterUID)
            {
                _sapi.Logger.Warning("[PantheonWars] Civilization founder must disband, not leave");
                return false;
            }

            // Remove religion from civilization
            _data.RemoveReligionFromCivilization(religionId);
            civ.MemberCount -= religion.MemberUIDs.Count;
            

            // Check if civilization falls below minimum
            if (civ.MemberReligionIds.Count < MIN_RELIGIONS)
            {
                DisbandCivilization(civ.CivId, civ.FounderUID);
                _sapi.Logger.Notification(
                    $"[PantheonWars] Civilization '{civ.Name}' disbanded (below minimum religions)");
            }
            else
            {
                _sapi.Logger.Notification(
                    $"[PantheonWars] Religion '{religion.ReligionName}' left civilization '{civ.Name}'");
            }

            return true;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Error leaving civilization: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Kicks a religion from a civilization
    /// </summary>
    public bool KickReligion(string civId, string religionId, string kickerUID)
    {
        try
        {
            var civ = _data.Civilizations.GetValueOrDefault(civId);
            if (civ == null)
            {
                _sapi.Logger.Warning($"[PantheonWars] Civilization '{civId}' not found");
                return false;
            }

            // Check if kicker is the civilization founder
            if (civ.FounderUID != kickerUID)
            {
                _sapi.Logger.Warning("[PantheonWars] Only civilization founder can kick religions");
                return false;
            }

            // Check if religion is a member
            if (!civ.HasReligion(religionId))
            {
                _sapi.Logger.Warning("[PantheonWars] Religion is not a member of this civilization");
                return false;
            }

            var religion = _religionManager.GetReligion(religionId);
            if (religion == null)
            {
                _sapi.Logger.Warning($"[PantheonWars] Religion '{religionId}' not found");
                return false;
            }

            // Cannot kick own religion
            var kickerReligion = _religionManager.GetPlayerReligion(kickerUID);
            if (kickerReligion?.ReligionUID == religionId)
            {
                _sapi.Logger.Warning("[PantheonWars] Cannot kick your own religion");
                return false;
            }

            // Remove religion from civilization
            _data.RemoveReligionFromCivilization(religionId);
            civ.MemberCount -= religion.MemberUIDs.Count;

            // Check if civilization falls below minimum
            if (civ.MemberReligionIds.Count < MIN_RELIGIONS)
            {
                DisbandCivilization(civ.CivId, civ.FounderUID);
                _sapi.Logger.Notification(
                    $"[PantheonWars] Civilization '{civ.Name}' disbanded (below minimum religions)");
            }
            else
            {
                _sapi.Logger.Notification(
                    $"[PantheonWars] Religion '{religion.ReligionName}' kicked from civilization '{civ.Name}'");
            }

            return true;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Error kicking religion: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Disbands a civilization
    /// </summary>
    public bool DisbandCivilization(string civId, string requesterUID)
    {
        try
        {
            var civ = _data.Civilizations.GetValueOrDefault(civId);
            if (civ == null)
            {
                _sapi.Logger.Warning($"[PantheonWars] Civilization '{civId}' not found");
                return false;
            }

            // Check if requester is the civilization founder
            if (civ.FounderUID != requesterUID)
            {
                _sapi.Logger.Warning("[PantheonWars] Only civilization founder can disband");
                return false;
            }

            // Mark as disbanded
            civ.DisbandedDate = DateTime.UtcNow;

            // Remove all pending invites for this civilization
            _data.PendingInvites.RemoveAll(i => i.CivId == civId);

            // Remove civilization
            _data.RemoveCivilization(civId);

            _sapi.Logger.Notification($"[PantheonWars] Civilization '{civ.Name}' disbanded by founder");
            return true;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Error disbanding civilization: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Query Methods

    /// <summary>
    ///     Gets a civilization by ID
    /// </summary>
    public Civilization? GetCivilization(string civId)
    {
        return _data.Civilizations.GetValueOrDefault(civId);
    }

    /// <summary>
    ///     Gets the civilization a religion belongs to
    /// </summary>
    public Civilization? GetCivilizationByReligion(string religionId)
    {
        return _data.GetCivilizationByReligion(religionId);
    }

    /// <summary>
    ///     Gets the civilization a player belongs to (via their religion)
    /// </summary>
    public Civilization? GetCivilizationByPlayer(string playerUID)
    {
        var religion = _religionManager.GetPlayerReligion(playerUID);
        if (religion == null)
            return null;

        return _data.GetCivilizationByReligion(religion.ReligionUID);
    }

    /// <summary>
    ///     Gets all civilizations
    /// </summary>
    public IEnumerable<Civilization> GetAllCivilizations()
    {
        return _data.Civilizations.Values;
    }

    /// <summary>
    ///     Gets all deity types in a civilization
    /// </summary>
    public HashSet<DeityType> GetCivDeityTypes(string civId)
    {
        var civ = _data.Civilizations.GetValueOrDefault(civId);
        if (civ == null)
            return new HashSet<DeityType>();

        var deities = new HashSet<DeityType>();
        foreach (var religionId in civ.MemberReligionIds)
        {
            var religion = _religionManager.GetReligion(religionId);
            if (religion != null) deities.Add(religion.Deity);
        }

        return deities;
    }

    /// <summary>
    ///     Gets all religions in a civilization
    /// </summary>
    public List<ReligionData> GetCivReligions(string civId)
    {
        var civ = _data.Civilizations.GetValueOrDefault(civId);
        if (civ == null)
            return new List<ReligionData>();

        var religions = new List<ReligionData>();
        foreach (var religionId in civ.MemberReligionIds)
        {
            var religion = _religionManager.GetReligion(religionId);
            if (religion != null) religions.Add(religion);
        }

        return religions;
    }

    /// <summary>
    ///     Gets all pending invites for a religion
    /// </summary>
    public List<CivilizationInvite> GetInvitesForReligion(string religionId)
    {
        return _data.GetInvitesForReligion(religionId);
    }

    /// <summary>
    ///     Gets all pending invites for a civilization
    /// </summary>
    public List<CivilizationInvite> GetInvitesForCiv(string civId)
    {
        return _data.GetInvitesForCivilization(civId);
    }
    
    /// <summary>
    ///     Updates member counts for all civilizations (should be called when religion membership changes)
    /// </summary>
    public void UpdateMemberCounts()
    {
        foreach (var civ in _data.Civilizations.Values)
        {
            var totalMembers = 0;
            foreach (var religionId in civ.MemberReligionIds)
            {
                var religion = _religionManager.GetReligion(religionId);
                if (religion != null) totalMembers += religion.MemberUIDs.Count;
            }

            civ.MemberCount = totalMembers;
        }
    }

    #endregion

    #region Persistence

    private void OnSaveGameLoaded()
    {
        LoadCivilizations();
    }

    private void OnGameWorldSave()
    {
        SaveCivilizations();
    }

    private void LoadCivilizations()
    {
        try
        {
            var data = _sapi.WorldManager.SaveGame.GetData(DATA_KEY);
            if (data != null)
            {
                var loadedData = SerializerUtil.Deserialize<CivilizationWorldData>(data);
                if (loadedData != null)
                {
                    _data = loadedData;
                    _sapi.Logger.Notification($"[PantheonWars] Loaded {_data.Civilizations.Count} civilizations");
                }
                else
                {
                    _sapi.Logger.Warning("[PantheonWars] Failed to deserialize civilization data");
                    _data = new CivilizationWorldData();
                }
            }
            else
            {
                _sapi.Logger.Debug("[PantheonWars] No civilization data found, starting fresh");
                _data = new CivilizationWorldData();
            }
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Failed to load civilizations: {ex.Message}");
            _data = new CivilizationWorldData();
        }
    }

    private void SaveCivilizations()
    {
        try
        {
            var serializedData = SerializerUtil.Serialize(_data);
            _sapi.WorldManager.SaveGame.StoreData(DATA_KEY, serializedData);
            _sapi.Logger.Debug($"[PantheonWars] Saved {_data.Civilizations.Count} civilizations");
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[PantheonWars] Failed to save civilizations: {ex.Message}");
        }
    }

    #endregion
}