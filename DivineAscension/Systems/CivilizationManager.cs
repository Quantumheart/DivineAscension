using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace DivineAscension.Systems;

/// <summary>
///     Manages civilizations - alliances of 1-4 religions with different deities
/// </summary>
public class CivilizationManager(ICoreServerAPI sapi, IReligionManager religionManager) : ICivilizationManager
{
    private const string DATA_KEY = "divineascension_civilizations";
    private const int MIN_RELIGIONS = 1;
    private const int MAX_RELIGIONS = 4;
    private const int INVITE_EXPIRY_DAYS = 7;

    private readonly IReligionManager _religionManager =
        religionManager ?? throw new ArgumentNullException(nameof(religionManager));

    private readonly ICoreServerAPI _sapi = sapi ?? throw new ArgumentNullException(nameof(sapi));
    private CivilizationWorldData _data = new();
    private readonly object _dataLock = new(); // Thread-safety lock for all _data access

    /// <summary>
    ///     Event fired when a civilization is disbanded
    /// </summary>
    public event Action<string>? OnCivilizationDisbanded;

    /// <summary>
    ///     Initializes the civilization manager
    /// </summary>
    public void Initialize()
    {
        _sapi.Logger.Notification("[DivineAscension] Initializing Civilization Manager...");

        // Register event handlers
        _sapi.Event.SaveGameLoaded += OnSaveGameLoaded;
        _sapi.Event.GameWorldSave += OnGameWorldSave;

        // Subscribe to religion deletion events
        _religionManager.OnReligionDeleted += HandleReligionDeleted;

        _sapi.Logger.Notification("[DivineAscension] Civilization Manager initialized");
    }

    /// <summary>
    ///     Cleans up event subscriptions
    /// </summary>
    public void Dispose()
    {
        // Unsubscribe from events
        _sapi.Event.SaveGameLoaded -= OnSaveGameLoaded;
        _sapi.Event.GameWorldSave -= OnGameWorldSave;
        _religionManager.OnReligionDeleted -= HandleReligionDeleted;
        OnCivilizationDisbanded = null;
    }

    #region Event Handlers

    /// <summary>
    ///     Handles religion deletion events from ReligionManager
    /// </summary>
    private void HandleReligionDeleted(string religionId)
    {
        try
        {
            Civilization? civ;
            bool isFounderReligion;
            bool shouldDisband;
            string civName;
            List<string> memberReligionIds;

            lock (_dataLock)
            {
                civ = _data.GetCivilizationByReligion(religionId);
                if (civ == null)
                    // Religion wasn't in a civilization, nothing to do
                    return;

                civName = civ.Name;
                _sapi.Logger.Debug(
                    $"[DivineAscension] Handling deletion of religion {religionId} from civilization {civName}");

                // Check if the deleted religion was the founder's religion
                isFounderReligion = civ.FounderReligionUID == religionId;

                // Remove religion from civilization
                _data.RemoveReligionFromCivilization(religionId);

                // Get snapshot of member religion IDs for external calls
                memberReligionIds = new List<string>(civ.MemberReligionIds);
            }

            // Update member count (recalculate from remaining religions) - external calls OUTSIDE lock
            var totalMembers = 0;
            foreach (var relId in memberReligionIds)
            {
                var religion = _religionManager.GetReligion(relId);
                if (religion != null) totalMembers += religion.MemberUIDs.Count;
            }

            lock (_dataLock)
            {
                if (civ != null)
                {
                    civ.MemberCount = totalMembers;
                    shouldDisband = isFounderReligion || civ.MemberReligionIds.Count < MIN_RELIGIONS;
                }
                else
                {
                    shouldDisband = false;
                }
            }

            // Disband if founder's religion was deleted OR if below minimum religions
            if (shouldDisband && civ != null)
            {
                // Use ForceDisband to bypass permission checks (system cleanup)
                ForceDisband(civ.CivId);
                if (isFounderReligion)
                    _sapi.Logger.Notification(
                        $"[DivineAscension] Civilization '{civName}' disbanded (founder's religion was deleted)");
                else
                    _sapi.Logger.Notification(
                        $"[DivineAscension] Civilization '{civName}' disbanded (religion {religionId} was deleted, below minimum)");
            }
            else if (civ != null)
            {
                _sapi.Logger.Notification(
                    $"[DivineAscension] Removed deleted religion {religionId} from civilization '{civName}'");
            }
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Error handling religion deletion: {ex.Message}");

            // If disband failed but religion was removed, manually clean up any orphaned civilizations
            List<Civilization> orphanedCivs;
            lock (_dataLock)
            {
                orphanedCivs = _data.Civilizations.Values
                    .Where(c => c.MemberReligionIds.Count == 0)
                    .ToList();
            }

            if (orphanedCivs.Any())
            {
                _sapi.Logger.Warning(
                    $"[DivineAscension] Found {orphanedCivs.Count} orphaned civilization(s) after exception, forcing cleanup");
                foreach (var orphan in orphanedCivs)
                {
                    try
                    {
                        ForceDisband(orphan.CivId);
                    }
                    catch (Exception cleanupEx)
                    {
                        _sapi.Logger.Error(
                            $"[DivineAscension] Failed to cleanup orphaned civilization {orphan.Name}: {cleanupEx.Message}");
                    }
                }
            }
        }
    }

