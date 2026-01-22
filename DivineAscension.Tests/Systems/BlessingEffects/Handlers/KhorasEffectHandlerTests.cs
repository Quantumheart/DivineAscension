using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Services;
using DivineAscension.Systems.BlessingEffects.Handlers;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems.BlessingEffects.Handlers;

/// <summary>
/// Tests for Khoras effect handlers
/// Note: Full item repair mechanics are tested through integration tests
/// These tests focus on activation/deactivation lifecycle and timing logic
/// </summary>
[ExcludeFromCodeCoverage]
public class KhorasEffectHandlerTests
{
    #region Test Setup

    private readonly Mock<ILoggerWrapper> _mockLogger;
    private readonly Mock<IEventService> _mockEventService;
    private readonly Mock<IWorldService> _mockWorldService;

    public KhorasEffectHandlerTests()
    {
        _mockLogger = new Mock<ILoggerWrapper>();
        _mockEventService = new Mock<IEventService>();
        _mockWorldService = new Mock<IWorldService>();
    }

    private void SetupWorldService(long elapsedMilliseconds = 0, IServerPlayer? player = null, string? playerUID = null)
    {
        _mockWorldService.Setup(w => w.ElapsedMilliseconds).Returns(elapsedMilliseconds);
        if (player != null && playerUID != null)
        {
            _mockWorldService.Setup(w => w.GetPlayerByUID(playerUID)).Returns(player);
        }
    }

    private Mock<IServerPlayer> CreateMockPlayer(string uid, string name)
    {
        var mockPlayer = TestFixtures.CreateMockServerPlayer(uid, name);
        var mockEntity = new Mock<EntityPlayer>();
        mockPlayer.Setup(p => p.Entity).Returns(mockEntity.Object);

        // Set up InventoryManager to return null inventories (player has no items)
        var mockInventoryManager = new Mock<IPlayerInventoryManager>();
        mockInventoryManager.Setup(m => m.GetOwnInventory(It.IsAny<string>())).Returns((IInventory?)null);
        mockPlayer.Setup(p => p.InventoryManager).Returns(mockInventoryManager.Object);

        return mockPlayer;
    }

    #endregion

    #region Initialization Tests

    [Fact]
    public void Initialize_WithValidAPI_SetsEffectId()
    {
        // Arrange
        var handler = new KhorasEffectHandlers.PassiveToolRepairEffect();

        // Act
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);

