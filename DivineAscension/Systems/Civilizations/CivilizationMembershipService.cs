using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Civilizations;

/// <summary>
///     The civilization membership lifecycle: create, invite/accept/decline, leave,
///     kick, disband and the cascade triggered by religion deletion. Lock-free — the
///     <see cref="CivilizationManager" /> facade serializes every call and raises the
///     events recorded in the returned <see cref="MembershipResult" />.
/// </summary>
internal sealed class CivilizationMembershipService
{
    private readonly CivilizationCapitalService _capital;
    private readonly CivilizationChronicler _chronicler;
    private readonly ILoggerWrapper _logger;
    private readonly IReligionManager _religionManager;
    private readonly IWorldService _worldService;

    public CivilizationMembershipService(
        ILoggerWrapper logger,
        IReligionManager religionManager,
        IWorldService worldService,
        CivilizationChronicler chronicler,
        CivilizationCapitalService capital)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _chronicler = chronicler ?? throw new ArgumentNullException(nameof(chronicler));
        _capital = capital ?? throw new ArgumentNullException(nameof(capital));
    }

    public Civilization? CreateCivilization(CivilizationWorldData data, string name, string founderUID,
        string founderReligionId, string icon, string description, CivilizationEthos? ethosOverride)
    {
        try
        {
            if (!CivilizationValidator.IsNameNonEmpty(name))
            {
                _logger.Warning("[DivineAscension] Cannot create civilization with empty name");
                return null;
            }

            if (!CivilizationValidator.IsNameLengthValid(name))
            {
                _logger.Warning("[DivineAscension] Civilization name must be 3-32 characters");
                return null;
            }

            if (!CivilizationValidator.IsDescriptionLengthValid(description))
            {
                _logger.Warning("[DivineAscension] Description must be 200 characters or less");
                return null;
            }

            if (CivilizationValidator.IsNameTaken(data, name))
            {
                _logger.Warning($"[DivineAscension] Civilization name '{name}' already exists");
                return null;
            }

            var founderReligion = _religionManager.GetReligion(founderReligionId);
            if (founderReligion == null)
            {
                _logger.Warning($"[DivineAscension] Founder religion '{founderReligionId}' not found");
                return null;
            }

            if (founderReligion.FounderUID != founderUID)
            {
                _logger.Warning("[DivineAscension] Only religion founders can create civilizations");
                return null;
            }

            if (data.GetCivilizationByReligion(founderReligionId) != null)
            {
                _logger.Warning(
                    $"[DivineAscension] Religion '{founderReligion.ReligionName}' is already in a civilization");
                return null;
            }

            var civId = Guid.NewGuid().ToString();
            var (derivedEthos, epithetKey) = CivilizationEthosDeriver.Derive(founderReligion.PatronDomain);
            var civ = new Civilization(civId, name, founderUID, founderReligionId)
            {
                MemberCount = founderReligion.MemberUIDs.Count,
                Icon = icon,
                Description = description,
                Ethos = ethosOverride ?? derivedEthos,
                FounderEpithet = LocalizationService.Instance.Get(epithetKey),
                CapitalName = $"{name} Seat"
            };

            data.AddCivilization(civ);

            var founderName = founderReligion.GetMemberName(founderUID) ?? founderUID;
            _chronicler.RecordFounded(civ, founderName);

            _logger.Notification($"[DivineAscension] Civilization '{name}' created by {founderUID}");
            return civ;
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error creating civilization: {ex.Message}");
            return null;
        }
    }

    public bool InviteReligion(CivilizationWorldData data, string civId, string religionId, string inviterUID)
    {
        try
        {
            var civ = data.Civilizations.GetValueOrDefault(civId);
            if (civ == null)
            {
                _logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                return false;
            }

            if (!civ.IsFounder(inviterUID))
            {
                _logger.Warning("[DivineAscension] Only civilization founder can invite religions");
                return false;
            }

            if (civ.MemberReligionIds.Count >= CivilizationValidator.MaxReligions)
            {
                _logger.Warning("[DivineAscension] Civilization is full (max 4 religions)");
                return false;
            }

            var targetReligion = _religionManager.GetReligion(religionId);
            if (targetReligion == null)
            {
                _logger.Warning($"[DivineAscension] Target religion '{religionId}' not found");
                return false;
            }

            if (civ.HasReligion(religionId))
            {
                _logger.Warning($"[DivineAscension] Religion '{targetReligion.ReligionName}' is already a member");
                return false;
            }

            if (data.GetCivilizationByReligion(religionId) != null)
            {
                _logger.Warning(
                    $"[DivineAscension] Religion '{targetReligion.ReligionName}' is already in a civilization");
                return false;
            }

            if (data.HasPendingInvite(civId, religionId))
            {
                _logger.Warning("[DivineAscension] Invite already sent to this religion");
                return false;
            }

            var civDeities = CivilizationQueries.GetDeityTypes(data, _religionManager, civId);
            if (civDeities.Contains(targetReligion.PatronDomain))
            {
                _logger.Warning(
                    $"[DivineAscension] Civilization already has a {targetReligion.PatronDomain} religion");
                return false;
            }

            var inviteId = Guid.NewGuid().ToString();
            var invite = new CivilizationInvite(inviteId, civId, religionId, DateTime.UtcNow);
            data.AddInvite(invite);

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

    public MembershipResult AcceptInvite(CivilizationWorldData data, string inviteId, string accepterUID)
    {
        try
        {
            var invite = data.GetInvite(inviteId);
            if (invite == null || !invite.IsValid)
            {
                _logger.Warning("[DivineAscension] Invite not found or expired");
                return MembershipResult.Failed();
            }

            var civ = data.Civilizations.GetValueOrDefault(invite.CivId);
            if (civ == null)
            {
                _logger.Warning($"[DivineAscension] Civilization '{invite.CivId}' not found");
                data.RemoveInvite(inviteId);
                return MembershipResult.Failed();
            }

            var religion = _religionManager.GetReligion(invite.ReligionId);
            if (religion == null)
            {
                _logger.Warning($"[DivineAscension] Religion '{invite.ReligionId}' not found");
                data.RemoveInvite(inviteId);
                return MembershipResult.Failed();
            }

            if (religion.FounderUID != accepterUID)
            {
                _logger.Warning("[DivineAscension] Only religion founder can accept civilization invites");
                return MembershipResult.Failed();
            }

            if (civ.MemberReligionIds.Count >= CivilizationValidator.MaxReligions)
            {
                _logger.Warning("[DivineAscension] Civilization is now full");
                data.RemoveInvite(inviteId);
                return MembershipResult.Failed();
            }

            if (!data.AddReligionToCivilization(invite.CivId, invite.ReligionId))
            {
                _logger.Error("[DivineAscension] Failed to add religion to civilization");
                return MembershipResult.Failed();
            }

            civ.MemberCount += religion.MemberUIDs.Count;
            data.RemoveInvite(inviteId);

            _logger.Notification(
                $"[DivineAscension] Religion '{religion.ReligionName}' joined civilization '{civ.Name}'");

            _chronicler.RecordReligionJoined(civ, religion.ReligionName, invite.ReligionId);

            var events = new List<CivEvent> { CivEvent.ReligionAdded(civ.CivId, invite.ReligionId) };
            return MembershipResult.Ok(events);
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error accepting invite: {ex.Message}");
            return MembershipResult.Failed();
        }
    }

    public bool DeclineInvite(CivilizationWorldData data, string inviteId, string declinerUID)
    {
        try
        {
            var invite = data.GetInvite(inviteId);
            if (invite == null || !invite.IsValid)
            {
                _logger.Warning("[DivineAscension] Invite not found or expired");
                return false;
            }

            var civ = data.Civilizations.GetValueOrDefault(invite.CivId);
            if (civ == null)
            {
                _logger.Warning($"[DivineAscension] Civilization '{invite.CivId}' not found");
                data.RemoveInvite(inviteId);
                return false;
            }

            var religion = _religionManager.GetReligion(invite.ReligionId);
            if (religion == null)
            {
                _logger.Warning($"[DivineAscension] Religion '{invite.ReligionId}' not found");
                data.RemoveInvite(inviteId);
                return false;
            }

            if (religion.FounderUID != declinerUID)
            {
                _logger.Warning("[DivineAscension] Only religion founder can decline civilization invites");
                return false;
            }

            data.RemoveInvite(inviteId);

            var civReligions = CivilizationQueries.GetReligions(data, _religionManager, civ.CivId);
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

    public MembershipResult LeaveReligion(CivilizationWorldData data, string religionId, string requesterUID)
    {
        var events = new List<CivEvent>();
        try
        {
            // Prefer the fast lookup map, but fall back to scanning in case the map is
            // out of sync with in-memory test manipulations.
            var civ = data.GetCivilizationByReligion(religionId)
                      ?? data.Civilizations.Values.FirstOrDefault(c => c.HasReligion(religionId));

            if (civ == null)
            {
                _logger.Warning("[DivineAscension] Religion is not in a civilization");
                return MembershipResult.Failed();
            }

            var religion = _religionManager.GetReligion(religionId);
            if (religion == null)
            {
                _logger.Warning($"[DivineAscension] Religion '{religionId}' not found");
                return MembershipResult.Failed();
            }

            if (religion.FounderUID != requesterUID)
            {
                _logger.Warning("[DivineAscension] Only religion founder can leave civilization");
                return MembershipResult.Failed();
            }

            if (civ.IsFounder(requesterUID))
            {
                _logger.Warning("[DivineAscension] Civilization founder must disband, not leave");
                return MembershipResult.Failed();
            }

            var civIdForEvent = civ.CivId;
            _capital.ClearCapitalIfOwnedByReligion(civ, religionId);
            data.RemoveReligionFromCivilization(religionId);
            civ.MemberCount -= religion.MemberUIDs.Count;

            events.Add(CivEvent.ReligionRemoved(civIdForEvent, religionId));

            _chronicler.RecordReligionLeft(civ, religion.ReligionName, religionId);

            if (civ.MemberReligionIds.Count < CivilizationValidator.MinReligions)
            {
                Disband(data, civ.CivId, civ.FounderUID, events);
                _logger.Notification(
                    $"[DivineAscension] Civilization '{civ.Name}' disbanded (below minimum religions)");
            }
            else
            {
                _logger.Notification(
                    $"[DivineAscension] Religion '{religion.ReligionName}' left civilization '{civ.Name}'");
            }

            return MembershipResult.Ok(events);
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error leaving civilization: {ex.Message}");
            return MembershipResult.Failed();
        }
    }

    public MembershipResult KickReligion(CivilizationWorldData data, string civId, string religionId, string kickerUID)
    {
        var events = new List<CivEvent>();
        try
        {
            var civ = data.Civilizations.GetValueOrDefault(civId);
            if (civ == null)
            {
                _logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                return MembershipResult.Failed();
            }

            if (!civ.IsFounder(kickerUID))
            {
                _logger.Warning("[DivineAscension] Only civilization founder can kick religions");
                return MembershipResult.Failed();
            }

            if (!civ.HasReligion(religionId))
            {
                _logger.Warning("[DivineAscension] Religion is not a member of this civilization");
                return MembershipResult.Failed();
            }

            var religion = _religionManager.GetReligion(religionId);
            if (religion == null)
            {
                _logger.Warning($"[DivineAscension] Religion '{religionId}' not found");
                return MembershipResult.Failed();
            }

            var kickerReligion = _religionManager.GetPlayerReligion(kickerUID);
            if (kickerReligion?.ReligionUID == religionId)
            {
                _logger.Warning("[DivineAscension] Cannot kick your own religion");
                return MembershipResult.Failed();
            }

            _capital.ClearCapitalIfOwnedByReligion(civ, religionId);
            data.RemoveReligionFromCivilization(religionId);
            civ.MemberCount -= religion.MemberUIDs.Count;

            events.Add(CivEvent.ReligionRemoved(civId, religionId));

            _chronicler.RecordReligionLeft(civ, religion.ReligionName, religionId);

            if (civ.MemberReligionIds.Count < CivilizationValidator.MinReligions)
            {
                Disband(data, civ.CivId, civ.FounderUID, events);
                _logger.Notification(
                    $"[DivineAscension] Civilization '{civ.Name}' disbanded (below minimum religions)");
            }
            else
            {
                _logger.Notification(
                    $"[DivineAscension] Religion '{religion.ReligionName}' kicked from civilization '{civ.Name}'");
            }

            return MembershipResult.Ok(events);
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Error kicking religion: {ex.Message}");
            return MembershipResult.Failed();
        }
    }

    public MembershipResult DisbandCivilization(CivilizationWorldData data, string civId, string requesterUID)
    {
        var events = new List<CivEvent>();
        var success = Disband(data, civId, requesterUID, events);
        return new MembershipResult(success, events);
    }

    /// <summary>
    ///     Disbands a civilization after verifying the requester is its founder.
    ///     Appends an <see cref="CivEventKind.Disbanded" /> event on success.
    /// </summary>
    private bool Disband(CivilizationWorldData data, string civId, string requesterUID, List<CivEvent> events)
    {
        try
        {
            var civ = data.Civilizations.GetValueOrDefault(civId);
            if (civ == null)
            {
                _logger.Warning($"[DivineAscension] Civilization '{civId}' not found");
                return false;
            }

            if (!civ.IsFounder(requesterUID))
            {
                _logger.Warning("[DivineAscension] Only civilization founder can disband");
                return false;
            }

            civ.DisbandedDate = DateTime.UtcNow;
            _chronicler.RecordDisbanded(civ);

            data.PendingInvites.RemoveAll(i => i.CivId == civId);
            data.RemoveCivilization(civId);

            events.Add(CivEvent.Disbanded(civId));

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
    ///     Force-disbands a civilization without permission checks (system cleanup).
    ///     Rethrows on failure to surface corruption to admins.
    /// </summary>
    private void ForceDisband(CivilizationWorldData data, string civId, List<CivEvent> events)
    {
        try
        {
            var civ = data.Civilizations.GetValueOrDefault(civId);
            if (civ == null)
            {
                _logger.Debug($"[DivineAscension] Civilization '{civId}' not found for forced disband");
                return;
            }

            civ.DisbandedDate = DateTime.UtcNow;
            _chronicler.RecordDisbanded(civ);

            data.PendingInvites.RemoveAll(i => i.CivId == civId);
            data.RemoveCivilization(civId);

            events.Add(CivEvent.Disbanded(civId));

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
    ///     Cascade triggered by religion deletion: removes the religion from its civ
    ///     and disbands the civ if it was the founder's religion or drops below the
    ///     minimum. Returns the ordered events for the facade to raise.
    /// </summary>
    public List<CivEvent> HandleReligionDeleted(CivilizationWorldData data, string religionId)
    {
        var events = new List<CivEvent>();
        try
        {
            var civ = data.GetCivilizationByReligion(religionId);
            if (civ == null)
                // Religion wasn't in a civilization, nothing to do
                return events;

            _logger.Debug(
                $"[DivineAscension] Handling deletion of religion {religionId} from civilization {civ.Name}");

            var isFounderReligion = civ.FounderReligionUID == religionId;
            var civIdForEvent = civ.CivId;

            _capital.ClearCapitalIfOwnedByReligion(civ, religionId);

            data.RemoveReligionFromCivilization(religionId);

            events.Add(CivEvent.ReligionRemoved(civIdForEvent, religionId));

            var totalMembers = 0;
            foreach (var relId in civ.GetMemberReligionIdsSnapshot())
            {
                var religion = _religionManager.GetReligion(relId);
                if (religion != null) totalMembers += religion.MemberUIDs.Count;
            }

            civ.MemberCount = totalMembers;

            if (isFounderReligion || civ.MemberReligionIds.Count < CivilizationValidator.MinReligions)
            {
                ForceDisband(data, civ.CivId, events);
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

            // If disband failed but religion was removed, manually clean up orphaned civilizations
            var orphanedCivs = data.Civilizations.Values
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
                        ForceDisband(data, orphan.CivId, events);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.Error(
                            $"[DivineAscension] Failed to cleanup orphaned civilization {orphan.Name}: {cleanupEx.Message}");
                    }
                }
            }
        }

        return events;
    }
}
