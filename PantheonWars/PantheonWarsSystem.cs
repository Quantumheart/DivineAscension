using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using PantheonWars.Network;
using PantheonWars.Network.Civilization;
using PantheonWars.Systems;
using PantheonWars.Systems.Networking.Server;
using PantheonWars.Systems.Patches;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars;

[ExcludeFromCodeCoverage]
public class PantheonWarsSystem : ModSystem
{
    public const string NETWORK_CHANNEL = "pantheonwars";

    // Client-side systems
    private ICoreClientAPI? _capi;

    // Use interfaces for better testability and dependency injection
    private IClientNetworkChannel? _clientChannel;
    private FavorSystem? _favorSystem;

    private Harmony? _harmony;
    private PlayerReligionDataManager? _playerReligionDataManager;
    private ReligionManager? _religionManager;

    // Server-side systems
    private IServerNetworkChannel? _serverChannel;
    private PlayerDataNetworkHandler? _playerDataNetworkHandler;
    private BlessingNetworkHandler? _blessingNetworkHandler;
    private ReligionNetworkHandler? _religionNetworkHandler;
    private CivilizationNetworkHandler? _civilizationNetworkHandler;

    public string ModName => "pantheonwars";

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.Logger.Notification("[PantheonWars] Mod loaded!");

        // Register Harmony Patches
        if (_harmony == null)
        {
            _harmony = new Harmony("com.pantheonwars.patches");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            api.Logger.Notification("[PantheonWars] Harmony patches registered.");
        }

