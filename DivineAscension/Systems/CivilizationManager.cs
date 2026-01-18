using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
///     Manages civilizations - alliances of 1-4 religions with different deities
/// </summary>
public class CivilizationManager : ICivilizationManager
{
    private const string DATA_KEY = "divineascension_civilizations";
    private const int MIN_RELIGIONS = 1;
    private const int MAX_RELIGIONS = 4;
    private const int INVITE_EXPIRY_DAYS = 7;
    private readonly IEventService _eventService;
    private readonly ILogger _logger;
    private readonly IPersistenceService _persistenceService;

    private readonly IReligionManager _religionManager;
    private readonly IWorldService _worldService;
    private CivilizationWorldData _data = new();

    /// <summary>
    ///     Lazy-initialized lock object for thread safety using Interlocked.CompareExchange
    /// </summary>
    private object? _lock;

    public CivilizationManager(
        ILogger logger,
        IEventService eventService,
        IPersistenceService persistenceService,
        IWorldService worldService,
        IReligionManager religionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
    }

    private object Lock
    {
        get
        {
            if (_lock == null)
            {
                Interlocked.CompareExchange(ref _lock, new object(), null);
            }

            return _lock;
        }
    }

    /// <summary>
    ///     Event fired when a civilization is disbanded
    /// </summary>
    public event Action<string>? OnCivilizationDisbanded;

    /// <summary>
    ///     Initializes the civilization manager
    /// </summary>
    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Civilization Manager...");

        // Register event handlers
        _eventService.OnSaveGameLoaded(OnSaveGameLoaded);
        _eventService.OnGameWorldSave(OnGameWorldSave);

        // Subscribe to religion deletion events
        _religionManager.OnReligionDeleted += HandleReligionDeleted;

