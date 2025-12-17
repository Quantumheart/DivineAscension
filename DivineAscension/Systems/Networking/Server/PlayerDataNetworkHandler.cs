using System.Diagnostics.CodeAnalysis;
using DivineAscension.Network;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Interfaces;
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
    private readonly DeityRegistry? _deityRegistry;
    private readonly IPlayerReligionDataManager? _playerReligionDataManager;
    private readonly IReligionManager? _religionManager;
    private readonly ICoreServerAPI? _sapi;
    private IServerNetworkChannel? _serverChannel;

    /// <summary>
    ///     Initialize the handler with all required dependencies.
    ///     This must be called before RegisterHandlers.
    /// </summary>
    public PlayerDataNetworkHandler(ICoreServerAPI sapi,
        IPlayerReligionDataManager playerReligionDataManager,
        IReligionManager religionManager,
        DeityRegistry deityRegistry,
        IServerNetworkChannel serverChannel)
    {
        _sapi = sapi;
        _playerReligionDataManager = playerReligionDataManager;
        _religionManager = religionManager;
        _deityRegistry = deityRegistry;
        _serverChannel = serverChannel;

        // Subscribe to events
        _playerReligionDataManager.OnPlayerDataChanged += OnPlayerDataChanged;
        _sapi!.Event.PlayerJoin += OnPlayerJoin;
    }

    public void RegisterHandlers()
    {
    }

    public void Dispose()
    {
        // Unsubscribe from events
        if (_playerReligionDataManager != null)
            _playerReligionDataManager.OnPlayerDataChanged -= OnPlayerDataChanged;

        if (_sapi != null)
            _sapi.Event.PlayerJoin -= OnPlayerJoin;
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
        var player = _sapi!.World.PlayerByUid(playerUID) as IServerPlayer;
        if (player != null) SendPlayerDataToClient(player);
    }

    /// <summary>
    ///     Send player's religion data to the client for HUD updates.
    ///     This is called by other network handlers after state changes.
    /// </summary>
    public void SendPlayerDataToClient(IServerPlayer player)
    {
        if (_playerReligionDataManager == null || _religionManager == null || _deityRegistry == null ||
            _serverChannel == null) return;

        var playerReligionData = _playerReligionDataManager!.GetOrCreatePlayerData(player.PlayerUID);
        var religionData = _religionManager!.GetPlayerReligion(player.PlayerUID);
        var deity = _deityRegistry.GetDeity(playerReligionData.ActiveDeity);
        var deityName = deity?.Name ?? "None";

        if (religionData != null)
        {
            var packet = new PlayerReligionDataPacket(
                religionData.ReligionName,
                deityName,
                playerReligionData.Favor,
                playerReligionData.FavorRank.ToString(),
                religionData.Prestige,
                religionData.PrestigeRank.ToString(),
                playerReligionData.TotalFavorEarned
            );

            _serverChannel.SendPacket(packet, player);
        }
    }
}