    #endregion

    #region Civilization CRUD

    /// <summary>
    ///     Creates a new civilization
    /// </summary>
    /// <param name="name">Name of the civilization</param>
    /// <param name="founderUID">Player UID of the founder</param>
    /// <param name="founderReligionId">Religion ID of the founder</param>
    /// <param name="icon">Optional icon name for the civilization (defaults to "default")</param>
    /// <param name="description">Optional description for the civilization</param>
    /// <returns>The created civilization, or null if creation failed</returns>
    public Civilization? CreateCivilization(string name, string founderUID, string founderReligionId,
        string icon = "default", string description = "")
    {
        try
        {
            // Validate inputs BEFORE lock
            if (string.IsNullOrWhiteSpace(name))
            {
                _sapi.Logger.Warning("[DivineAscension] Cannot create civilization with empty name");
                return null;
            }

            if (name.Length < 3 || name.Length > 32)
            {
                _sapi.Logger.Warning("[DivineAscension] Civilization name must be 3-32 characters");
                return null;
            }

            // Validate description length (max 200 characters)
            if (description.Length > 200)
            {
                _sapi.Logger.Warning("[DivineAscension] Description must be 200 characters or less");
                return null;
            }

            // Validate founder's religion exists (external call, OUTSIDE lock)
            var founderReligion = _religionManager.GetReligion(founderReligionId);
            if (founderReligion == null)
            {
                _sapi.Logger.Warning($"[DivineAscension] Founder religion '{founderReligionId}' not found");
                return null;
            }

            // Check if founder is the religion founder (OUTSIDE lock)
            if (founderReligion.FounderUID != founderUID)
            {
                _sapi.Logger.Warning("[DivineAscension] Only religion founders can create civilizations");
                return null;
            }

            Civilization civ;
            lock (_dataLock)
            {
                // Check if name already exists
                if (_data.Civilizations.Values.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    _sapi.Logger.Warning($"[DivineAscension] Civilization name '{name}' already exists");
                    return null;
                }

                // Check if religion is already in a civilization
                if (_data.GetCivilizationByReligion(founderReligionId) != null)
                {
                    _sapi.Logger.Warning(
                        $"[DivineAscension] Religion '{founderReligion.ReligionName}' is already in a civilization");
                    return null;
                }

                // Create civilization
                var civId = Guid.NewGuid().ToString();
                civ = new Civilization(civId, name, founderUID, founderReligionId)
                {
                    MemberCount = founderReligion.MemberUIDs.Count,
                    Icon = icon,
                    Description = description
                };

                _data.AddCivilization(civ);
            }

            // Logging OUTSIDE lock
            _sapi.Logger.Notification($"[DivineAscension] Civilization '{name}' created by {founderUID}");
            return civ;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Error creating civilization: {ex.Message}");
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
            // Validate target religion exists (external call, OUTSIDE lock)
            var targetReligion = _religionManager.GetReligion(religionId);
            if (targetReligion == null)
            {
                _sapi.Logger.Warning($"[DivineAscension] Target religion '{religionId}' not found");
                return false;
            }

            string civName;
            lock (_dataLock)
            {
                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _sapi.Logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                    return false;
                }

                // Check if inviter is the civilization founder
                if (!civ.IsFounder(inviterUID))
                {
                    _sapi.Logger.Warning("[DivineAscension] Only civilization founder can invite religions");
                    return false;
                }

                // Check if civilization is full
                if (civ.MemberReligionIds.Count >= MAX_RELIGIONS)
                {
                    _sapi.Logger.Warning("[DivineAscension] Civilization is full (max 4 religions)");
                    return false;
                }

                // Check if religion is already a member
                if (civ.HasReligion(religionId))
                {
                    _sapi.Logger.Warning($"[DivineAscension] Religion '{targetReligion.ReligionName}' is already a member");
                    return false;
                }

                // Check if religion is already in another civilization
                if (_data.GetCivilizationByReligion(religionId) != null)
                {
                    _sapi.Logger.Warning(
                        $"[DivineAscension] Religion '{targetReligion.ReligionName}' is already in a civilization");
                    return false;
                }

                // Check if invite already exists
                if (_data.HasPendingInvite(civId, religionId))
                {
                    _sapi.Logger.Warning("[DivineAscension] Invite already sent to this religion");
                    return false;
                }

                // Check deity diversity (no duplicate deities) - need to call GetCivDeityTypes which locks
                // So we need to get member religion IDs here and check domains
                var civDeities = new HashSet<DeityDomain>();
                foreach (var relId in civ.MemberReligionIds)
                {
                    var rel = _religionManager.GetReligion(relId);
                    if (rel != null) civDeities.Add(rel.Domain);
                }

                if (civDeities.Contains(targetReligion.Domain))
                {
                    _sapi.Logger.Warning($"[DivineAscension] Civilization already has a {targetReligion.Domain} religion");
                    return false;
                }

                // Create invite
                var inviteId = Guid.NewGuid().ToString();
                var invite = new CivilizationInvite(inviteId, civId, religionId, DateTime.UtcNow);
                _data.AddInvite(invite);

                civName = civ.Name;
            }

            // Logging OUTSIDE lock
            _sapi.Logger.Notification(
                $"[DivineAscension] Invited religion '{targetReligion.ReligionName}' to civilization '{civName}'");
            return true;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Error inviting religion: {ex.Message}");
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
            string? civId;
            string? religionId;

            lock (_dataLock)
            {
                var invite = _data.GetInvite(inviteId);
                if (invite == null || !invite.IsValid)
                {
                    _sapi.Logger.Warning("[DivineAscension] Invite not found or expired");
                    return false;
                }

                civId = invite.CivId;
                religionId = invite.ReligionId;

                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _sapi.Logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                    _data.RemoveInvite(inviteId);
                    return false;
                }
            }

