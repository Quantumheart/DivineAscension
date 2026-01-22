using System.Reflection;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Favor;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Xunit;

namespace DivineAscension.Tests.Systems.Favor;

public class ChiselingFavorTrackerTests
{
    private static StoneFavorTracker CreateTracker(
        IPlayerProgressionDataManager progression,
        ILoggerWrapper logger,
        IEventService eventService,
        FakeWorldService worldService,
        IFavorSystem favorSystem,
        IPlayerMessengerService? messenger = null)
    {
        messenger ??= new Mock<IPlayerMessengerService>().Object;
        return new StoneFavorTracker(
            progression,
            logger,
            eventService,
            worldService,
            favorSystem,
            messenger);
    }

    [Fact]
    public void HandleVoxelsChanged_SingleVoxel_AwardsHalfFavor()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var mockProgression = new Mock<IPlayerProgressionDataManager>();
        mockProgression.Setup(m => m.GetPlayerDeityType("player1"))
            .Returns(DeityDomain.Stone);

        var mockFavor = new Mock<IFavorSystem>();
        var fakeWorld = new FakeWorldService();
        var mockEvent = new Mock<IEventService>();
        var mockLogger = new Mock<ILoggerWrapper>();

        var tracker = CreateTracker(
            mockProgression.Object,
            mockLogger.Object,
            mockEvent.Object,
            fakeWorld,
            mockFavor.Object);

        tracker.Initialize();

        // Act - Invoke via reflection
        var method = typeof(StoneFavorTracker).GetMethod("HandleVoxelsChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 1 });

        // Assert
        mockFavor.Verify(f => f.AwardFavorForAction(
            mockPlayer.Object, "Stone Carving", 0.02f), Times.Once);
    }

    [Fact]
    public void HandleVoxelsChanged_MultipleVoxels_AwardsProportionalFavor()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var mockProgression = new Mock<IPlayerProgressionDataManager>();
        mockProgression.Setup(m => m.GetPlayerDeityType("player1"))
            .Returns(DeityDomain.Stone);

        var mockFavor = new Mock<IFavorSystem>();
        var fakeWorld = new FakeWorldService();
        var mockEvent = new Mock<IEventService>();
        var mockLogger = new Mock<ILoggerWrapper>();

        var tracker = CreateTracker(
            mockProgression.Object,
            mockLogger.Object,
            mockEvent.Object,
            fakeWorld,
            mockFavor.Object);

        tracker.Initialize();

        // Act - Invoke via reflection
        var method = typeof(StoneFavorTracker).GetMethod("HandleVoxelsChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 });

        // Assert
        mockFavor.Verify(f => f.AwardFavorForAction(
            mockPlayer.Object, "Stone Carving", 2f), Times.Once);
    }

    [Fact]
    public void HandleVoxelsChanged_LargeSculpture_AwardsHighFavor()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var mockProgression = new Mock<IPlayerProgressionDataManager>();
        mockProgression.Setup(m => m.GetPlayerDeityType("player1"))
            .Returns(DeityDomain.Stone);

        var mockFavor = new Mock<IFavorSystem>();
        var fakeWorld = new FakeWorldService();
        var mockEvent = new Mock<IEventService>();
        var mockLogger = new Mock<ILoggerWrapper>();

        var tracker = CreateTracker(
            mockProgression.Object,
            mockLogger.Object,
            mockEvent.Object,
            fakeWorld,
            mockFavor.Object);

        tracker.Initialize();

        // Act - Invoke via reflection
        var method = typeof(StoneFavorTracker).GetMethod("HandleVoxelsChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 2000 });

        // Assert
        mockFavor.Verify(f => f.AwardFavorForAction(
            mockPlayer.Object, "Stone Carving", 40f), Times.Once);
    }

    [Fact]
    public void HandleVoxelsChanged_NonStoneFollower_NoFavor()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var mockProgression = new Mock<IPlayerProgressionDataManager>();
        mockProgression.Setup(m => m.GetPlayerDeityType("player1"))
            .Returns(DeityDomain.Wild); // Not Stone domain

        var mockFavor = new Mock<IFavorSystem>();
        var fakeWorld = new FakeWorldService();
        var mockEvent = new Mock<IEventService>();
        var mockLogger = new Mock<ILoggerWrapper>();

        var tracker = CreateTracker(
            mockProgression.Object,
            mockLogger.Object,
            mockEvent.Object,
            fakeWorld,
            mockFavor.Object);

        tracker.Initialize();

        // Act - Invoke via reflection
        var method = typeof(StoneFavorTracker).GetMethod("HandleVoxelsChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 });

        // Assert - No favor awarded
        mockFavor.Verify(f => f.AwardFavorForAction(
            It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<float>()), Times.Never);
    }

    [Fact]
    public void ChiselingSession_FirstTwoActions_NoMultiplier()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var mockProgression = new Mock<IPlayerProgressionDataManager>();
        mockProgression.Setup(m => m.GetPlayerDeityType("player1"))
            .Returns(DeityDomain.Stone);

