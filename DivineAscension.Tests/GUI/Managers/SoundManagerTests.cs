using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Managers;
using DivineAscension.GUI.Models.Enum;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using Moq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace DivineAscension.Tests.GUI.Managers;

[ExcludeFromCodeCoverage]
public class SoundManagerTests
{
    private readonly Mock<ICoreClientAPI> _mockApi = new();
    private readonly Mock<EntityPlayer> _mockEntity = new();
    private readonly Mock<ILoggerWrapper> _mockLogger = new();
    private readonly Mock<IClientPlayer> _mockPlayer = new();
    private readonly Mock<IClientWorldAccessor> _mockWorld = new();

    private readonly SoundManager _sut;

    public SoundManagerTests()
    {
        _mockApi.SetupGet(a => a.World).Returns(_mockWorld.Object);

        _mockWorld.SetupGet(w => w.Player).Returns(_mockPlayer.Object);
        _mockPlayer.SetupGet(p => p.Entity).Returns(_mockEntity.Object);

        _sut = new SoundManager(_mockApi.Object, _mockLogger.Object);
    }

    [Fact]
    public void Play_WithKnownSound_CallsPlaySoundAt_WithExpectedParams()
    {
        // Act
        _sut.Play(SoundType.Click);

        // Assert
        _mockWorld.Verify(w => w.PlaySoundAt(
            It.Is<AssetLocation>(al => al != null && al.ToString() == "divineascension:sounds/click"),
            _mockEntity.Object,
            It.Is<IPlayer?>(p => p == null),
            false,
            8f,
            0.5f
        ), Times.Once);
    }

    [Fact]
    public void PlayClick_IsNoOp()
    {
        // Click sound intentionally disconnected from UI (see SoundManager.PlayClick).
        _sut.PlayClick();

        _mockWorld.Verify(w => w.PlaySoundAt(
            It.IsAny<AssetLocation>(),
            It.IsAny<Entity>(),
            It.IsAny<IPlayer?>(),
            It.IsAny<bool>(),
            It.IsAny<float>(),
            It.IsAny<float>()
        ), Times.Never);
    }

    [Fact]
    public void PlayError_PlaysErrorAtQuietVolume()
    {
        _sut.PlayError();

        _mockWorld.Verify(w => w.PlaySoundAt(
            It.Is<AssetLocation>(al => al.ToString() == "divineascension:sounds/error"),
            _mockEntity.Object,
            It.IsAny<IPlayer?>(),
            false,
            8f,
            0.3f
        ), Times.Once);
    }

    [Fact]
    public void PlaySuccess_PlaysWritingAtNormalVolume()
    {
        _sut.PlaySuccess();

        _mockWorld.Verify(w => w.PlaySoundAt(
            It.Is<AssetLocation>(al => al.ToString() == "divineascension:sounds/writing"),
            _mockEntity.Object,
            It.IsAny<IPlayer?>(),
            false,
            8f,
            0.5f
        ), Times.Once);
    }

    [Theory]
    [InlineData(DeityDomain.Craft)]
    [InlineData(DeityDomain.Wild)]
    [InlineData(DeityDomain.Harvest)]
    [InlineData(DeityDomain.Stone)]
    public void PlayDeityUnlock_PlaysWritingAtNormalVolume(DeityDomain deity)
    {
        _sut.PlayDeityUnlock(deity);

        _mockWorld.Verify(w => w.PlaySoundAt(
            It.Is<AssetLocation>(al => al.ToString() == "divineascension:sounds/writing"),
            _mockEntity.Object,
            It.IsAny<IPlayer?>(),
            false,
            8f,
            0.5f
        ), Times.Once);
    }

    [Fact]
    public void Play_WithUnknownSound_LogsWarning_AndDoesNotPlay()
    {
        // Act
        _sut.Play((SoundType)999);

        // Assert
        _mockLogger.Verify(l => l.Warning(It.Is<string>(s => s.Contains("not found in SoundPaths dictionary"))),
            Times.Once);
        _mockWorld.Verify(w => w.PlaySoundAt(
            It.IsAny<AssetLocation>(),
            It.IsAny<Entity>(),
            It.IsAny<IPlayer?>(),
            It.IsAny<bool>(),
            It.IsAny<float>(),
            It.IsAny<float>()
        ), Times.Never);
    }
}