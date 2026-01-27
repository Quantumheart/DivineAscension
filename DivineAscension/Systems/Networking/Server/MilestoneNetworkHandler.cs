using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Network;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Networking.Server;

/// <summary>
///     Handles milestone network requests from clients
/// </summary>
public class MilestoneNetworkHandler
{
    private readonly ILogger _logger;
    private readonly ICivilizationMilestoneManager _milestoneManager;
    private readonly ICivilizationManager _civilizationManager;
    private readonly IReligionManager _religionManager;
    private readonly INetworkService _networkService;
    private readonly IWorldService _worldService;

    public MilestoneNetworkHandler(
        ILogger logger,
        ICivilizationMilestoneManager milestoneManager,
        ICivilizationManager civilizationManager,
        IReligionManager religionManager,
        INetworkService networkService,
        IWorldService worldService)
    {
        _logger = logger;
        _milestoneManager = milestoneManager;
        _civilizationManager = civilizationManager;
        _religionManager = religionManager;
        _networkService = networkService;
        _worldService = worldService;
    }

    /// <summary>
    ///     Registers network handlers for milestone requests
    /// </summary>
    public void RegisterHandlers()
    {
        _networkService.RegisterMessageHandler<MilestoneProgressRequestPacket>(OnMilestoneProgressRequest);

        // Subscribe to milestone events to broadcast notifications
        _milestoneManager.OnMilestoneUnlocked += OnMilestoneUnlocked;

        _logger.Notification("[DivineAscension] MilestoneNetworkHandler registered");
    }

    /// <summary>
    ///     Handles milestone progress request from client
    /// </summary>
    private void OnMilestoneProgressRequest(IServerPlayer player, MilestoneProgressRequestPacket packet)
    {
        _logger.Debug(
            $"[MilestoneNetworkHandler] Received milestone progress request from {player.PlayerName} for civilization {packet.CivId}");

        // Validate player is member of requested civilization
        var civ = _civilizationManager.GetCivilization(packet.CivId);
        if (civ == null)
        {
            _logger.Warning(
                $"[MilestoneNetworkHandler] Player {player.PlayerName} requested milestones for non-existent civilization {packet.CivId}");
            _networkService.SendToPlayer(player, new MilestoneProgressResponsePacket { CivId = packet.CivId });
            return;
        }

        var playerReligion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (playerReligion == null || !civ.HasReligion(playerReligion.ReligionUID))
        {
            _logger.Warning(
                $"[MilestoneNetworkHandler] Player {player.PlayerName} requested milestones for civilization {packet.CivId} but is not a member");
            _networkService.SendToPlayer(player, new MilestoneProgressResponsePacket { CivId = packet.CivId });
            return;
        }

        // Get milestone progress
        var allProgress = _milestoneManager.GetAllMilestoneProgress(packet.CivId);
        var bonuses = _milestoneManager.GetActiveBonuses(packet.CivId);

        // Build response
        var response = new MilestoneProgressResponsePacket
        {
            CivId = packet.CivId,
            Rank = _milestoneManager.GetCivilizationRank(packet.CivId),
            CompletedMilestones = _milestoneManager.GetCompletedMilestones(packet.CivId).ToList(),
            Progress = allProgress.Values.Select(p => new MilestoneProgressDto
            {
                MilestoneId = p.MilestoneId,
                MilestoneName = p.MilestoneName,
                CurrentValue = p.CurrentValue,
                TargetValue = p.TargetValue,
                IsCompleted = p.IsCompleted
            }).ToList(),
            Bonuses = new CivilizationBonusesDto
            {
                PrestigeMultiplier = bonuses.PrestigeMultiplier,
                FavorMultiplier = bonuses.FavorMultiplier,
                ConquestMultiplier = bonuses.ConquestMultiplier,
                BonusHolySiteSlots = bonuses.BonusHolySiteSlots
            }
        };

        _logger.Debug(
            $"[MilestoneNetworkHandler] Sending milestone progress to {player.PlayerName}: Rank={response.Rank}, Completed={response.CompletedMilestones.Count}");

        _networkService.SendToPlayer(player, response);
    }

    /// <summary>
    ///     Broadcasts milestone unlock notification to all civilization members
    /// </summary>
    private void OnMilestoneUnlocked(string civId, string milestoneId)
    {
        var civ = _civilizationManager.GetCivilization(civId);
        if (civ == null)
            return;

        var progress = _milestoneManager.GetMilestoneProgress(civId, milestoneId);
        if (progress == null)
            return;

        var packet = new MilestoneUnlockedPacket
        {
            CivId = civId,
            MilestoneId = milestoneId,
            MilestoneName = progress.MilestoneName,
            NewRank = _milestoneManager.GetCivilizationRank(civId),
            PrestigePayout = 0, // TODO: Get from milestone definition if needed
            BenefitDescription = $"Civilization milestone '{progress.MilestoneName}' unlocked!"
        };

        // Send to all online players in the civilization
        foreach (var religionId in civ.GetMemberReligionIdsSnapshot())
        {
            var religion = _religionManager.GetReligion(religionId);
            if (religion == null)
                continue;

            foreach (var memberUID in religion.MemberUIDs)
            {
                var player = _worldService.GetPlayerByUID(memberUID) as IServerPlayer;
                if (player != null)
                {
                    _networkService.SendToPlayer(player, packet);
                }
            }
        }

        _logger.Notification(
            $"[MilestoneNetworkHandler] Broadcast milestone unlock '{milestoneId}' to civilization '{civ.Name}'");
    }
}