        var mockFavor = new Mock<IFavorSystem>();
        var fakeWorld = new FakeWorldService();
        var mockEvent = new Mock<IEventService>();
        var mockLogger = new Mock<ILoggerWrapper>();

        var tracker = CreateTracker(
            mockProgression.Object,
            mockLogger.Object,
            mockEvent.Object,
            fakeWorld,
            mockFavor.Object);

        tracker.Initialize();

        var method = typeof(StoneFavorTracker).GetMethod("HandleVoxelsChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Act - First action (combo 1)
        fakeWorld.SetElapsedMilliseconds(1000);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 });

        // Act - Second action (combo 2) - 4 seconds later to pass cooldown
        fakeWorld.SetElapsedMilliseconds(5000);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 });

        // Assert - Both should have 1.0x multiplier
        mockFavor.Verify(f => f.AwardFavorForAction(
            mockPlayer.Object, "Stone Carving", 2f), Times.Exactly(2)); // 100 × 0.02 × 1.0 = 2f
    }

    [Fact]
    public void ChiselingSession_ThirdAction_Tier2Multiplier()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var mockProgression = new Mock<IPlayerProgressionDataManager>();
        mockProgression.Setup(m => m.GetPlayerDeityType("player1"))
            .Returns(DeityDomain.Stone);

        var mockFavor = new Mock<IFavorSystem>();
        var fakeWorld = new FakeWorldService();
        var mockEvent = new Mock<IEventService>();
        var mockLogger = new Mock<ILoggerWrapper>();

        var tracker = CreateTracker(
            mockProgression.Object,
            mockLogger.Object,
            mockEvent.Object,
            fakeWorld,
            mockFavor.Object);

        tracker.Initialize();

        var method = typeof(StoneFavorTracker).GetMethod("HandleVoxelsChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Act - Build combo to 3 actions
        fakeWorld.SetElapsedMilliseconds(1000);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 }); // Combo 1

        fakeWorld.SetElapsedMilliseconds(5000);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 }); // Combo 2

        fakeWorld.SetElapsedMilliseconds(9000);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 }); // Combo 3

        // Assert - Third action should have 1.25x multiplier
        mockFavor.Verify(f => f.AwardFavorForAction(
            mockPlayer.Object, "Stone Carving", 2.5f), Times.Once); // 100 × 0.02 × 1.25 = 2.5f
    }

    [Fact]
    public void ChiselingSession_SixthAction_Tier3Multiplier()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var mockProgression = new Mock<IPlayerProgressionDataManager>();
        mockProgression.Setup(m => m.GetPlayerDeityType("player1"))
            .Returns(DeityDomain.Stone);

        var mockFavor = new Mock<IFavorSystem>();
        var fakeWorld = new FakeWorldService();
        var mockEvent = new Mock<IEventService>();
        var mockLogger = new Mock<ILoggerWrapper>();

        var tracker = CreateTracker(
            mockProgression.Object,
            mockLogger.Object,
            mockEvent.Object,
            fakeWorld,
            mockFavor.Object);

        tracker.Initialize();

        var method = typeof(StoneFavorTracker).GetMethod("HandleVoxelsChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Act - Build combo to 6 actions
        for (int i = 0; i < 6; i++)
        {
            fakeWorld.SetElapsedMilliseconds(1000 + (i * 4000)); // 4 seconds apart
            method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 });
        }

        // Assert - Sixth action should have 1.5x multiplier
        mockFavor.Verify(f => f.AwardFavorForAction(
            mockPlayer.Object, "Stone Carving", 3f), Times.Once); // 100 × 0.02 × 1.5 = 3f
    }

    [Fact]
    public void ChiselingSession_IdleTimeout_ResetsCombo()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var mockProgression = new Mock<IPlayerProgressionDataManager>();
        mockProgression.Setup(m => m.GetPlayerDeityType("player1"))
            .Returns(DeityDomain.Stone);

        var mockFavor = new Mock<IFavorSystem>();
        var fakeWorld = new FakeWorldService();
        var mockEvent = new Mock<IEventService>();
        var mockLogger = new Mock<ILoggerWrapper>();

        var tracker = CreateTracker(
            mockProgression.Object,
            mockLogger.Object,
            mockEvent.Object,
            fakeWorld,
            mockFavor.Object);

        tracker.Initialize();

        var method = typeof(StoneFavorTracker).GetMethod("HandleVoxelsChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Act - Build combo to 3 (should have 1.25x)
        fakeWorld.SetElapsedMilliseconds(1000);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 }); // Combo 1

        fakeWorld.SetElapsedMilliseconds(5000);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 }); // Combo 2

        fakeWorld.SetElapsedMilliseconds(9000);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 }); // Combo 3 (1.25x)

        // Wait 50 seconds (exceeds 45s timeout)
        fakeWorld.SetElapsedMilliseconds(59000);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 }); // Should reset to combo 1

        // Assert - After timeout, should be back to 1.0x multiplier
        mockFavor.Verify(f => f.AwardFavorForAction(
            mockPlayer.Object, "Stone Carving", 2f), Times.Exactly(3)); // Last action: 100 × 0.02 × 1.0 = 2f
    }

    [Fact]
    public void ChiselingSession_MaxCombo_ReachesTier5()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var mockProgression = new Mock<IPlayerProgressionDataManager>();
        mockProgression.Setup(m => m.GetPlayerDeityType("player1"))
            .Returns(DeityDomain.Stone);

        var mockFavor = new Mock<IFavorSystem>();
        var fakeWorld = new FakeWorldService();
        var mockEvent = new Mock<IEventService>();
        var mockLogger = new Mock<ILoggerWrapper>();

        var tracker = CreateTracker(
            mockProgression.Object,
            mockLogger.Object,
            mockEvent.Object,
            fakeWorld,
            mockFavor.Object);

        tracker.Initialize();

        var method = typeof(StoneFavorTracker).GetMethod("HandleVoxelsChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Act - Build combo to 21 actions (tier 5: 2.5x)
        for (int i = 0; i < 21; i++)
        {
            fakeWorld.SetElapsedMilliseconds(1000 + (i * 4000)); // 4 seconds apart
            method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 });
        }

        // Assert - 21st action should have 2.5x multiplier
        mockFavor.Verify(f => f.AwardFavorForAction(
            mockPlayer.Object, "Stone Carving", 5f), Times.Once); // 100 × 0.02 × 2.5 = 5f
    }

    [Fact]
    public void ChiselingSession_AntiSpam_CooldownPreventsComboAbuse()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var mockProgression = new Mock<IPlayerProgressionDataManager>();
        mockProgression.Setup(m => m.GetPlayerDeityType("player1"))
            .Returns(DeityDomain.Stone);

        var mockFavor = new Mock<IFavorSystem>();
        var fakeWorld = new FakeWorldService();
        var mockEvent = new Mock<IEventService>();
        var mockLogger = new Mock<ILoggerWrapper>();

        var tracker = CreateTracker(
            mockProgression.Object,
            mockLogger.Object,
            mockEvent.Object,
            fakeWorld,
            mockFavor.Object);

        tracker.Initialize();

        var method = typeof(StoneFavorTracker).GetMethod("HandleVoxelsChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Act - Try to spam actions within cooldown
        fakeWorld.SetElapsedMilliseconds(1000);
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 }); // Combo 1 (awarded)

        fakeWorld.SetElapsedMilliseconds(2000); // Only 1 second later (< 3s cooldown)
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 }); // Should be blocked

        fakeWorld.SetElapsedMilliseconds(5000); // 4 seconds later (> 3s cooldown)
        method!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 }); // Combo 2 (awarded)

        // Assert - Only 2 actions should be awarded (spam blocked by cooldown)
        mockFavor.Verify(f => f.AwardFavorForAction(
            It.IsAny<IServerPlayer>(), It.IsAny<string>(), It.IsAny<float>()), Times.Exactly(2));
    }

    [Fact]
    public void SessionDecayCheck_RemovesExpiredSessions()
    {
        // Arrange
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        mockPlayer.Setup(p => p.PlayerName).Returns("TestPlayer");

        var mockProgression = new Mock<IPlayerProgressionDataManager>();
        mockProgression.Setup(m => m.GetPlayerDeityType("player1"))
            .Returns(DeityDomain.Stone);

        var mockFavor = new Mock<IFavorSystem>();
        var fakeWorld = new FakeWorldService();
        var mockEvent = new Mock<IEventService>();
        var mockLogger = new Mock<ILoggerWrapper>();

        var tracker = CreateTracker(
            mockProgression.Object,
            mockLogger.Object,
            mockEvent.Object,
            fakeWorld,
            mockFavor.Object);

        tracker.Initialize();

        var voxelsMethod = typeof(StoneFavorTracker).GetMethod("HandleVoxelsChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var decayMethod = typeof(StoneFavorTracker).GetMethod("OnSessionDecayCheck",
            BindingFlags.Instance | BindingFlags.NonPublic);

        // Act - Create a session
        fakeWorld.SetElapsedMilliseconds(1000);
        voxelsMethod!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 });

        // Advance time by 95 seconds (exceeds 90s decay timeout)
        fakeWorld.SetElapsedMilliseconds(96000);

        // Manually trigger decay check
        decayMethod!.Invoke(tracker, new object[] { 0f });

        // Try to chisel again - should start fresh session with combo 1 (1.0x multiplier)
        fakeWorld.SetElapsedMilliseconds(97000);
        voxelsMethod!.Invoke(tracker, new object[] { mockPlayer.Object, new BlockPos(0, 0, 0), 100 });

        // Assert - Should have two awards with 1.0x multiplier (session was cleaned up and restarted)
        mockFavor.Verify(f => f.AwardFavorForAction(
            mockPlayer.Object, "Stone Carving", 2f), Times.Exactly(2)); // 100 × 0.02 × 1.0 = 2f
    }
}
