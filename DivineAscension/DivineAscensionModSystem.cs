using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using DivineAscension.Blocks;
using DivineAscension.Collectible;
using DivineAscension.Commands;
using DivineAscension.Configuration;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.GUI.State;
using DivineAscension.Network;
using DivineAscension.Network.Caravan;
using DivineAscension.Network.Civilization;
using DivineAscension.Network.Diplomacy;
using DivineAscension.Network.HolySite;
using DivineAscension.Services;
using DivineAscension.Systems;
using DivineAscension.Systems.Caravan;
using DivineAscension.Systems.Altar;
using DivineAscension.Systems.Lectern;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Client;
using DivineAscension.Systems.Networking.Server;
using DivineAscension.Systems.Patches;
using DivineAscension.Utilities;
using HarmonyLib;
using JetBrains.Annotations;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace DivineAscension;

[ExcludeFromCodeCoverage]
[UsedImplicitly]
public class DivineAscensionModSystem : ModSystem
{
    public const string NETWORK_CHANNEL = "divineascension";
    private const string CONFIG_DATA_KEY = "DivineAscension.ModConfig";

    private AltarDestructionHandler? _altarDestructionHandler;
    private AltarEventEmitter? _altarEventEmitter;
    private AltarPlacementHandler? _altarPlacementHandler;
    private AltarPrayerHandler? _altarPrayerHandler;
    private CaravanShrinePlacementHandler? _caravanShrinePlacementHandler;
    private CaravanShrineDestructionHandler? _caravanShrineDestructionHandler;
    private CaravanTradeSessionManager? _caravanTradeSessionManager;
    private LecternEventEmitter? _lecternEventEmitter;
    private LecternInteractionHandler? _lecternInteractionHandler;
    private BlessingNetworkHandler? _blessingNetworkHandler;
    private CivilizationManager? _civilizationManager;
    private CivilizationNetworkHandler? _civilizationNetworkHandler;
    private ModConfigData _configData = new();
    private ICooldownManager? _cooldownManager;
    private DiplomacyNetworkHandler? _diplomacyNetworkHandler;

    // Client-side systems
    private FavorSystem? _favorSystem;
    private GameBalanceConfig _gameBalanceConfig = new();

    private Harmony? _harmony;
    private IHolySiteManager? _holySiteManager;
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

    /// <summary>
    ///     Gets the game balance configuration (Tier 1 settings).
    /// </summary>
    public GameBalanceConfig GameBalanceConfig => _gameBalanceConfig;

    public string ModName => "divineascension";

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        // Initialize logging service FIRST (before any logging occurs). Starts fully enabled for
        // early bootstrap; the admin's GameBalanceConfig logging toggles are applied below once
        // ConfigLib has loaded the config.
        var loggingConfig = LoggingConfig.Default();
        LoggingService.Instance.Initialize(api.Logger, loggingConfig);

        // ModSystem startup intentionally logs through the raw api.Logger: these lines run
        // during Start/StartServerSide bootstrap, where the per-category ILoggerWrapper
        // instances aren't created yet. Everything past initialization uses ILoggerWrapper.
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

        // Register BlockBehavior classes
        // Required for JSON patching and client-server serialization
        api.RegisterBlockBehaviorClass("DivineAscensionAltar", typeof(BlockBehaviorAltar));
        api.RegisterBlockBehaviorClass("DivineAscensionCaravanShrine", typeof(BlockBehaviorCaravanShrine));
        api.RegisterBlockBehaviorClass("DivineAscensionLectern", typeof(BlockBehaviorLectern));
        api.RegisterBlockBehaviorClass("DivineAscensionStone", typeof(BlockBehaviorStone));
        api.RegisterBlockBehaviorClass("DivineAscensionOre", typeof(BlockBehaviorOre));
        api.RegisterBlockBehaviorClass("DivineAscensionBlessedCrop", typeof(BlockBehaviorBlessedCrop));
        api.RegisterCollectibleBehaviorClass("ChiselTracking", typeof(CollectibleBehaviorChiselTracking));
        api.Logger.Notification("[DivineAscension] Block and Collectible behavior classes registered");

        // Register with ConfigLib if available
        TryRegisterWithConfigLib(api);

        // Clamp blessing-slot fields in place first so out-of-range slot values are repaired
        // (and logged per field) instead of tripping Validate() and discarding the whole config (#616).
        foreach (var adjustment in _gameBalanceConfig.ClampBlessingSlots())
        {
            api.Logger.Warning($"[DivineAscension] Blessing slot config adjusted: {adjustment}");
        }

