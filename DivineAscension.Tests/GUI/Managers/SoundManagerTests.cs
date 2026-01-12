using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Managers;
using DivineAscension.GUI.Models.Enum;
using DivineAscension.Models.Enum;
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
    private readonly Mock<ILogger> _mockLogger = new();
    private readonly Mock<IClientPlayer> _mockPlayer = new();
    private readonly Mock<IClientWorldAccessor> _mockWorld = new();

    private readonly SoundManager _sut;

    public SoundManagerTests()
    {
        _mockApi.SetupGet(a => a.World).Returns(_mockWorld.Object);
        _mockApi.SetupGet(a => a.Logger).Returns(_mockLogger.Object);

        _mockWorld.SetupGet(w => w.Player).Returns(_mockPlayer.Object);
        _mockPlayer.SetupGet(p => p.Entity).Returns(_mockEntity.Object);

        _sut = new SoundManager(_mockApi.Object);
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
    public void PlayClick_PlaysClickAtNormalVolume()
    {
        _sut.PlayClick();

        _mockWorld.Verify(w => w.PlaySoundAt(
            It.Is<AssetLocation>(al => al.ToString() == "divineascension:sounds/click"),
            _mockEntity.Object,
            It.IsAny<IPlayer?>(),
            false,
            8f,
            0.5f
        ), Times.Once);
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
    public void PlaySuccess_PlaysUnlockAtLoudVolume()
    {
        _sut.PlaySuccess();

        _mockWorld.Verify(w => w.PlaySoundAt(
            It.Is<AssetLocation>(al => al.ToString() == "divineascension:sounds/unlock"),
            _mockEntity.Object,
            It.IsAny<IPlayer?>(),
            false,
            8f,
            0.7f
        ), Times.Once);
    }

    [Theory]
    [InlineData(DeityDomain.Craft, "divineascension:sounds/deities/Khoras")]
    [InlineData(DeityDomain.Wild, "divineascension:sounds/deities/Lysa")]
    [InlineData(DeityDomain.Harvest, "divineascension:sounds/deities/Aethra")]
    [InlineData(DeityDomain.Stone, "divineascension:sounds/deities/Gaia")]
    public void PlayDeityUnlock_MapsDeityToSpecificSound_AtLoudVolume(DeityDomain deity, string expectedPath)
    {
        _sut.PlayDeityUnlock(deity);

        _mockWorld.Verify(w => w.PlaySoundAt(
            It.Is<AssetLocation>(al => string.Equals(al.ToString(), expectedPath, StringComparison.OrdinalIgnoreCase)),
            _mockEntity.Object,
            It.IsAny<IPlayer?>(),
            false,
            8f,
            0.7f
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