        _logger.Notification("[DivineAscension] Civilization Manager initialized");
    }

    /// <summary>
    ///     Cleans up event subscriptions
    /// </summary>
    public void Dispose()
    {
        // Unsubscribe from events
        _eventService.UnsubscribeSaveGameLoaded(OnSaveGameLoaded);
        _eventService.UnsubscribeGameWorldSave(OnGameWorldSave);
        _religionManager.OnReligionDeleted -= HandleReligionDeleted;
        OnCivilizationDisbanded = null;
    }

    #region Event Handlers

    /// <summary>
    ///     Handles religion deletion events from ReligionManager
    /// </summary>
    private void HandleReligionDeleted(string religionId)
    {
        lock (Lock)
        {
            try
            {
                var civ = _data.GetCivilizationByReligion(religionId);
                if (civ == null)
                    // Religion wasn't in a civilization, nothing to do
                    return;

                _logger.Debug(
                    $"[DivineAscension] Handling deletion of religion {religionId} from civilization {civ.Name}");

                // Check if the deleted religion was the founder's religion
                var isFounderReligion = civ.FounderReligionUID == religionId;

                // Remove religion from civilization
                _data.RemoveReligionFromCivilization(religionId);

                // Update member count (recalculate from remaining religions)
                var totalMembers = 0;
                foreach (var relId in civ.GetMemberReligionIdsSnapshot())
                {
                    var religion = _religionManager.GetReligion(relId);
                    if (religion != null) totalMembers += religion.MemberUIDs.Count;
                }

                civ.MemberCount = totalMembers;

                // Disband if founder's religion was deleted OR if below minimum religions
                if (isFounderReligion || civ.MemberReligionIds.Count < MIN_RELIGIONS)
                {
                    // Use ForceDisband to bypass permission checks (system cleanup)
                    ForceDisband_Unlocked(civ.CivId);
                    if (isFounderReligion)
                        _logger.Notification(
                            $"[DivineAscension] Civilization '{civ.Name}' disbanded (founder's religion was deleted)");
                    else
                        _logger.Notification(
                            $"[DivineAscension] Civilization '{civ.Name}' disbanded (religion {religionId} was deleted, below minimum)");
                }
                else
                {
                    _logger.Notification(
                        $"[DivineAscension] Removed deleted religion {religionId} from civilization '{civ.Name}'");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error handling religion deletion: {ex.Message}");

                // If disband failed but religion was removed, manually clean up any orphaned civilizations
                var orphanedCivs = _data.Civilizations.Values
                    .Where(c => c.MemberReligionIds.Count == 0)
                    .ToList();

                if (orphanedCivs.Any())
                {
                    _logger.Warning(
                        $"[DivineAscension] Found {orphanedCivs.Count} orphaned civilization(s) after exception, forcing cleanup");
                    foreach (var orphan in orphanedCivs)
                    {
                        try
                        {
                            ForceDisband_Unlocked(orphan.CivId);
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.Error(
                                $"[DivineAscension] Failed to cleanup orphaned civilization {orphan.Name}: {cleanupEx.Message}");
                        }
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
        lock (Lock)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(name))
                {
                    _logger.Warning("[DivineAscension] Cannot create civilization with empty name");
                    return null;
                }

                if (name.Length < 3 || name.Length > 32)
                {
                    _logger.Warning("[DivineAscension] Civilization name must be 3-32 characters");
                    return null;
                }

                // Validate description length (max 200 characters)
                if (description.Length > 200)
                {
                    _logger.Warning("[DivineAscension] Description must be 200 characters or less");
                    return null;
                }

                // Check if name already exists
                if (_data.Civilizations.Values.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.Warning($"[DivineAscension] Civilization name '{name}' already exists");
                    return null;
                }

                // Validate founder's religion exists
                var founderReligion = _religionManager.GetReligion(founderReligionId);
                if (founderReligion == null)
                {
                    _logger.Warning($"[DivineAscension] Founder religion '{founderReligionId}' not found");
                    return null;
                }

                // Check if founder is the religion founder
                if (founderReligion.FounderUID != founderUID)
                {
                    _logger.Warning("[DivineAscension] Only religion founders can create civilizations");
                    return null;
                }

                // Check if religion is already in a civilization
                if (_data.GetCivilizationByReligion(founderReligionId) != null)
                {
                    _logger.Warning(
                        $"[DivineAscension] Religion '{founderReligion.ReligionName}' is already in a civilization");
                    return null;
                }

                // Create civilization
                var civId = Guid.NewGuid().ToString();
                var civ = new Civilization(civId, name, founderUID, founderReligionId)
                {
                    MemberCount = founderReligion.MemberUIDs.Count,
                    Icon = icon,
                    Description = description
                };

                _data.AddCivilization(civ);

                _logger.Notification($"[DivineAscension] Civilization '{name}' created by {founderUID}");
                return civ;
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error creating civilization: {ex.Message}");
                return null;
            }
        }
    }

    /// <summary>
    ///     Invites a religion to join a civilization
    /// </summary>
    public bool InviteReligion(string civId, string religionId, string inviterUID)
    {
        lock (Lock)
        {
            try
            {
                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                    return false;
                }

                // Check if inviter is the civilization founder
                if (!civ.IsFounder(inviterUID))
                {
                    _logger.Warning("[DivineAscension] Only civilization founder can invite religions");
                    return false;
                }

                // Check if civilization is full
                if (civ.MemberReligionIds.Count >= MAX_RELIGIONS)
                {
                    _logger.Warning("[DivineAscension] Civilization is full (max 4 religions)");
                    return false;
                }

                // Validate target religion exists
                var targetReligion = _religionManager.GetReligion(religionId);
                if (targetReligion == null)
                {
                    _logger.Warning($"[DivineAscension] Target religion '{religionId}' not found");
                    return false;
                }

                // Check if religion is already a member
                if (civ.HasReligion(religionId))
                {
                    _logger.Warning($"[DivineAscension] Religion '{targetReligion.ReligionName}' is already a member");
                    return false;
                }

                // Check if religion is already in another civilization
                if (_data.GetCivilizationByReligion(religionId) != null)
                {
                    _logger.Warning(
                        $"[DivineAscension] Religion '{targetReligion.ReligionName}' is already in a civilization");
                    return false;
                }

                // Check if invite already exists
                if (_data.HasPendingInvite(civId, religionId))
                {
                    _logger.Warning("[DivineAscension] Invite already sent to this religion");
                    return false;
                }

                // Check deity diversity (no duplicate deities)
                var civDeities = GetCivDeityTypes_Unlocked(civId);
                if (civDeities.Contains(targetReligion.Domain))
                {
                    _logger.Warning($"[DivineAscension] Civilization already has a {targetReligion.Domain} religion");
                    return false;
                }

                // Create invite
                var inviteId = Guid.NewGuid().ToString();
                var invite = new CivilizationInvite(inviteId, civId, religionId, DateTime.UtcNow);
                _data.AddInvite(invite);

                _logger.Notification(
                    $"[DivineAscension] Invited religion '{targetReligion.ReligionName}' to civilization '{civ.Name}'");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error inviting religion: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    ///     Accepts an invitation to join a civilization
    /// </summary>
    public bool AcceptInvite(string inviteId, string accepterUID)
    {
        lock (Lock)
        {
            try
            {
                var invite = _data.GetInvite(inviteId);
                if (invite == null || !invite.IsValid)
                {
                    _logger.Warning("[DivineAscension] Invite not found or expired");
                    return false;
                }

                var civ = _data.Civilizations.GetValueOrDefault(invite.CivId);
                if (civ == null)
                {
                    _logger.Warning($"[DivineAscension] Civilization '{invite.CivId}' not found");
                    _data.RemoveInvite(inviteId);
                    return false;
                }

                var religion = _religionManager.GetReligion(invite.ReligionId);
                if (religion == null)
                {
                    _logger.Warning($"[DivineAscension] Religion '{invite.ReligionId}' not found");
                    _data.RemoveInvite(inviteId);
                    return false;
                }

                // Check if accepter is the religion founder
                if (religion.FounderUID != accepterUID)
                {
                    _logger.Warning("[DivineAscension] Only religion founder can accept civilization invites");
                    return false;
                }

                // Check if civilization still has space
                if (civ.MemberReligionIds.Count >= MAX_RELIGIONS)
                {
                    _logger.Warning("[DivineAscension] Civilization is now full");
                    _data.RemoveInvite(inviteId);
                    return false;
                }

                // Add religion to civilization
                if (!_data.AddReligionToCivilization(invite.CivId, invite.ReligionId))
                {
                    _logger.Error("[DivineAscension] Failed to add religion to civilization");
                    return false;
                }

                // Update member count
                civ.MemberCount += religion.MemberUIDs.Count;

                // Remove invite
                _data.RemoveInvite(inviteId);

                _logger.Notification(
                    $"[DivineAscension] Religion '{religion.ReligionName}' joined civilization '{civ.Name}'");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error accepting invite: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    ///     Declines an invitation to join a civilization
    /// </summary>
    public bool DeclineInvite(string inviteId, string declinerUID)
    {
        lock (Lock)
        {
            try
            {
                var invite = _data.GetInvite(inviteId);
                if (invite == null || !invite.IsValid)
                {
                    _logger.Warning("[DivineAscension] Invite not found or expired");
                    return false;
                }

                var civ = _data.Civilizations.GetValueOrDefault(invite.CivId);
                if (civ == null)
                {
                    _logger.Warning($"[DivineAscension] Civilization '{invite.CivId}' not found");
                    _data.RemoveInvite(inviteId);
                    return false;
                }

                var religion = _religionManager.GetReligion(invite.ReligionId);
                if (religion == null)
                {
                    _logger.Warning($"[DivineAscension] Religion '{invite.ReligionId}' not found");
                    _data.RemoveInvite(inviteId);
                    return false;
                }

                // Check if decliner is the religion founder
                if (religion.FounderUID != declinerUID)
                {
                    _logger.Warning("[DivineAscension] Only religion founder can decline civilization invites");
                    return false;
                }

                // Remove invite
                _data.RemoveInvite(inviteId);

                // Notify online players in the inviting civilization
                var civReligions = GetCivReligions_Unlocked(civ.CivId);
                var message = $"[Civilization] {religion.ReligionName} has declined the invitation to join {civ.Name}.";

                foreach (var civReligion in civReligions)
                {
                    foreach (var memberUID in civReligion.MemberUIDs)
                    {
                        var player = _worldService.GetPlayerByUID(memberUID) as IServerPlayer;
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

                _logger.Notification(
                    $"[DivineAscension] Religion '{religion.ReligionName}' declined invitation to civilization '{civ.Name}'");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error declining invite: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    ///     A religion leaves a civilization voluntarily
    /// </summary>
    public bool LeaveReligion(string religionId, string requesterUID)
    {
        lock (Lock)
        {
            try
            {
                // First, determine if the religion is part of any civilization.
                // Prefer the fast lookup map, but also fall back to scanning in case
                // the map is out of sync with in-memory test manipulations.
                var civ = _data.GetCivilizationByReligion(religionId);
                if (civ == null)
                    // Fallback scan for robustness in tests or edge cases
                    civ = _data.Civilizations.Values.FirstOrDefault(c => c.HasReligion(religionId));

                if (civ == null)
                {
                    _logger.Warning("[DivineAscension] Religion is not in a civilization");
                    return false;
                }

                // Now ensure the religion itself exists
                var religion = _religionManager.GetReligion(religionId);
                if (religion == null)
                {
                    _logger.Warning($"[DivineAscension] Religion '{religionId}' not found");
                    return false;
                }

                // Check if requester is the religion founder
                if (religion.FounderUID != requesterUID)
                {
                    _logger.Warning("[DivineAscension] Only religion founder can leave civilization");
                    return false;
                }

                // If this is the civilization founder's religion, disband instead
                if (civ.IsFounder(requesterUID))
                {
                    _logger.Warning("[DivineAscension] Civilization founder must disband, not leave");
                    return false;
                }

                // Remove religion from civilization
                _data.RemoveReligionFromCivilization(religionId);
                civ.MemberCount -= religion.MemberUIDs.Count;


                // Check if civilization falls below minimum
                if (civ.MemberReligionIds.Count < MIN_RELIGIONS)
                {
                    DisbandCivilization_Unlocked(civ.CivId, civ.FounderUID);
                    _logger.Notification(
                        $"[DivineAscension] Civilization '{civ.Name}' disbanded (below minimum religions)");
                }
                else
                {
                    _logger.Notification(
                        $"[DivineAscension] Religion '{religion.ReligionName}' left civilization '{civ.Name}'");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error leaving civilization: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    ///     Kicks a religion from a civilization
    /// </summary>
    public bool KickReligion(string civId, string religionId, string kickerUID)
    {
        lock (Lock)
        {
            try
            {
                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                    return false;
                }

                // Check if kicker is the civilization founder
                if (!civ.IsFounder(kickerUID))
                {
                    _logger.Warning("[DivineAscension] Only civilization founder can kick religions");
                    return false;
                }

                // Check if religion is a member
                if (!civ.HasReligion(religionId))
                {
                    _logger.Warning("[DivineAscension] Religion is not a member of this civilization");
                    return false;
                }

                var religion = _religionManager.GetReligion(religionId);
                if (religion == null)
                {
                    _logger.Warning($"[DivineAscension] Religion '{religionId}' not found");
                    return false;
                }

                // Cannot kick own religion
                var kickerReligion = _religionManager.GetPlayerReligion(kickerUID);
                if (kickerReligion?.ReligionUID == religionId)
                {
                    _logger.Warning("[DivineAscension] Cannot kick your own religion");
                    return false;
                }

                // Remove religion from civilization
                _data.RemoveReligionFromCivilization(religionId);
                civ.MemberCount -= religion.MemberUIDs.Count;

                // Check if civilization falls below minimum
                if (civ.MemberReligionIds.Count < MIN_RELIGIONS)
                {
                    DisbandCivilization_Unlocked(civ.CivId, civ.FounderUID);
                    _logger.Notification(
                        $"[DivineAscension] Civilization '{civ.Name}' disbanded (below minimum religions)");
                }
                else
                {
                    _logger.Notification(
                        $"[DivineAscension] Religion '{religion.ReligionName}' kicked from civilization '{civ.Name}'");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error kicking religion: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    ///     Disbands a civilization
    /// </summary>
    public bool DisbandCivilization(string civId, string requesterUID)
    {
        lock (Lock)
        {
            return DisbandCivilization_Unlocked(civId, requesterUID);
        }
    }

    /// <summary>
    ///     Internal unlocked version of DisbandCivilization (caller must hold lock)
    /// </summary>
    private bool DisbandCivilization_Unlocked(string civId, string requesterUID)
    {
        try
        {
            var civ = _data.Civilizations.GetValueOrDefault(civId);
            if (civ == null)
            {
                _logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                return false;
            }

            // Check if requester is the civilization founder
            if (!civ.IsFounder(requesterUID))
            {
                _logger.Warning("[DivineAscension] Only civilization founder can disband");
                return false;
            }

            // Mark as disbanded
            civ.DisbandedDate = DateTime.UtcNow;

            // Remove all pending invites for this civilization
            _data.PendingInvites.RemoveAll(i => i.CivId == civId);

            // Remove civilization
            _data.RemoveCivilization(civId);

            // Fire event to notify other systems
            OnCivilizationDisbanded?.Invoke(civId);

            _logger.Notification($"[DivineAscension] Civilization '{civ.Name}' disbanded by founder");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error disbanding civilization: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Force-disbands a civilization without permission checks (for system cleanup)
    ///     Used when a civilization becomes invalid due to religion deletion or data corruption
    /// </summary>
    private void ForceDisband(string civId)
    {
        lock (Lock)
        {
            ForceDisband_Unlocked(civId);
        }
    }

    /// <summary>
    ///     Internal unlocked version of ForceDisband (caller must hold lock)
    /// </summary>
    private void ForceDisband_Unlocked(string civId)
    {
        try
        {
            var civ = _data.Civilizations.GetValueOrDefault(civId);
            if (civ == null)
            {
                _logger.Debug($"[DivineAscension] Civilization '{civId}' not found for forced disband");
                return;
            }

            // Mark as disbanded
            civ.DisbandedDate = DateTime.UtcNow;

            // Remove all pending invites for this civilization
            _data.PendingInvites.RemoveAll(i => i.CivId == civId);

            // Remove civilization
            _data.RemoveCivilization(civId);

            // Fire event to notify other systems (diplomacy cleanup)
            OnCivilizationDisbanded?.Invoke(civId);

            _logger.Notification(
                $"[DivineAscension] Civilization '{civ.Name}' forcibly disbanded (invalid state)");
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error force-disbanding civilization {civId}: {ex.Message}");
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
        lock (Lock)
        {
            try
            {
                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                    return false;
                }

                // Check if requestor is the civilization founder
                if (!civ.IsFounder(requestorUID))
                {
                    _logger.Warning("[DivineAscension] Only civilization founder can update icon");
                    return false;
                }

                // Validate icon name (basic validation)
                if (string.IsNullOrWhiteSpace(icon))
                {
                    _logger.Warning("[DivineAscension] Icon name cannot be empty");
                    return false;
                }

                // Update icon
                civ.UpdateIcon(icon);

                _logger.Notification($"[DivineAscension] Civilization '{civ.Name}' icon updated to '{icon}'");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error updating civilization icon: {ex.Message}");
                return false;
            }
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
        lock (Lock)
        {
            try
            {
                var civ = _data.Civilizations.GetValueOrDefault(civId);
                if (civ == null)
                {
                    _logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                    return false;
                }

                // Check if requestor is the civilization founder
                if (!civ.IsFounder(requestorUID))
                {
                    _logger.Warning("[DivineAscension] Only civilization founder can update description");
                    return false;
                }

                // Validate description length (max 200 characters, matching religion pattern)
                if (description.Length > 200)
                {
                    _logger.Warning("[DivineAscension] Description must be 200 characters or less");
                    return false;
                }

                // Update description (profanity check is done at command/network handler level)
                civ.UpdateDescription(description);

                _logger.Notification($"[DivineAscension] Civilization '{civ.Name}' description updated");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Error updating civilization description: {ex.Message}");
                return false;
            }
        }
    }

    #endregion

    #region Query Methods

    /// <summary>
    ///     Gets a civilization by ID
    /// </summary>
    public Civilization? GetCivilization(string civId)
    {
        lock (Lock)
        {
            return _data.Civilizations.GetValueOrDefault(civId);
        }
    }

    /// <summary>
    ///     Gets the civilization a religion belongs to
    /// </summary>
    public Civilization? GetCivilizationByReligion(string religionId)
    {
        lock (Lock)
        {
            return _data.GetCivilizationByReligion(religionId);
        }
    }

    /// <summary>
    ///     Gets the civilization a player belongs to (via their religion)
    /// </summary>
    public Civilization? GetCivilizationByPlayer(string playerUID)
    {
        var religion = _religionManager.GetPlayerReligion(playerUID);
        if (religion == null)
            return null;

        lock (Lock)
        {
            return _data.GetCivilizationByReligion(religion.ReligionUID);
        }
    }

    /// <summary>
    ///     Gets all civilizations (returns a snapshot for safe iteration)
    /// </summary>
    public IEnumerable<Civilization> GetAllCivilizations()
    {
        lock (Lock)
        {
            return _data.Civilizations.Values.ToList();
        }
    }

    /// <summary>
    ///     Gets all deity types in a civilization
    /// </summary>
    public HashSet<DeityDomain> GetCivDeityTypes(string civId)
    {
        lock (Lock)
        {
            return GetCivDeityTypes_Unlocked(civId);
        }
    }

    /// <summary>
    ///     Internal unlocked version of GetCivDeityTypes (caller must hold lock)
    /// </summary>
    private HashSet<DeityDomain> GetCivDeityTypes_Unlocked(string civId)
    {
        var civ = _data.Civilizations.GetValueOrDefault(civId);
        if (civ == null)
            return new HashSet<DeityDomain>();

        var deities = new HashSet<DeityDomain>();
        foreach (var religionId in civ.GetMemberReligionIdsSnapshot())
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
        lock (Lock)
        {
            return GetCivReligions_Unlocked(civId);
        }
    }

    /// <summary>
    ///     Internal unlocked version of GetCivReligions (caller must hold lock)
    /// </summary>
    private List<ReligionData> GetCivReligions_Unlocked(string civId)
    {
        var civ = _data.Civilizations.GetValueOrDefault(civId);
        if (civ == null)
            return new List<ReligionData>();

        var religions = new List<ReligionData>();
        foreach (var religionId in civ.GetMemberReligionIdsSnapshot())
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
        lock (Lock)
        {
            return _data.GetInvitesForReligion(religionId);
        }
    }

    /// <summary>
    ///     Gets all pending invites for a civilization
    /// </summary>
    public List<CivilizationInvite> GetInvitesForCiv(string civId)
    {
        lock (Lock)
        {
            return _data.GetInvitesForCivilization(civId);
        }
    }

    /// <summary>
    ///     Updates member counts for all civilizations (should be called when religion membership changes)
    /// </summary>
    public void UpdateMemberCounts()
    {
        lock (Lock)
        {
            foreach (var civ in _data.Civilizations.Values.ToList())
            {
                var totalMembers = 0;
                foreach (var religionId in civ.GetMemberReligionIdsSnapshot())
                {
                    var religion = _religionManager.GetReligion(religionId);
                    if (religion != null) totalMembers += religion.MemberUIDs.Count;
                }

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
        lock (Lock)
        {
            try
            {
                var loadedData = _persistenceService.Load<CivilizationWorldData>(DATA_KEY);
                if (loadedData != null)
                {
                    _data = loadedData;
                    _logger.Notification($"[DivineAscension] Loaded {_data.Civilizations.Count} civilizations");
                }
                else
                {
                    _logger.Debug("[DivineAscension] No civilization data found, starting fresh");
                    _data = new CivilizationWorldData();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Failed to load civilizations: {ex.Message}");
                _data = new CivilizationWorldData();
            }
        }
    }

    private void SaveCivilizations()
    {
        lock (Lock)
        {
            try
            {
                _persistenceService.Save(DATA_KEY, _data);
                _logger.Debug($"[DivineAscension] Saved {_data.Civilizations.Count} civilizations");
            }
            catch (Exception ex)
            {
                _logger.Error($"[DivineAscension] Failed to save civilizations: {ex.Message}");
            }
        }
    }

    #endregion
}