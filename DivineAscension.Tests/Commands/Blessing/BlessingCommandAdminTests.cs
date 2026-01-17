using System.Diagnostics.CodeAnalysis;
using DivineAscension.Commands;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Commands.Blessing;

/// <summary>
/// Tests for admin blessing commands (unlock, lock, reset, unlockall)
/// </summary>
[ExcludeFromCodeCoverage]
public class BlessingCommandAdminTests
{
    private readonly Mock<IBlessingEffectSystem> _mockBlessingEffectSystem;
    private readonly Mock<IBlessingRegistry> _mockBlessingRegistry;
    private readonly Mock<IPlayerProgressionDataManager> _mockPlayerDataManager;
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly Mock<ICoreServerAPI> _mockSapi;
    private readonly Mock<IServerNetworkChannel> _mockServerChannel;
    private readonly BlessingCommands _sut;

    public BlessingCommandAdminTests()
    {
        _mockSapi = new Mock<ICoreServerAPI>();
        _mockBlessingRegistry = new Mock<IBlessingRegistry>();
        _mockPlayerDataManager = new Mock<IPlayerProgressionDataManager>();
        _mockReligionManager = new Mock<IReligionManager>();
        _mockBlessingEffectSystem = new Mock<IBlessingEffectSystem>();
        _mockServerChannel = new Mock<IServerNetworkChannel>();

        var mockLogger = new Mock<ILogger>();
        _mockSapi.Setup(s => s.Logger).Returns(mockLogger.Object);

        _sut = new BlessingCommands(
            _mockSapi.Object,
            _mockBlessingRegistry.Object,
            _mockPlayerDataManager.Object,
            _mockReligionManager.Object,
            _mockBlessingEffectSystem.Object,
            _mockServerChannel.Object);
    }

    #region Helper Methods

    private Mock<IServerPlayer> CreateMockPlayer(string uid, string name)
    {
        var mock = new Mock<IServerPlayer>();
        mock.Setup(p => p.PlayerUID).Returns(uid);
        mock.Setup(p => p.PlayerName).Returns(name);
        return mock;
    }

    private TextCommandCallingArgs CreateCommandArgs(IServerPlayer player)
    {
        return new TextCommandCallingArgs
        {
            LanguageCode = "en",
            Caller = new Caller
            {
                Type = EnumCallerType.Player,
                Player = player,
                CallerPrivileges = new[] { "chat", "root" },
                CallerRole = "admin",
                Pos = new Vec3d(0, 0, 0)
            },
            Parsers = new List<ICommandArgumentParser>()
        };
    }

    private void SetupParsers(TextCommandCallingArgs args, params object[] parsedValues)
    {
        args.Parsers.Clear();
        foreach (var value in parsedValues)
        {
            var mockParser = new Mock<ICommandArgumentParser>();
            mockParser.Setup(p => p.GetValue()).Returns(value);
            mockParser.Setup(p => p.ArgCount).Returns(1);
            args.Parsers.Add(mockParser.Object);
        }
    }

    #endregion

    #region /blessings admin unlock tests

