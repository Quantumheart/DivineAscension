using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Commands;
using DivineAscension.Services;
using DivineAscension.Systems.BuffSystem;
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
    /// <returns>InitializationResult containing all initialized managers, commands, and handlers</returns>
    public static InitializationResult InitializeServerSystems(
        ICoreServerAPI api,
        IServerNetworkChannel serverChannel)
    {
        api.Logger.Notification("[DivineAscension] Starting server-side system initialization...");

        // Initialize localization service for server
        LocalizationService.Instance.InitializeServer(api);

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

        api.RegisterEntityBehaviorClass("DivineAscensionBuffTracker", typeof(EntityBehaviorBuffTracker));

        var religionManager = new ReligionManager(api);
        religionManager.Initialize();

        // Migrate existing religions with empty deity names (for backward compatibility)
        var migratedReligionUIDs = religionManager.MigrateEmptyDeityNames();

        var civilizationManager = new CivilizationManager(api, religionManager);
        civilizationManager.Initialize();

        var playerReligionDataManager = new PlayerProgressionDataManager(api, religionManager);
        playerReligionDataManager.Initialize();

        // CRITICAL: MUST be initialized before FavorSystem
        var religionPrestigeManager = new ReligionPrestigeManager(api, religionManager);
        religionPrestigeManager.Initialize();

        var favorSystem = new FavorSystem(api, playerReligionDataManager, religionManager,
            religionPrestigeManager);
        favorSystem.Initialize();

        var diplomacyManager = new DiplomacyManager(api, civilizationManager, religionPrestigeManager, religionManager);
        diplomacyManager.Initialize();

        var pvpManager = new PvPManager(api, playerReligionDataManager, religionManager, religionPrestigeManager,
            civilizationManager, diplomacyManager);
        pvpManager.Initialize();

        var blessingRegistry = new BlessingRegistry(api);
        blessingRegistry.Initialize();

        var blessingEffectSystem =
            new BlessingEffectSystem(api, blessingRegistry, playerReligionDataManager, religionManager);
        blessingEffectSystem.Initialize();

        // CRITICAL: Must be called AFTER BlessingEffectSystem is initialized
        religionPrestigeManager.SetBlessingSystems(blessingRegistry, blessingEffectSystem);

        // CRITICAL: Must be called AFTER DiplomacyManager is initialized
        religionPrestigeManager.SetDiplomacyManager(diplomacyManager, civilizationManager);

        var favorCommands = new FavorCommands(api, playerReligionDataManager, religionManager);
        favorCommands.RegisterCommands();

        var blessingCommands = new BlessingCommands(api, blessingRegistry, playerReligionDataManager, religionManager,
            blessingEffectSystem, serverChannel);
        blessingCommands.RegisterCommands();

        var roleManager = new RoleManager(religionManager);

        var religionCommands = new ReligionCommands(api, religionManager, playerReligionDataManager,
            religionPrestigeManager, serverChannel, roleManager);
        religionCommands.RegisterCommands();

        var roleCommands = new RoleCommands(api, roleManager, religionManager, playerReligionDataManager);
        roleCommands.RegisterCommands();

        var civilizationCommands =
            new CivilizationCommands(api, civilizationManager, religionManager, playerReligionDataManager);
        civilizationCommands.RegisterCommands();

        // Create and initialize network handlers
        var playerDataHandler =
            new PlayerDataNetworkHandler(api, playerReligionDataManager, religionManager, serverChannel);
        playerDataHandler.RegisterHandlers();

        var blessingHandler = new BlessingNetworkHandler(
            api,
            blessingRegistry,
            blessingEffectSystem,
            playerReligionDataManager,
            religionManager,
            serverChannel);
        blessingHandler.RegisterHandlers();

        var religionHandler = new ReligionNetworkHandler(
            api,
            religionManager,
            playerReligionDataManager,
            roleManager,
            serverChannel);
        religionHandler.RegisterHandlers();

        var civilizationHandler = new CivilizationNetworkHandler(
            api,
            civilizationManager,
            religionManager,
            serverChannel);
        civilizationHandler.RegisterHandlers();

        var diplomacyHandler = new DiplomacyNetworkHandler(
            api,
            diplomacyManager,
            civilizationManager,
            religionManager,
            playerReligionDataManager,
            serverChannel);
        diplomacyHandler.RegisterHandlers();

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
            ReligionManager = religionManager,
            CivilizationManager = civilizationManager,
            PlayerProgressionDataManager = playerReligionDataManager,
            ReligionPrestigeManager = religionPrestigeManager,
            FavorSystem = favorSystem,
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
    // 11 Managers
    public ReligionManager ReligionManager { get; init; } = null!;
    public CivilizationManager CivilizationManager { get; init; } = null!;
    public PlayerProgressionDataManager PlayerProgressionDataManager { get; init; } = null!;
    public ReligionPrestigeManager ReligionPrestigeManager { get; init; } = null!;
    public FavorSystem FavorSystem { get; init; } = null!;
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

    /// <summary>
    ///     Set of religion UIDs that were migrated with auto-generated deity names.
    ///     Used to notify founders on first login after migration.
    /// </summary>
    public HashSet<string> MigratedReligionUIDs { get; init; } = new();
}