            // External call OUTSIDE lock
            var religion = _religionManager.GetReligion(religionId);
            if (religion == null)
            {
                lock (_dataLock)
                {
                    _sapi.Logger.Warning($"[DivineAscension] Religion '{religionId}' not found");
                    _data.RemoveInvite(inviteId);
                }
                return false;
            }

            // Check if accepter is the religion founder (OUTSIDE lock)
            if (religion.FounderUID != accepterUID)
            {
                _sapi.Logger.Warning("[DivineAscension] Only religion founder can accept civilization invites");
                return false;
            }

            string civName;
            string religionName = religion.ReligionName;
            int memberCount = religion.MemberUIDs.Count;

            lock (_dataLock)
            {
                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _sapi.Logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                    return false;
                }

                // Check if civilization still has space
                if (civ.MemberReligionIds.Count >= MAX_RELIGIONS)
                {
                    _sapi.Logger.Warning("[DivineAscension] Civilization is now full");
                    _data.RemoveInvite(inviteId);
                    return false;
                }

                // Add religion to civilization
                if (!_data.AddReligionToCivilization(civId, religionId))
                {
                    _sapi.Logger.Error("[DivineAscension] Failed to add religion to civilization");
                    return false;
                }

                // Update member count
                civ.MemberCount += memberCount;

