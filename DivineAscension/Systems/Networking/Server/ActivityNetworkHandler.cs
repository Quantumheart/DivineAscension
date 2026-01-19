using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Network;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Networking.Server;

/// <summary>
///     Handles activity log network requests from clients
/// </summary>
public class ActivityNetworkHandler
{
    private readonly ILogger _logger;
    private readonly IActivityLogManager _activityLogManager;
    private readonly IReligionManager _religionManager;
    private readonly INetworkService _networkService;

    public ActivityNetworkHandler(
        ILogger logger,
        IActivityLogManager activityLogManager,
        IReligionManager religionManager,
        INetworkService networkService)
    {
        _logger = logger;
        _activityLogManager = activityLogManager;
        _religionManager = religionManager;
        _networkService = networkService;
    }

    /// <summary>
    ///     Registers network handlers for activity log requests
    /// </summary>
    public void RegisterHandlers()
    {
        _networkService.RegisterMessageHandler<ActivityLogRequestPacket>(OnActivityLogRequest);

        _logger.Notification("[DivineAscension] ActivityNetworkHandler registered");
    }

    /// <summary>
    ///     Handles activity log request from client
    /// </summary>
    private void OnActivityLogRequest(IServerPlayer player, ActivityLogRequestPacket packet)
    {
        _logger.Debug(
            $"[ActivityNetworkHandler] Received activity log request from {player.PlayerName} for religion {packet.ReligionUID}");

        // Validate player is member of requested religion
        var playerReligion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (string.IsNullOrEmpty(playerReligion?.ReligionUID) ||
            playerReligion.ReligionUID != packet.ReligionUID)
        {
            _logger.Warning(
                $"[ActivityNetworkHandler] Player {player.PlayerName} requested activity log for religion {packet.ReligionUID} but is not a member");

            // Send empty response if not member
            _networkService.SendToPlayer(player, new ActivityLogResponsePacket());
            return;
        }

        // Fetch activity log
        var entries = _activityLogManager.GetActivityLog(packet.ReligionUID, packet.Limit);

        _logger.Debug(
            $"[ActivityNetworkHandler] Sending {entries.Count} activity entries to {player.PlayerName}");

        // Convert to response packet
        var response = new ActivityLogResponsePacket
        {
            Entries = entries.Select(e => new ActivityLogResponsePacket.ActivityEntry
            {
                EntryId = e.EntryId,
                PlayerUID = e.PlayerUID,
                PlayerName = e.PlayerName,
                ActionType = e.ActionType,
                FavorAmount = e.FavorAmount,
                PrestigeAmount = e.PrestigeAmount,
                TimestampTicks = e.Timestamp.Ticks,
                DeityDomain = e.DeityDomain
            }).ToList()
        };

        _networkService.SendToPlayer(player, response);
    }
}