        // Validate config regardless of ConfigLib presence
        try
        {
            _gameBalanceConfig.Validate();
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[DivineAscension] Config validation failed: {ex.Message}. Using defaults.");
            // Reset values IN PLACE — never replace the instance, ConfigLib holds a reference to it
            // and writes GUI changes into it. Swapping it strands every later config change.
            _gameBalanceConfig.CopyFrom(new GameBalanceConfig());
        }

        // Apply the admin-selected logging levels now that ConfigLib (if present) has populated
        // the config. Early bootstrap above logs at full verbosity since the toggles aren't known yet.
        LoggingService.Instance.ApplyConfig(_gameBalanceConfig.BuildLoggingConfig());
        api.Logger.Notification(
            $"[DivineAscension] Logging levels — Debug:{_gameBalanceConfig.EnableDebugLogs} " +
            $"Notification:{_gameBalanceConfig.EnableNotificationLogs} Warning:{_gameBalanceConfig.EnableWarningLogs} " +
            $"Error:{_gameBalanceConfig.EnableErrorLogs}");

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
            .RegisterMessageType<EditMottoRequestPacket>()
            .RegisterMessageType<EditMottoResponsePacket>()
            .RegisterMessageType<EditFoundingMythRequestPacket>()
            .RegisterMessageType<EditFoundingMythResponsePacket>()
            .RegisterMessageType<AddFeastDayRequestPacket>()
            .RegisterMessageType<AddFeastDayResponsePacket>()
            .RegisterMessageType<RemoveFeastDayRequestPacket>()
            .RegisterMessageType<RemoveFeastDayResponsePacket>()
            .RegisterMessageType<HolidayKeptToastPacket>()
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
            .RegisterMessageType<UnlearnBlessingRequestPacket>()
            .RegisterMessageType<UnlearnBlessingResponsePacket>()
            .RegisterMessageType<UnlearnReligionBlessingRequestPacket>()
            .RegisterMessageType<UnlearnReligionBlessingResponsePacket>()
            .RegisterMessageType<BlessingDataRequestPacket>()
            .RegisterMessageType<BlessingDataResponsePacket>()
            .RegisterMessageType<ReligionStateChangedPacket>()
            .RegisterMessageType<CivilizationListRequestPacket>()
            .RegisterMessageType<CivilizationListResponsePacket>()
            .RegisterMessageType<CivilizationInfoRequestPacket>()
            .RegisterMessageType<CivilizationInfoResponsePacket>()
            .RegisterMessageType<CivilizationActionRequestPacket>()
            .RegisterMessageType<CivilizationActionResponsePacket>()
            .RegisterMessageType<LeaderboardRequestPacket>()
            .RegisterMessageType<LeaderboardResponsePacket>()
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
            .RegisterMessageType<ActivityLogResponsePacket>()
            .RegisterMessageType<AvailableDomainsRequestPacket>()
            .RegisterMessageType<AvailableDomainsResponsePacket>()
            .RegisterMessageType<HolySiteRequestPacket>()
            .RegisterMessageType<HolySiteResponsePacket>()
            .RegisterMessageType<HolySiteUpdateRequestPacket>()
            .RegisterMessageType<HolySiteUpdateResponsePacket>()
            .RegisterMessageType<RitualRequestPacket>()
            .RegisterMessageType<RitualResponsePacket>()
            .RegisterMessageType<MilestoneProgressRequestPacket>()
            .RegisterMessageType<MilestoneProgressResponsePacket>()
            .RegisterMessageType<MilestoneUnlockedPacket>()
            .RegisterMessageType<OpenMenuPacket>()
            .RegisterMessageType<CloseMenuPacket>()
            .RegisterMessageType<OpenTradeRequestPacket>()
            .RegisterMessageType<JoinTradeRequestPacket>()
            .RegisterMessageType<OfferUpdatePacket>()
            .RegisterMessageType<SetReadyPacket>()
            .RegisterMessageType<CancelTradePacket>()
            .RegisterMessageType<TradeStateSyncPacket>();
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);

        // Block behaviors are now applied via JSON patches in assets/divineascension/patches/
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        base.StartServerSide(api);
        _sapi = api;

        // Initialize thread safety utilities for telemetry
        ThreadSafetyUtils.Initialize(api);

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
        var result =
            DivineAscensionSystemInitializer.InitializeServerSystems(api, _serverChannel, _gameBalanceConfig,
                _configData);

        // Store references to managers for disposal and event subscriptions
        _cooldownManager = result.CooldownManager;
        _religionManager = result.ReligionManager;
        _playerReligionDataManager = result.PlayerProgressionDataManager;
        _favorSystem = result.FavorSystem;
        _holySiteManager = result.HolySiteManager;
        _civilizationManager = result.CivilizationManager;
        _altarPlacementHandler = result.AltarPlacementHandler;
        _altarDestructionHandler = result.AltarDestructionHandler;
        _caravanShrinePlacementHandler = result.CaravanShrinePlacementHandler;
        _caravanShrineDestructionHandler = result.CaravanShrineDestructionHandler;
        _caravanTradeSessionManager = result.CaravanTradeSessionManager;
        _altarPrayerHandler = result.AltarPrayerHandler;
        _altarEventEmitter = result.AltarEventEmitter;
        _lecternEventEmitter = result.LecternEventEmitter;
        _lecternInteractionHandler = result.LecternInteractionHandler;
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
            () => _configData.ProfanityFilterEnabled,
            _configData,
            SaveModConfig);
        configCommands.RegisterCommands();

        api.Logger.Notification("[DivineAscension] Server-side initialization complete");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        api.Logger.Notification("[DivineAscension] Initializing client-side systems...");

        // Initialize thread safety utilities for telemetry
        ThreadSafetyUtils.Initialize(api);

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
        _cooldownManager?.Dispose();
        _favorSystem?.Dispose();
        _holySiteManager?.Dispose();
        _altarPlacementHandler?.Dispose();
        _altarDestructionHandler?.Dispose();
        _caravanShrinePlacementHandler?.Dispose();
        _caravanShrineDestructionHandler?.Dispose();
        _caravanTradeSessionManager?.Dispose();
        _altarPrayerHandler?.Dispose();
        _lecternInteractionHandler?.Dispose();
        _playerReligionDataManager?.Dispose();
        _religionManager?.Dispose();
        _civilizationManager?.Dispose();

        // Clear static events
        _altarEventEmitter?.ClearSubscribers();
        _lecternEventEmitter?.ClearSubscribers();
        PitKilnPatches.ClearSubscribers();
        AnvilPatches.ClearSubscribers();
        CookingPatches.ClearSubscribers();
        EatingPatches.ClearSubscribers();
        CropPlantingPatches.ClearSubscribers();
        ForagingPatches.ClearSubscribers();
        BlockCropPatches.ClearSubscribers();
        FlowerPatches.ClearSubscribers();
        MushroomPatches.ClearSubscribers();
        TraderPatches.ClearSubscribers();
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
                religion.PatronName);

            player.SendMessage(0, message, EnumChatType.Notification);

            // Remove from migrated set so notification is only sent once
            _migratedReligionUIDs.Remove(religion.ReligionUID);

            _sapi?.Logger.Debug(
                $"[DivineAscension] Sent deity name migration notice to founder {player.PlayerName} for {religion.ReligionName}");
        }
    }

    #endregion

    #region ConfigLib Integration

    /// <summary>
    /// Attempts to register the game balance configuration with ConfigLib if it's available.
    /// Uses reflection to avoid compile-time dependency on ConfigLib.
    /// Falls back gracefully to hardcoded defaults if ConfigLib is not installed or incompatible.
    /// </summary>
    private void TryRegisterWithConfigLib(ICoreAPI api)
    {
        if (!api.ModLoader.IsModEnabled("configlib"))
        {
            api.Logger.Notification(
                "[DivineAscension] ConfigLib not installed. Using hardcoded default configuration. Install ConfigLib for in-game configuration GUI.");
            return;
        }

        api.Logger.Notification("[DivineAscension] ConfigLib is enabled. Config file: ModConfig/divineascension.yaml");

        try
        {
            // Get ConfigLib mod system using reflection (ConfigLib may not be available at compile time)
            // We need to iterate through all mod systems since GetModSystem<T>() requires a type reference
            ModSystem? configLibModSystem = null;

            foreach (var modSystem in api.ModLoader.Systems)
            {
                if (modSystem.GetType().Name == "ConfigLibModSystem")
                {
                    configLibModSystem = modSystem;
                    break;
                }
            }

            if (configLibModSystem == null)
            {
                api.Logger.Warning("[DivineAscension] ConfigLib mod system not found");
                return;
            }

            // Register our config using reflection
            var registerMethod = configLibModSystem.GetType().GetMethod("RegisterCustomManagedConfig");

            if (registerMethod == null)
            {
                api.Logger.Warning(
                    "[DivineAscension] ConfigLib found but RegisterCustomManagedConfig method not available");
                return;
            }

            // Validate method signature to ensure compatibility
            var parameters = registerMethod.GetParameters();

            // Expected signature:
            // RegisterCustomManagedConfig(string domain, object configObject,
            //     string? path = null, Action? onSyncedFromServer = null,
            //     Action<string>? onSettingChanged = null, Action? onConfigSaved = null)
            bool isValidSignature = parameters.Length == 6 &&
                                    parameters[0].ParameterType == typeof(string) &&
                                    parameters[1].ParameterType == typeof(object) &&
                                    parameters[2].ParameterType == typeof(string) &&
                                    parameters[3].ParameterType == typeof(Action) &&
                                    parameters[4].ParameterType == typeof(Action<string>) &&
                                    parameters[5].ParameterType == typeof(Action);

            if (!isValidSignature)
            {
                api.Logger.Warning(
                    "[DivineAscension] ConfigLib API has changed - incompatible RegisterCustomManagedConfig signature");
                api.Logger.Warning(
                    $"[DivineAscension] Expected: (string, object, string, Action, Action<string>, Action), Got: ({string.Join(", ", parameters.Select(p => p.ParameterType.Name))})");
                return;
            }

            // Parameters: domain, configObject, path, onSyncedFromServer, onSettingChanged, onConfigSaved
            registerMethod.Invoke(configLibModSystem, new object?[]
            {
                "divineascension", // domain (string)
                _gameBalanceConfig, // configObject (object)
                null, // path (string?) - optional, use default
                (Action)OnConfigReloaded, // onSyncedFromServer - re-apply logging after a server sync
                (Action<string>)OnConfigChanged, // onSettingChanged (Action<string>?) - our callback
                (Action)OnConfigReloaded // onConfigSaved - re-apply logging after a GUI save
            });

            api.Logger.Notification(
                "[DivineAscension] ConfigLib integration enabled. Config file: ModConfig/divineascension.yaml");
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[DivineAscension] Failed to register with ConfigLib: {ex.Message}");
            api.Logger.Notification("[DivineAscension] Using hardcoded default configuration");
        }
    }

    /// <summary>
    /// Called by ConfigLib after the config is saved (GUI edit) or synced from the server.
    /// Re-applies logging levels regardless of which callback ConfigLib uses for the change.
    /// </summary>
    private void OnConfigReloaded()
    {
        LoggingService.Instance.ApplyConfig(_gameBalanceConfig.BuildLoggingConfig());
    }

    /// <summary>
    /// Called by ConfigLib when configuration settings change at runtime.
    /// </summary>
    private void OnConfigChanged(string settingCode)
    {
        // Re-apply logging levels FIRST and unconditionally. Logging is independent of balance
        // validation, so an invalid balance value (e.g. out-of-order thresholds changed in the same
        // save) must not throw past this and leave the logs un-silenced. ApplyConfig mutates the
        // shared LoggingConfig in place, so loggers already handed out pick up the change live.
        LoggingService.Instance.ApplyConfig(_gameBalanceConfig.BuildLoggingConfig());

        try
        {
            foreach (var adjustment in _gameBalanceConfig.ClampBlessingSlots())
            {
                _sapi?.Logger.Warning($"[DivineAscension] Blessing slot config adjusted: {adjustment}");
            }

            _gameBalanceConfig.Validate();
            _sapi?.Logger.Notification($"[DivineAscension] Configuration updated: {settingCode}");

            // Future: Notify systems to reload their cached values
            // For now, most systems will read config values on-the-fly
        }
        catch (Exception ex)
        {
            _sapi?.Logger.Error($"[DivineAscension] Config validation failed after update: {ex.Message}");
        }
    }

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

    /// <summary>
    ///     Persists the user's chosen dialog window size and last-visited codex page
    ///     into <c>UiPrefs</c> and writes the mod config. Window size is only updated
    ///     when valid (&gt; 0); the nav id is always persisted so the codex reopens on
    ///     the last page (#474). Safe to call on the client; <c>SaveModConfig</c>
    ///     silently no-ops if no server save-game is available.
    /// </summary>
    public void SaveUiPrefs(int width, int height, SidebarNavId lastNavId)
    {
        _configData.UiPrefs.LastNavId = lastNavId;
        if (width > 0 && height > 0)
        {
            _configData.UiPrefs.WindowWidth = width;
            _configData.UiPrefs.WindowHeight = height;
        }

        SaveModConfig();
    }

    #endregion
}