using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.API.Interfaces;
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
public class DiplomacyNetworkHandler : IServerNetworkHandler
{
    private readonly ILogger _logger;
    private readonly IDiplomacyManager _diplomacyManager;
    private readonly CivilizationManager _civilizationManager;
    private readonly IReligionManager _religionManager;
    private readonly IPlayerProgressionDataManager _playerProgressionDataManager;
    private readonly INetworkService _networkService;
    private readonly IPlayerMessengerService _messengerService;
    private readonly IWorldService _worldService;

    public DiplomacyNetworkHandler(
        ILogger logger,
        IDiplomacyManager diplomacyManager,
        CivilizationManager civilizationManager,
        IReligionManager religionManager,
        IPlayerProgressionDataManager playerProgressionDataManager,
        INetworkService networkService,
        IPlayerMessengerService messengerService,
        IWorldService worldService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _diplomacyManager = diplomacyManager ?? throw new ArgumentNullException(nameof(diplomacyManager));
        _civilizationManager = civilizationManager ?? throw new ArgumentNullException(nameof(civilizationManager));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _playerProgressionDataManager = playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));
        _networkService = networkService ?? throw new ArgumentNullException(nameof(networkService));
        _messengerService = messengerService ?? throw new ArgumentNullException(nameof(messengerService));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
    }

    public void RegisterHandlers()
    {
        // Register handlers for diplomacy system packets
        _networkService.RegisterMessageHandler<DiplomacyInfoRequestPacket>(OnDiplomacyInfoRequest);
        _networkService.RegisterMessageHandler<DiplomacyActionRequestPacket>(OnDiplomacyActionRequest);
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
        _logger.Debug(
            $"{DiplomacyConstants.LogPrefix} Diplomacy info requested by {fromPlayer.PlayerName} for civilization {packet.CivId}");

        var civ = _civilizationManager.GetCivilization(packet.CivId);
        if (civ == null)
        {
            _logger.Warning(
                $"{DiplomacyConstants.LogPrefix} Civilization {packet.CivId} not found for diplomacy info request");
            _networkService.SendToPlayer(fromPlayer,
                new DiplomacyInfoResponsePacket(packet.CivId, new List<DiplomacyInfoResponsePacket.RelationshipInfo>(),
                    new List<DiplomacyInfoResponsePacket.ProposalInfo>(),
                    new List<DiplomacyInfoResponsePacket.ProposalInfo>()));
            return;
        }

        // Get all relationships for this civilization
        var relationships = _diplomacyManager.GetRelationshipsForCiv(packet.CivId);
        var relationshipInfos = new List<DiplomacyInfoResponsePacket.RelationshipInfo>();

        foreach (var relationship in relationships)
        {
            var otherCivId = relationship.GetOtherCivilization(packet.CivId);
            var otherCiv = _civilizationManager.GetCivilization(otherCivId);
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
        var proposals = _diplomacyManager.GetProposalsForCiv(packet.CivId);
        var incomingProposals = new List<DiplomacyInfoResponsePacket.ProposalInfo>();
        var outgoingProposals = new List<DiplomacyInfoResponsePacket.ProposalInfo>();

        foreach (var proposal in proposals)
        {
            var isIncoming = proposal.TargetCivId == packet.CivId;
            var otherCivId = isIncoming ? proposal.ProposerCivId : proposal.TargetCivId;
            var otherCiv = _civilizationManager.GetCivilization(otherCivId);
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
        _networkService.SendToPlayer(fromPlayer, response);
        _logger.Debug(
            $"{DiplomacyConstants.LogPrefix} Sent diplomacy info: {relationshipInfos.Count} relationships, {incomingProposals.Count} incoming proposals, {outgoingProposals.Count} outgoing proposals");
    }

    /// <summary>
    ///     Handle diplomacy action request from client
    /// </summary>
    private void OnDiplomacyActionRequest(IServerPlayer fromPlayer, DiplomacyActionRequestPacket packet)
    {
        _logger.Debug(
            $"{DiplomacyConstants.LogPrefix} Diplomacy action '{packet.Action}' requested by {fromPlayer.PlayerName}");

        var religion = _religionManager.GetPlayerReligion(fromPlayer.PlayerUID);
        if (!_religionManager.HasReligion(fromPlayer.PlayerUID))
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

        var playerCiv = _civilizationManager.GetCivilizationByReligion(religion.ReligionUID);
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

        var result = _diplomacyManager.ProposeRelationship(
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

        var result = _diplomacyManager.AcceptProposal(packet.ProposalId, player.PlayerUID);

        SendActionResponse(player, result.success, result.message, packet.Action,
            relationshipId: result.relationshipId);

        if (result.success)
        {
            // Notify proposer civilization (the other civ in the newly established relationship)
            var relationship = _diplomacyManager.GetRelationship(result.relationshipId!);
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

        var result = _diplomacyManager.DeclineProposal(packet.ProposalId, player.PlayerUID);

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

        var result = _diplomacyManager.ScheduleBreak(civId, packet.TargetCivId, player.PlayerUID);

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

        var result = _diplomacyManager.CancelScheduledBreak(civId, packet.TargetCivId, player.PlayerUID);

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

        var result = _diplomacyManager.DeclareWar(civId, packet.TargetCivId, player.PlayerUID);

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

        var result = _diplomacyManager.DeclarePeace(civId, packet.TargetCivId, player.PlayerUID);

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
        _networkService.SendToPlayer(player, response);
    }

    /// <summary>
    ///     Notify the founder of a target civilization
    /// </summary>
    private void NotifyTargetCivilization(string targetCivId, string message)
    {
        var targetCiv = _civilizationManager.GetCivilization(targetCivId);
        if (targetCiv == null) return;

        var founderPlayer = _worldService.GetAllOnlinePlayers().FirstOrDefault(p => p.PlayerUID == targetCiv.FounderUID);
        if (founderPlayer is IServerPlayer serverPlayer)
        {
            var prefix = LocalizationService.Instance.Get(LocalizationKeys.NET_DIPLOMACY_PREFIX);
            _messengerService.SendMessage(
                serverPlayer,
                $"{prefix} {message}",
                EnumChatType.Notification);
        }
    }

    /// <summary>
    ///     Get civilization name by ID
    /// </summary>
    private string GetCivName(string civId)
    {
        var civ = _civilizationManager.GetCivilization(civId);
        return civ?.Name ?? civId;
    }
}