    [Fact]
    public void OnAdminUnlock_PlayerBlessing_UnlocksSuccessfully()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var target = CreateMockPlayer("player-1", "Player");
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Craft);
        var religion = TestFixtures.CreateTestReligion("religion-1", "TestReligion", DeityDomain.Craft, "player-1");
        var blessing =
            TestFixtures.CreateTestBlessing("khoras_strength", "Strength", DeityDomain.Craft, BlessingKind.Player);

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "khoras_strength", "Player");

        // Setup mocks
        _mockSapi.Setup(s => s.World.AllPlayers).Returns(new[] { target.Object });
        _mockPlayerDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _mockReligionManager.Setup(m => m.GetPlayerActiveDeityDomain("player-1")).Returns(DeityDomain.Craft);
        _mockReligionManager.Setup(m => m.GetPlayerReligion("player-1")).Returns(religion);
        _mockBlessingRegistry.Setup(r => r.GetBlessing("khoras_strength")).Returns(blessing);

        // Act
        var result = _sut.OnAdminUnlock(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Force-unlocked Strength for Player", result.StatusMessage);
        Assert.Contains("khoras_strength", playerData.UnlockedBlessings);
        _mockBlessingEffectSystem.Verify(s => s.RefreshPlayerBlessings("player-1"), Times.Once);
    }

    [Fact]
    public void OnAdminUnlock_PlayerBlessing_TargetSelf()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var playerData = TestFixtures.CreateTestPlayerReligionData("admin-1", DeityDomain.Wild);
        var religion = TestFixtures.CreateTestReligion("religion-1", "TestReligion", DeityDomain.Wild, "admin-1");
        var blessing =
            TestFixtures.CreateTestBlessing("lysa_healing", "Healing", DeityDomain.Wild, BlessingKind.Player);

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "lysa_healing", null);

        // Setup mocks
        _mockPlayerDataManager.Setup(m => m.GetOrCreatePlayerData("admin-1")).Returns(playerData);
        _mockReligionManager.Setup(m => m.GetPlayerActiveDeityDomain("admin-1")).Returns(DeityDomain.Wild);
        _mockReligionManager.Setup(m => m.GetPlayerReligion("admin-1")).Returns(religion);
        _mockBlessingRegistry.Setup(r => r.GetBlessing("lysa_healing")).Returns(blessing);

        // Act
        var result = _sut.OnAdminUnlock(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("lysa_healing", playerData.UnlockedBlessings);
    }

    [Fact]
    public void OnAdminUnlock_BlessingNotFound_ReturnsError()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var target = CreateMockPlayer("player-1", "Player");
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Craft);

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "invalid_blessing", "Player");

        _mockSapi.Setup(s => s.World.AllPlayers).Returns(new[] { target.Object });
        _mockPlayerDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _mockReligionManager.Setup(m => m.GetPlayerActiveDeityDomain("player-1")).Returns(DeityDomain.Craft);
        _mockBlessingRegistry.Setup(r => r.GetBlessing("invalid_blessing"))
            .Returns((DivineAscension.Models.Blessing)null);

        // Act
        var result = _sut.OnAdminUnlock(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("not found", result.StatusMessage);
    }

    [Fact]
    public void OnAdminUnlock_AlreadyUnlocked_ReturnsSuccess()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var target = CreateMockPlayer("player-1", "Player");
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Craft);
        playerData.UnlockBlessing("khoras_strength");
        var religion = TestFixtures.CreateTestReligion("religion-1", "TestReligion", DeityDomain.Craft, "player-1");
        var blessing =
            TestFixtures.CreateTestBlessing("khoras_strength", "Strength", DeityDomain.Craft, BlessingKind.Player);

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "khoras_strength", "Player");

        _mockSapi.Setup(s => s.World.AllPlayers).Returns(new[] { target.Object });
        _mockPlayerDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _mockReligionManager.Setup(m => m.GetPlayerActiveDeityDomain("player-1")).Returns(DeityDomain.Craft);
        _mockReligionManager.Setup(m => m.GetPlayerReligion("player-1")).Returns(religion);
        _mockBlessingRegistry.Setup(r => r.GetBlessing("khoras_strength")).Returns(blessing);

        // Act
        var result = _sut.OnAdminUnlock(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("already", result.StatusMessage);
    }

    #endregion

    #region /blessings admin lock tests

    [Fact]
    public void OnAdminLock_PlayerBlessing_LocksSuccessfully()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var target = CreateMockPlayer("player-1", "Player");
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Craft);
        playerData.UnlockBlessing("khoras_strength");
        var religion = TestFixtures.CreateTestReligion("religion-1", "TestReligion", DeityDomain.Craft, "player-1");
        var blessing =
            TestFixtures.CreateTestBlessing("khoras_strength", "Strength", DeityDomain.Craft, BlessingKind.Player);

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "khoras_strength", "Player");

        _mockSapi.Setup(s => s.World.AllPlayers).Returns(new[] { target.Object });
        _mockPlayerDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _mockReligionManager.Setup(m => m.GetPlayerActiveDeityDomain("player-1")).Returns(DeityDomain.Craft);
        _mockReligionManager.Setup(m => m.GetPlayerReligion("player-1")).Returns(religion);
        _mockBlessingRegistry.Setup(r => r.GetBlessing("khoras_strength")).Returns(blessing);

        // Act
        var result = _sut.OnAdminLock(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Removed Strength from Player", result.StatusMessage);
        Assert.DoesNotContain("khoras_strength", playerData.UnlockedBlessings);
        _mockBlessingEffectSystem.Verify(s => s.RefreshPlayerBlessings("player-1"), Times.Once);
    }

    [Fact]
    public void OnAdminLock_NotUnlocked_ReturnsFriendlyMessage()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var target = CreateMockPlayer("player-1", "Player");
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Craft);
        var religion = TestFixtures.CreateTestReligion("religion-1", "TestReligion", DeityDomain.Craft, "player-1");
        var blessing =
            TestFixtures.CreateTestBlessing("khoras_strength", "Strength", DeityDomain.Craft, BlessingKind.Player);

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "khoras_strength", "Player");

        _mockSapi.Setup(s => s.World.AllPlayers).Returns(new[] { target.Object });
        _mockPlayerDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _mockReligionManager.Setup(m => m.GetPlayerActiveDeityDomain("player-1")).Returns(DeityDomain.Craft);
        _mockReligionManager.Setup(m => m.GetPlayerReligion("player-1")).Returns(religion);
        _mockBlessingRegistry.Setup(r => r.GetBlessing("khoras_strength")).Returns(blessing);

        // Act
        var result = _sut.OnAdminLock(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("doesn't have", result.StatusMessage);
    }

    #endregion

    #region /blessings admin reset tests

    [Fact]
    public void OnAdminReset_WithBlessings_ClearsAll()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var target = CreateMockPlayer("player-1", "Player");
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Craft);
        playerData.UnlockBlessing("khoras_strength");
        playerData.UnlockBlessing("khoras_defense");

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "Player");

        _mockSapi.Setup(s => s.World.AllPlayers).Returns(new[] { target.Object });
        _mockPlayerDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _mockReligionManager.Setup(m => m.GetPlayerActiveDeityDomain("player-1")).Returns(DeityDomain.Craft);

        // Act
        var result = _sut.OnAdminReset(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Reset all blessings for Player", result.StatusMessage);
        Assert.Empty(playerData.UnlockedBlessings);
        _mockBlessingEffectSystem.Verify(s => s.RefreshPlayerBlessings("player-1"), Times.Once);
    }

    [Fact]
    public void OnAdminReset_NoBlessings_ReturnsFriendlyMessage()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var target = CreateMockPlayer("player-1", "Player");
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Craft);

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "Player");

        _mockSapi.Setup(s => s.World.AllPlayers).Returns(new[] { target.Object });
        _mockPlayerDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _mockReligionManager.Setup(m => m.GetPlayerActiveDeityDomain("player-1")).Returns(DeityDomain.Craft);

        // Act
        var result = _sut.OnAdminReset(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("no blessings", result.StatusMessage);
    }

    #endregion

    #region /blessings admin unlockall tests

    [Fact]
    public void OnAdminUnlockAll_UnlocksAllPlayerBlessings()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var target = CreateMockPlayer("player-1", "Player");
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.Craft);
        var religion = TestFixtures.CreateTestReligion("religion-1", "TestReligion", DeityDomain.Craft, "player-1");

        var playerBlessings = new List<DivineAscension.Models.Blessing>
        {
            TestFixtures.CreateTestBlessing("khoras_strength", "Strength", DeityDomain.Craft, BlessingKind.Player),
            TestFixtures.CreateTestBlessing("khoras_defense", "Defense", DeityDomain.Craft, BlessingKind.Player)
        };

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "Player");

        _mockSapi.Setup(s => s.World.AllPlayers).Returns(new[] { target.Object });
        _mockPlayerDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _mockReligionManager.Setup(m => m.GetPlayerActiveDeityDomain("player-1")).Returns(DeityDomain.Craft);
        _mockReligionManager.Setup(m => m.GetPlayerReligion("player-1")).Returns(religion);
        _mockBlessingRegistry.Setup(r => r.GetBlessingsForDeity(DeityDomain.Craft, BlessingKind.Player))
            .Returns(playerBlessings);
        _mockBlessingRegistry.Setup(r => r.GetBlessingsForDeity(DeityDomain.Craft, BlessingKind.Religion))
            .Returns(new List<DivineAscension.Models.Blessing>());

        // Act
        var result = _sut.OnAdminUnlockAll(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Unlocked all 2 blessings for Player", result.StatusMessage);
        Assert.Contains("khoras_strength", playerData.UnlockedBlessings);
        Assert.Contains("khoras_defense", playerData.UnlockedBlessings);
        _mockBlessingEffectSystem.Verify(s => s.RefreshPlayerBlessings("player-1"), Times.Once);
    }

    [Fact]
    public void OnAdminUnlockAll_PlayerNotInReligion_ReturnsError()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var target = CreateMockPlayer("player-1", "Player");
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-1", DeityDomain.None);

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "Player");

        _mockSapi.Setup(s => s.World.AllPlayers).Returns(new[] { target.Object });
        _mockPlayerDataManager.Setup(m => m.GetOrCreatePlayerData("player-1")).Returns(playerData);
        _mockReligionManager.Setup(m => m.GetPlayerActiveDeityDomain("player-1")).Returns(DeityDomain.None);

        // Act
        var result = _sut.OnAdminUnlockAll(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("not in a religion", result.StatusMessage);
    }

    #endregion
}