                // Remove invite
                _data.RemoveInvite(inviteId);

                civName = civ.Name;
            }

            // Logging OUTSIDE lock
            _sapi.Logger.Notification(
                $"[DivineAscension] Religion '{religionName}' joined civilization '{civName}'");
            return true;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Error accepting invite: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Declines an invitation to join a civilization
    /// </summary>
    public bool DeclineInvite(string inviteId, string declinerUID)
    {
        try
        {
            string? civId;
            string? religionId;
            string civName;

            lock (_dataLock)
            {
                var invite = _data.GetInvite(inviteId);
                if (invite == null || !invite.IsValid)
                {
                    _sapi.Logger.Warning("[DivineAscension] Invite not found or expired");
                    return false;
                }

                civId = invite.CivId;
                religionId = invite.ReligionId;

                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _sapi.Logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                    _data.RemoveInvite(inviteId);
                    return false;
                }

                civName = civ.Name;
            }

            // External call OUTSIDE lock
            var religion = _religionManager.GetReligion(religionId);
            if (religion == null)
            {
                lock (_dataLock)
                {
                    _sapi.Logger.Warning($"[DivineAscension] Religion '{religionId}' not found");
                    _data.RemoveInvite(inviteId);
                }
                return false;
            }

            // Check if decliner is the religion founder (OUTSIDE lock)
            if (religion.FounderUID != declinerUID)
            {
                _sapi.Logger.Warning("[DivineAscension] Only religion founder can decline civilization invites");
                return false;
            }

            lock (_dataLock)
            {
                // Remove invite
                _data.RemoveInvite(inviteId);
            }

            // Notify online players in the inviting civilization (OUTSIDE lock - player messaging is expensive)
            var civReligions = GetCivReligions(civId);
            var message = $"[Civilization] {religion.ReligionName} has declined the invitation to join {civName}.";

            foreach (var civReligion in civReligions)
            {
                foreach (var memberUID in civReligion.MemberUIDs)
                {
                    var player = _sapi.World.PlayerByUid(memberUID) as IServerPlayer;
                    if (player != null)
                    {
                        player.SendMessage(
                            GlobalConstants.GeneralChatGroup,
                            message,
                            EnumChatType.Notification
                        );
                    }
                }
            }

            // Logging OUTSIDE lock
            _sapi.Logger.Notification(
                $"[DivineAscension] Religion '{religion.ReligionName}' declined invitation to civilization '{civName}'");
            return true;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Error declining invite: {ex.Message}");
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
            Civilization? civ;
            string civId;
            string founderUID;
            bool shouldDisband;

            lock (_dataLock)
            {
                // First, determine if the religion is part of any civilization.
                // Prefer the fast lookup map, but also fall back to scanning in case
                // the map is out of sync with in-memory test manipulations.
                civ = _data.GetCivilizationByReligion(religionId);
                if (civ == null)
                    // Fallback scan for robustness in tests or edge cases
                    civ = _data.Civilizations.Values.FirstOrDefault(c => c.MemberReligionIds.Contains(religionId));

                if (civ == null)
                {
                    _sapi.Logger.Warning("[DivineAscension] Religion is not in a civilization");
                    return false;
                }

                civId = civ.CivId;
                founderUID = civ.FounderUID;
            }

            // External call OUTSIDE lock
            var religion = _religionManager.GetReligion(religionId);
            if (religion == null)
            {
                _sapi.Logger.Warning($"[DivineAscension] Religion '{religionId}' not found");
                return false;
            }

            // Check if requester is the religion founder (OUTSIDE lock)
            if (religion.FounderUID != requesterUID)
            {
                _sapi.Logger.Warning("[DivineAscension] Only religion founder can leave civilization");
                return false;
            }

            // If this is the civilization founder's religion, disband instead (OUTSIDE lock check)
            if (civ.IsFounder(requesterUID))
            {
                _sapi.Logger.Warning("[DivineAscension] Civilization founder must disband, not leave");
                return false;
            }

            string civName;
            string religionName = religion.ReligionName;
            int memberCount = religion.MemberUIDs.Count;

            lock (_dataLock)
            {
                // Remove religion from civilization
                _data.RemoveReligionFromCivilization(religionId);
                civ.MemberCount -= memberCount;

                civName = civ.Name;
                shouldDisband = civ.MemberReligionIds.Count < MIN_RELIGIONS;
            }

            // Check if civilization falls below minimum (disband OUTSIDE lock)
            if (shouldDisband)
            {
                DisbandCivilization(civId, founderUID);
                _sapi.Logger.Notification(
                    $"[DivineAscension] Civilization '{civName}' disbanded (below minimum religions)");
            }
            else
            {
                _sapi.Logger.Notification(
                    $"[DivineAscension] Religion '{religionName}' left civilization '{civName}'");
            }

            return true;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Error leaving civilization: {ex.Message}");
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
            string founderUID;
            bool shouldDisband;

            lock (_dataLock)
            {
                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _sapi.Logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                    return false;
                }

                // Check if kicker is the civilization founder
                if (!civ.IsFounder(kickerUID))
                {
                    _sapi.Logger.Warning("[DivineAscension] Only civilization founder can kick religions");
                    return false;
                }

                // Check if religion is a member
                if (!civ.HasReligion(religionId))
                {
                    _sapi.Logger.Warning("[DivineAscension] Religion is not a member of this civilization");
                    return false;
                }

                founderUID = civ.FounderUID;
            }

            // External calls OUTSIDE lock
            var religion = _religionManager.GetReligion(religionId);
            if (religion == null)
            {
                _sapi.Logger.Warning($"[DivineAscension] Religion '{religionId}' not found");
                return false;
            }

            // Cannot kick own religion
            var kickerReligion = _religionManager.GetPlayerReligion(kickerUID);
            if (kickerReligion?.ReligionUID == religionId)
            {
                _sapi.Logger.Warning("[DivineAscension] Cannot kick your own religion");
                return false;
            }

            string civName;
            string religionName = religion.ReligionName;
            int memberCount = religion.MemberUIDs.Count;

            lock (_dataLock)
            {
                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _sapi.Logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                    return false;
                }

                // Remove religion from civilization
                _data.RemoveReligionFromCivilization(religionId);
                civ.MemberCount -= memberCount;

                civName = civ.Name;
                shouldDisband = civ.MemberReligionIds.Count < MIN_RELIGIONS;
            }

            // Check if civilization falls below minimum (disband OUTSIDE lock)
            if (shouldDisband)
            {
                DisbandCivilization(civId, founderUID);
                _sapi.Logger.Notification(
                    $"[DivineAscension] Civilization '{civName}' disbanded (below minimum religions)");
            }
            else
            {
                _sapi.Logger.Notification(
                    $"[DivineAscension] Religion '{religionName}' kicked from civilization '{civName}'");
            }

            return true;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Error kicking religion: {ex.Message}");
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
            string civName;

            lock (_dataLock)
            {
                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _sapi.Logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                    return false;
                }

                // Check if requester is the civilization founder
                if (!civ.IsFounder(requesterUID))
                {
                    _sapi.Logger.Warning("[DivineAscension] Only civilization founder can disband");
                    return false;
                }

                // Mark as disbanded
                civ.DisbandedDate = DateTime.UtcNow;

                // Remove all pending invites for this civilization
                _data.PendingInvites.RemoveAll(i => i.CivId == civId);

                // Remove civilization
                _data.RemoveCivilization(civId);

                civName = civ.Name;
            }

            // Fire event to notify other systems (OUTSIDE lock - event handlers may do expensive work)
            OnCivilizationDisbanded?.Invoke(civId);

            // Logging OUTSIDE lock
            _sapi.Logger.Notification($"[DivineAscension] Civilization '{civName}' disbanded by founder");
            return true;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Error disbanding civilization: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Force-disbands a civilization without permission checks (for system cleanup)
    ///     Used when a civilization becomes invalid due to religion deletion or data corruption
    /// </summary>
    private void ForceDisband(string civId)
    {
        try
        {
            string civName;

            lock (_dataLock)
            {
                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _sapi.Logger.Debug($"[DivineAscension] Civilization '{civId}' not found for forced disband");
                    return;
                }

                // Mark as disbanded
                civ.DisbandedDate = DateTime.UtcNow;

                // Remove all pending invites for this civilization
                _data.PendingInvites.RemoveAll(i => i.CivId == civId);

                // Remove civilization
                _data.RemoveCivilization(civId);

                civName = civ.Name;
            }

            // Fire event to notify other systems (OUTSIDE lock - diplomacy cleanup may do expensive work)
            OnCivilizationDisbanded?.Invoke(civId);

            // Logging OUTSIDE lock
            _sapi.Logger.Notification(
                $"[DivineAscension] Civilization '{civName}' forcibly disbanded (invalid state)");
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Error force-disbanding civilization {civId}: {ex.Message}");
            // Do not swallow - let it propagate to alert admins
            throw;
        }
    }

    /// <summary>
    ///     Updates a civilization's icon
    /// </summary>
    /// <param name="civId">ID of the civilization</param>
    /// <param name="requestorUID">Player UID requesting the update</param>
    /// <param name="icon">New icon name</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool UpdateCivilizationIcon(string civId, string requestorUID, string icon)
    {
        try
        {
            // Validate icon name BEFORE lock
            if (string.IsNullOrWhiteSpace(icon))
            {
                _sapi.Logger.Warning("[DivineAscension] Icon name cannot be empty");
                return false;
            }

            string civName;
            lock (_dataLock)
            {
                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _sapi.Logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                    return false;
                }

                // Check if requestor is the civilization founder
                if (!civ.IsFounder(requestorUID))
                {
                    _sapi.Logger.Warning("[DivineAscension] Only civilization founder can update icon");
                    return false;
                }

                // Update icon
                civ.UpdateIcon(icon);

                civName = civ.Name;
            }

            // Logging OUTSIDE lock
            _sapi.Logger.Notification($"[DivineAscension] Civilization '{civName}' icon updated to '{icon}'");
            return true;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Error updating civilization icon: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Updates a civilization's description
    /// </summary>
    /// <param name="civId">ID of the civilization</param>
    /// <param name="requestorUID">Player UID requesting the update</param>
    /// <param name="description">New description text</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool UpdateCivilizationDescription(string civId, string requestorUID, string description)
    {
        try
        {
            // Validate description length BEFORE lock
            if (description.Length > 200)
            {
                _sapi.Logger.Warning("[DivineAscension] Description must be 200 characters or less");
                return false;
            }

            string civName;
            lock (_dataLock)
            {
                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _sapi.Logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                    return false;
                }

                // Check if requestor is the civilization founder
                if (!civ.IsFounder(requestorUID))
                {
                    _sapi.Logger.Warning("[DivineAscension] Only civilization founder can update description");
                    return false;
                }

                // Update description (profanity check is done at command/network handler level)
                civ.UpdateDescription(description);

                civName = civ.Name;
            }

            // Logging OUTSIDE lock
            _sapi.Logger.Notification($"[DivineAscension] Civilization '{civName}' description updated");
            return true;
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Error updating civilization description: {ex.Message}");
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
        lock (_dataLock)
        {
            return _data.Civilizations.GetValueOrDefault(civId);
        }
    }

    /// <summary>
    ///     Gets the civilization a religion belongs to
    /// </summary>
    public Civilization? GetCivilizationByReligion(string religionId)
    {
        lock (_dataLock)
        {
            return _data.GetCivilizationByReligion(religionId);
        }
    }

    /// <summary>
    ///     Gets the civilization a player belongs to (via their religion)
    /// </summary>
    public Civilization? GetCivilizationByPlayer(string playerUID)
    {
        // External call OUTSIDE lock
        var religion = _religionManager.GetPlayerReligion(playerUID);
        if (religion == null)
            return null;

        lock (_dataLock)
        {
            return _data.GetCivilizationByReligion(religion.ReligionUID);
        }
    }

    /// <summary>
    ///     Gets all civilizations
    /// </summary>
    public IEnumerable<Civilization> GetAllCivilizations()
    {
        lock (_dataLock)
        {
            // Return snapshot to avoid concurrent modification
            return _data.Civilizations.Values.ToList();
        }
    }

    /// <summary>
    ///     Gets all deity types in a civilization
    /// </summary>
    public HashSet<DeityDomain> GetCivDeityTypes(string civId)
    {
        List<string> memberReligionIds;

        lock (_dataLock)
        {
            var civ = _data.Civilizations.GetValueOrDefault(civId);
            if (civ == null)
                return new HashSet<DeityDomain>();

            // Get snapshot of member religion IDs for external calls
            memberReligionIds = new List<string>(civ.MemberReligionIds);
        }

        // External calls OUTSIDE lock
        var deities = new HashSet<DeityDomain>();
        foreach (var religionId in memberReligionIds)
        {
            var religion = _religionManager.GetReligion(religionId);
            if (religion != null) deities.Add(religion.Domain);
        }

        return deities;
    }

    /// <summary>
    ///     Gets all religions in a civilization
    /// </summary>
    public List<ReligionData> GetCivReligions(string civId)
    {
        List<string> memberReligionIds;

        lock (_dataLock)
        {
            var civ = _data.Civilizations.GetValueOrDefault(civId);
            if (civ == null)
                return new List<ReligionData>();

            // Get snapshot of member religion IDs for external calls
            memberReligionIds = new List<string>(civ.MemberReligionIds);
        }

        // External calls OUTSIDE lock
        var religions = new List<ReligionData>();
        foreach (var religionId in memberReligionIds)
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
        lock (_dataLock)
        {
            return _data.GetInvitesForReligion(religionId);
        }
    }

    /// <summary>
    ///     Gets all pending invites for a civilization
    /// </summary>
    public List<CivilizationInvite> GetInvitesForCiv(string civId)
    {
        lock (_dataLock)
        {
            return _data.GetInvitesForCivilization(civId);
        }
    }

    /// <summary>
    ///     Updates member counts for all civilizations (should be called when religion membership changes)
    /// </summary>
    public void UpdateMemberCounts()
    {
        List<Civilization> civs;

        lock (_dataLock)
        {
            // Get snapshot of civilizations for external calls
            civs = _data.Civilizations.Values.ToList();
        }

        // External calls OUTSIDE lock
        foreach (var civ in civs)
        {
            var totalMembers = 0;
            foreach (var religionId in civ.MemberReligionIds)
            {
                var religion = _religionManager.GetReligion(religionId);
                if (religion != null) totalMembers += religion.MemberUIDs.Count;
            }

            lock (_dataLock)
            {
                civ.MemberCount = totalMembers;
            }
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
                    lock (_dataLock)
                    {
                        _data = loadedData;
                    }
                    _sapi.Logger.Notification($"[DivineAscension] Loaded {loadedData.Civilizations.Count} civilizations");
                }
                else
                {
                    _sapi.Logger.Warning("[DivineAscension] Failed to deserialize civilization data");
                    lock (_dataLock)
                    {
                        _data = new CivilizationWorldData();
                    }
                }
            }
            else
            {
                _sapi.Logger.Debug("[DivineAscension] No civilization data found, starting fresh");
                lock (_dataLock)
                {
                    _data = new CivilizationWorldData();
                }
            }
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Failed to load civilizations: {ex.Message}");
            lock (_dataLock)
            {
                _data = new CivilizationWorldData();
            }
        }
    }

    private void SaveCivilizations()
    {
        try
        {
            byte[] serializedData;
            int count;

            lock (_dataLock)
            {
                serializedData = SerializerUtil.Serialize(_data);
                count = _data.Civilizations.Count;
            }

            // Expensive I/O operation OUTSIDE lock
            _sapi.WorldManager.SaveGame.StoreData(DATA_KEY, serializedData);
            _sapi.Logger.Debug($"[DivineAscension] Saved {count} civilizations");
        }
        catch (Exception ex)
        {
            _sapi.Logger.Error($"[DivineAscension] Failed to save civilizations: {ex.Message}");
        }
    }

    #endregion
}