using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems;
using DivineAscension.Systems.BuffSystem.Interfaces;
using DivineAscension.Systems.Interfaces;
using Moq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Helpers;

/// <summary>
///     Provides reusable test fixtures and mock objects for unit tests
/// </summary>
[ExcludeFromCodeCoverage]
public static class TestFixtures
{
    #region Mock Players

    /// <summary>
    ///     Creates a mock IServerPlayer with the specified UID and name
    /// </summary>
    public static Mock<IServerPlayer> CreateMockServerPlayer(string uid = "test-player-uid", string name = "TestPlayer")
    {
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns(uid);
        mockPlayer.Setup(p => p.PlayerName).Returns(name);
        return mockPlayer;
    }

    #endregion

    #region Mock Entity Objects

    /// <summary>
    ///     Creates a mock EntityAgent for buff/debuff testing
    /// </summary>
    public static Mock<EntityAgent> CreateMockEntity()
    {
        var mockEntity = new Mock<EntityAgent>(MockBehavior.Loose);
        mockEntity.CallBase = false;
        return mockEntity;
    }

    #endregion

    #region Mock API Objects

    /// <summary>
    ///     Creates a mock ICoreAPI with basic logger setup
    /// </summary>
    public static Mock<ICoreAPI> CreateMockCoreAPI()
    {
        var mockAPI = new Mock<ICoreAPI>();
        var mockLogger = new Mock<ILogger>();
        mockAPI.Setup(a => a.Logger).Returns(mockLogger.Object);
        return mockAPI;
    }

    /// <summary>
    ///     Creates a mock ICoreServerAPI with basic logger and world setup
    /// </summary>
    public static Mock<ICoreServerAPI> CreateMockServerAPI()
    {
        var mockAPI = new Mock<ICoreServerAPI>();
        var mockLogger = new Mock<ILogger>();
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockEventAPI = new Mock<IServerEventAPI>();

        mockAPI.Setup(a => a.Logger).Returns(mockLogger.Object);
        mockAPI.Setup(a => a.World).Returns(mockWorld.Object);
        mockAPI.Setup(a => a.Event).Returns(mockEventAPI.Object);

        return mockAPI;
    }

    /// <summary>
    ///     Creates a mock ICoreClientAPI with basic logger setup
    /// </summary>
    public static Mock<ICoreClientAPI> CreateMockClientAPI()
    {
        var mockAPI = new Mock<ICoreClientAPI>();
        var mockLogger = new Mock<ILogger>();
        mockAPI.Setup(a => a.Logger).Returns(mockLogger.Object);
        return mockAPI;
    }

    #endregion

    #region Mock System Interfaces

    /// <summary>
    ///     Creates a mock IDeityRegistry with Khoras and Lysa deities
    /// </summary>
    public static Mock<IDeityRegistry> CreateMockDeityRegistry()
    {
        var mockRegistry = new Mock<IDeityRegistry>();

        var khoras = CreateTestDeity(DeityType.Khoras, "Khoras", "War");
        var lysa = CreateTestDeity(DeityType.Lysa, "Lysa", "Hunt");

        mockRegistry.Setup(r => r.GetDeity(DeityType.Khoras)).Returns(khoras);
        mockRegistry.Setup(r => r.GetDeity(DeityType.Lysa)).Returns(lysa);
        mockRegistry.Setup(r => r.HasDeity(DeityType.Khoras)).Returns(true);
        mockRegistry.Setup(r => r.HasDeity(DeityType.Lysa)).Returns(true);
        mockRegistry.Setup(r => r.GetAllDeities()).Returns(new List<Deity> { khoras, lysa });

        return mockRegistry;
    }

