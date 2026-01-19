using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Configuration;
using DivineAscension.Network;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Networking.Server;

/// <summary>
///     Handles player data synchronization between server and client.
///     Sends player religion data (favor, rank, prestige) to clients when:
///     - Player joins the server
///     - Player's data changes (favor gain, religion changes, etc.)
/// </summary>
[ExcludeFromCodeCoverage]
public class PlayerDataNetworkHandler : IServerNetworkHandler
{
    private readonly IPlayerProgressionDataManager? _playerProgressionDataManager;
    private readonly IReligionManager? _religionManager;
    private readonly ILogger? _logger;
    private readonly IWorldService? _worldService;
    private readonly IEventService? _eventService;
    private readonly INetworkService? _networkService;
    private readonly GameBalanceConfig _config;

    /// <summary>
    ///     Initialize the handler with all required dependencies.
    ///     This must be called before RegisterHandlers.
    /// </summary>
    public PlayerDataNetworkHandler(ILogger logger,
        IWorldService worldService,
        IEventService eventService,
        INetworkService networkService,
        IPlayerProgressionDataManager playerProgressionDataManager,
        IReligionManager religionManager,
        GameBalanceConfig config)
    {
        _logger = logger;
        _worldService = worldService;
        _eventService = eventService;
        _networkService = networkService;
        _playerProgressionDataManager = playerProgressionDataManager;
        _religionManager = religionManager;
        _config = config;

        // Subscribe to events
        _playerProgressionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _eventService!.OnPlayerJoin(OnPlayerJoin);
    }

    public void RegisterHandlers()
    {
    }

    public void Dispose()
    {
        // Unsubscribe from events
        if (_playerProgressionDataManager != null)
            _playerProgressionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;

        if (_eventService != null)
            _eventService.UnsubscribePlayerJoin(OnPlayerJoin);
    }

    private void OnPlayerJoin(IServerPlayer player)
    {
        // Send initial player data to client
        SendPlayerDataToClient(player);
    }

    /// <summary>
    ///     Handle player data changes (favor, rank, etc.) and notify client
    /// </summary>
    private void OnPlayerDataChanged(string playerUID)
    {
        var player = _worldService!.GetPlayerByUID(playerUID);
        if (player != null) SendPlayerDataToClient(player);
    }

    /// <summary>
    ///     Send player's religion data to the client for HUD updates.
    ///     This is called by other network handlers after state changes.
    /// </summary>
    public void SendPlayerDataToClient(IServerPlayer player)
    {
        if (_playerProgressionDataManager == null || _religionManager == null ||
            _networkService == null) return;

        if (!_playerProgressionDataManager.TryGetPlayerData(player.PlayerUID, out var playerReligionData))
            return;

        var religionData = _religionManager.GetPlayerReligion(player.PlayerUID);
        var deity = _playerProgressionDataManager.GetPlayerDeityType(player.PlayerUID);

        if (religionData != null)
        {
            var packet = new PlayerReligionDataPacket(
                religionData.ReligionName,
                deity.ToString(),
                religionData.DeityName,
                playerReligionData!.Favor,
                _playerProgressionDataManager.GetPlayerFavorRank(player.PlayerUID).ToString(),
                religionData.Prestige,
                religionData.PrestigeRank.ToString(),
                playerReligionData.TotalFavorEarned
            )
            {
                // Send config thresholds so client UI displays correct values
                DiscipleThreshold = _config.DiscipleThreshold,
                ZealotThreshold = _config.ZealotThreshold,
                ChampionThreshold = _config.ChampionThreshold,
                AvatarThreshold = _config.AvatarThreshold,
                EstablishedThreshold = _config.EstablishedThreshold,
                RenownedThreshold = _config.RenownedThreshold,
                LegendaryThreshold = _config.LegendaryThreshold,
                MythicThreshold = _config.MythicThreshold
            };

            _networkService.SendToPlayer(player, packet);
        }
    }
}