using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DivineAscension.Network;
using DivineAscension.Network.Civilization;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Client;
using DivineAscension.Systems.Networking.Server;
using DivineAscension.Systems.Patches;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension;

[ExcludeFromCodeCoverage]
public class DivineAscensionModSystem : ModSystem
{
    public const string NETWORK_CHANNEL = "pantheonwars";
    private BlessingNetworkHandler? _blessingNetworkHandler;
    private CivilizationManager? _civilizationManager;
    private CivilizationNetworkHandler? _civilizationNetworkHandler;

    // Client-side systems
    private FavorSystem? _favorSystem;

    private Harmony? _harmony;
    private PlayerDataNetworkHandler? _playerDataNetworkHandler;
    private PlayerReligionDataManager? _playerReligionDataManager;
    private ReligionManager? _religionManager;
    private ReligionNetworkHandler? _religionNetworkHandler;

    // Server-side systems
    private IServerNetworkChannel? _serverChannel;

    // Public network client for UI dialogs
    public PantheonWarsNetworkClient? NetworkClient { get; private set; }
    public IUiService UiService { get; private set; } = null!;

    public string ModName => "pantheonwars";

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.Logger.Notification("[DivineAscension] Mod loaded!");

        // Register Harmony Patches
        if (_harmony == null)
        {
            _harmony = new Harmony("com.pantheonwars.patches");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
            api.Logger.Notification("[DivineAscension] Harmony patches registered.");
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
            .RegisterMessageType<ReligionRolesRequest>()
            .RegisterMessageType<ReligionRolesResponse>()
            .RegisterMessageType<CreateRoleRequest>()
            .RegisterMessageType<CreateRoleResponse>()
            .RegisterMessageType<ModifyRolePermissionsRequest>()
            .RegisterMessageType<ModifyRolePermissionsResponse>()
            .RegisterMessageType<AssignRoleRequest>()
            .RegisterMessageType<AssignRoleResponse>()
            .RegisterMessageType<DeleteRoleRequest>()
            .RegisterMessageType<DeleteRoleResponse>()
            .RegisterMessageType<TransferFounderRequest>()
            .RegisterMessageType<TransferFounderResponse>()
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
        _civilizationManager = result.CivilizationManager;
        _playerDataNetworkHandler = result.PlayerDataNetworkHandler;
        _blessingNetworkHandler = result.BlessingNetworkHandler;
        _religionNetworkHandler = result.ReligionNetworkHandler;
        _civilizationNetworkHandler = result.CivilizationNetworkHandler;

        api.Logger.Notification("[DivineAscension] Server-side initialization complete");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        api.Logger.Notification("[DivineAscension] Initializing client-side systems...");

        // Setup network client
        var clientChannel = api.Network.GetChannel(NETWORK_CHANNEL);
        NetworkClient = new PantheonWarsNetworkClient();
        NetworkClient.Initialize(api);
        NetworkClient.RegisterHandlers(clientChannel);
        UiService = new UiService(NetworkClient);

        api.Logger.Notification("[DivineAscension] Client-side initialization complete");
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

        // Cleanup network client
        NetworkClient?.Dispose();

        // Cleanup systems
        _favorSystem?.Dispose();
        _playerReligionDataManager?.Dispose();
        _religionManager?.Dispose();
        _civilizationManager?.Dispose();

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
}