    /// <summary>
    ///     Creates a mock IPlayerReligionDataManager with basic setup
    /// </summary>
    public static Mock<IPlayerProgressionDataManager> CreateMockPlayerProgressionDataManager()
    {
        var mock = new Mock<IPlayerProgressionDataManager>();

        // Default: return empty player data
        mock.Setup(m => m.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns((string uid) => CreateTestPlayerReligionData(uid));

        return mock;
    }

    /// <summary>
    ///     Creates a mock IReligionManager
    /// </summary>
    public static Mock<IReligionManager> CreateMockReligionManager()
    {
        return new Mock<IReligionManager>();
    }

    /// <summary>
    ///     Creates a mock IReligionPrestigeManager
    /// </summary>
    public static Mock<IReligionPrestigeManager> CreateMockReligionPrestigeManager()
    {
        return new Mock<IReligionPrestigeManager>();
    }

    /// <summary>
    ///     Creates a mock IBlessingRegistry
    /// </summary>
    public static Mock<IBlessingRegistry> CreateMockBlessingRegistry()
    {
        return new Mock<IBlessingRegistry>();
    }

    /// <summary>
    ///     Creates a mock IBlessingEffectSystem
    /// </summary>
    public static Mock<IBlessingEffectSystem> CreateMockBlessingEffectSystem()
    {
        return new Mock<IBlessingEffectSystem>();
    }

    /// <summary>
    ///     Creates a mock IBuffManager
    /// </summary>
    public static Mock<IBuffManager> CreateMockBuffManager()
    {
        return new Mock<IBuffManager>();
    }

    /// <summary>
    ///     Creates a mock IFavorSystem
    /// </summary>
    public static Mock<IFavorSystem> CreateMockFavorSystem()
    {
        return new Mock<IFavorSystem>();
    }

    #endregion

    #region Test Data Objects

    /// <summary>
    ///     Creates a test Deity with default values
    /// </summary>
    public static Deity CreateTestDeity(
        DeityType type = DeityType.Khoras,
        string name = "Khoras",
        string domain = "War")
    {
        return new Deity(type, name, domain)
        {
            Description = $"The God/Goddess of {domain}",
            Alignment = DeityAlignment.Lawful,
            PrimaryColor = "#8B0000",
            SecondaryColor = "#FFD700",
            Playstyle = "Test playstyle",
            AbilityIds = new List<string> { "test_ability_1", "test_ability_2" }
        };
    }

    /// <summary>
    ///     Creates test PlayerReligionData with default values
    /// </summary>
    public static PlayerProgressionData CreateTestPlayerReligionData(string playerUID = "test-player-uid",
        DeityType deity = DeityType.Khoras,
        string? religionUID = "test-religion-uid",
        int favor = 100,
        int totalFavorEarned = 500)
    {
        return new PlayerProgressionData()
        {
            Id = playerUID,
            Favor = favor,
            TotalFavorEarned = totalFavorEarned,
            UnlockedBlessings = new()
        };
    }

    /// <summary>
    ///     Creates test ReligionData with default values
    /// </summary>
    public static ReligionData CreateTestReligion(
        string religionUID = "test-religion-uid",
        string religionName = "Test Religion",
        DeityType deity = DeityType.Khoras,
        string founderUID = "founder-uid")
    {
        return new ReligionData
        {
            ReligionUID = religionUID,
            ReligionName = religionName,
            Deity = deity,
            FounderUID = founderUID,
            Description = "A test religion",
            IsPublic = true,
            MemberUIDs = new List<string> { founderUID },
            Prestige = 0,
            TotalPrestige = 0,
            PrestigeRank = PrestigeRank.Fledgling,
            UnlockedBlessings = new Dictionary<string, bool>()
        };
    }

    /// <summary>
    ///     Creates a test Blessing with default values
    /// </summary>
    public static Blessing CreateTestBlessing(
        string id = "test_blessing",
        string name = "Test Blessing",
        DeityType deity = DeityType.Khoras,
        BlessingKind kind = BlessingKind.Player)
    {
        return new Blessing(id, name, deity)
        {
            Description = "A test blessing",
            Category = BlessingCategory.Combat,
            RequiredFavorRank = 1,
            RequiredPrestigeRank = 0,
            PrerequisiteBlessings = new List<string>(),
            StatModifiers = new Dictionary<string, float>
            {
                { "walkspeed", 0.1f }
            },
            Kind = kind,
            SpecialEffects = new List<string>()
        };
    }

    #endregion

    #region Assertion Helpers

    /// <summary>
    ///     Verifies that a logger notification was called with the expected message substring
    /// </summary>
    public static void VerifyLoggerNotification(Mock<ILogger> mockLogger, string expectedSubstring)
    {
        mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains(expectedSubstring))),
            Times.AtLeastOnce(),
            $"Expected logger notification containing: {expectedSubstring}"
        );
    }

    /// <summary>
    ///     Verifies that a logger debug message was called with the expected message substring
    /// </summary>
    public static void VerifyLoggerDebug(Mock<ILogger> mockLogger, string expectedSubstring)
    {
        mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains(expectedSubstring))),
            Times.AtLeastOnce(),
            $"Expected logger debug containing: {expectedSubstring}"
        );
    }

    #endregion

    #region Localization Helpers

    /// <summary>
    ///     Initializes the LocalizationService for testing by loading translations from en.json.
    ///     Call this in test constructors or setup methods.
    /// </summary>
    public static void InitializeLocalizationForTests()
    {
        // Load translations from the actual en.json file
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var enJsonPath = Path.Combine(projectRoot, "DivineAscension", "assets", "divineascension", "lang", "en.json");

        var logPath = Path.Combine(AppContext.BaseDirectory, "test_localization_debug.log");
        using var logWriter = new StreamWriter(logPath, append: true);

        logWriter.WriteLine($"=== TestFixtures.InitializeLocalizationForTests called at {DateTime.Now} ===");
        logWriter.WriteLine($"Base directory: {AppContext.BaseDirectory}");
        logWriter.WriteLine($"Project root: {projectRoot}");
        logWriter.WriteLine($"en.json path: {enJsonPath}");
        logWriter.WriteLine($"File exists: {File.Exists(enJsonPath)}");

        Dictionary<string, string> translations = new();

        try
        {
            if (File.Exists(enJsonPath))
            {
                var json = File.ReadAllText(enJsonPath);
                logWriter.WriteLine($"Loaded JSON, length: {json.Length}");

                var deserialized = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                logWriter.WriteLine($"Deserialized: {deserialized != null}");

                if (deserialized != null)
                {
                    logWriter.WriteLine($"Total entries before filter: {deserialized.Count}");

                    // Filter out comment entries (start with "_")
                    translations = deserialized
                        .Where(kvp => !kvp.Key.StartsWith("_"))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    logWriter.WriteLine($"Total entries after filter: {translations.Count}");

                    // Log first few translations
                    logWriter.WriteLine("First 5 translations:");
                    foreach (var kvp in translations.Take(5))
                    {
                        logWriter.WriteLine($"  {kvp.Key} => {kvp.Value}");
                    }
                }
            }
            else
            {
                logWriter.WriteLine($"File not found at path: {enJsonPath}");
            }
        }
        catch (Exception ex)
        {
            logWriter.WriteLine($"ERROR: Failed to load en.json for tests: {ex.Message}");
            logWriter.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        // If no translations loaded, use minimal fallback
        if (translations.Count == 0)
        {
            logWriter.WriteLine("Using fallback translations");
            translations = new Dictionary<string, string>
            {
                [LocalizationKeys.CMD_ERROR_PLAYERS_ONLY] = "This command can only be used by players.",
                [LocalizationKeys.CMD_ERROR_NO_RELIGION] = "You are not in a religion."
            };
        }

        logWriter.WriteLine($"Initializing LocalizationService with {translations.Count} translations");
        LocalizationService.Instance.InitializeForTesting(translations);

        // Verify a key works
        var testKey = LocalizationKeys.CMD_ERROR_NO_RELIGION;
        var testValue = LocalizationService.Instance.Get(testKey);
        logWriter.WriteLine($"Test lookup: {testKey} => {testValue}");
        logWriter.WriteLine("=== End initialization ===\n");
    }

    /// <summary>
    ///     Resets the LocalizationService. Call this in test teardown if needed.
    /// </summary>
    public static void ResetLocalization()
    {
        LocalizationService.Instance.ClearCache();
    }

    #endregion
}