using System.Linq;
using DivineAscension.Network;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Networking.Server;

/// <summary>
///     Handles activity log network requests from clients
/// </summary>
public class ActivityNetworkHandler
{
    private readonly IActivityLogManager _activityLogManager;
    private readonly IReligionManager _religionManager;
    private readonly ICoreServerAPI _sapi;
    private readonly IServerNetworkChannel _serverChannel;

    public ActivityNetworkHandler(ICoreServerAPI sapi, IActivityLogManager activityLogManager,
        IReligionManager religionManager, IServerNetworkChannel serverChannel)
    {
        _sapi = sapi;
        _activityLogManager = activityLogManager;
        _religionManager = religionManager;
        _serverChannel = serverChannel;
    }

    /// <summary>
    ///     Registers network handlers for activity log requests
    /// </summary>
    public void RegisterHandlers()
    {
        _serverChannel.SetMessageHandler<ActivityLogRequestPacket>(OnActivityLogRequest);

        _sapi.Logger.Notification("[DivineAscension] ActivityNetworkHandler registered");
    }

    /// <summary>
    ///     Handles activity log request from client
    /// </summary>
    private void OnActivityLogRequest(IServerPlayer player, ActivityLogRequestPacket packet)
    {
        _sapi.Logger.Debug(
            $"[ActivityNetworkHandler] Received activity log request from {player.PlayerName} for religion {packet.ReligionUID}");

        // Validate player is member of requested religion
        var playerReligion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (string.IsNullOrEmpty(playerReligion?.ReligionUID) ||
            playerReligion.ReligionUID != packet.ReligionUID)
        {
            _sapi.Logger.Warning(
                $"[ActivityNetworkHandler] Player {player.PlayerName} requested activity log for religion {packet.ReligionUID} but is not a member");

            // Send empty response if not member
            _serverChannel.SendPacket(new ActivityLogResponsePacket(), player);
            return;
        }

        // Fetch activity log
        var entries = _activityLogManager.GetActivityLog(packet.ReligionUID, packet.Limit);

        _sapi.Logger.Debug(
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

        _serverChannel.SendPacket(response, player);
    }
}