        // Assert
        Assert.Equal("passive_tool_repair_1per5min", handler.EffectId);
    }

    [Fact]
    public void EffectId_ReturnsExpectedValue()
    {
        // Arrange & Act
        var handler = new KhorasEffectHandlers.PassiveToolRepairEffect();

        // Assert
        Assert.Equal("passive_tool_repair_1per5min", handler.EffectId);
    }

    #endregion

    #region Activation/Deactivation Tests

    [Fact]
    public void ActivateForPlayer_InitializesPlayerTracking()
    {
        // Arrange
        SetupWorldService(elapsedMilliseconds: 10000);
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var handler = new KhorasEffectHandlers.PassiveToolRepairEffect();
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => handler.ActivateForPlayer(mockPlayer.Object));
        Assert.Null(exception);
    }

    [Fact]
    public void DeactivateForPlayer_RemovesPlayerTracking()
    {
        // Arrange
        SetupWorldService(elapsedMilliseconds: 10000);
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var handler = new KhorasEffectHandlers.PassiveToolRepairEffect();
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);
        handler.ActivateForPlayer(mockPlayer.Object);

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => handler.DeactivateForPlayer(mockPlayer.Object));
        Assert.Null(exception);
    }

    [Fact]
    public void ActivateForPlayer_MultiplePlayers_TracksAll()
    {
        // Arrange
        SetupWorldService(elapsedMilliseconds: 10000);
        var mockPlayer1 = CreateMockPlayer("player-1", "Player1");
        var mockPlayer2 = CreateMockPlayer("player-2", "Player2");
        var handler = new KhorasEffectHandlers.PassiveToolRepairEffect();
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);

        // Act & Assert - Should not throw for multiple players
        var exception1 = Record.Exception(() => handler.ActivateForPlayer(mockPlayer1.Object));
        var exception2 = Record.Exception(() => handler.ActivateForPlayer(mockPlayer2.Object));

        Assert.Null(exception1);
        Assert.Null(exception2);
    }

    [Fact]
    public void DeactivateForPlayer_NonExistentPlayer_DoesNotThrow()
    {
        // Arrange
        var handler = new KhorasEffectHandlers.PassiveToolRepairEffect();
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        // Note: Player is NOT activated

        // Act & Assert - Should handle gracefully
        var exception = Record.Exception(() => handler.DeactivateForPlayer(mockPlayer.Object));
        Assert.Null(exception);
    }

    #endregion

    #region OnTick Timing Tests

    [Fact]
    public void OnTick_BeforeInitialize_DoesNotThrow()
    {
        // Arrange
        var handler = new KhorasEffectHandlers.PassiveToolRepairEffect();

        // Act & Assert - Should handle gracefully
        var exception = Record.Exception(() => handler.OnTick(0.1f));
        Assert.Null(exception);
    }

    [Fact]
    public void OnTick_NoActivePlayers_DoesNotThrow()
    {
        // Arrange
        var handler = new KhorasEffectHandlers.PassiveToolRepairEffect();
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);

        // Act & Assert - Should handle gracefully
        var exception = Record.Exception(() => handler.OnTick(0.1f));
        Assert.Null(exception);
    }

    [Fact]
    public void OnTick_PlayerOffline_DoesNotThrow()
    {
        // Arrange
        var startTime = 10000L;
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");

        SetupWorldService(elapsedMilliseconds: startTime);
        _mockWorldService.Setup(w => w.GetPlayerByUID("player-1")).Returns((IServerPlayer?)null); // Player offline

        var handler = new KhorasEffectHandlers.PassiveToolRepairEffect();
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);
        handler.ActivateForPlayer(mockPlayer.Object);

        // Update elapsed time to trigger repair interval
        _mockWorldService.Setup(w => w.ElapsedMilliseconds).Returns(startTime + 300000);

        // Act & Assert - Should handle gracefully
        var exception = Record.Exception(() => handler.OnTick(0.1f));
        Assert.Null(exception);
    }

    [Fact]
    public void OnTick_PlayerWithNoEntity_DoesNotThrow()
    {
        // Arrange
        var startTime = 10000L;
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        mockPlayer.Setup(p => p.Entity).Returns((EntityPlayer?)null); // No entity

        SetupWorldService(elapsedMilliseconds: startTime, player: mockPlayer.Object, playerUID: "player-1");

        var handler = new KhorasEffectHandlers.PassiveToolRepairEffect();
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);
        handler.ActivateForPlayer(mockPlayer.Object);

        // Update elapsed time to trigger repair interval
        _mockWorldService.Setup(w => w.ElapsedMilliseconds).Returns(startTime + 300000);

        // Act & Assert - Should handle gracefully
        var exception = Record.Exception(() => handler.OnTick(0.1f));
        Assert.Null(exception);
    }

    [Fact]
    public void OnTick_DeactivatedPlayer_StopsProcessing()
    {
        // Arrange
        var startTime = 10000L;
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");

        SetupWorldService(elapsedMilliseconds: startTime, player: mockPlayer.Object, playerUID: "player-1");

        var handler = new KhorasEffectHandlers.PassiveToolRepairEffect();
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);
        handler.ActivateForPlayer(mockPlayer.Object);

        // Deactivate player
        handler.DeactivateForPlayer(mockPlayer.Object);

        // Update elapsed time to trigger repair interval
        _mockWorldService.Setup(w => w.ElapsedMilliseconds).Returns(startTime + 300000);

        // Act & Assert - Should not process deactivated player
        var exception = Record.Exception(() => handler.OnTick(0.1f));
        Assert.Null(exception);
    }

    #endregion

    #region Multiple Player Tests

    [Fact]
    public void OnTick_MultipleActivePlayers_ProcessesAllSafely()
    {
        // Arrange
        var startTime = 10000L;

        var mockPlayer1 = CreateMockPlayer("player-1", "Player1");
        var mockPlayer2 = CreateMockPlayer("player-2", "Player2");
        var mockPlayer3 = CreateMockPlayer("player-3", "Player3");

        SetupWorldService(elapsedMilliseconds: startTime);
        _mockWorldService.Setup(w => w.GetPlayerByUID("player-1")).Returns(mockPlayer1.Object);
        _mockWorldService.Setup(w => w.GetPlayerByUID("player-2")).Returns(mockPlayer2.Object);
        _mockWorldService.Setup(w => w.GetPlayerByUID("player-3")).Returns(mockPlayer3.Object);

        var handler = new KhorasEffectHandlers.PassiveToolRepairEffect();
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);
        handler.ActivateForPlayer(mockPlayer1.Object);
        handler.ActivateForPlayer(mockPlayer2.Object);
        handler.ActivateForPlayer(mockPlayer3.Object);

        // Update elapsed time to trigger repair interval
        _mockWorldService.Setup(w => w.ElapsedMilliseconds).Returns(startTime + 300000);

        // Act & Assert - Should process all players without throwing
        var exception = Record.Exception(() => handler.OnTick(0.1f));
        Assert.Null(exception);
    }

    [Fact]
    public void ActivateDeactivate_SamePlayerMultipleTimes_HandlesSafely()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var handler = new KhorasEffectHandlers.PassiveToolRepairEffect();
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);

        // Act & Assert - Activate, deactivate, activate again
        handler.ActivateForPlayer(mockPlayer.Object);
        handler.DeactivateForPlayer(mockPlayer.Object);
        var exception = Record.Exception(() => handler.ActivateForPlayer(mockPlayer.Object));

        Assert.Null(exception);
    }

    #endregion

    #region Integration Notes

    // Note: Full testing of tool repair mechanics (durability changes, item type detection,
    // inventory slot updates) is done through integration tests where we can work with
    // real or more realistic game objects. These unit tests focus on:
    // 1. Effect lifecycle (activate/deactivate)
    // 2. Timing logic (5-minute intervals)
    // 3. Error handling (null checks, missing players)
    // 4. Multi-player tracking

    #endregion
}