using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Implementation;
using DivineAscension.Blocks;
using DivineAscension.Collectible;
using DivineAscension.Commands;
using DivineAscension.Configuration;
using DivineAscension.Data;
using DivineAscension.Services;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems.Altar;
using DivineAscension.Systems.Altar.Pipeline;
using DivineAscension.Systems.Altar.Pipeline.Steps;
using DivineAscension.Systems.BuffSystem;
using DivineAscension.Systems.HolySite;
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
        var commandService = new ServerChatCommandService(api.ChatCommands);
        var timeService = new ServerTimeService(api.World);

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
        BlockBehaviorStone.ClearSubscribers();
        CollectibleBehaviorChiselTracking.ClearSubscribers();

        api.RegisterEntityBehaviorClass("DivineAscensionBuffTracker", typeof(EntityBehaviorBuffTracker));

        var religionManager = new ReligionManager(logger, eventService, persistenceService, worldService);
        religionManager.Initialize();

        // Migrate existing religions with empty deity names (for backward compatibility)
        var migratedReligionUIDs = religionManager.MigrateEmptyDeityNames();

        var activityLogManager = new ActivityLogManager(LoggingService.Instance.CreateLogger("ActivityLogManager")
            , worldService, religionManager);
        activityLogManager.Initialize();

        var civilizationManager =
            new CivilizationManager(LoggingService.Instance.CreateLogger("CivilizationManager"),
                eventService, persistenceService, worldService, religionManager);
        civilizationManager.Initialize();

        // Create messenger service after managers are initialized
        var messengerService = new PlayerMessengerService(worldService, religionManager, civilizationManager);

        var playerReligionDataManager = new PlayerProgressionDataManager(
            LoggingService.Instance.CreateLogger("PlayerProgressionDataManager")
            , eventService, persistenceService,
            worldService, religionManager, gameBalanceConfig, timeService);
        playerReligionDataManager.Initialize();

        // CRITICAL: MUST be initialized before FavorSystem
        var religionPrestigeManager =
            new ReligionPrestigeManager(LoggingService.Instance.CreateLogger("ReligionPrestigeManager")
                , worldService, religionManager, gameBalanceConfig);
        religionPrestigeManager.Initialize();

        // Create AltarEventEmitter (service locator for BlockBehaviorAltar)
        var altarEventEmitter = new AltarEventEmitter();
        BlockBehaviorAltar.SetEventEmitter(altarEventEmitter);

        // Initialize Holy Site Manager (depends on ReligionManager)
        var holySiteManager = new HolySiteManager(
            LoggingService.Instance.CreateLogger("HolySiteManager"),
            eventService,
            persistenceService,
            worldService,
            religionManager);
        holySiteManager.Initialize();

        // Subscribe to religion deletion events for cascading cleanup
        religionManager.OnReligionDeleted += holySiteManager.HandleReligionDeleted;

        // Initialize Holy Site Area Tracker (tracks player enter/exit events for holy sites)
        var holySiteAreaTracker = new HolySiteAreaTracker(
            eventService,
            worldService,
            holySiteManager,
            LoggingService.Instance.CreateLogger("HolySiteAreaTracker"));
        holySiteAreaTracker.Initialize();

        // Initialize Altar Placement Handler (automatically creates holy sites when altars are placed)
        var altarPlacementHandler = new AltarPlacementHandler(
            LoggingService.Instance.CreateLogger("AltarPlacementHandler"),
            holySiteManager,
            religionManager,
            worldService,
            messengerService,
            altarEventEmitter);
        altarPlacementHandler.Initialize();

        // Initialize Altar Destruction Handler (automatically deconsecrates holy sites when altars are destroyed)
        var altarDestructionHandler = new AltarDestructionHandler(
            LoggingService.Instance.CreateLogger("AltarDestructionHandler"),
            holySiteManager,
            messengerService,
            altarEventEmitter);
        altarDestructionHandler.Initialize();

        // NOTE: AltarPrayerHandler initialized after FavorSystem (needs IFavorSystem and IActivityLogManager)

        var favorSystem = new FavorSystem(
            LoggingService.Instance.CreateLogger("FavorSystem"),
            eventService,
            worldService,
            playerReligionDataManager,
            religionManager,
            religionPrestigeManager,
            activityLogManager,
            gameBalanceConfig,
            messengerService,
            timeService);

        // Set patrol dependencies before initialization
        favorSystem.SetPatrolDependencies(holySiteAreaTracker, civilizationManager, holySiteManager);
        favorSystem.Initialize();

        // Create offering loader for JSON-based offering definitions (must be before AltarPrayerHandler)
        IOfferingLoader offeringLoader = new OfferingLoader(LoggingService.Instance.CreateLogger("OfferingLoader")
            , api.Assets);
        offeringLoader.LoadOfferings();

        // Create ritual loader for JSON-based ritual definitions (must be before RitualProgressManager)
        IRitualLoader ritualLoader = new RitualLoader(LoggingService.Instance.CreateLogger("RitualLoader")
            , api.Assets);
        ritualLoader.LoadRituals();

        // Create milestone definition loader for civilization progression milestones
        IMilestoneDefinitionLoader milestoneLoader = new MilestoneDefinitionLoader(
            LoggingService.Instance.CreateLogger("MilestoneLoader"),
            api.Assets);
        milestoneLoader.LoadMilestones();

        // Initialize Buff Manager (must be before AltarPrayerHandler)
        var buffManager = new BuffManager(logger, worldService);

        // Create progression service facade (encapsulates favor, prestige, and activity logging)
        IPlayerProgressionService progressionService = new PlayerProgressionService(
            favorSystem,
            religionPrestigeManager,
            activityLogManager);

        // Initialize Ritual Progress Manager (handles ritual tracking for holy site tier upgrades)
        var ritualProgressManager = new RitualProgressManager(
            LoggingService.Instance.CreateLogger("RitualProgressManager"),
            ritualLoader,
            holySiteManager,
            religionManager);

        // Initialize Civilization Milestone Manager (handles civilization progression milestones)
        var civilizationMilestoneManager = new CivilizationMilestoneManager(
            LoggingService.Instance.CreateLogger("CivilizationMilestoneManager"),
            civilizationManager,
            religionManager,
            holySiteManager,
            religionPrestigeManager,
            milestoneLoader);
        civilizationMilestoneManager.Initialize();

        // Wire up ritual progress manager for milestone detection (late binding due to initialization order)
        civilizationMilestoneManager.SetRitualProgressManager(ritualProgressManager);

        // Initialize Civilization Bonus System (provides civ-wide bonuses from milestones)
        var civilizationBonusSystem = new CivilizationBonusSystem(
            LoggingService.Instance.CreateLogger("CivilizationBonusSystem"),
            civilizationManager,
            civilizationMilestoneManager,
            religionManager);

        // Wire up civilization bonus system for favor/prestige multipliers and holy site slots (late binding)
        favorSystem.SetCivilizationBonusSystem(civilizationBonusSystem);
        holySiteManager.SetCivilizationBonusSystem(civilizationBonusSystem);

        // Create offering evaluator service (encapsulates offering value calculation)
        IOfferingEvaluator offeringEvaluator = new OfferingEvaluator(offeringLoader);

        // Create prayer effects service (handles VFX/SFX for prayers)
        IPrayerEffectsService prayerEffectsService = new PrayerEffectsService(
            worldService,
            commandService,
            LoggingService.Instance.CreateLogger("PrayerEffectsService"));

        // Create ritual contribution service (handles ritual auto-discovery and contributions)
        IRitualContributionService ritualContributionService = new RitualContributionService(
            ritualProgressManager,
            ritualLoader,
            offeringEvaluator,
            progressionService,
            worldService,
            LoggingService.Instance.CreateLogger("RitualContributionService"));

        // Create prayer pipeline steps in execution order
        var prayerSteps = new IPrayerStep[]
        {
            // Validation steps (may short-circuit)
            new HolySiteValidationStep(holySiteManager),
            new ReligionValidationStep(religionManager),
            new CooldownValidationStep(playerReligionDataManager),
            // Processing steps
            new RitualContributionStep(ritualContributionService),
            new OfferingEvaluationStep(offeringEvaluator),
            new RewardCalculationStep(gameBalanceConfig),
            // Progression award (must be after reward calculation, skipped for ritual contributions)
            new ProgressionAwardStep(progressionService),
            // Side-effect steps (only execute on success)
            new BuffApplicationStep(buffManager),
            new CooldownUpdateStep(playerReligionDataManager),
            new OfferingConsumptionStep(),
            new PrayerEffectsStep(prayerEffectsService)
        };

        var prayerPipeline = new PrayerPipeline(prayerSteps,
            LoggingService.Instance.CreateLogger("PrayerPipeline"));

        // Initialize Altar Prayer Handler (handles prayer interactions at altars)
        var altarPrayerHandler = new AltarPrayerHandler(
            altarEventEmitter,
            prayerPipeline,
            messengerService,
            timeService,
            LoggingService.Instance.CreateLogger("AltarPrayerHandler"));
        altarPrayerHandler.Initialize();

        var diplomacyManager = new DiplomacyManager(LoggingService.Instance.CreateLogger("DiplomacyManager"),
            eventService, persistenceService, civilizationManager,
            religionPrestigeManager, religionManager, cooldownManager);
        diplomacyManager.Initialize();

        var pvpManager = new PvPManager(LoggingService.Instance.CreateLogger("PvPManager"), eventService, worldService,
            playerReligionDataManager, religionManager,
            religionPrestigeManager,
            civilizationManager, diplomacyManager, gameBalanceConfig);
        pvpManager.Initialize();

        // Wire up civilization bonus system for conquest multipliers (late binding)
        pvpManager.SetCivilizationBonusSystem(civilizationBonusSystem);

        // Wire up PvP manager for war kill milestone tracking (late binding)
        civilizationMilestoneManager.SetPvPManager(pvpManager);

        // Create blessing loader for JSON-based blessing definitions
        IBlessingLoader blessingLoader =
            new BlessingLoader(api, LoggingService.Instance.CreateLogger("BlessingLoader"));
        var blessingRegistry = new BlessingRegistry(api, blessingLoader);
        blessingRegistry.Initialize();

        var blessingEffectSystem =
            new BlessingEffectSystem(
                LoggingService.Instance.CreateLogger("RitualProgressManager"),
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
            religionPrestigeManager, networkService, roleManager, cooldownManager, messengerService, worldService,
            logger);
        religionCommands.RegisterCommands();

        var roleCommands =
            new RoleCommands(api, roleManager, religionManager, playerReligionDataManager, messengerService);
        roleCommands.RegisterCommands();

        var civilizationCommands =
            new CivilizationCommands(api, civilizationManager, religionManager, playerReligionDataManager,
                cooldownManager, messengerService, worldService, logger);
        civilizationCommands.RegisterCommands();

        var holySiteCommands = new HolySiteCommands(
            commandService,
            holySiteManager,
            religionManager);
        holySiteCommands.RegisterCommands();

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

        var holySiteHandler = new HolySiteNetworkHandler(
            logger,
            holySiteManager,
            religionManager,
            networkService,
            ritualProgressManager,
            ritualLoader);
        holySiteHandler.RegisterHandlers();

        var milestoneHandler = new MilestoneNetworkHandler(
            logger,
            civilizationMilestoneManager,
            civilizationManager,
            religionManager,
            networkService,
            worldService,
            milestoneLoader,
            blessingRegistry);
        milestoneHandler.RegisterHandlers();

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
            CivilizationMilestoneManager = civilizationMilestoneManager,
            CivilizationBonusSystem = civilizationBonusSystem,
            PlayerProgressionDataManager = playerReligionDataManager,
            ReligionPrestigeManager = religionPrestigeManager,
            HolySiteManager = holySiteManager,
            HolySiteAreaTracker = holySiteAreaTracker,
            AltarPlacementHandler = altarPlacementHandler,
            AltarDestructionHandler = altarDestructionHandler,
            AltarPrayerHandler = altarPrayerHandler,
            FavorSystem = favorSystem,
            ActivityLogManager = activityLogManager,
            PvPManager = pvpManager,
            DiplomacyManager = diplomacyManager,
            BlessingRegistry = blessingRegistry,
            BlessingEffectSystem = blessingEffectSystem,
            RoleManager = roleManager,
            AltarEventEmitter = altarEventEmitter,
            RitualProgressManager = ritualProgressManager,
            FavorCommands = favorCommands,
            BlessingCommands = blessingCommands,
            ReligionCommands = religionCommands,
            RoleCommands = roleCommands,
            CivilizationCommands = civilizationCommands,
            HolySiteCommands = holySiteCommands,
            PlayerDataNetworkHandler = playerDataHandler,
            BlessingNetworkHandler = blessingHandler,
            ReligionNetworkHandler = religionHandler,
            CivilizationNetworkHandler = civilizationHandler,
            DiplomacyNetworkHandler = diplomacyHandler,
            ActivityNetworkHandler = activityHandler,
            HolySiteNetworkHandler = holySiteHandler,
            MilestoneNetworkHandler = milestoneHandler,
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
    // 18 Managers
    public ICooldownManager CooldownManager { get; init; } = null!;
    public ReligionManager ReligionManager { get; init; } = null!;
    public CivilizationManager CivilizationManager { get; init; } = null!;
    public ICivilizationMilestoneManager CivilizationMilestoneManager { get; init; } = null!;
    public ICivilizationBonusSystem CivilizationBonusSystem { get; init; } = null!;
    public PlayerProgressionDataManager PlayerProgressionDataManager { get; init; } = null!;
    public ReligionPrestigeManager ReligionPrestigeManager { get; init; } = null!;
    public IHolySiteManager HolySiteManager { get; init; } = null!;
    public IHolySiteAreaTracker HolySiteAreaTracker { get; init; } = null!;
    public AltarPlacementHandler AltarPlacementHandler { get; init; } = null!;
    public AltarDestructionHandler AltarDestructionHandler { get; init; } = null!;
    public AltarPrayerHandler AltarPrayerHandler { get; init; } = null!;
    public FavorSystem FavorSystem { get; init; } = null!;
    public ActivityLogManager ActivityLogManager { get; init; } = null!;
    public PvPManager PvPManager { get; init; } = null!;
    public DiplomacyManager DiplomacyManager { get; init; } = null!;
    public BlessingRegistry BlessingRegistry { get; init; } = null!;
    public BlessingEffectSystem BlessingEffectSystem { get; init; } = null!;
    public RoleManager RoleManager { get; init; } = null!;
    public AltarEventEmitter AltarEventEmitter { get; init; } = null!;
    public IRitualProgressManager RitualProgressManager { get; init; } = null!;

    // 6 Commands
    public FavorCommands FavorCommands { get; init; } = null!;
    public BlessingCommands BlessingCommands { get; init; } = null!;
    public ReligionCommands ReligionCommands { get; init; } = null!;
    public RoleCommands RoleCommands { get; init; } = null!;
    public CivilizationCommands CivilizationCommands { get; init; } = null!;
    public HolySiteCommands HolySiteCommands { get; init; } = null!;

    // Network Handlers
    public PlayerDataNetworkHandler PlayerDataNetworkHandler { get; init; } = null!;
    public BlessingNetworkHandler BlessingNetworkHandler { get; init; } = null!;
    public ReligionNetworkHandler ReligionNetworkHandler { get; init; } = null!;
    public CivilizationNetworkHandler CivilizationNetworkHandler { get; init; } = null!;
    public DiplomacyNetworkHandler DiplomacyNetworkHandler { get; init; } = null!;
    public ActivityNetworkHandler ActivityNetworkHandler { get; init; } = null!;
    public HolySiteNetworkHandler HolySiteNetworkHandler { get; init; } = null!;
    public MilestoneNetworkHandler MilestoneNetworkHandler { get; init; } = null!;

    /// <summary>
    ///     Set of religion UIDs that were migrated with auto-generated deity names.
    ///     Used to notify founders on first login after migration.
    /// </summary>
    public HashSet<string> MigratedReligionUIDs { get; init; } = new();
}