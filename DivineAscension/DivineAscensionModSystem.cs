using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DivineAscension.Commands;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Network;
using DivineAscension.Network.Civilization;
using DivineAscension.Network.Diplomacy;
using DivineAscension.Services;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Client;
using DivineAscension.Systems.Networking.Server;
using DivineAscension.Systems.Patches;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace DivineAscension;

[ExcludeFromCodeCoverage]
public class DivineAscensionModSystem : ModSystem
{
    public const string NETWORK_CHANNEL = "divineascension";
    private const string CONFIG_DATA_KEY = "DivineAscension.ModConfig";

    private BlessingNetworkHandler? _blessingNetworkHandler;
    private CivilizationManager? _civilizationManager;
    private CivilizationNetworkHandler? _civilizationNetworkHandler;
    private ModConfigData _configData = new();
    private DiplomacyNetworkHandler? _diplomacyNetworkHandler;

    // Client-side systems
    private FavorSystem? _favorSystem;

    private Harmony? _harmony;
    private HashSet<string> _migratedReligionUIDs = new();
    private PlayerDataNetworkHandler? _playerDataNetworkHandler;
    private PlayerProgressionDataManager? _playerReligionDataManager;
    private ReligionManager? _religionManager;
    private ReligionNetworkHandler? _religionNetworkHandler;
    private ICoreServerAPI? _sapi;

    // Server-side systems
    private IServerNetworkChannel? _serverChannel;

    // Public network client for UI dialogs
    public DivineAscensionNetworkClient? NetworkClient { get; private set; }
    public IUiService UiService { get; private set; } = null!;

    /// <summary>
    ///     Gets the mod configuration data (server-side only).
    /// </summary>
    public ModConfigData Config => _configData;

    public string ModName => "divineascension";

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.Logger.Notification("[DivineAscension] Mod loaded!");

        // Initialize profanity filter service
        ProfanityFilterService.Instance.Initialize(api);

        // Register Harmony Patches
        if (_harmony == null)
        {
            _harmony = new Harmony("com.divineascension.patches");
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
            .RegisterMessageType<CivilizationActionResponsePacket>()
            .RegisterMessageType<DiplomacyInfoRequestPacket>()
            .RegisterMessageType<DiplomacyInfoResponsePacket>()
            .RegisterMessageType<DiplomacyActionRequestPacket>()
            .RegisterMessageType<DiplomacyActionResponsePacket>()
            .RegisterMessageType<WarDeclarationPacket>()
            .RegisterMessageType<ReligionDetailRequestPacket>()
            .RegisterMessageType<ReligionDetailResponsePacket>()
            .RegisterMessageType<SetDeityNameRequestPacket>()
            .RegisterMessageType<SetDeityNameResponsePacket>()
            .RegisterMessageType<ActivityLogRequestPacket>()
            .RegisterMessageType<ActivityLogResponsePacket>();
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        _sapi = api;

        // Subscribe to world save/load events for config persistence
        api.Event.SaveGameLoaded += OnSaveGameLoaded;
        api.Event.GameWorldSave += OnGameWorldSave;

        // Load config immediately (in case SaveGameLoaded already fired)
        LoadModConfig();

        // Setup network channel and handlers
        _serverChannel = api.Network.GetChannel(NETWORK_CHANNEL);
        _serverChannel.SetMessageHandler<PlayerReligionDataPacket>(OnServerMessageReceived);
        SetupServerNetworking(api);

        // Initialize all server systems using the initializer
        var result = DivineAscensionSystemInitializer.InitializeServerSystems(api, _serverChannel);

        // Store references to managers for disposal and event subscriptions
        _religionManager = result.ReligionManager;
        _playerReligionDataManager = result.PlayerProgressionDataManager;
        _favorSystem = result.FavorSystem;
        _civilizationManager = result.CivilizationManager;
        _playerDataNetworkHandler = result.PlayerDataNetworkHandler;
        _blessingNetworkHandler = result.BlessingNetworkHandler;
        _religionNetworkHandler = result.ReligionNetworkHandler;
        _civilizationNetworkHandler = result.CivilizationNetworkHandler;
        _diplomacyNetworkHandler = result.DiplomacyNetworkHandler;
        _migratedReligionUIDs = result.MigratedReligionUIDs;

        // Register player join handler for migration notifications
        if (_migratedReligionUIDs.Count > 0)
        {
            api.Event.PlayerJoin += OnPlayerJoinMigrationNotify;
        }

        // Register config commands (outside initializer since it needs mod system callbacks)
        var configCommands = new ConfigCommands(
            api,
            SetProfanityFilterEnabled,
            () => _configData.ProfanityFilterEnabled);
        configCommands.RegisterCommands();

        api.Logger.Notification("[DivineAscension] Server-side initialization complete");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        api.Logger.Notification("[DivineAscension] Initializing client-side systems...");

        // Initialize localization service
        LocalizationService.Instance.InitializeClient(api);

        // Initialize clipboard service (before ImGui initialization)
        ClipboardService.Instance.Initialize(api);

        // Setup network client
        var clientChannel = api.Network.GetChannel(NETWORK_CHANNEL);
        NetworkClient = new DivineAscensionNetworkClient();
        NetworkClient.Initialize(api);
        NetworkClient.RegisterHandlers(clientChannel);
        UiService = new UiService(NetworkClient);

        api.Logger.Notification("[DivineAscension] Client-side initialization complete");
    }

