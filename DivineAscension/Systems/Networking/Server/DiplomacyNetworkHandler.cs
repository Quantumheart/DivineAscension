using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.Constants;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Diplomacy;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Networking.Server;

/// <summary>
///     Handles diplomacy-related network requests from clients.
///     Manages diplomacy info requests and action requests
///     (propose, accept, decline, schedulebreak, cancelbreak, declarewar, declarepeace).
/// </summary>
[ExcludeFromCodeCoverage]
#pragma warning disable CS9113 // Parameter is unread - kept for interface consistency and future use
public class DiplomacyNetworkHandler(
    ICoreServerAPI sapi,
    IDiplomacyManager diplomacyManager,
    CivilizationManager civilizationManager,
    IReligionManager religionManager,
    IPlayerProgressionDataManager playerProgressionDataManager,
    IServerNetworkChannel serverChannel)
#pragma warning restore CS9113
    : IServerNetworkHandler
{
    public void RegisterHandlers()
    {
        // Register handlers for diplomacy system packets
        serverChannel.SetMessageHandler<DiplomacyInfoRequestPacket>(OnDiplomacyInfoRequest);
        serverChannel.SetMessageHandler<DiplomacyActionRequestPacket>(OnDiplomacyActionRequest);
    }

    public void Dispose()
    {
        // No resources to dispose
    }

    /// <summary>
    ///     Handle diplomacy info request from client
    /// </summary>
    private void OnDiplomacyInfoRequest(IServerPlayer fromPlayer, DiplomacyInfoRequestPacket packet)
    {
        sapi.Logger.Debug(
            $"{DiplomacyConstants.LogPrefix} Diplomacy info requested by {fromPlayer.PlayerName} for civilization {packet.CivId}");

        var civ = civilizationManager.GetCivilization(packet.CivId);
        if (civ == null)
        {
            sapi.Logger.Warning(
                $"{DiplomacyConstants.LogPrefix} Civilization {packet.CivId} not found for diplomacy info request");
            serverChannel.SendPacket(
                new DiplomacyInfoResponsePacket(packet.CivId, new List<DiplomacyInfoResponsePacket.RelationshipInfo>(),
                    new List<DiplomacyInfoResponsePacket.ProposalInfo>(),
                    new List<DiplomacyInfoResponsePacket.ProposalInfo>()), fromPlayer);
            return;
        }

        // Get all relationships for this civilization
        var relationships = diplomacyManager.GetRelationshipsForCiv(packet.CivId);
        var relationshipInfos = new List<DiplomacyInfoResponsePacket.RelationshipInfo>();

        foreach (var relationship in relationships)
        {
            var otherCivId = relationship.GetOtherCivilization(packet.CivId);
            var otherCiv = civilizationManager.GetCivilization(otherCivId);
            var otherCivName = otherCiv?.Name ?? otherCivId;

            relationshipInfos.Add(new DiplomacyInfoResponsePacket.RelationshipInfo(
                relationship.RelationshipId,
                otherCivId,
                otherCivName,
                relationship.Status,
                relationship.EstablishedDate,
                relationship.ExpiresDate,
                relationship.ViolationCount,
                relationship.BreakScheduledDate
            ));
        }

        // Get all proposals for this civilization
        var proposals = diplomacyManager.GetProposalsForCiv(packet.CivId);
        var incomingProposals = new List<DiplomacyInfoResponsePacket.ProposalInfo>();
        var outgoingProposals = new List<DiplomacyInfoResponsePacket.ProposalInfo>();

        foreach (var proposal in proposals)
        {
            var isIncoming = proposal.TargetCivId == packet.CivId;
            var otherCivId = isIncoming ? proposal.ProposerCivId : proposal.TargetCivId;
            var otherCiv = civilizationManager.GetCivilization(otherCivId);
            var otherCivName = otherCiv?.Name ?? otherCivId;

            var proposalInfo = new DiplomacyInfoResponsePacket.ProposalInfo(
                proposal.ProposalId,
                otherCivId,
                otherCivName,
                proposal.ProposedStatus,
                proposal.SentDate,
                proposal.ExpiresDate,
                proposal.Duration
            );

            if (isIncoming)
                incomingProposals.Add(proposalInfo);
            else
                outgoingProposals.Add(proposalInfo);
        }

        var response = new DiplomacyInfoResponsePacket(packet.CivId, relationshipInfos, incomingProposals,
            outgoingProposals);
        serverChannel.SendPacket(response, fromPlayer);
        sapi.Logger.Debug(
            $"{DiplomacyConstants.LogPrefix} Sent diplomacy info: {relationshipInfos.Count} relationships, {incomingProposals.Count} incoming proposals, {outgoingProposals.Count} outgoing proposals");
    }

    /// <summary>
    ///     Handle diplomacy action request from client
    /// </summary>
    private void OnDiplomacyActionRequest(IServerPlayer fromPlayer, DiplomacyActionRequestPacket packet)
    {
        sapi.Logger.Debug(
            $"{DiplomacyConstants.LogPrefix} Diplomacy action '{packet.Action}' requested by {fromPlayer.PlayerName}");

        var religion = religionManager.GetPlayerReligion(fromPlayer.PlayerUID);
        if (!religionManager.HasReligion(fromPlayer.PlayerUID))
        {
            SendActionResponse(fromPlayer, false,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_MUST_BE_IN_RELIGION), packet.Action);
            return;
        }

        // Verify the player is the founder of their civilization
        if (religion == null)
        {
            SendActionResponse(fromPlayer, false,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_RELIGION_NO_LONGER_EXISTS),
                packet.Action);
            return;
        }

        var playerCiv = civilizationManager.GetCivilizationByReligion(religion.ReligionUID);
        if (playerCiv == null)
        {
            SendActionResponse(fromPlayer, false,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_NOT_PART_OF_CIVILIZATION),
                packet.Action);
            return;
        }

        switch (packet.Action.ToLowerInvariant())
        {
            case "propose":
                HandleProposeAction(fromPlayer, playerCiv.CivId, packet);
                break;
            case "accept":
                HandleAcceptAction(fromPlayer, playerCiv.CivId, packet);
                break;
            case "decline":
                HandleDeclineAction(fromPlayer, playerCiv.CivId, packet);
                break;
            case "schedulebreak":
                HandleScheduleBreakAction(fromPlayer, playerCiv.CivId, packet);
                break;
            case "cancelbreak":
                HandleCancelBreakAction(fromPlayer, playerCiv.CivId, packet);
                break;
            case "declarewar":
                HandleDeclareWarAction(fromPlayer, playerCiv.CivId, packet);
                break;
            case "declarepeace":
                HandleDeclarePeaceAction(fromPlayer, playerCiv.CivId, packet);
                break;
            default:
                SendActionResponse(fromPlayer, false,
                    LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_UNKNOWN_ACTION, packet.Action),
                    packet.Action);
                break;
        }
    }

    private void HandleProposeAction(IServerPlayer player, string civId, DiplomacyActionRequestPacket packet)
    {
        if (string.IsNullOrEmpty(packet.TargetCivId))
        {
            SendActionResponse(player, false,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_TARGET_CIV_REQUIRED), packet.Action);
            return;
        }

        if (string.IsNullOrEmpty(packet.ProposedStatus))
        {
            SendActionResponse(player, false,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_PROPOSED_STATUS_REQUIRED),
                packet.Action);
            return;
        }

        if (!Enum.TryParse<DiplomaticStatus>(packet.ProposedStatus, out var proposedStatus))
        {
            SendActionResponse(player, false,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_INVALID_STATUS, packet.ProposedStatus),
                packet.Action);
            return;
        }

        var result = diplomacyManager.ProposeRelationship(
            civId,
            packet.TargetCivId,
            proposedStatus,
            player.PlayerUID,
            packet.Duration);

        SendActionResponse(player, result.success, result.message, packet.Action, proposalId: result.proposalId);

        if (result.success)
        {
            // Notify target civilization founder
            NotifyTargetCivilization(packet.TargetCivId,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_PROPOSAL_RECEIVED, proposedStatus,
                    GetCivName(civId)));
        }
    }

    private void HandleAcceptAction(IServerPlayer player, string civId, DiplomacyActionRequestPacket packet)
    {
        if (string.IsNullOrEmpty(packet.ProposalId))
        {
            SendActionResponse(player, false,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_PROPOSAL_ID_REQUIRED), packet.Action);
            return;
        }

        var result = diplomacyManager.AcceptProposal(packet.ProposalId, player.PlayerUID);

        SendActionResponse(player, result.success, result.message, packet.Action,
            relationshipId: result.relationshipId);

        if (result.success)
        {
            // Notify proposer civilization (the other civ in the newly established relationship)
            var relationship = diplomacyManager.GetRelationship(result.relationshipId!);
            if (relationship != null)
            {
                var otherCivId = relationship.GetOtherCivilization(civId);
                NotifyTargetCivilization(otherCivId,
                    LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_PROPOSAL_ACCEPTED,
                        GetCivName(civId), relationship.Status));
            }
        }
    }

    private void HandleDeclineAction(IServerPlayer player, string civId, DiplomacyActionRequestPacket packet)
    {
        if (string.IsNullOrEmpty(packet.ProposalId))
        {
            SendActionResponse(player, false,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_PROPOSAL_ID_REQUIRED), packet.Action);
            return;
        }

        var result = diplomacyManager.DeclineProposal(packet.ProposalId, player.PlayerUID);

        SendActionResponse(player, result.success, result.message, packet.Action);

        // Note: DiplomacyManager already notifies the proposer, so we don't need to do it here
    }

    private void HandleScheduleBreakAction(IServerPlayer player, string civId, DiplomacyActionRequestPacket packet)
    {
        if (string.IsNullOrEmpty(packet.TargetCivId))
        {
            SendActionResponse(player, false,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_TARGET_CIV_REQUIRED), packet.Action);
            return;
        }

        var result = diplomacyManager.ScheduleBreak(civId, packet.TargetCivId, player.PlayerUID);

        SendActionResponse(player, result.success, result.message, packet.Action);

        if (result.success)
        {
            NotifyTargetCivilization(packet.TargetCivId,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_TREATY_BREAK_SCHEDULED,
                    GetCivName(civId)));
        }
    }

    private void HandleCancelBreakAction(IServerPlayer player, string civId, DiplomacyActionRequestPacket packet)
    {
        if (string.IsNullOrEmpty(packet.TargetCivId))
        {
            SendActionResponse(player, false,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_TARGET_CIV_REQUIRED), packet.Action);
            return;
        }

        var result = diplomacyManager.CancelScheduledBreak(civId, packet.TargetCivId, player.PlayerUID);

        SendActionResponse(player, result.success, result.message, packet.Action);

        if (result.success)
        {
            NotifyTargetCivilization(packet.TargetCivId,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_TREATY_BREAK_CANCELED,
                    GetCivName(civId)));
        }
    }

    private void HandleDeclareWarAction(IServerPlayer player, string civId, DiplomacyActionRequestPacket packet)
    {
        if (string.IsNullOrEmpty(packet.TargetCivId))
        {
            SendActionResponse(player, false,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_TARGET_CIV_REQUIRED), packet.Action);
            return;
        }

        var result = diplomacyManager.DeclareWar(civId, packet.TargetCivId, player.PlayerUID);

        SendActionResponse(player, result.success, result.message, packet.Action);

        // Note: DiplomacyManager fires OnWarDeclared event which triggers ReligionPrestigeManager's broadcast
    }

    private void HandleDeclarePeaceAction(IServerPlayer player, string civId, DiplomacyActionRequestPacket packet)
    {
        if (string.IsNullOrEmpty(packet.TargetCivId))
        {
            SendActionResponse(player, false,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_TARGET_CIV_REQUIRED), packet.Action);
            return;
        }

        var result = diplomacyManager.DeclarePeace(civId, packet.TargetCivId, player.PlayerUID);

        SendActionResponse(player, result.success, result.message, packet.Action);

        if (result.success)
        {
            NotifyTargetCivilization(packet.TargetCivId,
                LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_PEACE_DECLARED, GetCivName(civId)));
        }
    }

    /// <summary>
    ///     Send a diplomacy action response to the player
    /// </summary>
    private void SendActionResponse(
        IServerPlayer player,
        bool success,
        string message,
        string action,
        string? relationshipId = null,
        string? proposalId = null,
        int? violationCount = null)
    {
        var response = new DiplomacyActionResponsePacket(success, message, action, relationshipId, proposalId,
            violationCount);
        serverChannel.SendPacket(response, player);
    }

    /// <summary>
    ///     Notify the founder of a target civilization
    /// </summary>
    private void NotifyTargetCivilization(string targetCivId, string message)
    {
        var targetCiv = civilizationManager.GetCivilization(targetCivId);
        if (targetCiv == null) return;

        var founderPlayer = sapi.World.AllOnlinePlayers.FirstOrDefault(p => p.PlayerUID == targetCiv.FounderUID);
        if (founderPlayer is IServerPlayer serverPlayer)
        {
            var prefix = LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_PREFIX);
            serverPlayer.SendMessage(
                GlobalConstants.GeneralChatGroup,
                $"{prefix} {message}",
                EnumChatType.Notification,
                null);
        }
    }

    /// <summary>
    ///     Get civilization name by ID
    /// </summary>
    private string GetCivName(string civId)
    {
        var civ = civilizationManager.GetCivilization(civId);
        return civ?.Name ?? civId;
    }
}