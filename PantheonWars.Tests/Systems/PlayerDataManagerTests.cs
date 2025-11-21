using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Systems;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Tests.Systems;

[ExcludeFromCodeCoverage]
public class PlayerDataManagerTests
{
    #region Test Setup Helpers

    private Mock<ICoreServerAPI> CreateMockServerAPI()
    {
        var mockLogger = new Mock<ILogger>();
        var mockEvent = new Mock<IServerEventAPI>();
        var mockWorldManager = new Mock<IWorldManagerAPI>();
        var mockSaveGame = new Mock<ISaveGame>();

        mockWorldManager.Setup(w => w.SaveGame).Returns(mockSaveGame.Object);

        var mockApi = new Mock<ICoreServerAPI>();
        mockApi.Setup(a => a.Logger).Returns(mockLogger.Object);
        mockApi.Setup(a => a.Event).Returns(mockEvent.Object);
        mockApi.Setup(a => a.WorldManager).Returns(mockWorldManager.Object);

        return mockApi;
    }

    #endregion

    #region AddFavor Tests (Integer)

    [Fact]
    public void AddFavor_ShouldIncreaseFavor_ForPlayer()
    {
        // Given: Manager and player
        var mockApi = CreateMockServerAPI();
        var manager = new PlayerDataManager(mockApi.Object);
        var playerUID = "test-player";

        // When: Add favor
        manager.AddFavor(playerUID, 100, "Test reward");

        // Then: Player should have favor
        var playerData = manager.GetOrCreatePlayerData(playerUID);
        Assert.Equal(100, playerData.DivineFavor);
        Assert.Equal(100, playerData.TotalFavorEarned);
    }
    
    #endregion

    #region AddFractionalFavor Tests (Phase 1)

    [Fact]
    public void AddFractionalFavor_ShouldAccumulateFractionalAmount()
    {
        // Given: Manager and player
        var mockApi = CreateMockServerAPI();
        var manager = new PlayerDataManager(mockApi.Object);
        var playerUID = "test-player";

        // When: Add fractional favor
        manager.AddFractionalFavor(playerUID, 0.5f, "Passive devotion");

        // Then: Fractional favor should accumulate
        var playerData = manager.GetOrCreatePlayerData(playerUID);
        Assert.Equal(0, playerData.DivineFavor); // Not awarded yet
        Assert.Equal(0.5f, playerData.AccumulatedFractionalFavor, precision: 2);
    }

    [Fact]
    public void AddFractionalFavor_ShouldAwardFavor_WhenReachingOne()
    {
        // Given: Manager and player with existing fractional favor
        var mockApi = CreateMockServerAPI();
        var manager = new PlayerDataManager(mockApi.Object);
        var playerUID = "test-player";

        var playerData = manager.GetOrCreatePlayerData(playerUID);
        playerData.AccumulatedFractionalFavor = 0.7f;

        // When: Add fractional favor to cross 1.0
        manager.AddFractionalFavor(playerUID, 0.5f, "Passive devotion");

        // Then: Should award 1 favor, keep remainder
        Assert.Equal(1, playerData.DivineFavor);
        Assert.Equal(1, playerData.TotalFavorEarned);
        Assert.Equal(0.2f, playerData.AccumulatedFractionalFavor, precision: 2);
    }

    [Fact]
    public void AddFractionalFavor_ShouldHandleMultiplePlayers_Independently()
    {
        // Given: Manager with multiple players
        var mockApi = CreateMockServerAPI();
        var manager = new PlayerDataManager(mockApi.Object);
        var player1UID = "player-1";
        var player2UID = "player-2";

        // When: Add different fractional amounts to each
        manager.AddFractionalFavor(player1UID, 0.7f, "passive");
        manager.AddFractionalFavor(player2UID, 0.3f, "passive");

        // Then: Each player should have correct independent amounts
        var player1Data = manager.GetOrCreatePlayerData(player1UID);
        var player2Data = manager.GetOrCreatePlayerData(player2UID);

        Assert.Equal(0.7f, player1Data.AccumulatedFractionalFavor, precision: 2);
        Assert.Equal(0, player1Data.DivineFavor);

        Assert.Equal(0.3f, player2Data.AccumulatedFractionalFavor, precision: 2);
        Assert.Equal(0, player2Data.DivineFavor);
    }

    [Fact]
    public void AddFractionalFavor_ShouldNotInterfere_WithIntegerAddFavor()
    {
        // Given: Manager and player
        var mockApi = CreateMockServerAPI();
        var manager = new PlayerDataManager(mockApi.Object);
        var playerUID = "test-player";

        // When: Mix integer and fractional favor additions
        manager.AddFavor(playerUID, 50, "PvP kill");
        manager.AddFractionalFavor(playerUID, 0.6f, "Passive");
        manager.AddFavor(playerUID, 25, "Another kill");
        manager.AddFractionalFavor(playerUID, 0.5f, "Passive");

        // Then: Both should work correctly
        var playerData = manager.GetOrCreatePlayerData(playerUID);
        Assert.Equal(76, playerData.DivineFavor); // 50 + 25 + 1 (from 0.6 + 0.5)
        Assert.Equal(76, playerData.TotalFavorEarned);
        Assert.Equal(0.1f, playerData.AccumulatedFractionalFavor, precision: 2);
    }

