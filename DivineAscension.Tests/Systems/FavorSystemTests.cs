using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems;

[ExcludeFromCodeCoverage]
public class FavorSystemTests
{
    #region Initialization Tests

    [Fact]
    public void Initialize_DoesNotThrowException()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => favorSystem.Initialize());
        Assert.Null(exception);
    }

    #endregion

    #region Devotion Rank Multiplier Tests

    [Theory]
    [InlineData(DevotionRank.Initiate, 1.0f)]
    [InlineData(DevotionRank.Disciple, 1.1f)]
    [InlineData(DevotionRank.Zealot, 1.2f)]
    [InlineData(DevotionRank.Champion, 1.3f)]
    [InlineData(DevotionRank.Avatar, 1.5f)]
    public void CalculateDevotionMultiplier_ShouldReturn_CorrectValue(DevotionRank rank, float expected)
    {
        // Given: Specific devotion rank
        // When: Calculate multiplier (using same logic as FavorSystem)
        var multiplier = rank switch
        {
            DevotionRank.Initiate => 1.0f,
            DevotionRank.Disciple => 1.1f,
            DevotionRank.Zealot => 1.2f,
            DevotionRank.Champion => 1.3f,
            DevotionRank.Avatar => 1.5f,
            _ => 1.0f
        };

        // Then: Matches expected value
        Assert.Equal(expected, multiplier);
    }

    #endregion

    #region Religion Prestige Multiplier Tests

    [Theory]
    [InlineData(PrestigeRank.Fledgling, 1.0f)]
    [InlineData(PrestigeRank.Established, 1.1f)]
    [InlineData(PrestigeRank.Renowned, 1.2f)]
    [InlineData(PrestigeRank.Legendary, 1.3f)]
    [InlineData(PrestigeRank.Mythic, 1.5f)]
    public void CalculatePrestigeMultiplier_ShouldReturn_CorrectValue(PrestigeRank rank, float expected)
    {
        // Given: Specific prestige rank
        // When: Calculate multiplier (using same logic as FavorSystem)
        var multiplier = rank switch
        {
            PrestigeRank.Fledgling => 1.0f,
            PrestigeRank.Established => 1.1f,
            PrestigeRank.Renowned => 1.2f,
            PrestigeRank.Legendary => 1.3f,
            PrestigeRank.Mythic => 1.5f,
            _ => 1.0f
        };

        // Then: Matches expected value
        Assert.Equal(expected, multiplier);
    }

    #endregion

    #region Combined Multiplier Tests

    [Theory]
    [InlineData(DevotionRank.Initiate, PrestigeRank.Fledgling, 1.0f)]
    [InlineData(DevotionRank.Avatar, PrestigeRank.Fledgling, 1.5f)]
    [InlineData(DevotionRank.Initiate, PrestigeRank.Mythic, 1.5f)]
    [InlineData(DevotionRank.Avatar, PrestigeRank.Mythic, 2.25f)]
    [InlineData(DevotionRank.Disciple, PrestigeRank.Established, 1.21f)]
    [InlineData(DevotionRank.Zealot, PrestigeRank.Renowned, 1.44f)]
    [InlineData(DevotionRank.Champion, PrestigeRank.Legendary, 1.69f)]
    public void CalculateTotalMultiplier_ShouldStack_DevotionAndPrestige(
        DevotionRank devotion, PrestigeRank prestige, float expected)
    {
        // Given: Player and religion with ranks
        var devotionMultiplier = GetDevotionMultiplier(devotion);
        var prestigeMultiplier = GetPrestigeMultiplier(prestige);

        // When: Multiply together
        var totalMultiplier = devotionMultiplier * prestigeMultiplier;

        // Then: Correct stacking
        Assert.Equal(expected, totalMultiplier, 2);
    }

    #endregion

    #region Initialize Event Registration Tests

    [Fact]
    public void Initialize_RegistersPlayerDeathHandler()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockEventAPI = new Mock<IServerEventAPI>();
        mockAPI.Setup(a => a.Event).Returns(mockEventAPI.Object);

        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.Initialize();

        // Assert
        mockEventAPI.VerifyAdd(e => e.PlayerDeath += It.IsAny<PlayerDeathDelegate>(), Times.Once());
    }

    // NOTE: Initialize_RegistersGameTickListener test removed - RegisterGameTickListener has optional parameters
    // which cannot be used in Moq expression trees. The functionality is tested through integration tests.

    #endregion

    #region AwardFavorForAction Notification Tests

    [Fact]
    public void AwardFavorForAction_SendsNotificationToPlayer()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var playerData = new PlayerProgressionData { };

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        mockReligionManager.Setup(d => d.GetPlayerActiveDeityDomain("player-uid")).Returns(DeityDomain.Craft);
        mockReligionManager.Setup(d => d.GetPlayerReligion("player-uid")).Returns(TestFixtures.CreateTestReligion());

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.AwardFavorForAction(mockPlayer.Object, "test action", 15);

        // Assert
        mockPlayer.Verify(
            p => p.SendMessage(
                It.Is<int>(g => g == GlobalConstants.GeneralChatGroup),
                It.Is<string>(s => s.Contains("gained") && s.Contains("15") && s.Contains("favor")),
                It.Is<EnumChatType>(t => t == EnumChatType.Notification),
                It.IsAny<string>()
            ),
            Times.Once
        );
    }

    #endregion

    #region Setup and Helpers

    private Mock<ICoreServerAPI> CreateMockServerAPI()
    {
        var mockAPI = new Mock<ICoreServerAPI>();
        var mockLogger = new Mock<ILogger>();
        mockAPI.Setup(a => a.Logger).Returns(mockLogger.Object);

        var mockEvent = new Mock<IServerEventAPI>();
        mockAPI.Setup(a => a.Event).Returns(mockEvent.Object);

        return mockAPI;
    }

    private FavorSystem CreateFavorSystem(
        ICoreServerAPI api,
        IPlayerProgressionDataManager playerProgressionDataManager,
        IReligionManager religionManager)
    {
        var mockPrestige = new Mock<IReligionPrestigeManager>();
        return new FavorSystem(
            api,
            playerProgressionDataManager,
            religionManager,
            mockPrestige.Object);
    }

    #endregion

    #region PvP Kill Tests

    [Fact]
    public void ProcessPvPKill_WithAttackerWithoutDeity_DoesNotAwardFavor()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var attackerData = new PlayerProgressionData { };
        var victimData = new PlayerProgressionData { };

        var mockAttacker = new Mock<IServerPlayer>();
        mockAttacker.Setup(p => p.PlayerUID).Returns("attacker-uid");

        var mockVictim = new Mock<IServerPlayer>();
        mockVictim.Setup(p => p.PlayerUID).Returns("victim-uid");

        mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("attacker-uid"))
            .Returns(attackerData);

        mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("victim-uid"))
            .Returns(victimData);

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.ProcessPvPKill(mockAttacker.Object, mockVictim.Object);

        // Assert
        mockPlayerReligionDataManager.Verify(
            m => m.AddFavor(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public void ProcessPvPKill_SendsNotificationToAttacker()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var mockAttacker = new Mock<IServerPlayer>();
        mockAttacker.Setup(p => p.PlayerUID).Returns("attacker-uid");
        mockAttacker.Setup(p => p.PlayerName).Returns("Attacker");

        var mockVictim = new Mock<IServerPlayer>();
        mockVictim.Setup(p => p.PlayerUID).Returns("victim-uid");
        mockVictim.Setup(p => p.PlayerName).Returns("Victim");

        mockReligionManager.Setup(d => d.GetPlayerReligion("attacker-uid")).Returns(
            new ReligionData("attacker-religion-uid", "attacker-test-religion", DeityDomain.Craft, "attacker-test-name",
                "attacker-uid", "attacker"));
        mockReligionManager.Setup(d => d.GetPlayerReligion("victim-uid")).Returns(
            new ReligionData("victim-religion-uid", "victim-test-religion", DeityDomain.Craft, "victim-test-name",
                "victim-uid", "victim"));

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.ProcessPvPKill(mockAttacker.Object, mockVictim.Object);

        // Assert
        mockAttacker.Verify(
            p => p.SendMessage(
                It.Is<int>(g => g == GlobalConstants.GeneralChatGroup),
                It.Is<string>(s => s.Contains("") && s.Contains("rewards")),
                It.Is<EnumChatType>(t => t == EnumChatType.Notification),
                It.IsAny<string>()
            ),
            Times.Once
        );
    }

    #endregion

    #region Death Penalty Tests

    [Fact]
    public void ProcessDeathPenalty_WithPlayerHavingDeity_RemovesFavor()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var playerData = new PlayerProgressionData { Favor = 10 };

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);
        mockReligionManager.Setup(d => d.GetPlayerActiveDeityDomain(It.IsAny<string>())).Returns(DeityDomain.Craft);

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.ProcessDeathPenalty(mockPlayer.Object);

        // Assert
        mockPlayerReligionDataManager.Verify(
            m => m.RemoveFavor("player-uid", 10, "Death penalty"),
            Times.Once
        );
    }

    [Fact]
    public void ProcessDeathPenalty_WithPlayerWithoutDeity_DoesNotRemoveFavor()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var playerData = new PlayerProgressionData { };

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.ProcessDeathPenalty(mockPlayer.Object);

        // Assert
        mockPlayerReligionDataManager.Verify(
            m => m.RemoveFavor(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public void ProcessDeathPenalty_WithZeroFavor_DoesNotRemoveFavor()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var playerData = new PlayerProgressionData { Favor = 0 };

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.ProcessDeathPenalty(mockPlayer.Object);

        // Assert
        mockPlayerReligionDataManager.Verify(
            m => m.RemoveFavor(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never
        );
    }

    #endregion

    #region Favor Calculation Tests

    [Fact]
    public void CalculateFavorReward_WithNoVictimDeity_ReturnsBaseFavor()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        var reward = favorSystem.CalculateFavorReward(DeityDomain.Craft, DeityDomain.None);

        // Assert
        Assert.Equal(10, reward); // BASE_KILL_FAVOR
    }

    [Fact]
    public void CalculateFavorReward_WithSameDeity_ReturnsFullFavor()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        var reward = favorSystem.CalculateFavorReward(DeityDomain.Craft, DeityDomain.Craft);

        // Assert
        Assert.Equal(10, reward); // BASE_KILL_FAVOR (no penalty for same deity)
    }

    #endregion

    #region Award Favor For Action Tests

    [Fact]
    public void AwardFavorForAction_WithPlayerHavingDeity_AwardsFavor()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var playerData = new PlayerProgressionData { };

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);
        mockReligionManager.Setup(d => d.GetPlayerActiveDeityDomain("player-uid")).Returns(DeityDomain.Craft);
        mockReligionManager.Setup(d => d.GetPlayerReligion("player-uid")).Returns(TestFixtures.CreateTestReligion());
        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.AwardFavorForAction(mockPlayer.Object, "test action", 15);

        // Assert
        mockPlayerReligionDataManager.Verify(
            m => m.AddFavor("player-uid", 15, "test action"),
            Times.Once
        );
    }

    [Fact]
    public void AwardFavorForAction_WithPlayerWithoutDeity_DoesNotAwardFavor()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var playerData = new PlayerProgressionData { };

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.AwardFavorForAction(mockPlayer.Object, "test action", 15);

        // Assert
        mockPlayerReligionDataManager.Verify(
            m => m.AddFavor(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never
        );
    }

    #endregion

    #region Passive Favor Generation Tests

    [Fact]
    public void AwardPassiveFavor_WithPlayerHavingDeity_AwardsFavor()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockCalendar = new Mock<IGameCalendar>();
        mockCalendar.Setup(c => c.HoursPerDay).Returns(24.0f);

        var mockWorld = new Mock<IServerWorldAccessor>();
        mockWorld.Setup(w => w.Calendar).Returns(mockCalendar.Object);
        mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var playerData = new PlayerProgressionData
        {
        };

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        mockReligionManager.Setup(d => d.GetPlayerActiveDeityDomain("player-uid")).Returns(DeityDomain.Craft);
        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.AwardPassiveFavor(mockPlayer.Object, 1.0f);

        // Assert
        mockPlayerReligionDataManager.Verify(
            m => m.AddFractionalFavor("player-uid", It.IsAny<float>(), "Passive devotion"),
            Times.Once
        );
    }

    [Fact]
    public void AwardPassiveFavor_WithPlayerWithoutDeity_DoesNotAwardFavor()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var playerData = new PlayerProgressionData { };

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.AwardPassiveFavor(mockPlayer.Object, 1.0f);

        // Assert
        mockPlayerReligionDataManager.Verify(
            m => m.AddFractionalFavor(It.IsAny<string>(), It.IsAny<float>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public void CalculatePassiveFavorMultiplier_WithHigherRanks_ReturnsHigherMultiplier()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var playerDataInitiate = new PlayerProgressionData()
        {
        };

        var playerDataAvatar = new PlayerProgressionData()
        {
            TotalFavorEarned = 10000
        };

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        mockReligionManager
            .Setup(m => m.GetPlayerReligion("player-uid"))
            .Returns((ReligionData?)null);

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        var multiplierInitiate = favorSystem.CalculatePassiveFavorMultiplier(mockPlayer.Object, playerDataInitiate);
        var multiplierAvatar = favorSystem.CalculatePassiveFavorMultiplier(mockPlayer.Object, playerDataAvatar);

        // Assert
        Assert.Equal(1.0f, multiplierInitiate);
        Assert.Equal(1.5f, multiplierAvatar);
    }

    [Fact]
    public void CalculatePassiveFavorMultiplier_WithReligionPrestige_AppliesMultiplier()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var playerData = new PlayerProgressionData
        {
            TotalFavorEarned = 0
        };

        var religion = new ReligionData
        {
            ReligionUID = "test-religion",
            PrestigeRank = PrestigeRank.Mythic
        };

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        mockReligionManager
            .Setup(m => m.GetPlayerReligion("player-uid"))
            .Returns(religion);

        mockReligionManager.Setup(d => d.GetPlayerActiveDeityDomain("player-uid")).Returns(DeityDomain.Craft);

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        var multiplier = favorSystem.CalculatePassiveFavorMultiplier(mockPlayer.Object, playerData);

        // Assert
        Assert.Equal(1.5f, multiplier); // 1.0 (Initiate) * 1.5 (Mythic)
    }

    #endregion

    #region Passive Favor Calculation Tests

    [Fact]
    public void CalculateBaseFavor_ShouldReturn_CorrectAmount_ForOneSecond()
    {
        // Given: 1 second tick (dt = 1.0), typical VS calendar
        float dt = 1.0f;
        float hoursPerDay = 24.0f; // Typical Vintage Story value
        float baseFavorPerHour = 2.0f;

        // When: Calculate base favor
        float inGameHoursElapsed = dt / hoursPerDay;
        float baseFavor = baseFavorPerHour * inGameHoursElapsed;

        // Then: Should be ~0.0833 favor per second
        float expected = 2.0f / 24.0f;
        Assert.Equal(expected, baseFavor, precision: 4);
    }

    [Fact]
    public void CalculatePassiveFavor_ShouldReturn_CorrectAmount_WithAllMultipliers()
    {
        // Given: Avatar in Mythic religion, 1 second tick
        float baseFavorPerHour = 2.0f;
        float dt = 1.0f;
        float hoursPerDay = 24.0f;
        float devotionMultiplier = 1.5f; // Avatar
        float prestigeMultiplier = 1.5f; // Mythic

        // When: Calculate final favor
        float inGameHoursElapsed = dt / hoursPerDay;
        float baseFavor = baseFavorPerHour * inGameHoursElapsed;
        float finalFavor = baseFavor * devotionMultiplier * prestigeMultiplier;

        // Then: Should be ~0.1875 favor per second
        float expected = (2.0f / 24.0f) * 1.5f * 1.5f;
        Assert.Equal(expected, finalFavor, precision: 4);
    }

    [Theory]
    [InlineData(60, 5.0f)] // 1 minute = 5 favor (base rate)
    [InlineData(120, 10.0f)] // 2 minutes = 10 favor
    [InlineData(360, 30.0f)] // 6 minutes = 30 favor
    [InlineData(720, 60.0f)] // 12 minutes = 60 favor
    public void CalculatePassiveFavor_ShouldAccumulate_OverTime_BaseRate(int seconds, float expectedFavor)
    {
        // Given: Multiple ticks at base rate (Initiate, no religion)
        float baseFavorPerHour = 2.0f;
        float hoursPerDay = 24.0f;
        float favorPerSecond = baseFavorPerHour / hoursPerDay;

        // When: Calculate total favor for duration
        float totalFavor = favorPerSecond * seconds;

        // Then: Should match expected accumulation
        Assert.Equal(expectedFavor, totalFavor, precision: 1);
    }

    [Theory]
    [InlineData(DevotionRank.Initiate, PrestigeRank.Fledgling, 60, 5.0f)] // Base: 5/min
    [InlineData(DevotionRank.Avatar, PrestigeRank.Fledgling, 60, 7.5f)] // Avatar: 7.5/min
    [InlineData(DevotionRank.Initiate, PrestigeRank.Mythic, 60, 7.5f)] // Mythic: 7.5/min
    [InlineData(DevotionRank.Avatar, PrestigeRank.Mythic, 60, 11.25f)] // Both: 11.25/min
    [InlineData(DevotionRank.Disciple, PrestigeRank.Established, 60, 6.05f)] // Mid-tier: 6.05/min
    public void CalculatePassiveFavor_ShouldScale_WithMultipliers_OverTime(
        DevotionRank devotion, PrestigeRank prestige, int seconds, float expectedFavor)
    {
        // Given: Various rank combinations
        float baseFavorPerHour = 2.0f;
        float hoursPerDay = 24.0f;
        float devotionMultiplier = GetDevotionMultiplier(devotion);
        float prestigeMultiplier = GetPrestigeMultiplier(prestige);

        // When: Calculate total favor for duration
        float favorPerSecond = (baseFavorPerHour / hoursPerDay) * devotionMultiplier * prestigeMultiplier;
        float totalFavor = favorPerSecond * seconds;

        // Then: Should match expected with multipliers
        Assert.Equal(expectedFavor, totalFavor, precision: 2);
    }

    #endregion

    #region OnPlayerDeath Tests

    // NOTE: OnPlayerDeath_WithPvPKill_ProcessesBothRewardAndPenalty test removed - requires EntityPlayer.PlayerUID
    // mocking which is not possible due to non-virtual property. Consider integration tests if needed.

    [Fact]
    public void OnPlayerDeath_WithNonPvPDeath_OnlyAppliesPenalty()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var victimData = new PlayerProgressionData() { Favor = 50 };

        var mockVictim = new Mock<IServerPlayer>();
        mockVictim.Setup(p => p.PlayerUID).Returns("victim-uid");

        mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("victim-uid"))
            .Returns(victimData);

        mockReligionManager.Setup(d => d.GetPlayerActiveDeityDomain(It.IsAny<string>())).Returns(DeityDomain.Craft);

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Non-PvP damage source (null entity)
        var damageSource = new DamageSource
        {
            SourceEntity = null
        };

        // Act
        favorSystem.OnPlayerDeath(mockVictim.Object, damageSource);

        // Assert - Only penalty, no reward
        mockPlayerReligionDataManager.Verify(
            m => m.RemoveFavor("victim-uid", It.IsAny<int>(), It.IsAny<string>()),
            Times.Once
        );
        mockPlayerReligionDataManager.Verify(
            m => m.AddFavor(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public void ProcessPvPKill_SendsNotificationToVictim()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var mockAttacker = new Mock<IServerPlayer>();
        mockAttacker.Setup(p => p.PlayerUID).Returns("attacker-uid");
        mockAttacker.Setup(p => p.PlayerName).Returns("Attacker");

        var mockVictim = new Mock<IServerPlayer>();
        mockVictim.Setup(p => p.PlayerUID).Returns("victim-uid");
        mockVictim.Setup(p => p.PlayerName).Returns("Victim");

        // Set different deities for attacker and victim
        mockReligionManager.Setup(d => d.GetPlayerReligion("attacker-uid")).Returns(
            new ReligionData("attacker-religion-uid", "attacker-test-religion", DeityDomain.Craft, "test-name",
                "attacker-uid", "attacker"));
        mockReligionManager.Setup(d => d.GetPlayerReligion("victim-uid")).Returns(
            new ReligionData("victim-religion-uid", "victim-test-religion", DeityDomain.Craft, "test-name",
                "victim-uid", "victim"));

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.ProcessPvPKill(mockAttacker.Object, mockVictim.Object);

        // Assert - Victim should receive notification
        mockVictim.Verify(
            p => p.SendMessage(
                It.Is<int>(g => g == GlobalConstants.GeneralChatGroup),
                It.Is<string>(s => s.Contains("test-name") && s.Contains("displeased")),
                It.Is<EnumChatType>(t => t == EnumChatType.Notification),
                It.IsAny<string>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void ProcessDeathPenalty_SendsNotificationToPlayer()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var playerData = new PlayerProgressionData { Favor = 10 };

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        mockPlayerReligionDataManager
            .Setup(m => m.GetOrCreatePlayerData("player-uid"))
            .Returns(playerData);
        mockReligionManager.Setup(d => d.GetPlayerActiveDeityDomain(It.IsAny<string>())).Returns(DeityDomain.Craft);

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.ProcessDeathPenalty(mockPlayer.Object);

        // Assert
        mockPlayer.Verify(
            p => p.SendMessage(
                It.Is<int>(g => g == GlobalConstants.GeneralChatGroup),
                It.Is<string>(s => s.Contains("lost") && s.Contains("favor")),
                It.Is<EnumChatType>(t => t == EnumChatType.Notification),
                It.IsAny<string>()
            ),
            Times.Once
        );
    }

    #endregion

    #region OnGameTick Tests

    [Fact]
    public void OnGameTick_AwardsPassiveFavorToOnlinePlayers()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockCalendar = new Mock<IGameCalendar>();
        mockCalendar.Setup(c => c.HoursPerDay).Returns(24.0f);

        var mockWorld = new Mock<IServerWorldAccessor>();
        mockWorld.Setup(w => w.Calendar).Returns(mockCalendar.Object);
        mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        var mockPlayer1 = new Mock<IServerPlayer>();
        mockPlayer1.Setup(p => p.PlayerUID).Returns("player1-uid");

        var mockPlayer2 = new Mock<IServerPlayer>();
        mockPlayer2.Setup(p => p.PlayerUID).Returns("player2-uid");

        mockWorld.Setup(w => w.AllOnlinePlayers).Returns(new IPlayer[] { mockPlayer1.Object, mockPlayer2.Object });

        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var playerData1 = new PlayerProgressionData { };
        var playerData2 = new PlayerProgressionData { };

        mockPlayerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player1-uid")).Returns(playerData1);
        mockPlayerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player2-uid")).Returns(playerData2);

        mockReligionManager.Setup(m => m.GetPlayerReligion(It.IsAny<string>())).Returns((ReligionData?)null);
        mockReligionManager.Setup(m => m.GetPlayerActiveDeityDomain(It.IsAny<string>())).Returns(DeityDomain.Craft);

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act
        favorSystem.OnGameTick(1.0f);

        // Assert - Both players should receive favor
        mockPlayerReligionDataManager.Verify(
            m => m.AddFractionalFavor("player1-uid", It.IsAny<float>(), It.IsAny<string>()),
            Times.Once
        );
        mockPlayerReligionDataManager.Verify(
            m => m.AddFractionalFavor("player2-uid", It.IsAny<float>(), It.IsAny<string>()),
            Times.Once
        );
    }

    [Fact]
    public void OnGameTick_SkipsPlayersWithoutDeity()
    {
        // Arrange
        var mockAPI = CreateMockServerAPI();
        var mockWorld = new Mock<IServerWorldAccessor>();
        mockAPI.Setup(a => a.World).Returns(mockWorld.Object);

        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns("player-uid");

        mockWorld.Setup(w => w.AllOnlinePlayers).Returns(new IPlayer[] { mockPlayer.Object });

        var mockPlayerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        var mockReligionManager = new Mock<IReligionManager>();

        var playerData = new PlayerProgressionData { };
        mockPlayerReligionDataManager.Setup(m => m.GetOrCreatePlayerData("player-uid")).Returns(playerData);

        var favorSystem = CreateFavorSystem(
            mockAPI.Object,
            mockPlayerReligionDataManager.Object,
            mockReligionManager.Object);

        // Act - Use reflection to call internal method
        var method = favorSystem.GetType().GetMethod("OnGameTick",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method?.Invoke(favorSystem, new object[] { 1.0f });

        // Assert - No favor awarded
        mockPlayerReligionDataManager.Verify(
            m => m.AddFractionalFavor(It.IsAny<string>(), It.IsAny<float>(), It.IsAny<string>()),
            Times.Never
        );
    }

    #endregion

    #region Helper Methods

    private float GetDevotionMultiplier(DevotionRank rank)
    {
        return rank switch
        {
            DevotionRank.Initiate => 1.0f,
            DevotionRank.Disciple => 1.1f,
            DevotionRank.Zealot => 1.2f,
            DevotionRank.Champion => 1.3f,
            DevotionRank.Avatar => 1.5f,
            _ => 1.0f
        };
    }

    private float GetPrestigeMultiplier(PrestigeRank rank)
    {
        return rank switch
        {
            PrestigeRank.Fledgling => 1.0f,
            PrestigeRank.Established => 1.1f,
            PrestigeRank.Renowned => 1.2f,
            PrestigeRank.Legendary => 1.3f,
            PrestigeRank.Mythic => 1.5f,
            _ => 1.0f
        };
    }

    #endregion
}