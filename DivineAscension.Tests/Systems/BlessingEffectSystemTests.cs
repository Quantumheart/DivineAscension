using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Moq;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Systems;
using PantheonWars.Systems.Interfaces;
using PantheonWars.Tests.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Tests.Systems;

/// <summary>
///     Unit tests for BlessingEffectSystem
///     Tests stat modifier calculation, caching, and blessing management
/// </summary>
[ExcludeFromCodeCoverage]
public class BlessingEffectSystemTests
{
    private readonly BlessingEffectSystem _effectSystem;
    private readonly Mock<ICoreServerAPI> _mockAPI;
    private readonly Mock<IBlessingRegistry> _mockBlessingRegistry;
    private readonly Mock<IPlayerReligionDataManager> _mockPlayerReligionDataManager;
    private readonly Mock<IReligionManager> _mockReligionManager;

    public BlessingEffectSystemTests()
    {
        _mockAPI = TestFixtures.CreateMockServerAPI();
        _mockBlessingRegistry = new Mock<IBlessingRegistry>();
        _mockPlayerReligionDataManager = TestFixtures.CreateMockPlayerReligionDataManager();
        _mockReligionManager = new Mock<IReligionManager>();

        _effectSystem = new BlessingEffectSystem(
            _mockAPI.Object,
            _mockBlessingRegistry.Object,
            _mockPlayerReligionDataManager.Object,
            _mockReligionManager.Object
        );
    }

    #region RefreshPlayerBlessings Tests

    [Fact]
    public void RefreshPlayerBlessings_ClearsCachedModifiers()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid");
        playerData.UnlockBlessing("blessing1");

        var blessing = TestFixtures.CreateTestBlessing("blessing1", "Blessing 1");
        blessing.StatModifiers["walkspeed"] = 0.1f;

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        _mockBlessingRegistry.Setup(r => r.GetBlessing("blessing1")).Returns(blessing);

