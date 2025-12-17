using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Unit tests for PlayerReligionDataManager
///     Tests player data management, favor tracking, and religion membership
/// </summary>
[ExcludeFromCodeCoverage]
public class PlayerReligionDataManagerTests
{
    private readonly PlayerReligionDataManager _dataManager;
    private readonly Mock<ICoreServerAPI> _mockAPI;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IReligionManager> _mockReligionManager;

    public PlayerReligionDataManagerTests()
    {
        _mockAPI = TestFixtures.CreateMockServerAPI();
        _mockLogger = new Mock<ILogger>();
        _mockAPI.Setup(a => a.Logger).Returns(_mockLogger.Object);

        _mockReligionManager = new Mock<IReligionManager>();

        _dataManager = new PlayerReligionDataManager(_mockAPI.Object, _mockReligionManager.Object);
    }

    #region Initialization Tests

    [Fact]
    public void Initialize_RegistersEventHandlers()
    {
        // Arrange
        var mockEventAPI = new Mock<IServerEventAPI>();
        _mockAPI.Setup(a => a.Event).Returns(mockEventAPI.Object);

        // Act
        _dataManager.Initialize();

        // Assert
        mockEventAPI.VerifyAdd(e => e.PlayerJoin += It.IsAny<PlayerDelegate>(), Times.Once());
        mockEventAPI.VerifyAdd(e => e.PlayerDisconnect += It.IsAny<PlayerDelegate>(), Times.Once());
        mockEventAPI.VerifyAdd(e => e.SaveGameLoaded += It.IsAny<Action>(), Times.Once());
        mockEventAPI.VerifyAdd(e => e.GameWorldSave += It.IsAny<Action>(), Times.Once());
    }

