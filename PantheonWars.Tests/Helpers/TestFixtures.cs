using System;
using System.Collections.Generic;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.BuffSystem.Interfaces;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using IPlayerDataManager = PantheonWars.Systems.Interfaces.IPlayerDataManager;

namespace PantheonWars.Tests.Helpers;

/// <summary>
///     Provides reusable test fixtures and mock objects for unit tests
/// </summary>
public static class TestFixtures
{
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

    #region Mock System Interfaces

    /// <summary>
    ///     Creates a mock IDeityRegistry with Khoras and Lysa deities
    /// </summary>
    public static Mock<IDeityRegistry> CreateMockDeityRegistry()
    {
        var mockRegistry = new Mock<IDeityRegistry>();

        var khoras = CreateTestDeity(DeityType.Aethra, "Khoras", "War");
        var lysa = CreateTestDeity(DeityType.Gaia, "Lysa", "Hunt");

        mockRegistry.Setup(r => r.GetDeity(DeityType.Aethra)).Returns(khoras);
        mockRegistry.Setup(r => r.GetDeity(DeityType.Gaia)).Returns(lysa);
        mockRegistry.Setup(r => r.HasDeity(DeityType.Aethra)).Returns(true);
        mockRegistry.Setup(r => r.HasDeity(DeityType.Gaia)).Returns(true);
        mockRegistry.Setup(r => r.GetAllDeities()).Returns(new List<Deity> { khoras, lysa });

        // Setup relationships
        mockRegistry.Setup(r => r.GetRelationship(DeityType.Aethra, DeityType.Gaia))
            .Returns(DeityRelationshipType.Allied);
        mockRegistry.Setup(r => r.GetRelationship(DeityType.Gaia, DeityType.Aethra))
            .Returns(DeityRelationshipType.Allied);

        // Setup favor multipliers
        mockRegistry.Setup(r => r.GetFavorMultiplier(DeityType.Aethra, DeityType.Gaia)).Returns(0.5f);
        mockRegistry.Setup(r => r.GetFavorMultiplier(DeityType.Aethra, DeityType.Morthen)).Returns(2.0f);

        return mockRegistry;
    }

    /// <summary>
    ///     Creates a mock IPlayerDataManager
    /// </summary>
    public static Mock<IPlayerDataManager> CreateMockPlayerDataManager()
    {
        return new Mock<IPlayerDataManager>();
    }

    /// <summary>
    ///     Creates a mock IPlayerReligionDataManager with basic setup
    /// </summary>
    public static Mock<IPlayerReligionDataManager> CreateMockPlayerReligionDataManager()
    {
        var mock = new Mock<IPlayerReligionDataManager>();

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
        DeityType type = DeityType.Aethra,
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
            Relationships = new Dictionary<DeityType, DeityRelationshipType>
            {
                { DeityType.Gaia, DeityRelationshipType.Allied },
                { DeityType.Morthen, DeityRelationshipType.Rival }
            },
            AbilityIds = new List<string> { "test_ability_1", "test_ability_2" }
        };
    }

    /// <summary>
    ///     Creates test PlayerReligionData with default values
    /// </summary>
    public static PlayerReligionData CreateTestPlayerReligionData(
        string playerUID = "test-player-uid",
        DeityType deity = DeityType.Aethra,
        string? religionUID = "test-religion-uid")
    {
        return new PlayerReligionData
        {
            PlayerUID = playerUID,
            ActiveDeity = deity,
            ReligionUID = religionUID,
            LastReligionSwitch = DateTime.UtcNow.AddDays(-30)
        };
    }

    /// <summary>
    ///     Creates test ReligionData with default values
    /// </summary>
    public static ReligionData CreateTestReligion(
        string religionUID = "test-religion-uid",
        string religionName = "Test Religion",
        DeityType deity = DeityType.Aethra,
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
        DeityType deity = DeityType.Aethra,
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
}
