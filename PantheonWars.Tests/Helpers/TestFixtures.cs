using System;
using System.Collections.Generic;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
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

    // CreateMockDeityRegistry removed - deity system deleted

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

    // CreateMockReligionPrestigeManager removed - prestige system deleted
    // CreateMockBlessingRegistry removed - blessing system deleted
    // CreateMockBlessingEffectSystem removed - blessing system deleted
    // CreateMockBuffManager removed - buff system deleted
    // CreateMockFavorSystem removed - favor system deleted

    #endregion

    #region Test Data Objects

    // CreateTestDeity removed - deity system deleted

    /// <summary>
    ///     Creates test PlayerReligionData with default values
    /// </summary>
    public static PlayerReligionData CreateTestPlayerReligionData(
        string playerUID = "test-player-uid",
        DeityType deity = DeityType.Khoras,
        string? religionUID = "test-religion-uid",
        int favor = 100,
        int totalFavorEarned = 500)
    {
        return new PlayerReligionData
        {
            PlayerUID = playerUID,
            ActiveDeity = deity,
            ReligionUID = religionUID,
            Favor = favor,
            TotalFavorEarned = totalFavorEarned,
            FavorRank = FavorRank.Disciple,
            KillCount = 0,
            LastReligionSwitch = DateTime.UtcNow.AddDays(-30),
            UnlockedBlessings = new Dictionary<string, bool>()
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

    // CreateTestBlessing removed - blessing system deleted

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