    [Fact]
    public void Initialize_LogsNotification()
    {
        // Arrange
        var mockEventAPI = new Mock<IServerEventAPI>();
        _mockAPI.Setup(a => a.Event).Returns(mockEventAPI.Object);

        // Act
        _dataManager.Initialize();

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("Initializing") && s.Contains("Player Religion Data Manager"))),
            Times.Once()
        );
    }

    #endregion

    #region GetOrCreatePlayerData Tests

    [Fact]
    public void GetOrCreatePlayerData_CreatesNewData_WhenNotExists()
    {
        // Act
        var data = _dataManager.GetOrCreatePlayerData("new-player-uid");

        // Assert
        Assert.NotNull(data);
        Assert.Equal("new-player-uid", data.PlayerUID);
        Assert.Equal(DeityType.None, data.ActiveDeity);
        Assert.Equal(0, data.Favor);
    }

    [Fact]
    public void GetOrCreatePlayerData_ReturnsExistingData_WhenExists()
    {
        // Arrange
        var firstCall = _dataManager.GetOrCreatePlayerData("player-uid");
        firstCall.Favor = 100;

        // Act
        var secondCall = _dataManager.GetOrCreatePlayerData("player-uid");

        // Assert
        Assert.Same(firstCall, secondCall);
        Assert.Equal(100, secondCall.Favor);
    }

    [Fact]
    public void GetOrCreatePlayerData_LogsDebugOnCreation()
    {
        // Act
        _dataManager.GetOrCreatePlayerData("new-player-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s =>
                s.Contains("Created new player religion data") && s.Contains("new-player-uid"))),
            Times.Once()
        );
    }

    #endregion

    #region AddFavor Tests

    [Fact]
    public void AddFavor_IncreasesFavorAmount()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.Favor = 50;

        // Act
        _dataManager.AddFavor("player-uid", 25, "Test reason");

        // Assert
        Assert.Equal(75, data.Favor);
        Assert.Equal(25, data.TotalFavorEarned); // Total also increased
    }

    [Fact]
    public void AddFavor_WithReason_LogsDebugMessage()
    {
        // Act
        _dataManager.AddFavor("player-uid", 10, "PvP kill");

        // Assert
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("player-uid") && s.Contains("10") && s.Contains("PvP kill"))),
            Times.Once()
        );
    }

    [Fact]
    public void AddFavor_ThatCausesRankUp_FiresEvent()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.TotalFavorEarned = 490; // Close to Disciple threshold (500)

        var eventFired = false;
        _dataManager.OnPlayerDataChanged += (playerUID) => eventFired = true;

        var mockWorld = new Mock<IServerWorldAccessor>();
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        // Act
        _dataManager.AddFavor("player-uid", 20); // Should rank up

        // Assert
        Assert.Equal(FavorRank.Disciple, data.FavorRank);
        Assert.True(eventFired);
    }

    [Fact]
    public void AddFavor_FiresOnPlayerDataChangedEvent()
    {
        // Arrange
        string? firedPlayerUID = null;
        _dataManager.OnPlayerDataChanged += (playerUID) => firedPlayerUID = playerUID;

        // Act
        _dataManager.AddFavor("player-uid", 10);

        // Assert
        Assert.Equal("player-uid", firedPlayerUID);
    }

    #endregion

    #region AddFractionalFavor Tests

    [Fact]
    public void AddFractionalFavor_AccumulatesCorrectly()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");

        // Act
        _dataManager.AddFractionalFavor("player-uid", 0.3f);
        _dataManager.AddFractionalFavor("player-uid", 0.3f);
        _dataManager.AddFractionalFavor("player-uid", 0.5f); // Should award 1 favor

        // Assert
        Assert.Equal(1, data.Favor);
        Assert.True(data.AccumulatedFractionalFavor < 1.0f);
    }

    [Fact]
    public void AddFractionalFavor_WhenAwardingFullFavor_FiresEvent()
    {
        // Arrange
        var eventFired = false;
        _dataManager.OnPlayerDataChanged += (_) => eventFired = true;

        // Act - Add enough fractional favor to award 1 full favor
        _dataManager.AddFractionalFavor("player-uid", 1.2f);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void AddFractionalFavor_WithoutAwardingFullFavor_DoesNotFireEvent()
    {
        // Arrange
        var eventFired = false;
        _dataManager.OnPlayerDataChanged += (_) => eventFired = true;

        // Act - Add fractional favor that doesn't reach 1.0
        _dataManager.AddFractionalFavor("player-uid", 0.3f);

        // Assert
        Assert.False(eventFired);
    }

    #endregion

    #region RemoveFavor Tests

    [Fact]
    public void RemoveFavor_DecreasesFavorAmount()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.Favor = 50;

        // Act
        var success = _dataManager.RemoveFavor("player-uid", 20, "Blessing unlock");

        // Assert
        Assert.True(success);
        Assert.Equal(30, data.Favor);
    }

    [Fact]
    public void RemoveFavor_WithInsufficientFavor_ReturnsFalse()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.Favor = 10;

        // Act
        var success = _dataManager.RemoveFavor("player-uid", 20);

        // Assert
        Assert.False(success);
        Assert.Equal(10, data.Favor); // Unchanged
    }

    [Fact]
    public void RemoveFavor_OnSuccess_FiresEvent()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.Favor = 50;

        var eventFired = false;
        _dataManager.OnPlayerDataChanged += (_) => eventFired = true;

        // Act
        _dataManager.RemoveFavor("player-uid", 10);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void RemoveFavor_WithReason_LogsDebugMessage()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.Favor = 50;

        // Act
        _dataManager.RemoveFavor("player-uid", 10, "Blessing unlock");

        // Assert
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s =>
                s.Contains("player-uid") && s.Contains("spent 10 favor") && s.Contains("Blessing unlock"))),
            Times.Once()
        );
    }

    #endregion

    #region UpdateFavorRank Tests

    [Fact]
    public void UpdateFavorRank_UpdatesRankCorrectly()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.TotalFavorEarned = 500; // Should be Disciple rank

        // Act
        _dataManager.UpdateFavorRank("player-uid");

        // Assert
        Assert.Equal(FavorRank.Disciple, data.FavorRank);
    }

    [Fact]
    public void UpdateFavorRank_WithRankChange_LogsNotification()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.TotalFavorEarned = 500;
        data.FavorRank = FavorRank.Initiate;

        // Act
        _dataManager.UpdateFavorRank("player-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("rank changed") && s.Contains("Initiate") && s.Contains("Disciple"))),
            Times.Once()
        );
    }

    #endregion

    #region UnlockPlayerBlessing Tests

    [Fact]
    public void UnlockPlayerBlessing_UnlocksNewBlessing_ReturnsTrue()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");

        // Act
        var result = _dataManager.UnlockPlayerBlessing("player-uid", "blessing_id");

        // Assert
        Assert.True(result);
        Assert.True(data.IsBlessingUnlocked("blessing_id"));
    }

    [Fact]
    public void UnlockPlayerBlessing_AlreadyUnlocked_ReturnsFalse()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.UnlockBlessing("blessing_id");

        // Act
        var result = _dataManager.UnlockPlayerBlessing("player-uid", "blessing_id");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UnlockPlayerBlessing_LogsNotification()
    {
        // Act
        _dataManager.UnlockPlayerBlessing("player-uid", "blessing_id");

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("player-uid") && s.Contains("unlocked blessing") && s.Contains("blessing_id"))),
            Times.Once()
        );
    }

    #endregion

    #region GetActivePlayerBlessings Tests

    [Fact]
    public void GetActivePlayerBlessings_ReturnsUnlockedBlessings()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.UnlockBlessing("blessing1");
        data.UnlockBlessing("blessing2");

        // Act
        var blessings = _dataManager.GetActivePlayerBlessings("player-uid");

        // Assert
        Assert.Equal(2, blessings.Count);
        Assert.Contains("blessing1", blessings);
        Assert.Contains("blessing2", blessings);
    }

    [Fact]
    public void GetActivePlayerBlessings_WithNoUnlockedBlessings_ReturnsEmptyList()
    {
        // Act
        var blessings = _dataManager.GetActivePlayerBlessings("player-uid");

        // Assert
        Assert.Empty(blessings);
    }

    #endregion

    #region JoinReligion Tests

    [Fact]
    public void JoinReligion_SetsReligionAndDeity()
    {
        // Arrange
        var religion = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityType.Khoras);
        _mockReligionManager.Setup(m => m.GetReligion("religion-uid")).Returns(religion);

        var data = _dataManager.GetOrCreatePlayerData("player-uid");

        // Act
        _dataManager.JoinReligion("player-uid", "religion-uid");

        // Assert
        Assert.Equal("religion-uid", data.ReligionUID);
        Assert.Equal(DeityType.Khoras, data.ActiveDeity);
        Assert.NotNull(data.LastReligionSwitch);
    }

    [Fact]
    public void JoinReligion_AddsPlayerToReligion()
    {
        // Arrange
        var religion = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityType.Khoras);
        _mockReligionManager.Setup(m => m.GetReligion("religion-uid")).Returns(religion);

        // Act
        _dataManager.JoinReligion("player-uid", "religion-uid");

        // Assert
        _mockReligionManager.Verify(m => m.AddMember("religion-uid", "player-uid"), Times.Once());
    }

    [Fact]
    public void JoinReligion_WithInvalidReligion_LogsError()
    {
        // Arrange
        _mockReligionManager.Setup(m => m.GetReligion("invalid-uid")).Returns((ReligionData)null!);

        // Act
        _dataManager.JoinReligion("player-uid", "invalid-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Error(It.Is<string>(s => s.Contains("Cannot join non-existent religion"))),
            Times.Once()
        );
    }

    #endregion

    #region LeaveReligion Tests

    [Fact]
    public void LeaveReligion_ClearsPlayerData()
    {
        // Arrange
        var religion = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityType.Khoras);
        _mockReligionManager.Setup(m => m.GetReligion("religion-uid")).Returns(religion);

        var mockWorld = new Mock<IServerWorldAccessor>();
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        _dataManager.JoinReligion("player-uid", "religion-uid");
        data.Favor = 100;
        int count = 0;
        _dataManager.OnPlayerLeavesReligion += (player, uid) => count++;

        // Act
        _dataManager.LeaveReligion("player-uid");

        // Assert
        Assert.Null(data.ReligionUID);
        Assert.Equal(DeityType.None, data.ActiveDeity);
        Assert.Equal(0, data.Favor);
        Assert.Equal(0, data.TotalFavorEarned);
        Assert.Equal(FavorRank.Initiate, data.FavorRank);
    }

    [Fact]
    public void LeaveReligion_RemovesPlayerFromReligion()
    {
        // Arrange
        var religion = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityType.Khoras);
        _mockReligionManager.Setup(m => m.GetReligion("religion-uid")).Returns(religion);

        var mockWorld = new Mock<IServerWorldAccessor>();
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        _dataManager.JoinReligion("player-uid", "religion-uid");
        int count = 0;
        _dataManager.OnPlayerLeavesReligion += (player, uid) => count++;

        // Act
        _dataManager.LeaveReligion("player-uid");

        // Assert
        _mockReligionManager.Verify(m => m.RemoveMember("religion-uid", "player-uid"), Times.Once());
    }

    [Fact]
    public void LeaveReligion_WithNoReligion_DoesNothing()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        Assert.Null(data.ReligionUID);

        // Act & Assert - Should not throw
        _dataManager.LeaveReligion("player-uid");
    }

    #endregion

    #region HandleReligionSwitch Tests

    [Fact]
    public void HandleReligionSwitch_AppliesPenalty()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.Favor = 100;
        data.UnlockBlessing("blessing1");

        // Act
        _dataManager.HandleReligionSwitch("player-uid");

        // Assert
        Assert.Equal(0, data.Favor);
        Assert.Empty(data.UnlockedBlessings.Where(kvp => kvp.Value)); // All blessings locked
    }

    [Fact]
    public void HandleReligionSwitch_LogsNotification()
    {
        // Act
        _dataManager.HandleReligionSwitch("player-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("Applying religion switch penalty") && s.Contains("player-uid"))),
            Times.Once()
        );
    }

    #endregion

    #region Event Firing Tests

    [Fact]
    public void LeaveReligion_FiresOnPlayerLeavesReligionEvent()
    {
        // Arrange
        var religion = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityType.Khoras);
        _mockReligionManager.Setup(m => m.GetReligion("religion-uid")).Returns(religion);

        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        mockWorld.Setup(w => w.PlayerByUid("player-uid")).Returns(mockPlayer.Object);
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        _dataManager.JoinReligion("player-uid", "religion-uid");

        IServerPlayer? capturedPlayer = null;
        string? capturedReligionUID = null;
        _dataManager.OnPlayerLeavesReligion += (player, uid) =>
        {
            capturedPlayer = player;
            capturedReligionUID = uid;
        };

        // Act
        _dataManager.LeaveReligion("player-uid");

        // Assert
        Assert.NotNull(capturedPlayer);
        Assert.Equal("player-uid", capturedPlayer!.PlayerUID);
        Assert.Equal("religion-uid", capturedReligionUID);
    }

    [Fact]
    public void JoinReligion_LogsNotificationWithReligionName()
    {
        // Arrange
        var religion = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityType.Khoras);
        _mockReligionManager.Setup(m => m.GetReligion("religion-uid")).Returns(religion);

        // Act
        _dataManager.JoinReligion("player-uid", "religion-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("player-uid") && s.Contains("joined religion") && s.Contains("Test Religion"))),
            Times.Once()
        );
    }

    [Fact]
    public void LeaveReligion_LogsNotification()
    {
        // Arrange
        var religion = TestFixtures.CreateTestReligion("religion-uid", "Test Religion", DeityType.Khoras);
        _mockReligionManager.Setup(m => m.GetReligion("religion-uid")).Returns(religion);

        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");
        mockWorld.Setup(w => w.PlayerByUid("player-uid")).Returns(mockPlayer.Object);
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        // Subscribe to the event to prevent null reference
        _dataManager.OnPlayerLeavesReligion += (player, uid) => { };

        _dataManager.JoinReligion("player-uid", "religion-uid");

        // Act
        _dataManager.LeaveReligion("player-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("player-uid") && s.Contains("left religion"))),
            Times.Once()
        );
    }

    #endregion

    #region Rank-up Behavior Tests

    [Fact]
    public void AddFavorRankUp_WithNullPlayer_DoesNotCrash()
    {
        // Arrange
        var mockWorld = new Mock<IServerWorldAccessor>();
        mockWorld.Setup(w => w.PlayerByUid("player-uid")).Returns((IServerPlayer)null!);
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.TotalFavorEarned = 490; // Close to Disciple threshold (500)

        // Act & Assert - Should not throw
        _dataManager.AddFavor("player-uid", 20);

        // Verify rank changed
        Assert.Equal(FavorRank.Disciple, data.FavorRank);
    }

    [Fact]
    public void AddFractionalFavor_ThatCausesRankUp_UpdatesRank()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.TotalFavorEarned = 495; // Close to Disciple threshold (500)

        var mockWorld = new Mock<IServerWorldAccessor>();
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        // Act
        _dataManager.AddFractionalFavor("player-uid", 10.5f); // Should award 10 favor and rank up

        // Assert
        Assert.Equal(FavorRank.Disciple, data.FavorRank);
    }

    #endregion

    #region IsBlessingUnlocked Tests

    [Fact]
    public void IsBlessingUnlocked_WithUnlockedBlessing_ReturnsTrue()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.UnlockBlessing("blessing_id");

        // Act
        var isUnlocked = data.IsBlessingUnlocked("blessing_id");

        // Assert
        Assert.True(isUnlocked);
    }

    [Fact]
    public void IsBlessingUnlocked_WithLockedBlessing_ReturnsFalse()
    {
        // Arrange
        var data = _dataManager.GetOrCreatePlayerData("player-uid");

        // Act
        var isUnlocked = data.IsBlessingUnlocked("blessing_id");

        // Assert
        Assert.False(isUnlocked);
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public void SavePlayerData_WithExistingData_SavesSuccessfully()
    {
        // Arrange
        var mockWorldManager = new Mock<IWorldManagerAPI>();
        var mockSaveGame = new Mock<ISaveGame>();
        _mockAPI.Setup(a => a.WorldManager).Returns(mockWorldManager.Object);
        mockWorldManager.Setup(w => w.SaveGame).Returns(mockSaveGame.Object);

        var data = _dataManager.GetOrCreatePlayerData("player-uid");
        data.Favor = 100;
        data.ActiveDeity = DeityType.Khoras;

        byte[]? savedData = null;
        mockSaveGame
            .Setup(s => s.StoreData(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Callback<string, byte[]>((key, bytes) => savedData = bytes);

        // Act
        _dataManager.SavePlayerData("player-uid");

        // Assert
        Assert.NotNull(savedData);
        mockSaveGame.Verify(s => s.StoreData("divineascension_playerreligiondata_player-uid", It.IsAny<byte[]>()),
            Times.Once());
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("Saved religion data") && s.Contains("player-uid"))),
            Times.Once()
        );
    }

    [Fact]
    public void SavePlayerData_WithNonExistentPlayer_DoesNotSave()
    {
        // Arrange
        var mockWorldManager = new Mock<IWorldManagerAPI>();
        var mockSaveGame = new Mock<ISaveGame>();
        _mockAPI.Setup(a => a.WorldManager).Returns(mockWorldManager.Object);
        mockWorldManager.Setup(w => w.SaveGame).Returns(mockSaveGame.Object);

        // Act
        _dataManager.SavePlayerData("non-existent-uid");

        // Assert
        mockSaveGame.Verify(s => s.StoreData(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never());
    }

    [Fact]
    public void SavePlayerData_WhenExceptionOccurs_LogsError()
    {
        // Arrange
        var mockWorldManager = new Mock<IWorldManagerAPI>();
        var mockSaveGame = new Mock<ISaveGame>();
        _mockAPI.Setup(a => a.WorldManager).Returns(mockWorldManager.Object);
        mockWorldManager.Setup(w => w.SaveGame).Returns(mockSaveGame.Object);

        _dataManager.GetOrCreatePlayerData("player-uid");

        mockSaveGame
            .Setup(s => s.StoreData(It.IsAny<string>(), It.IsAny<byte[]>()))
            .Throws(new Exception("Save failed"));

        // Act
        _dataManager.SavePlayerData("player-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Error(It.Is<string>(s => s.Contains("Failed to save") && s.Contains("player-uid"))),
            Times.Once()
        );
    }

    [Fact]
    public void LoadPlayerData_WithExistingData_LoadsSuccessfully()
    {
        // Arrange
        var mockWorldManager = new Mock<IWorldManagerAPI>();
        var mockSaveGame = new Mock<ISaveGame>();
        _mockAPI.Setup(a => a.WorldManager).Returns(mockWorldManager.Object);
        mockWorldManager.Setup(w => w.SaveGame).Returns(mockSaveGame.Object);

        var savedData = new PlayerReligionData("player-uid")
        {
            Favor = 150,
            ActiveDeity = DeityType.Lysa,
            TotalFavorEarned = 200
        };
        var serialized = SerializerUtil.Serialize(savedData);

        mockSaveGame
            .Setup(s => s.GetData("divineascension_playerreligiondata_player-uid"))
            .Returns(serialized);

        // Act
        _dataManager.LoadPlayerData("player-uid");

        // Assert
        var loadedData = _dataManager.GetOrCreatePlayerData("player-uid");
        Assert.Equal(150, loadedData.Favor);
        Assert.Equal(DeityType.Lysa, loadedData.ActiveDeity);
        Assert.Equal(200, loadedData.TotalFavorEarned);
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("Loaded religion data") && s.Contains("player-uid"))),
            Times.Once()
        );
    }

    [Fact]
    public void LoadPlayerData_WithNoData_DoesNotCreateData()
    {
        // Arrange
        var mockWorldManager = new Mock<IWorldManagerAPI>();
        var mockSaveGame = new Mock<ISaveGame>();
        _mockAPI.Setup(a => a.WorldManager).Returns(mockWorldManager.Object);
        mockWorldManager.Setup(w => w.SaveGame).Returns(mockSaveGame.Object);

        mockSaveGame
            .Setup(s => s.GetData(It.IsAny<string>()))
            .Returns((byte[]?)null);

        // Act
        _dataManager.LoadPlayerData("player-uid");

        // Assert - Should not log anything when no data exists
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s => s.Contains("Loaded religion data"))),
            Times.Never()
        );
    }

    [Fact]
    public void LoadPlayerData_WhenExceptionOccurs_LogsError()
    {
        // Arrange
        var mockWorldManager = new Mock<IWorldManagerAPI>();
        var mockSaveGame = new Mock<ISaveGame>();
        _mockAPI.Setup(a => a.WorldManager).Returns(mockWorldManager.Object);
        mockWorldManager.Setup(w => w.SaveGame).Returns(mockSaveGame.Object);

        mockSaveGame
            .Setup(s => s.GetData(It.IsAny<string>()))
            .Throws(new Exception("Load failed"));

        // Act
        _dataManager.LoadPlayerData("player-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Error(It.Is<string>(s => s.Contains("Failed to load") && s.Contains("player-uid"))),
            Times.Once()
        );
    }

    [Fact]
    public void SaveAllPlayerData_SavesAllPlayers()
    {
        // Arrange
        var mockWorldManager = new Mock<IWorldManagerAPI>();
        var mockSaveGame = new Mock<ISaveGame>();
        _mockAPI.Setup(a => a.WorldManager).Returns(mockWorldManager.Object);
        mockWorldManager.Setup(w => w.SaveGame).Returns(mockSaveGame.Object);

        _dataManager.GetOrCreatePlayerData("player-1");
        _dataManager.GetOrCreatePlayerData("player-2");
        _dataManager.GetOrCreatePlayerData("player-3");

        // Act
        _dataManager.SaveAllPlayerData();

        // Assert
        mockSaveGame.Verify(s => s.StoreData(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Exactly(3));
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("Saving all player religion data"))),
            Times.Once()
        );
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("Saved religion data for 3 players"))),
            Times.Once()
        );
    }

    [Fact]
    public void LoadAllPlayerData_LogsNotification()
    {
        // Act
        _dataManager.LoadAllPlayerData();

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("Loading all player religion data"))),
            Times.Once()
        );
    }

    #endregion

    #region Event Handler Tests

    [Fact]
    public void OnPlayerJoin_LoadsPlayerData()
    {
        // Arrange
        var mockWorldManager = new Mock<IWorldManagerAPI>();
        var mockSaveGame = new Mock<ISaveGame>();
        _mockAPI.Setup(a => a.WorldManager).Returns(mockWorldManager.Object);
        mockWorldManager.Setup(w => w.SaveGame).Returns(mockSaveGame.Object);

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        var savedData = new PlayerReligionData("player-uid") { Favor = 50 };
        var serialized = SerializerUtil.Serialize(savedData);

        mockSaveGame
            .Setup(s => s.GetData("divineascension_playerreligiondata_player-uid"))
            .Returns(serialized);

        // Act
        _dataManager.OnPlayerJoin(mockPlayer.Object);

        // Assert
        var loadedData = _dataManager.GetOrCreatePlayerData("player-uid");
        Assert.Equal(50, loadedData.Favor);
    }

    [Fact]
    public void OnSaveGameLoaded_CallsLoadAllPlayerData()
    {
        // Act
        _dataManager.OnSaveGameLoaded();

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("Loading all player religion data"))),
            Times.Once()
        );
    }

    [Fact]
    public void OnGameWorldSave_SavesAllPlayerData()
    {
        // Arrange
        var mockWorldManager = new Mock<IWorldManagerAPI>();
        var mockSaveGame = new Mock<ISaveGame>();
        _mockAPI.Setup(a => a.WorldManager).Returns(mockWorldManager.Object);
        mockWorldManager.Setup(w => w.SaveGame).Returns(mockSaveGame.Object);

        _dataManager.GetOrCreatePlayerData("player-1");
        _dataManager.GetOrCreatePlayerData("player-2");

        // Act
        _dataManager.OnGameWorldSave();

        // Assert
        mockSaveGame.Verify(s => s.StoreData(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Exactly(2));
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("Saved religion data for 2 players"))),
            Times.Once()
        );
    }

    #endregion
}