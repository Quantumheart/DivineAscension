using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Unit tests for CooldownManager
///     Tests cooldown tracking, admin bypass, and cleanup functionality
/// </summary>
[ExcludeFromCodeCoverage]
public class CooldownManagerTests
{
    private readonly ModConfigData _config;
    private readonly CooldownManager _cooldownManager;
    private readonly FakeEventService _fakeEventService;
    private readonly FakeWorldService _fakeWorldService;
    private readonly Mock<ILogger> _mockLogger;

    public CooldownManagerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _fakeEventService = new FakeEventService();
        _fakeWorldService = new FakeWorldService();
        _config = new ModConfigData();

        _cooldownManager = new CooldownManager(_mockLogger.Object, _fakeEventService, _fakeWorldService, _config);
    }

    #region ClearPlayerCooldowns Tests

    [Fact]
    public void ClearPlayerCooldowns_RemovesAllCooldowns()
    {
        // Arrange
        const string playerUID = "test-player";
        _cooldownManager.RecordOperation(playerUID, CooldownType.ReligionDeletion);
        _cooldownManager.RecordOperation(playerUID, CooldownType.MemberKick);
        _cooldownManager.RecordOperation(playerUID, CooldownType.Invite);

        // Act
        _cooldownManager.ClearPlayerCooldowns(playerUID);

        // Assert
        Assert.Equal(0.0, _cooldownManager.GetRemainingCooldown(playerUID, CooldownType.ReligionDeletion));
        Assert.Equal(0.0, _cooldownManager.GetRemainingCooldown(playerUID, CooldownType.MemberKick));
        Assert.Equal(0.0, _cooldownManager.GetRemainingCooldown(playerUID, CooldownType.Invite));
        Assert.Equal(0, _cooldownManager.GetPlayerCount());
    }

    #endregion

    #region ClearSpecificCooldown Tests

    [Fact]
    public void ClearSpecificCooldown_RemovesOnlySpecifiedCooldown()
    {
        // Arrange
        const string playerUID = "test-player";
        _cooldownManager.RecordOperation(playerUID, CooldownType.ReligionDeletion);
        _cooldownManager.RecordOperation(playerUID, CooldownType.MemberKick);

        // Act
        _cooldownManager.ClearSpecificCooldown(playerUID, CooldownType.ReligionDeletion);

        // Assert
        Assert.Equal(0.0, _cooldownManager.GetRemainingCooldown(playerUID, CooldownType.ReligionDeletion));
        Assert.True(_cooldownManager.GetRemainingCooldown(playerUID, CooldownType.MemberKick) > 0);
    }

    #endregion

    #region GetActiveCooldownCount Tests

    [Fact]
    public void GetActiveCooldownCount_ReturnsCorrectCount()
    {
        // Arrange
        const string player1 = "player1";
        const string player2 = "player2";

        _cooldownManager.RecordOperation(player1, CooldownType.ReligionDeletion);
        _cooldownManager.RecordOperation(player1, CooldownType.MemberKick);
        _cooldownManager.RecordOperation(player2, CooldownType.Invite);

        // Act
        var count = _cooldownManager.GetActiveCooldownCount();

        // Assert
        Assert.Equal(3, count);
    }

    #endregion

    #region GetPlayerCount Tests

    [Fact]
    public void GetPlayerCount_ReturnsCorrectCount()
    {
        // Arrange
        _cooldownManager.RecordOperation("player1", CooldownType.ReligionDeletion);
        _cooldownManager.RecordOperation("player2", CooldownType.MemberKick);
        _cooldownManager.RecordOperation("player3", CooldownType.Invite);

        // Act
        var count = _cooldownManager.GetPlayerCount();

        // Assert
        Assert.Equal(3, count);
    }

    #endregion

    #region Cleanup Tests

    [Fact]
    public void CleanupExpiredCooldowns_RemovesExpiredCooldowns()
    {
        // Arrange
        const string player1 = "player1";
        const string player2 = "player2";

        _cooldownManager.RecordOperation(player1, CooldownType.Invite); // 2 second cooldown
        _cooldownManager.RecordOperation(player2, CooldownType.ReligionDeletion); // 60 second cooldown

        // Act
        _cooldownManager.Initialize();

        // Trigger cleanup manually by advancing time and calling the registered callback
        _fakeWorldService.SetElapsedMilliseconds(_fakeWorldService.ElapsedMilliseconds + 5000);
        _fakeEventService.TriggerPeriodicCallbacks(0); // Call all periodic callbacks

        // Assert
        // player1's Invite cooldown (2s) should be expired and removed
        Assert.Equal(0.0, _cooldownManager.GetRemainingCooldown(player1, CooldownType.Invite));
        // player2's ReligionDeletion cooldown (60s) should still be active
        Assert.True(_cooldownManager.GetRemainingCooldown(player2, CooldownType.ReligionDeletion) > 0);
        // player1 should be removed from tracking (no active cooldowns)
        Assert.Equal(1, _cooldownManager.GetPlayerCount());
    }

    #endregion

    #region Multiple Cooldown Types Tests

    [Fact]
    public void MultipleCooldownTypes_TrackedIndependently()
    {
        // Arrange
        const string playerUID = "test-player";

        // Act
        _cooldownManager.RecordOperation(playerUID, CooldownType.ReligionDeletion);
        _cooldownManager.RecordOperation(playerUID, CooldownType.MemberKick);
        _cooldownManager.RecordOperation(playerUID, CooldownType.Invite);

        // Assert
        Assert.True(_cooldownManager.GetRemainingCooldown(playerUID, CooldownType.ReligionDeletion) > 0);
        Assert.True(_cooldownManager.GetRemainingCooldown(playerUID, CooldownType.MemberKick) > 0);
        Assert.True(_cooldownManager.GetRemainingCooldown(playerUID, CooldownType.Invite) > 0);
        Assert.Equal(3, _cooldownManager.GetActiveCooldownCount());
    }

    #endregion

    #region CanPerformOperation Tests

    [Fact]
    public void CanPerformOperation_WithNoCooldown_AllowsOperation()
    {
        // Arrange
        const string playerUID = "test-player";

        // Act
        var allowed =
            _cooldownManager.CanPerformOperation(playerUID, CooldownType.ReligionDeletion, out var errorMessage);

        // Assert
        Assert.True(allowed);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void CanPerformOperation_WithActiveCooldown_DeniesOperation()
    {
        // Arrange
        const string playerUID = "test-player";
        _cooldownManager.RecordOperation(playerUID, CooldownType.ReligionDeletion);

        // Act
        var allowed =
            _cooldownManager.CanPerformOperation(playerUID, CooldownType.ReligionDeletion, out var errorMessage);

        // Assert
        Assert.False(allowed);
        Assert.NotNull(errorMessage);
        Assert.Contains("wait", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanPerformOperation_WithExpiredCooldown_AllowsOperation()
    {
        // Arrange
        const string playerUID = "test-player";
        _cooldownManager.RecordOperation(playerUID, CooldownType.MemberKick); // 5 second cooldown

        // Act - Advance time past cooldown
        _fakeWorldService.SetElapsedMilliseconds(_fakeWorldService.ElapsedMilliseconds + 6000); // 6 seconds
        var allowed = _cooldownManager.CanPerformOperation(playerUID, CooldownType.MemberKick, out var errorMessage);

        // Assert
        Assert.True(allowed);
        Assert.Null(errorMessage);
    }

    [Fact]
    public void CanPerformOperation_AdminPlayer_BypassesCooldown()
    {
        // Arrange
        const string adminUID = "admin-player";
        var mockAdminPlayer = new Mock<IServerPlayer>();
        mockAdminPlayer.Setup(p => p.PlayerUID).Returns(adminUID);
        mockAdminPlayer.Setup(p => p.PlayerName).Returns("Admin Player");
        mockAdminPlayer.Setup(p => p.Privileges).Returns(new[] { Privilege.root });
        _fakeWorldService.AddPlayer(mockAdminPlayer.Object);

        _cooldownManager.RecordOperation(adminUID, CooldownType.ReligionDeletion);

        // Act - Admin should bypass cooldown
        var allowed =
            _cooldownManager.CanPerformOperation(adminUID, CooldownType.ReligionDeletion, out var errorMessage);

        // Assert
        Assert.True(allowed);
        Assert.Null(errorMessage);
    }

    #endregion

    #region RecordOperation Tests

    [Fact]
    public void RecordOperation_SetsCooldown()
    {
        // Arrange
        const string playerUID = "test-player";

        // Act
        _cooldownManager.RecordOperation(playerUID, CooldownType.Invite);

        // Assert
        var remaining = _cooldownManager.GetRemainingCooldown(playerUID, CooldownType.Invite);
        Assert.True(remaining > 0);
    }

    [Fact]
    public void RecordOperation_AdminPlayer_DoesNotSetCooldown()
    {
        // Arrange
        const string adminUID = "admin-player";
        var mockAdminPlayer = new Mock<IServerPlayer>();
        mockAdminPlayer.Setup(p => p.PlayerUID).Returns(adminUID);
        mockAdminPlayer.Setup(p => p.PlayerName).Returns("Admin Player");
        mockAdminPlayer.Setup(p => p.Privileges).Returns(new[] { Privilege.root });
        _fakeWorldService.AddPlayer(mockAdminPlayer.Object);

        // Act
        _cooldownManager.RecordOperation(adminUID, CooldownType.ReligionDeletion);

        // Assert
        var remaining = _cooldownManager.GetRemainingCooldown(adminUID, CooldownType.ReligionDeletion);
        Assert.Equal(0.0, remaining);
    }

    #endregion

    #region TryPerformOperation Tests

    [Fact]
    public void TryPerformOperation_WithNoCooldown_SucceedsAndRecords()
    {
        // Arrange
        const string playerUID = "test-player";

        // Act
        var success = _cooldownManager.TryPerformOperation(playerUID, CooldownType.Proposal, out var errorMessage);

        // Assert
        Assert.True(success);
        Assert.Null(errorMessage);
        Assert.True(_cooldownManager.GetRemainingCooldown(playerUID, CooldownType.Proposal) > 0);
    }

    [Fact]
    public void TryPerformOperation_WithActiveCooldown_Fails()
    {
        // Arrange
        const string playerUID = "test-player";
        _cooldownManager.RecordOperation(playerUID, CooldownType.WarDeclaration);

        // Act
        var success =
            _cooldownManager.TryPerformOperation(playerUID, CooldownType.WarDeclaration, out var errorMessage);

        // Assert
        Assert.False(success);
        Assert.NotNull(errorMessage);
        Assert.Contains("wait", errorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region GetRemainingCooldown Tests

    [Fact]
    public void GetRemainingCooldown_NoCooldown_ReturnsZero()
    {
        // Arrange
        const string playerUID = "test-player";

        // Act
        var remaining = _cooldownManager.GetRemainingCooldown(playerUID, CooldownType.ReligionCreation);

        // Assert
        Assert.Equal(0.0, remaining);
    }

    [Fact]
    public void GetRemainingCooldown_ActiveCooldown_ReturnsCorrectValue()
    {
        // Arrange
        const string playerUID = "test-player";
        _cooldownManager.RecordOperation(playerUID, CooldownType.MemberBan); // 10 second cooldown

        // Act
        _fakeWorldService.SetElapsedMilliseconds(_fakeWorldService.ElapsedMilliseconds + 3000); // 3 seconds elapsed
        var remaining = _cooldownManager.GetRemainingCooldown(playerUID, CooldownType.MemberBan);

        // Assert
        Assert.True(remaining > 6.0 && remaining <= 7.0); // ~7 seconds remaining
    }

    [Fact]
    public void GetRemainingCooldown_ExpiredCooldown_ReturnsZero()
    {
        // Arrange
        const string playerUID = "test-player";
        _cooldownManager.RecordOperation(playerUID, CooldownType.Invite); // 2 second cooldown

        // Act
        _fakeWorldService.SetElapsedMilliseconds(_fakeWorldService.ElapsedMilliseconds + 3000); // 3 seconds elapsed
        var remaining = _cooldownManager.GetRemainingCooldown(playerUID, CooldownType.Invite);

        // Assert
        Assert.Equal(0.0, remaining);
    }

    #endregion

    #region GetCooldownDuration Tests

    [Fact]
    public void GetCooldownDuration_ReturnsCorrectDefaults()
    {
        // Arrange & Act & Assert
        Assert.Equal(60, _cooldownManager.GetCooldownDuration(CooldownType.ReligionDeletion));
        Assert.Equal(5, _cooldownManager.GetCooldownDuration(CooldownType.MemberKick));
        Assert.Equal(10, _cooldownManager.GetCooldownDuration(CooldownType.MemberBan));
        Assert.Equal(2, _cooldownManager.GetCooldownDuration(CooldownType.Invite));
        Assert.Equal(300, _cooldownManager.GetCooldownDuration(CooldownType.ReligionCreation));
        Assert.Equal(30, _cooldownManager.GetCooldownDuration(CooldownType.Proposal));
        Assert.Equal(60, _cooldownManager.GetCooldownDuration(CooldownType.WarDeclaration));
    }

    [Fact]
    public void GetCooldownDuration_RespectsCustomConfig()
    {
        // Arrange
        var customConfig = new ModConfigData
        {
            ReligionDeletionCooldown = 120,
            MemberKickCooldown = 10
        };
        var customManager = new CooldownManager(_mockLogger.Object, _fakeEventService, _fakeWorldService, customConfig);

        // Act & Assert
        Assert.Equal(120, customManager.GetCooldownDuration(CooldownType.ReligionDeletion));
        Assert.Equal(10, customManager.GetCooldownDuration(CooldownType.MemberKick));
    }

    #endregion

    #region Initialize and Dispose Tests

    [Fact]
    public void Initialize_RegistersCleanupCallback()
    {
        // Act
        _cooldownManager.Initialize();

        // Assert - Just verify it doesn't throw and logs correctly
        TestFixtures.VerifyLoggerNotification(_mockLogger, "Cooldown Manager initialized");
    }

    [Fact]
    public void Dispose_UnregistersCleanupCallback()
    {
        // Arrange
        _cooldownManager.Initialize();

        // Act & Assert - Just verify it doesn't throw
        var exception = Record.Exception(() => _cooldownManager.Dispose());
        Assert.Null(exception);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void CanPerformOperation_NullPlayerUID_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _cooldownManager.CanPerformOperation(null!, CooldownType.ReligionDeletion, out _));
    }

    [Fact]
    public void RecordOperation_NullPlayerUID_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _cooldownManager.RecordOperation(null!, CooldownType.ReligionDeletion));
    }

    [Fact]
    public void GetRemainingCooldown_NullPlayerUID_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _cooldownManager.GetRemainingCooldown(null!, CooldownType.ReligionDeletion));
    }

    [Fact]
    public void ClearPlayerCooldowns_NullPlayerUID_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _cooldownManager.ClearPlayerCooldowns(null!));
    }

    #endregion
}