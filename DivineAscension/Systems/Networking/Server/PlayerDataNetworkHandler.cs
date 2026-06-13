using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Configuration;
using DivineAscension.Models.Enum;
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
    private static readonly IReadOnlyList<DeityDomain> AllDeities = DeityDomains.All;

    private readonly IPlayerProgressionDataManager? _playerProgressionDataManager;
    private readonly IReligionManager? _religionManager;
    private readonly IReligionPrestigeManager? _religionPrestigeManager;
    private readonly IWorldService? _worldService;
    private readonly IEventService? _eventService;
    private readonly INetworkService? _networkService;
    private readonly GameBalanceConfig _config;

    /// <summary>
    ///     Initialize the handler with all required dependencies.
    ///     This must be called before RegisterHandlers.
    /// </summary>
    public PlayerDataNetworkHandler(
        IWorldService worldService,
        IEventService eventService,
        INetworkService networkService,
        IPlayerProgressionDataManager playerProgressionDataManager,
        IReligionManager religionManager,
        IReligionPrestigeManager religionPrestigeManager,
        GameBalanceConfig config)
    {
        _worldService = worldService;
        _eventService = eventService;
        _networkService = networkService;
        _playerProgressionDataManager = playerProgressionDataManager;
        _religionManager = religionManager;
        _religionPrestigeManager = religionPrestigeManager;
        _config = config;

        // Subscribe to events
        _playerProgressionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _religionPrestigeManager.OnPrestigeRankChanged += OnPrestigeRankChanged;
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

        if (_religionPrestigeManager != null)
            _religionPrestigeManager.OnPrestigeRankChanged -= OnPrestigeRankChanged;

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
    ///     Religion prestige rank changed (#445): every member needs a refreshed packet
    ///     so the client can detect any change in <c>MaxBlessingSlots</c> and toast.
    /// </summary>
    private void OnPrestigeRankChanged(string religionUID, PrestigeRank oldRank, PrestigeRank newRank)
    {
        if (_religionManager == null || _worldService == null) return;

        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null) return;

        foreach (var memberUID in religion.MemberUIDs)
        {
            var player = _worldService.GetPlayerByUID(memberUID);
            if (player != null) SendPlayerDataToClient(player);
        }
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

        if (religionData != null)
        {
            var favorByDeity = new Dictionary<DeityDomain, int>();
            var ranksByDeity = new Dictionary<DeityDomain, string>();
            var totalByDeity = new Dictionary<DeityDomain, int>();
            foreach (var domain in AllDeities)
            {
                favorByDeity[domain] = playerReligionData!.GetFavor(domain);
                ranksByDeity[domain] = _playerProgressionDataManager
                    .GetPlayerFavorRank(player.PlayerUID, domain).ToString();
                totalByDeity[domain] = playerReligionData.GetTotalFavorEarned(domain);
            }

            // Effective slot cap: the player's patron-domain favor rank plus the religion's
            // prestige bonus. Patron rank is the canonical "player's favor rank" per design
            // (#423) and must match the server-side unlock check in BlessingRegistry, else the
            // header advertises slots the unlock path then refuses (#472).
            var patronFavorRank = _playerProgressionDataManager.GetPlayerPatronFavorRank(player.PlayerUID);
            var maxBlessingSlots =
                BlessingSlotCalculator.GetMaxUnlocks(_config, patronFavorRank, religionData.PrestigeRank);

            var packet = new PlayerReligionDataPacket(
                religionData.ReligionName,
                religionData.PatronDomain,
                religionData.PatronName,
                favorByDeity,
                ranksByDeity,
                totalByDeity,
                religionData.Prestige,
                religionData.PrestigeRank.ToString()
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
                MythicThreshold = _config.MythicThreshold,
                MaxBlessingSlots = maxBlessingSlots
            };

            _networkService.SendToPlayer(player, packet);
        }
    }
}