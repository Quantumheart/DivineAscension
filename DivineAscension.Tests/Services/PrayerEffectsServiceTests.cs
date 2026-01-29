using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Services;

public class PrayerEffectsServiceTests
{
    private readonly Mock<IWorldService> _worldService;
    private readonly Mock<IChatCommandService> _chatCommandService;
    private readonly Mock<ILoggerWrapper> _logger;
    private readonly PrayerEffectsService _service;

    public PrayerEffectsServiceTests()
    {
        _worldService = new Mock<IWorldService>();
        _chatCommandService = new Mock<IChatCommandService>();
        _logger = new Mock<ILoggerWrapper>();
        _service = new PrayerEffectsService(_worldService.Object, _chatCommandService.Object, _logger.Object);
    }

    [Fact]
    public void Constructor_NullWorldService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PrayerEffectsService(null!, _chatCommandService.Object, _logger.Object));
    }

    [Fact]
    public void Constructor_NullChatCommandService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PrayerEffectsService(_worldService.Object, null!, _logger.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PrayerEffectsService(_worldService.Object, _chatCommandService.Object, null!));
    }

    [Fact]
    public void PlayPrayerEffects_NullPlayer_DoesNotTriggerEffects()
    {
        // Arrange
        var altarPos = new BlockPos(100, 64, 200);

        // Act
        _service.PlayPrayerEffects(null!, altarPos, 1, DeityDomain.Craft);

        // Assert - no effects triggered
        _worldService.Verify(w => w.World, Times.Never);
    }

    [Fact]
    public void PlayPrayerEffects_InvalidTier_DoesNotTriggerEffects()
    {
        // Arrange
        var mockServerPlayer = new Mock<IServerPlayer>(MockBehavior.Loose);
        var mockEntity = new Mock<EntityPlayer>(MockBehavior.Loose);
        mockServerPlayer.Setup(p => p.Entity).Returns(mockEntity.Object);
        var altarPos = new BlockPos(100, 64, 200);

        // Act
        _service.PlayPrayerEffects(mockServerPlayer.Object, altarPos, 0, DeityDomain.Stone);

        // Assert - no effects triggered (tier 0 is invalid, so no world access should occur)
        _worldService.Verify(w => w.World, Times.Never);
    }

    [Fact]
    public void PlayPrayerEffects_ValidPlayer_TriggersPlayerBowEmote()
    {
        // Arrange
        var mockServerPlayer = new Mock<IServerPlayer>(MockBehavior.Loose);
        var mockEntity = new Mock<EntityPlayer>(MockBehavior.Loose);
        mockServerPlayer.Setup(p => p.Entity).Returns(mockEntity.Object);
        var altarPos = new BlockPos(100, 64, 200);

        var mockWorld = new Mock<IServerWorldAccessor>(MockBehavior.Loose);
        _worldService.Setup(w => w.World).Returns(mockWorld.Object);

        // Act
        _service.PlayPrayerEffects(mockServerPlayer.Object, altarPos, 1, DeityDomain.Craft);

        // Assert - IChatCommandService.ExecuteUnparsed is called with /emote bow
        _chatCommandService.Verify(cmd => cmd.ExecuteUnparsed(
            "/emote bow",
            mockServerPlayer.Object), Times.Once);
    }

    [Fact]
    public void PlayPrayerEffects_SpawnsParticlesFromAllFiveSidesAndTop()
    {
        // Arrange
        var mockServerPlayer = new Mock<IServerPlayer>(MockBehavior.Loose);
        var mockEntity = new Mock<EntityPlayer>(MockBehavior.Loose);
        mockServerPlayer.Setup(p => p.Entity).Returns(mockEntity.Object);
        var altarPos = new BlockPos(100, 64, 200);

        var mockWorld = new Mock<IServerWorldAccessor>(MockBehavior.Loose);
        _worldService.Setup(w => w.World).Returns(mockWorld.Object);

        // Act
        _service.PlayPrayerEffects(mockServerPlayer.Object, altarPos, 1, DeityDomain.Wild);

        // Assert - particles spawn from all 4 sides + top of altar
        mockWorld.Verify(w => w.SpawnParticles(
            It.IsAny<SimpleParticleProperties>(),
            null), Times.Exactly(5));
    }

    [Fact]
    public void PlayPrayerEffects_PlaysSoundAtAltarPosition()
    {
        // Arrange
        var mockServerPlayer = new Mock<IServerPlayer>(MockBehavior.Loose);
        var mockEntity = new Mock<EntityPlayer>(MockBehavior.Loose);
        mockServerPlayer.Setup(p => p.Entity).Returns(mockEntity.Object);
        var altarPos = new BlockPos(100, 64, 200);

        var mockWorld = new Mock<IServerWorldAccessor>(MockBehavior.Loose);
        _worldService.Setup(w => w.World).Returns(mockWorld.Object);

        // Act
        _service.PlayPrayerEffects(mockServerPlayer.Object, altarPos, 1, DeityDomain.Harvest);

        // Assert - null player means send to all nearby players
        mockWorld.Verify(w => w.PlaySoundAt(
            It.IsAny<AssetLocation>(),
            100.5, 64.5, 200.5, // Center of block
            null, // null = send to all players
            false, // No pitch randomization
            32f, // Range
            1.0f // Volume
        ), Times.Once);
    }

    [Theory]
    [InlineData(1, "game:sounds/player/collect1")]
    [InlineData(2, "game:sounds/player/collect2")]
    [InlineData(3, "game:sounds/player/collect3")]
    public void GetBellSoundForTier_ReturnsCorrectSound(int tier, string expectedPath)
    {
        // Act
        var result = _service.GetBellSoundForTier(tier);

        // Assert
        Assert.Equal(expectedPath, result.ToString());
    }

    [Fact]
    public void CreateDivineParticles_ReturnsValidParticleProperties()
    {
        // Arrange
        var basePos = new Vec3d(100, 64, 200);
        var velocity = new Vec3f(0.5f, 0.5f, 0);

        // Act
        var result = _service.CreateDivineParticles(1, basePos, velocity, DeityDomain.Conquest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(velocity, result.MinVelocity);
    }

    [Theory]
    [InlineData(DeityDomain.Craft)]
    [InlineData(DeityDomain.Wild)]
    [InlineData(DeityDomain.Conquest)]
    [InlineData(DeityDomain.Harvest)]
    [InlineData(DeityDomain.Stone)]
    public void CreateDivineParticles_SetsDomainBasedColor(DeityDomain domain)
    {
        // Arrange
        var basePos = new Vec3d(100, 64, 200);
        var velocity = new Vec3f(0.5f, 0.5f, 0);

        // Act
        var result = _service.CreateDivineParticles(1, basePos, velocity, domain);

        // Assert - each domain should produce a non-zero color
        Assert.NotNull(result);
        Assert.NotEqual(0, result.Color);
    }

    [Theory]
    [InlineData(1, 22f)] // Shrine tier
    [InlineData(2, 45f)] // Temple tier
    [InlineData(3, 68f)] // Cathedral tier
    public void CreateDivineParticles_ScalesQuantityByTier(int tier, float expectedMinQty)
    {
        // Arrange
        var basePos = new Vec3d(100, 64, 200);
        var velocity = new Vec3f(0.5f, 0.5f, 0);

        // Act
        var result = _service.CreateDivineParticles(tier, basePos, velocity, DeityDomain.Craft);

        // Assert
        Assert.Equal(expectedMinQty, result.MinQuantity);
    }
}
