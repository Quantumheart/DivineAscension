using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Implementation;
using DivineAscension.Commands;
using DivineAscension.Configuration;
using DivineAscension.Data;
using DivineAscension.Services;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems.BuffSystem;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Server;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
///     Handles initialization of all DivineAscension server-side systems.
///     This class extracts the complex initialization logic from DivineAscensionModSystem.cs
///     to improve maintainability and testability.
/// </summary>
[ExcludeFromCodeCoverage]
public static class DivineAscensionSystemInitializer
{
    /// <summary>
    ///     Initialize all server-side systems in the correct order.
    ///     CRITICAL: The initialization order must be preserved exactly as specified.
    /// </summary>
    /// <param name="api">The server API</param>
    /// <param name="serverChannel">The network channel for server communications</param>
    /// <param name="gameBalanceConfig">The game balance configuration</param>
    /// <param name="modConfig">The mod configuration data</param>
    /// <returns>InitializationResult containing all initialized managers, commands, and handlers</returns>
    public static InitializationResult InitializeServerSystems(
        ICoreServerAPI api,
        IServerNetworkChannel serverChannel,
        GameBalanceConfig gameBalanceConfig,
        ModConfigData modConfig)
    {
        api.Logger.Notification("[DivineAscension] Starting server-side system initialization...");

        // Create API wrapper services
        var logger = api.Logger;
        var eventService = new ServerEventService(api.Event);
        var persistenceService = new ServerPersistenceService(api.WorldManager.SaveGame);
        var worldService = new ServerWorldService(api.World);
        var networkService = new ServerNetworkService(serverChannel);

        // Initialize localization service for server
        LocalizationService.Instance.InitializeServer(api);

        // Initialize cooldown manager (early to prevent griefing attacks)
        var cooldownManager = new CooldownManager(logger, eventService, worldService, modConfig);
        cooldownManager.Initialize();

        // Step 1: Clear any static event subscribers from previous loads
        PitKilnPatches.ClearSubscribers();
        AnvilPatches.ClearSubscribers();
        CookingPatches.ClearSubscribers();
        EatingPatches.ClearSubscribers();
        CropPlantingPatches.ClearSubscribers();
        ForagingPatches.ClearSubscribers();
        BlockCropPatches.ClearSubscribers();
        FlowerPatches.ClearSubscribers();
        MushroomPatches.ClearSubscribers();
        SkinningPatches.ClearSubscribers();

        api.RegisterEntityBehaviorClass("DivineAscensionBuffTracker", typeof(EntityBehaviorBuffTracker));

        var religionManager = new ReligionManager(logger, eventService, persistenceService, worldService);
        religionManager.Initialize();

        // Migrate existing religions with empty deity names (for backward compatibility)
        var migratedReligionUIDs = religionManager.MigrateEmptyDeityNames();

        var activityLogManager = new ActivityLogManager(logger, worldService, religionManager);
        activityLogManager.Initialize();

        var civilizationManager =
            new CivilizationManager(logger, eventService, persistenceService, worldService, religionManager);
        civilizationManager.Initialize();

        // Create messenger service after managers are initialized
        var messengerService = new PlayerMessengerService(worldService, religionManager, civilizationManager);

        var playerReligionDataManager = new PlayerProgressionDataManager(logger, eventService, persistenceService,
            worldService, religionManager, gameBalanceConfig);
        playerReligionDataManager.Initialize();

        // CRITICAL: MUST be initialized before FavorSystem
        var religionPrestigeManager =
            new ReligionPrestigeManager(logger, worldService, religionManager, gameBalanceConfig);
        religionPrestigeManager.Initialize();

        var favorSystem = new FavorSystem(
            logger,
            eventService,
            worldService,
            playerReligionDataManager,
            religionManager,
            religionPrestigeManager,
            activityLogManager,
            gameBalanceConfig);
        favorSystem.Initialize();

        var diplomacyManager = new DiplomacyManager(logger, eventService, persistenceService, civilizationManager,
            religionPrestigeManager, religionManager, cooldownManager);
        diplomacyManager.Initialize();

        var pvpManager = new PvPManager(logger, eventService, worldService, playerReligionDataManager, religionManager,
            religionPrestigeManager,
            civilizationManager, diplomacyManager, gameBalanceConfig);
        pvpManager.Initialize();

        // Create blessing loader for JSON-based blessing definitions
        IBlessingLoader blessingLoader = new BlessingLoader(api);
        var blessingRegistry = new BlessingRegistry(api, blessingLoader);
        blessingRegistry.Initialize();

        var blessingEffectSystem =
            new BlessingEffectSystem(
                logger,
                eventService,
                worldService,
                blessingRegistry,
                playerReligionDataManager,
                religionManager);
        blessingEffectSystem.Initialize();

        // CRITICAL: Must be called AFTER BlessingEffectSystem is initialized
        religionPrestigeManager.SetBlessingSystems(blessingRegistry, blessingEffectSystem);

        // CRITICAL: Must be called AFTER DiplomacyManager is initialized
        religionPrestigeManager.SetDiplomacyManager(diplomacyManager, civilizationManager);

        var favorCommands = new FavorCommands(api, playerReligionDataManager, religionManager, messengerService);
        favorCommands.RegisterCommands();

        var blessingCommands = new BlessingCommands(api, blessingRegistry, playerReligionDataManager, religionManager,
            blessingEffectSystem, networkService, messengerService);
        blessingCommands.RegisterCommands();

        var roleManager = new RoleManager(religionManager);

        var religionCommands = new ReligionCommands(api, religionManager, playerReligionDataManager,
            religionPrestigeManager, networkService, roleManager, cooldownManager, messengerService, worldService, logger);
        religionCommands.RegisterCommands();

        var roleCommands = new RoleCommands(api, roleManager, religionManager, playerReligionDataManager, messengerService);
        roleCommands.RegisterCommands();

        var civilizationCommands =
            new CivilizationCommands(api, civilizationManager, religionManager, playerReligionDataManager,
                cooldownManager, messengerService, worldService, logger);
        civilizationCommands.RegisterCommands();

        // Create and initialize network handlers
        var playerDataHandler = new PlayerDataNetworkHandler(
            logger,
            worldService,
            eventService,
            networkService,
            playerReligionDataManager,
            religionManager,
            gameBalanceConfig);
        playerDataHandler.RegisterHandlers();

        var blessingHandler = new BlessingNetworkHandler(
            logger,
            blessingRegistry,
            blessingEffectSystem,
            playerReligionDataManager,
            religionManager,
            networkService,
            messengerService,
            worldService);
        blessingHandler.RegisterHandlers();

        var religionHandler = new ReligionNetworkHandler(
            logger,
            religionManager,
            playerReligionDataManager,
            roleManager,
            networkService,
            messengerService,
            cooldownManager,
            worldService);
        religionHandler.RegisterHandlers();

        var civilizationHandler = new CivilizationNetworkHandler(
            logger,
            api,
            civilizationManager,
            religionManager,
            networkService,
            cooldownManager);
        civilizationHandler.RegisterHandlers();

        var diplomacyHandler = new DiplomacyNetworkHandler(
            logger,
            diplomacyManager,
            civilizationManager,
            religionManager,
            playerReligionDataManager,
            networkService,
            messengerService,
            worldService);
        diplomacyHandler.RegisterHandlers();

        var activityHandler = new ActivityNetworkHandler(
            logger,
            activityLogManager,
            religionManager,
            networkService);
        activityHandler.RegisterHandlers();

        // Validate all memberships after initialization
        api.Logger.Notification("[DivineAscension] Running membership validation...");
        var (total, consistent, repaired, failed) =
            religionManager.ValidateAllMemberships();

        if (failed > 0)
        {
            api.Logger.Warning(
                $"[DivineAscension] Membership validation completed with {failed} failed repair(s). " +
                "Manual intervention may be required.");
        }
        else if (repaired > 0)
        {
            api.Logger.Notification(
                $"[DivineAscension] Membership validation completed successfully. " +
                $"Automatically repaired {repaired} inconsistenc{(repaired == 1 ? "y" : "ies")}.");
        }
        else
        {
            api.Logger.Notification(
                $"[DivineAscension] Membership validation completed. All {total} player membership(s) are consistent.");
        }

        api.Logger.Notification("[DivineAscension] All server-side systems initialized successfully");

        // Return all initialized components
        return new InitializationResult
        {
            CooldownManager = cooldownManager,
            ReligionManager = religionManager,
            CivilizationManager = civilizationManager,
            PlayerProgressionDataManager = playerReligionDataManager,
            ReligionPrestigeManager = religionPrestigeManager,
            FavorSystem = favorSystem,
            ActivityLogManager = activityLogManager,
            PvPManager = pvpManager,
            DiplomacyManager = diplomacyManager,
            BlessingRegistry = blessingRegistry,
            BlessingEffectSystem = blessingEffectSystem,
            RoleManager = roleManager,
            FavorCommands = favorCommands,
            BlessingCommands = blessingCommands,
            ReligionCommands = religionCommands,
            RoleCommands = roleCommands,
            CivilizationCommands = civilizationCommands,
            PlayerDataNetworkHandler = playerDataHandler,
            BlessingNetworkHandler = blessingHandler,
            ReligionNetworkHandler = religionHandler,
            CivilizationNetworkHandler = civilizationHandler,
            DiplomacyNetworkHandler = diplomacyHandler,
            ActivityNetworkHandler = activityHandler,
            MigratedReligionUIDs = migratedReligionUIDs
        };
    }
}