    public override void Dispose()
    {
        base.Dispose();

        // Unpatch Harmony
        _harmony?.UnpatchAll("com.divineascension.patches");

        // Unsubscribe from events
        if (_sapi != null)
        {
            _sapi.Event.PlayerJoin -= OnPlayerJoinMigrationNotify;
            _sapi.Event.SaveGameLoaded -= OnSaveGameLoaded;
            _sapi.Event.GameWorldSave -= OnGameWorldSave;
        }

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
        AnvilPatches.ClearSubscribers();
        CookingPatches.ClearSubscribers();
        EatingPatches.ClearSubscribers();
        CropPlantingPatches.ClearSubscribers();
        ForagingPatches.ClearSubscribers();
        BlockCropPatches.ClearSubscribers();
        FlowerPatches.ClearSubscribers();
        MushroomPatches.ClearSubscribers();
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

    /// <summary>
    ///     Notifies founders of migrated religions that their deity name was auto-generated
    ///     and can be customized using /religion setdeityname
    /// </summary>
    private void OnPlayerJoinMigrationNotify(IServerPlayer player)
    {
        if (_religionManager == null || _migratedReligionUIDs.Count == 0)
            return;

        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
            return;

        // Check if player is founder of a migrated religion
        if (religion.IsFounder(player.PlayerUID) && _migratedReligionUIDs.Contains(religion.ReligionUID))
        {
            // Send notification to founder
            var message = LocalizationService.Instance.Get(
                LocalizationKeys.MIGRATION_DEITY_NAME_NOTICE,
                religion.DeityName);

            player.SendMessage(0, message, EnumChatType.Notification);

            // Remove from migrated set so notification is only sent once
            _migratedReligionUIDs.Remove(religion.ReligionUID);

            _sapi?.Logger.Debug(
                $"[DivineAscension] Sent deity name migration notice to founder {player.PlayerName} for {religion.ReligionName}");
        }
    }

    #endregion

    #region Configuration Persistence

    private void OnSaveGameLoaded()
    {
        LoadModConfig();
    }

    private void OnGameWorldSave()
    {
        SaveModConfig();
    }

    private void LoadModConfig()
    {
        try
        {
            var data = _sapi?.WorldManager.SaveGame.GetData(CONFIG_DATA_KEY);
            if (data != null)
            {
                _configData = SerializerUtil.Deserialize<ModConfigData>(data) ?? new ModConfigData();
            }
            else
            {
                _configData = new ModConfigData();
            }

            // Apply config to services
            ProfanityFilterService.Instance.SetEnabled(_configData.ProfanityFilterEnabled);

            _sapi?.Logger.Debug(
                $"[DivineAscension] Loaded mod config (ProfanityFilter: {_configData.ProfanityFilterEnabled})");
        }
        catch (Exception ex)
        {
            _sapi?.Logger.Error($"[DivineAscension] Error loading mod config: {ex.Message}");
            _configData = new ModConfigData();
        }
    }

    private void SaveModConfig()
    {
        try
        {
            var data = SerializerUtil.Serialize(_configData);
            _sapi?.WorldManager.SaveGame.StoreData(CONFIG_DATA_KEY, data);
            _sapi?.Logger.Debug("[DivineAscension] Saved mod config");
        }
        catch (Exception ex)
        {
            _sapi?.Logger.Error($"[DivineAscension] Error saving mod config: {ex.Message}");
        }
    }

    /// <summary>
    ///     Sets the profanity filter enabled state and saves the config.
    /// </summary>
    public void SetProfanityFilterEnabled(bool enabled)
    {
        _configData.ProfanityFilterEnabled = enabled;
        ProfanityFilterService.Instance.SetEnabled(enabled);
        SaveModConfig();
    }

    #endregion
}