        // Register network channel and message types
        api.Network.RegisterChannel(NETWORK_CHANNEL)
            .RegisterMessageType<PlayerReligionDataPacket>()
            .RegisterMessageType<ReligionListRequestPacket>()
            .RegisterMessageType<ReligionListResponsePacket>()
            .RegisterMessageType<PlayerReligionInfoRequestPacket>()
            .RegisterMessageType<PlayerReligionInfoResponsePacket>()
            .RegisterMessageType<ReligionActionRequestPacket>()
            .RegisterMessageType<ReligionActionResponsePacket>()
            .RegisterMessageType<CreateReligionRequestPacket>()
            .RegisterMessageType<CreateReligionResponsePacket>()
            .RegisterMessageType<EditDescriptionRequestPacket>()
            .RegisterMessageType<EditDescriptionResponsePacket>()
            .RegisterMessageType<BlessingUnlockRequestPacket>()
            .RegisterMessageType<BlessingUnlockResponsePacket>()
            .RegisterMessageType<BlessingDataRequestPacket>()
            .RegisterMessageType<BlessingDataResponsePacket>()
            .RegisterMessageType<ReligionStateChangedPacket>()
            .RegisterMessageType<CivilizationListRequestPacket>()
            .RegisterMessageType<CivilizationListResponsePacket>()
            .RegisterMessageType<CivilizationInfoRequestPacket>()
            .RegisterMessageType<CivilizationInfoResponsePacket>()
            .RegisterMessageType<CivilizationActionRequestPacket>()
            .RegisterMessageType<CivilizationActionResponsePacket>();
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);

        // Setup network channel and handlers
        _serverChannel = api.Network.GetChannel(NETWORK_CHANNEL);
        _serverChannel.SetMessageHandler<PlayerReligionDataPacket>(OnServerMessageReceived);
        SetupServerNetworking(api);

        // Initialize all server systems using the initializer
        var result = PantheonWarsSystemInitializer.InitializeServerSystems(api, _serverChannel);

        // Store references to managers for disposal and event subscriptions
        _religionManager = result.ReligionManager;
        _playerReligionDataManager = result.PlayerReligionDataManager;
        _favorSystem = result.FavorSystem;
        _playerDataNetworkHandler = result.PlayerDataNetworkHandler;
        _blessingNetworkHandler = result.BlessingNetworkHandler;
        _religionNetworkHandler = result.ReligionNetworkHandler;
        _civilizationNetworkHandler = result.CivilizationNetworkHandler;

        api.Logger.Notification("[PantheonWars] Server-side initialization complete");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        _capi = api;
        api.Logger.Notification("[PantheonWars] Initializing client-side systems...");

        // Setup network handlers
        SetupClientNetworking(api);


        api.Logger.Notification("[PantheonWars] Client-side initialization complete");
    }

    public override void Dispose()
    {
        base.Dispose();

        // Unpatch Harmony
        _harmony?.UnpatchAll("com.pantheonwars.patches");

        // Cleanup network handlers
        _playerDataNetworkHandler?.Dispose();
        _blessingNetworkHandler?.Dispose();
        _religionNetworkHandler?.Dispose();
        _civilizationNetworkHandler?.Dispose();

        // Cleanup systems
        _favorSystem?.Dispose();
        _playerReligionDataManager?.Dispose();
        _religionManager?.Dispose();

        // Clear static events
        PitKilnPatches.ClearSubscribers();
    }

    #region Server Networking

    private void SetupServerNetworking(ICoreServerAPI api)
    {
        // Placeholder for any future server networking setup not handled by network handlers
    }

    private void OnServerMessageReceived(IServerPlayer fromPlayer, PlayerReligionDataPacket packet)
    {
        // Handle any client-to-server messages here
        // Currently not used, but necessary for channel setup
        // Future implementation: Handle deity selection from client dialog
    }

    #endregion

    #region Client Networking

    private void SetupClientNetworking(ICoreClientAPI api)
    {
        _clientChannel = api.Network.GetChannel(NETWORK_CHANNEL);
        _clientChannel.SetMessageHandler<PlayerReligionDataPacket>(OnServerPlayerDataUpdate);
        _clientChannel.SetMessageHandler<ReligionListResponsePacket>(OnReligionListResponse);
        _clientChannel.SetMessageHandler<PlayerReligionInfoResponsePacket>(OnPlayerReligionInfoResponse);
        _clientChannel.SetMessageHandler<ReligionActionResponsePacket>(OnReligionActionResponse);
        _clientChannel.SetMessageHandler<CreateReligionResponsePacket>(OnCreateReligionResponse);
        _clientChannel.SetMessageHandler<EditDescriptionResponsePacket>(OnEditDescriptionResponse);
        _clientChannel.SetMessageHandler<BlessingUnlockResponsePacket>(OnBlessingUnlockResponse);
        _clientChannel.SetMessageHandler<BlessingDataResponsePacket>(OnBlessingDataResponse);
        _clientChannel.SetMessageHandler<ReligionStateChangedPacket>(OnReligionStateChanged);
        _clientChannel.SetMessageHandler<CivilizationListResponsePacket>(OnCivilizationListResponse);
        _clientChannel.SetMessageHandler<CivilizationInfoResponsePacket>(OnCivilizationInfoResponse);
        _clientChannel.SetMessageHandler<CivilizationActionResponsePacket>(OnCivilizationActionResponse);
        _clientChannel.RegisterMessageType(typeof(PlayerReligionDataPacket));
    }

    private void OnServerPlayerDataUpdate(PlayerReligionDataPacket packet)
    {
        // Trigger event for BlessingDialog and other UI components
        PlayerReligionDataUpdated?.Invoke(packet);
    }

    private void OnReligionListResponse(ReligionListResponsePacket packet)
    {
        ReligionListReceived?.Invoke(packet);
    }

    private void OnPlayerReligionInfoResponse(PlayerReligionInfoResponsePacket packet)
    {
        PlayerReligionInfoReceived?.Invoke(packet);
    }

    private void OnReligionActionResponse(ReligionActionResponsePacket packet)
    {
        ReligionActionCompleted?.Invoke(packet);
    }

    private void OnCreateReligionResponse(CreateReligionResponsePacket packet)
    {
        if (packet.Success)
        {
            _capi?.ShowChatMessage(packet.Message);

            // Request fresh blessing data (now in a religion)
            // Use a small delay to ensure server has processed the religion creation
            _capi?.Event.RegisterCallback(dt =>
            {
                var request = new BlessingDataRequestPacket();
                _clientChannel?.SendPacket(request);
            }, 100);
        }
        else
        {
            _capi?.ShowChatMessage($"Error: {packet.Message}");

            // Play error sound
            _capi?.World.PlaySoundAt(new AssetLocation("pantheonwars:sounds/error"),
                _capi.World.Player.Entity, null, false, 8f, 0.3f);
        }
    }

    private void OnEditDescriptionResponse(EditDescriptionResponsePacket packet)
    {
        if (packet.Success)
            _capi?.ShowChatMessage(packet.Message);
        else
            _capi?.ShowChatMessage($"Error: {packet.Message}");
    }

    private void OnBlessingUnlockResponse(BlessingUnlockResponsePacket packet)
    {
        if (packet.Success)
        {
            _capi?.ShowChatMessage(packet.Message);
            _capi?.Logger.Notification($"[PantheonWars] Blessing unlocked: {packet.BlessingId}");

            // Trigger blessing unlock event for UI refresh
            BlessingUnlocked?.Invoke(packet.BlessingId, packet.Success);
        }
        else
        {
            _capi?.ShowChatMessage($"Error: {packet.Message}");
            _capi?.Logger.Warning($"[PantheonWars] Failed to unlock blessing: {packet.Message}");

            // Trigger event even on failure so UI can update
            BlessingUnlocked?.Invoke(packet.BlessingId, packet.Success);
        }
    }

    private void OnBlessingDataResponse(BlessingDataResponsePacket packet)
    {
        _capi?.Logger.Debug($"[PantheonWars] Received blessing data: HasReligion={packet.HasReligion}");

        // Trigger event for BlessingDialog to consume
        BlessingDataReceived?.Invoke(packet);
    }

    private void OnReligionStateChanged(ReligionStateChangedPacket packet)
    {
        _capi?.Logger.Notification($"[PantheonWars] Religion state changed: {packet.Reason}");

        // Show notification to user
        _capi?.ShowChatMessage(packet.Reason);

        // Trigger event for BlessingDialog to refresh its data
        ReligionStateChanged?.Invoke(packet);
    }

    private void OnCivilizationListResponse(CivilizationListResponsePacket packet)
    {
        _capi?.Logger.Debug($"[PantheonWars] Received civilization list: {packet.Civilizations.Count} civilizations");
        CivilizationListReceived?.Invoke(packet);
    }

    private void OnCivilizationInfoResponse(CivilizationInfoResponsePacket packet)
    {
        _capi?.Logger.Debug("[PantheonWars] Received civilization info");
        CivilizationInfoReceived?.Invoke(packet);
    }

    private void OnCivilizationActionResponse(CivilizationActionResponsePacket packet)
    {
        _capi?.Logger.Debug($"[PantheonWars] Civilization action '{packet.Action}' response: {packet.Success}");

        // Show message to user
        if (!string.IsNullOrEmpty(packet.Message)) _capi?.ShowChatMessage(packet.Message);

        CivilizationActionCompleted?.Invoke(packet);
    }

    /// <summary>
    ///     Request blessing data from the server
    /// </summary>
    public void RequestBlessingData()
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request blessing data: client channel not initialized");
            return;
        }

        var request = new BlessingDataRequestPacket();
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug("[PantheonWars] Sent blessing data request to server");
    }

    /// <summary>
    ///     Send a blessing unlock request to the server
    /// </summary>
    public void RequestBlessingUnlock(string blessingId)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot unlock blessing: client channel not initialized");
            return;
        }

        var request = new BlessingUnlockRequestPacket(blessingId);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent unlock request for blessing: {blessingId}");
    }

    /// <summary>
    ///     Request religion list from the server
    /// </summary>
    public void RequestReligionList(string deityFilter = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request religion list: client channel not initialized");
            return;
        }

        var request = new ReligionListRequestPacket(deityFilter);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent religion list request with filter: {deityFilter}");
    }

    /// <summary>
    ///     Send a religion action request to the server (join, leave, kick, invite)
    /// </summary>
    public void RequestReligionAction(string action, string religionUID = "", string targetPlayerUID = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot perform religion action: client channel not initialized");
            return;
        }

        var request = new ReligionActionRequestPacket(action, religionUID, targetPlayerUID);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent religion action request: {action}");
    }

    /// <summary>
    ///     Request to create a new religion
    /// </summary>
    public void RequestCreateReligion(string religionName, string deity, bool isPublic)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot create religion: client channel not initialized");
            return;
        }

        var request = new CreateReligionRequestPacket(religionName, deity, isPublic);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent create religion request: {religionName}, {deity}");
    }

    /// <summary>
    ///     Request player's religion info (for management overlay)
    /// </summary>
    public void RequestPlayerReligionInfo()
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request religion info: client channel not initialized");
            return;
        }

        var request = new PlayerReligionInfoRequestPacket();
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug("[PantheonWars] Sent player religion info request");
    }

    /// <summary>
    ///     Request to edit religion description
    /// </summary>
    public void RequestEditDescription(string religionUID, string description)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot edit description: client channel not initialized");
            return;
        }

        var request = new EditDescriptionRequestPacket(religionUID, description);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug("[PantheonWars] Sent edit description request");
    }

    /// <summary>
    ///     Request list of all civilizations
    /// </summary>
    public void RequestCivilizationList(string deityFilter = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request civilization list: client channel not initialized");
            return;
        }

        var request = new CivilizationListRequestPacket(deityFilter);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent civilization list request with filter: '{deityFilter}'");
    }

    /// <summary>
    ///     Request detailed information about a specific civilization
    /// </summary>
    public void RequestCivilizationInfo(string civId)
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request civilization info: client channel not initialized");
            return;
        }

        var request = new CivilizationInfoRequestPacket(civId);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent civilization info request for {civId}");
    }

    /// <summary>
    ///     Request a civilization action (create, invite, accept, leave, kick, disband)
    /// </summary>
    public void RequestCivilizationAction(string action, string civId = "", string targetId = "", string name = "")
    {
        if (_clientChannel == null)
        {
            _capi?.Logger.Error("[PantheonWars] Cannot request civilization action: client channel not initialized");
            return;
        }

        var request = new CivilizationActionRequestPacket(action, civId, targetId, name);
        _clientChannel.SendPacket(request);
        _capi?.Logger.Debug($"[PantheonWars] Sent civilization action request: {action}");
    }

    /// <summary>
    ///     Event fired when player religion data is updated from the server
    /// </summary>
    public event Action<PlayerReligionDataPacket>? PlayerReligionDataUpdated;

    /// <summary>
    ///     Event fired when blessing data is received from the server
    /// </summary>
    public event Action<BlessingDataResponsePacket>? BlessingDataReceived;

    /// <summary>
    ///     Event fired when a blessing unlock response is received from the server
    ///     Parameters: (blessingId, success)
    /// </summary>
    public event Action<string, bool>? BlessingUnlocked;

    /// <summary>
    ///     Event fired when the player's religion state changes (disbanded, kicked, etc.)
    /// </summary>
    public event Action<ReligionStateChangedPacket>? ReligionStateChanged;

    /// <summary>
    ///     Event fired when religion list is received from server
    /// </summary>
    public event Action<ReligionListResponsePacket>? ReligionListReceived;

    /// <summary>
    ///     Event fired when religion action is completed (join, leave, etc.)
    /// </summary>
    public event Action<ReligionActionResponsePacket>? ReligionActionCompleted;

    /// <summary>
    ///     Event fired when player religion info is received from server
    /// </summary>
    public event Action<PlayerReligionInfoResponsePacket>? PlayerReligionInfoReceived;

    /// <summary>
    ///     Event fired when civilization list is received from server
    /// </summary>
    public event Action<CivilizationListResponsePacket>? CivilizationListReceived;

    /// <summary>
    ///     Event fired when civilization info is received from server
    /// </summary>
    public event Action<CivilizationInfoResponsePacket>? CivilizationInfoReceived;

    /// <summary>
    ///     Event fired when civilization action is completed (create, invite, accept, leave, kick, disband)
    /// </summary>
    public event Action<CivilizationActionResponsePacket>? CivilizationActionCompleted;

    #endregion
}