    [Fact]
    public void AddFractionalFavor_OverTime_ShouldAccumulateCorrectly()
    {
        // Given: Manager and player, simulating passive favor ticks
        var mockApi = CreateMockServerAPI();
        var manager = new PlayerDataManager(mockApi.Object);
        var playerUID = "test-player";

        // When: Simulate 30 seconds of passive favor (0.04/sec base rate)
        for (int i = 0; i < 30; i++)
        {
            manager.AddFractionalFavor(playerUID, 0.04f, "Passive tick");
        }

        // Then: Should have accumulated 1.2 favor total (1 awarded, 0.2 remaining)
        var playerData = manager.GetOrCreatePlayerData(playerUID);
        Assert.Equal(1, playerData.DivineFavor); // 30 * 0.04 = 1.2, awards 1
        Assert.Equal(0.2f, playerData.AccumulatedFractionalFavor, precision: 2);
    }

    #endregion

    #region RemoveFavor Tests

    [Fact]
    public void RemoveFavor_ShouldDecreaseFavor_WhenSufficient()
    {
        // Given: Manager and player with favor
        var mockApi = CreateMockServerAPI();
        var manager = new PlayerDataManager(mockApi.Object);
        var playerUID = "test-player";

        manager.AddFavor(playerUID, 100, "Initial");

        // When: Remove favor
        var result = manager.RemoveFavor(playerUID, 50, "Ability cost");

        // Then: Should succeed
        Assert.True(result);
        var playerData = manager.GetOrCreatePlayerData(playerUID);
        Assert.Equal(50, playerData.DivineFavor);
    }

    [Fact]
    public void RemoveFavor_ShouldFail_WhenInsufficient()
    {
        // Given: Manager and player with insufficient favor
        var mockApi = CreateMockServerAPI();
        var manager = new PlayerDataManager(mockApi.Object);
        var playerUID = "test-player";

        manager.AddFavor(playerUID, 30, "Initial");

        // When: Try to remove more than available
        var result = manager.RemoveFavor(playerUID, 50, "Ability cost");

        // Then: Should fail and favor unchanged
        Assert.False(result);
        var playerData = manager.GetOrCreatePlayerData(playerUID);
        Assert.Equal(30, playerData.DivineFavor);
    }

    #endregion

    #region GetOrCreatePlayerData Tests

    [Fact]
    public void GetOrCreatePlayerData_ShouldCreateNew_WhenNotExists()
    {
        // Given: Manager
        var mockApi = CreateMockServerAPI();
        var manager = new PlayerDataManager(mockApi.Object);
        var playerUID = "new-player";

        // When: Get or create
        var playerData = manager.GetOrCreatePlayerData(playerUID);

        // Then: Should create new player data
        Assert.NotNull(playerData);
        Assert.Equal(playerUID, playerData.PlayerUID);
        Assert.Equal(0, playerData.DivineFavor);
    }

    [Fact]
    public void GetOrCreatePlayerData_ShouldReturnExisting_WhenExists()
    {
        // Given: Manager with existing player
        var mockApi = CreateMockServerAPI();
        var manager = new PlayerDataManager(mockApi.Object);
        var playerUID = "existing-player";

        var original = manager.GetOrCreatePlayerData(playerUID);
        original.DivineFavor = 100;

        // When: Get again
        var retrieved = manager.GetOrCreatePlayerData(playerUID);

        // Then: Should return same instance
        Assert.Same(original, retrieved);
        Assert.Equal(100, retrieved.DivineFavor);
    }

    #endregion

    #region HasDeity Tests

    [Fact]
    public void HasDeity_ShouldReturnFalse_WhenNoDeity()
    {
        // Given: Manager and player without deity
        var mockApi = CreateMockServerAPI();
        var manager = new PlayerDataManager(mockApi.Object);
        var playerUID = "test-player";

        // When: Check has deity
        var result = manager.HasDeity(playerUID);

        // Then: Should return false
        Assert.False(result);
    }

    [Fact]
    public void HasDeity_ShouldReturnTrue_WhenHasDeity()
    {
        // Given: Manager and player with deity
        var mockApi = CreateMockServerAPI();
        var manager = new PlayerDataManager(mockApi.Object);
        var playerUID = "test-player";

        var playerData = manager.GetOrCreatePlayerData(playerUID);
        playerData.DeityType = DeityType.Aethra;

        // When: Check has deity
        var result = manager.HasDeity(playerUID);

        // Then: Should return true
        Assert.True(result);
    }

    #endregion
}