        var mockWorld = new Mock<IServerWorldAccessor>();
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.PlayerByUid("player-uid")).Returns((IServerPlayer)null!);

        // First call to populate cache
        _effectSystem.GetPlayerStatModifiers("player-uid");

        // Act - Refresh should clear cache
        _effectSystem.RefreshPlayerBlessings("player-uid");

        // Second call should fetch data again
        _effectSystem.GetPlayerStatModifiers("player-uid");

        // Assert - Should have fetched player data twice (once before refresh, once after)
        _mockPlayerReligionDataManager.Verify(m => m.GetOrCreatePlayerData("player-uid"), Times.Exactly(3));
    }

    #endregion

    #region ClearAllCaches Tests

    [Fact]
    public void ClearAllCaches_ClearsAllCachedModifiers()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid");
        playerData.UnlockBlessing("blessing1");

        var blessing = TestFixtures.CreateTestBlessing("blessing1", "Blessing 1");
        blessing.StatModifiers["walkspeed"] = 0.1f;

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        _mockBlessingRegistry.Setup(r => r.GetBlessing("blessing1")).Returns(blessing);

        // Populate cache
        _effectSystem.GetPlayerStatModifiers("player-uid");

        // Act
        _effectSystem.ClearAllCaches();

        // Get modifiers again - should fetch from manager again
        _effectSystem.GetPlayerStatModifiers("player-uid");

        // Assert - Should have been called twice (before and after clear)
        _mockPlayerReligionDataManager.Verify(m => m.GetOrCreatePlayerData("player-uid"), Times.Exactly(2));
    }

    #endregion

    #region ApplyBlessingsToPlayer Tests

    [Fact]
    public void ApplyBlessingsToPlayer_WithNullPlayer_LogsWarning()
    {
        // Act
        _effectSystem.ApplyBlessingsToPlayer(null!);

        // Assert
        var mockLogger = Mock.Get(_mockAPI.Object.Logger);
        mockLogger.Verify(
            l => l.Warning(It.Is<string>(s => s.Contains("entity") && s.Contains("null"))),
            Times.Once()
        );
    }

    // NOTE: ApplyBlessingsToPlayer tests removed - they require Entity.Stats mocking which is not possible
    // due to non-virtual properties. Consider integration tests if this functionality needs testing.

    #endregion

    #region RemoveBlessingsFromPlayer Tests

    [Fact]
    public void RemoveBlessingsFromPlayer_WithNullPlayer_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        var exception = Record.Exception(() => _effectSystem.GetType()
            .GetMethod("RemoveBlessingsFromPlayer", BindingFlags.NonPublic | BindingFlags.Instance)?
            .Invoke(_effectSystem, new object[] { null! }));

        Assert.Null(exception);
    }

    #endregion

    #region FormatStatName Tests

    [Theory]
    [InlineData("meleeWeaponsDamage", "Melee Damage")]
    [InlineData("rangedWeaponsDamage", "Ranged Damage")]
    [InlineData("meleeWeaponsSpeed", "Attack Speed")]
    [InlineData("walkspeed", "Walk Speed")]
    [InlineData("maxhealthExtraPoints", "Max Health")]
    [InlineData("healingeffectivness", "Health Regen")]
    [InlineData("unknown_stat", "unknown_stat")]
    public void FormatStatName_FormatsCorrectly(string statKey, string expected)
    {
        // Act - Use reflection to call internal method
        var method = _effectSystem.GetType().GetMethod("FormatStatName",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var result = method?.Invoke(_effectSystem, new object[] { statKey }) as string;

        // Assert
        Assert.Contains(expected, result ?? "", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Initialization Tests

    [Fact]
    public void Initialize_RegistersEventHandlers()
    {
        // Arrange
        var mockEventAPI = new Mock<IServerEventAPI>();
        _mockAPI.Setup(a => a.Event).Returns(mockEventAPI.Object);

        // Act
        _effectSystem.Initialize();

        // Assert
        mockEventAPI.VerifyAdd(e => e.PlayerJoin += It.IsAny<PlayerDelegate>(), Times.Once());
    }

    [Fact]
    public void Initialize_LogsNotification()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        _mockAPI.Setup(a => a.Logger).Returns(mockLogger.Object);

        // Act
        _effectSystem.Initialize();

        // Assert
        mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("Initializing") && s.Contains("Blessing"))),
            Times.Once()
        );
    }

    #endregion

    #region GetPlayerStatModifiers Tests

    [Fact]
    public void GetPlayerStatModifiers_WithNoUnlockedBlessings_ReturnsEmptyDictionary()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        // Act
        var modifiers = _effectSystem.GetPlayerStatModifiers("player-uid");

        // Assert
        Assert.Empty(modifiers);
    }

    [Fact]
    public void GetPlayerStatModifiers_WithUnlockedBlessings_ReturnsCombinedModifiers()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid");
        playerData.UnlockBlessing("blessing1");
        playerData.UnlockBlessing("blessing2");

        var blessing1 = TestFixtures.CreateTestBlessing("blessing1", "Blessing 1");
        blessing1.StatModifiers["walkspeed"] = 0.1f;
        blessing1.StatModifiers["maxhealthExtraPoints"] = 2.0f;

        var blessing2 = TestFixtures.CreateTestBlessing("blessing2", "Blessing 2");
        blessing2.StatModifiers["walkspeed"] = 0.05f;
        blessing2.StatModifiers["meleeWeaponsDamage"] = 0.15f;

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        _mockBlessingRegistry.Setup(r => r.GetBlessing("blessing1")).Returns(blessing1);
        _mockBlessingRegistry.Setup(r => r.GetBlessing("blessing2")).Returns(blessing2);

        // Act
        var modifiers = _effectSystem.GetPlayerStatModifiers("player-uid");

        // Assert
        Assert.Equal(3, modifiers.Count);
        Assert.Equal(0.15f, modifiers["walkspeed"]); // 0.1 + 0.05 combined
        Assert.Equal(2.0f, modifiers["maxhealthExtraPoints"]);
        Assert.Equal(0.15f, modifiers["meleeWeaponsDamage"]);
    }

    [Fact]
    public void GetPlayerStatModifiers_IgnoresReligionBlessings()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid");
        playerData.UnlockBlessing("religion_blessing");

        var religionBlessing = TestFixtures.CreateTestBlessing("religion_blessing", "Religion Blessing",
            DeityType.Khoras, BlessingKind.Religion);
        religionBlessing.StatModifiers["walkspeed"] = 0.2f;

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        _mockBlessingRegistry.Setup(r => r.GetBlessing("religion_blessing")).Returns(religionBlessing);

        // Act
        var modifiers = _effectSystem.GetPlayerStatModifiers("player-uid");

        // Assert
        Assert.Empty(modifiers); // Should not include religion blessings
    }

    [Fact]
    public void GetPlayerStatModifiers_UsesCaching()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid");
        playerData.UnlockBlessing("blessing1");

        var blessing = TestFixtures.CreateTestBlessing("blessing1", "Blessing 1");
        blessing.StatModifiers["walkspeed"] = 0.1f;

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        _mockBlessingRegistry.Setup(r => r.GetBlessing("blessing1")).Returns(blessing);

        // Act - Call twice
        var modifiers1 = _effectSystem.GetPlayerStatModifiers("player-uid");
        var modifiers2 = _effectSystem.GetPlayerStatModifiers("player-uid");

        // Assert - Should only fetch player data once due to caching
        _mockPlayerReligionDataManager.Verify(m => m.GetOrCreatePlayerData("player-uid"), Times.Once());
        Assert.Equal(modifiers1["walkspeed"], modifiers2["walkspeed"]);
    }

    #endregion

    #region GetReligionStatModifiers Tests

    [Fact]
    public void GetReligionStatModifiers_WithNoUnlockedBlessings_ReturnsEmptyDictionary()
    {
        // Arrange
        var religion = TestFixtures.CreateTestReligion("religion-uid");

        _mockReligionManager
            .Setup(m => m.GetReligion("religion-uid"))
            .Returns(religion);

        // Act
        var modifiers = _effectSystem.GetReligionStatModifiers("religion-uid");

        // Assert
        Assert.Empty(modifiers);
    }

    [Fact]
    public void GetReligionStatModifiers_WithUnlockedBlessings_ReturnsCombinedModifiers()
    {
        // Arrange
        var religion = TestFixtures.CreateTestReligion("religion-uid");
        religion.UnlockedBlessings["blessing1"] = true;
        religion.UnlockedBlessings["blessing2"] = true;

        var blessing1 =
            TestFixtures.CreateTestBlessing("blessing1", "Blessing 1", DeityType.Khoras, BlessingKind.Religion);
        blessing1.StatModifiers["meleeWeaponsDamage"] = 0.1f;

        var blessing2 =
            TestFixtures.CreateTestBlessing("blessing2", "Blessing 2", DeityType.Khoras, BlessingKind.Religion);
        blessing2.StatModifiers["meleeWeaponsDamage"] = 0.05f;
        blessing2.StatModifiers["rangedWeaponsDamage"] = 0.1f;

        _mockReligionManager
            .Setup(m => m.GetReligion("religion-uid"))
            .Returns(religion);

        _mockBlessingRegistry.Setup(r => r.GetBlessing("blessing1")).Returns(blessing1);
        _mockBlessingRegistry.Setup(r => r.GetBlessing("blessing2")).Returns(blessing2);

        // Act
        var modifiers = _effectSystem.GetReligionStatModifiers("religion-uid");

        // Assert
        Assert.Equal(3, modifiers.Count);
        Assert.Equal(0.15f, modifiers["meleeWeaponsDamage"]); // 0.1 + 0.05
        Assert.Equal(0.1f, modifiers["rangedWeaponsDamage"]);
    }

    [Fact]
    public void GetReligionStatModifiers_WithNonExistentReligion_ReturnsEmptyDictionary()
    {
        // Arrange
        _mockReligionManager
            .Setup(m => m.GetReligion("invalid-uid"))
            .Returns((ReligionData)null!);

        // Act
        var modifiers = _effectSystem.GetReligionStatModifiers("invalid-uid");

        // Assert
        Assert.Empty(modifiers);
    }

    #endregion

    #region GetCombinedStatModifiers Tests

    [Fact]
    public void GetCombinedStatModifiers_CombinesPlayerAndReligionModifiers()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Khoras, "religion-uid");
        playerData.UnlockBlessing("player_blessing");

        var religion = TestFixtures.CreateTestReligion("religion-uid");
        religion.UnlockedBlessings["religion_blessing"] = true;

        var playerBlessing = TestFixtures.CreateTestBlessing("player_blessing", "Player Blessing");
        playerBlessing.StatModifiers["walkspeed"] = 0.1f;
        playerBlessing.StatModifiers["meleeWeaponsDamage"] = 0.1f;

        var religionBlessing = TestFixtures.CreateTestBlessing("religion_blessing", "Religion Blessing",
            DeityType.Khoras, BlessingKind.Religion);
        religionBlessing.StatModifiers["walkspeed"] = 0.05f;
        religionBlessing.StatModifiers["maxhealthExtraPoints"] = 5.0f;

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        _mockReligionManager
            .Setup(m => m.GetReligion("religion-uid"))
            .Returns(religion);

        _mockBlessingRegistry.Setup(r => r.GetBlessing("player_blessing")).Returns(playerBlessing);
        _mockBlessingRegistry.Setup(r => r.GetBlessing("religion_blessing")).Returns(religionBlessing);

        // Act
        var modifiers = _effectSystem.GetCombinedStatModifiers("player-uid");

        // Assert
        Assert.Equal(3, modifiers.Count);
        Assert.Equal(0.15f, modifiers["walkspeed"]); // 0.1 + 0.05
        Assert.Equal(0.1f, modifiers["meleeWeaponsDamage"]);
        Assert.Equal(5.0f, modifiers["maxhealthExtraPoints"]);
    }

    [Fact]
    public void GetCombinedStatModifiers_WithoutReligion_ReturnsOnlyPlayerModifiers()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Khoras, null);
        playerData.UnlockBlessing("player_blessing");

        var playerBlessing = TestFixtures.CreateTestBlessing("player_blessing", "Player Blessing");
        playerBlessing.StatModifiers["walkspeed"] = 0.1f;

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        _mockBlessingRegistry.Setup(r => r.GetBlessing("player_blessing")).Returns(playerBlessing);

        // Act
        var modifiers = _effectSystem.GetCombinedStatModifiers("player-uid");

        // Assert
        Assert.Single(modifiers);
        Assert.Equal(0.1f, modifiers["walkspeed"]);
    }

    #endregion

    #region RefreshReligionBlessings Tests

    [Fact]
    public void RefreshReligionBlessings_ClearsCacheAndRefreshesAllMembers()
    {
        // Arrange
        var religion = TestFixtures.CreateTestReligion("religion-uid");
        religion.MemberUIDs.Add("member1");
        religion.MemberUIDs.Add("member2");

        var member1Data = TestFixtures.CreateTestPlayerReligionData("member1", DeityType.Khoras, "religion-uid");
        var member2Data = TestFixtures.CreateTestPlayerReligionData("member2", DeityType.Khoras, "religion-uid");

        _mockReligionManager
            .Setup(m => m.GetReligion("religion-uid"))
            .Returns(religion);

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("member1"))
            .Returns(member1Data);

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("member2"))
            .Returns(member2Data);

        var mockWorld = new Mock<IServerWorldAccessor>();
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.PlayerByUid(It.IsAny<string>())).Returns((IServerPlayer)null!);

        // Act
        _effectSystem.RefreshReligionBlessings("religion-uid");

        // Assert - Should attempt to get data for both members
        _mockPlayerReligionDataManager.Verify(m => m.GetOrCreatePlayerData("member1"), Times.Once());
        _mockPlayerReligionDataManager.Verify(m => m.GetOrCreatePlayerData("member2"), Times.Once());
    }

    [Fact]
    public void RefreshReligionBlessings_WithNonExistentReligion_DoesNotThrow()
    {
        // Arrange
        _mockReligionManager
            .Setup(m => m.GetReligion("invalid-uid"))
            .Returns((ReligionData)null!);

        // Act & Assert - Should not throw
        _effectSystem.RefreshReligionBlessings("invalid-uid");
    }

    #endregion

    #region GetActiveBlessings Tests

    [Fact]
    public void GetActiveBlessings_ReturnsPlayerAndReligionBlessings()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Khoras, "religion-uid");
        playerData.UnlockBlessing("player_blessing");

        var religion = TestFixtures.CreateTestReligion("religion-uid");
        religion.UnlockedBlessings["religion_blessing"] = true;

        var playerBlessing = TestFixtures.CreateTestBlessing("player_blessing", "Player Blessing");
        var religionBlessing = TestFixtures.CreateTestBlessing("religion_blessing", "Religion Blessing");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        _mockReligionManager
            .Setup(m => m.GetReligion("religion-uid"))
            .Returns(religion);

        _mockBlessingRegistry.Setup(r => r.GetBlessing("player_blessing")).Returns(playerBlessing);
        _mockBlessingRegistry.Setup(r => r.GetBlessing("religion_blessing")).Returns(religionBlessing);

        // Act
        var (playerBlessings, religionBlessings) = _effectSystem.GetActiveBlessings("player-uid");

        // Assert
        Assert.Single(playerBlessings);
        Assert.Single(religionBlessings);
        Assert.Equal("Player Blessing", playerBlessings[0].Name);
        Assert.Equal("Religion Blessing", religionBlessings[0].Name);
    }

    [Fact]
    public void GetActiveBlessings_WithoutReligion_ReturnsOnlyPlayerBlessings()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Khoras, null);
        playerData.UnlockBlessing("player_blessing");

        var playerBlessing = TestFixtures.CreateTestBlessing("player_blessing", "Player Blessing");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        _mockBlessingRegistry.Setup(r => r.GetBlessing("player_blessing")).Returns(playerBlessing);

        // Act
        var (playerBlessings, religionBlessings) = _effectSystem.GetActiveBlessings("player-uid");

        // Assert
        Assert.Single(playerBlessings);
        Assert.Empty(religionBlessings);
    }

    #endregion

    #region FormatStatModifiers Tests

    [Fact]
    public void FormatStatModifiers_WithNoModifiers_ReturnsNoActiveMessage()
    {
        // Arrange
        var modifiers = new Dictionary<string, float>();

        // Act
        var formatted = _effectSystem.FormatStatModifiers(modifiers);

        // Assert
        Assert.Contains("No active", formatted);
    }

    [Fact]
    public void FormatStatModifiers_FormatsPositiveModifiers()
    {
        // Arrange
        var modifiers = new Dictionary<string, float>
        {
            { "walkspeed", 0.15f },
            { "maxhealthExtraPoints", 0.10f }
        };

        // Act
        var formatted = _effectSystem.FormatStatModifiers(modifiers);

        // Assert
        Assert.Contains("+15.0%", formatted);
        Assert.Contains("+10.0%", formatted);
    }

    [Fact]
    public void FormatStatModifiers_FormatsNegativeModifiers()
    {
        // Arrange
        var modifiers = new Dictionary<string, float>
        {
            { "walkspeed", -0.10f }
        };

        // Act
        var formatted = _effectSystem.FormatStatModifiers(modifiers);

        // Assert
        Assert.Contains("-10.0%", formatted);
    }

    #endregion

    #region CombineModifiers Tests

    [Fact]
    public void CombineModifiers_AddsNewModifiers()
    {
        // Arrange
        var target = new Dictionary<string, float>
        {
            { "walkspeed", 0.1f }
        };

        var source = new Dictionary<string, float>
        {
            { "meleeWeaponsDamage", 0.15f }
        };

        // Act - Use reflection to call internal method
        var method = _effectSystem.GetType().GetMethod("CombineModifiers",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(_effectSystem, new object[] { target, source });

        // Assert
        Assert.Equal(2, target.Count);
        Assert.Equal(0.1f, target["walkspeed"]);
        Assert.Equal(0.15f, target["meleeWeaponsDamage"]);
    }

    [Fact]
    public void CombineModifiers_CombinesExistingModifiers()
    {
        // Arrange
        var target = new Dictionary<string, float>
        {
            { "walkspeed", 0.1f }
        };

        var source = new Dictionary<string, float>
        {
            { "walkspeed", 0.05f }
        };

        // Act - Use reflection to call internal method
        var method = _effectSystem.GetType().GetMethod("CombineModifiers",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(_effectSystem, new object[] { target, source });

        // Assert
        Assert.Single(target);
        Assert.Equal(0.15f, target["walkspeed"]); // 0.1 + 0.05 = 0.15
    }

    #endregion

    #region Event Handler Tests

    [Fact]
    public void OnPlayerJoin_RefreshesPlayerBlessings()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid");
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-uid", "TestPlayer");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        var mockWorld = new Mock<IServerWorldAccessor>();
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.PlayerByUid("player-uid")).Returns(mockPlayer.Object);

        // Act - Use reflection to call internal method
        var method = _effectSystem.GetType().GetMethod("OnPlayerJoin",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(_effectSystem, new object[] { mockPlayer.Object });

        // Assert - Should have accessed player data to refresh
        _mockPlayerReligionDataManager.Verify(m => m.GetOrCreatePlayerData("player-uid"), Times.AtLeastOnce());
    }

    [Fact]
    public void OnPlayerLeavesReligion_ClearsCacheAndRefreshes()
    {
        // Arrange
        var playerData = TestFixtures.CreateTestPlayerReligionData("player-uid", DeityType.Khoras, "religion-uid");
        var religion = TestFixtures.CreateTestReligion("religion-uid");
        var mockPlayer = TestFixtures.CreateMockServerPlayer("player-uid", "TestPlayer");

        _mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        _mockReligionManager
            .Setup(m => m.GetReligion("religion-uid"))
            .Returns(religion);

        var mockWorld = new Mock<IServerWorldAccessor>();
        _mockAPI.Setup(a => a.World).Returns(mockWorld.Object);
        mockWorld.Setup(w => w.PlayerByUid("player-uid")).Returns((IServerPlayer)null!);

        // Act - Use reflection to call internal method
        var method = _effectSystem.GetType().GetMethod("OnPlayerLeavesReligion",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(_effectSystem, new object[] { mockPlayer.Object, "religion-uid" });

        // Assert - Should access both player and religion data
        _mockPlayerReligionDataManager.Verify(m => m.GetOrCreatePlayerData("player-uid"), Times.AtLeastOnce());
        _mockReligionManager.Verify(m => m.GetReligion("religion-uid"), Times.AtLeastOnce());
    }

    [Fact]
    public void Initialize_RegistersPlayerLeavesReligionHandler()
    {
        // Arrange
        var mockEventAPI = new Mock<IServerEventAPI>();
        _mockAPI.Setup(a => a.Event).Returns(mockEventAPI.Object);

        // Act
        _effectSystem.Initialize();

        // Assert
        _mockPlayerReligionDataManager.VerifyAdd(
            m => m.OnPlayerLeavesReligion += It.IsAny<PlayerReligionDataManager.PlayerReligionDataChangedDelegate>(),
            Times.Once());
    }

    #endregion
}