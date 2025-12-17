using System.Diagnostics.CodeAnalysis;
using DivineAscension.Commands;
using DivineAscension.Systems.BuffSystem;
using DivineAscension.Systems.Networking.Server;
using DivineAscension.Systems.Patches;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
///     Handles initialization of all DivineAscension server-side systems.
///     This class extracts the complex initialization logic from PantheonWarsSystem.cs
///     to improve maintainability and testability.
/// </summary>
[ExcludeFromCodeCoverage]
public static class PantheonWarsSystemInitializer
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

        // Step 1: Clear any static event subscribers from previous loads
        PitKilnPatches.ClearSubscribers();
        AnvilPatches.ClearSubscribers();

        api.RegisterEntityBehaviorClass("PantheonWarsBuffTracker", typeof(EntityBehaviorBuffTracker));

        var deityRegistry = new DeityRegistry(api);
        deityRegistry.Initialize();

        var religionManager = new ReligionManager(api);
        religionManager.Initialize();

        var civilizationManager = new CivilizationManager(api, religionManager);
        civilizationManager.Initialize();

        var playerReligionDataManager = new PlayerReligionDataManager(api, religionManager);
        playerReligionDataManager.Initialize();

        // CRITICAL: MUST be initialized before FavorSystem
        var religionPrestigeManager = new ReligionPrestigeManager(api, religionManager);
        religionPrestigeManager.Initialize();

        var favorSystem = new FavorSystem(api, playerReligionDataManager, deityRegistry, religionManager,
            religionPrestigeManager);
        favorSystem.Initialize();

        var pvpManager = new PvPManager(api, playerReligionDataManager, religionManager, religionPrestigeManager,
            deityRegistry);
        pvpManager.Initialize();

        var blessingRegistry = new BlessingRegistry(api);
        blessingRegistry.Initialize();

        var blessingEffectSystem =
            new BlessingEffectSystem(api, blessingRegistry, playerReligionDataManager, religionManager);
        blessingEffectSystem.Initialize();

        // CRITICAL: Must be called AFTER BlessingEffectSystem is initialized
        religionPrestigeManager.SetBlessingSystems(blessingRegistry, blessingEffectSystem);

        var favorCommands = new FavorCommands(api, deityRegistry, playerReligionDataManager);
        favorCommands.RegisterCommands();

        var blessingCommands = new BlessingCommands(api, blessingRegistry, playerReligionDataManager, religionManager,
            blessingEffectSystem);
        blessingCommands.RegisterCommands();

        var roleManager = new RoleManager(religionManager);

        var religionCommands = new ReligionCommands(api, religionManager, playerReligionDataManager, serverChannel);
        religionCommands.RegisterCommands();

        var roleCommands = new RoleCommands(api, roleManager, religionManager, playerReligionDataManager);
        roleCommands.RegisterCommands();

        var civilizationCommands =
            new CivilizationCommands(api, civilizationManager, religionManager, playerReligionDataManager);
        civilizationCommands.RegisterCommands();

        // Create and initialize network handlers
        var playerDataHandler = new PlayerDataNetworkHandler(api, playerReligionDataManager, religionManager,
            deityRegistry, serverChannel);
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
            playerReligionDataManager,
            serverChannel);
        civilizationHandler.RegisterHandlers();

        api.Logger.Notification("[DivineAscension] All server-side systems initialized successfully");

        // Return all initialized components
        return new InitializationResult
        {
            DeityRegistry = deityRegistry,
            ReligionManager = religionManager,
            CivilizationManager = civilizationManager,
            PlayerReligionDataManager = playerReligionDataManager,
            ReligionPrestigeManager = religionPrestigeManager,
            FavorSystem = favorSystem,
            PvPManager = pvpManager,
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
            CivilizationNetworkHandler = civilizationHandler
        };
    }
}

/// <summary>
///     Container for all initialized server-side systems, commands, and handlers.
/// </summary>
[ExcludeFromCodeCoverage]
public class InitializationResult
{
    // 10 Managers
    public DeityRegistry DeityRegistry { get; init; } = null!;
    public ReligionManager ReligionManager { get; init; } = null!;
    public CivilizationManager CivilizationManager { get; init; } = null!;
    public PlayerReligionDataManager PlayerReligionDataManager { get; init; } = null!;
    public ReligionPrestigeManager ReligionPrestigeManager { get; init; } = null!;
    public FavorSystem FavorSystem { get; init; } = null!;
    public PvPManager PvPManager { get; init; } = null!;
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
}