/// <summary>
///     Container for all initialized server-side systems, commands, and handlers.
/// </summary>
[ExcludeFromCodeCoverage]
public class InitializationResult
{
    // 13 Managers
    public ICooldownManager CooldownManager { get; init; } = null!;
    public ReligionManager ReligionManager { get; init; } = null!;
    public CivilizationManager CivilizationManager { get; init; } = null!;
    public PlayerProgressionDataManager PlayerProgressionDataManager { get; init; } = null!;
    public ReligionPrestigeManager ReligionPrestigeManager { get; init; } = null!;
    public FavorSystem FavorSystem { get; init; } = null!;
    public ActivityLogManager ActivityLogManager { get; init; } = null!;
    public PvPManager PvPManager { get; init; } = null!;
    public DiplomacyManager DiplomacyManager { get; init; } = null!;
    public BlessingRegistry BlessingRegistry { get; init; } = null!;
    public BlessingEffectSystem BlessingEffectSystem { get; init; } = null!;
    public RoleManager RoleManager { get; init; } = null!;

    // 5 Commands
    public FavorCommands FavorCommands { get; init; } = null!;
    public BlessingCommands BlessingCommands { get; init; } = null!;
    public ReligionCommands ReligionCommands { get; init; } = null!;
    public RoleCommands RoleCommands { get; init; } = null!;
    public CivilizationCommands CivilizationCommands { get; init; } = null!;

    // Network Handlers
    public PlayerDataNetworkHandler PlayerDataNetworkHandler { get; init; } = null!;
    public BlessingNetworkHandler BlessingNetworkHandler { get; init; } = null!;
    public ReligionNetworkHandler ReligionNetworkHandler { get; init; } = null!;
    public CivilizationNetworkHandler CivilizationNetworkHandler { get; init; } = null!;
    public DiplomacyNetworkHandler DiplomacyNetworkHandler { get; init; } = null!;
    public ActivityNetworkHandler ActivityNetworkHandler { get; init; } = null!;

    /// <summary>
    ///     Set of religion UIDs that were migrated with auto-generated deity names.
    ///     Used to notify founders on first login after migration.
    /// </summary>
    public HashSet<string> MigratedReligionUIDs { get; init; } = new();
}