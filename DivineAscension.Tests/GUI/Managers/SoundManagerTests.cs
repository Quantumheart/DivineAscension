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
    private readonly Mock<IGuiAPI> _mockGui = new();
    private readonly Mock<ILoggerWrapper> _mockLogger = new();

    private readonly SoundManager _sut;

    public SoundManagerTests()
    {
        _mockApi.SetupGet(a => a.Gui).Returns(_mockGui.Object);

        _sut = new SoundManager(_mockApi.Object, _mockLogger.Object);
    }

    [Fact]
    public void Play_WithKnownSound_PlaysNonPositional2DSound_WithExpectedParams()
    {
        // Act
        _sut.Play(SoundType.Click);

        // Assert — UI sounds use the 2D Gui.PlaySound overload, not positional PlaySoundAt.
        _mockGui.Verify(g => g.PlaySound(
            It.Is<AssetLocation>(al => al != null && al.ToString() == "divineascension:sounds/click"),
            false,
            0.5f
        ), Times.Once);
    }

    [Fact]
    public void PlayClick_IsNoOp()
    {
        // Click sound intentionally disconnected from UI (see SoundManager.PlayClick).
        _sut.PlayClick();

        _mockGui.Verify(g => g.PlaySound(
            It.IsAny<AssetLocation>(),
            It.IsAny<bool>(),
            It.IsAny<float>()
        ), Times.Never);
    }

    [Fact]
    public void PlayError_PlaysErrorAtQuietVolume()
    {
        _sut.PlayError();

        _mockGui.Verify(g => g.PlaySound(
            It.Is<AssetLocation>(al => al.ToString() == "divineascension:sounds/error"),
            false,
            0.3f
        ), Times.Once);
    }

    [Fact]
    public void PlaySuccess_PlaysWritingAtNormalVolume()
    {
        _sut.PlaySuccess();

        _mockGui.Verify(g => g.PlaySound(
            It.Is<AssetLocation>(al => al.ToString() == "divineascension:sounds/writing"),
            false,
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

        _mockGui.Verify(g => g.PlaySound(
            It.Is<AssetLocation>(al => al.ToString() == "divineascension:sounds/writing"),
            false,
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
        _mockGui.Verify(g => g.PlaySound(
            It.IsAny<AssetLocation>(),
            It.IsAny<bool>(),
            It.IsAny<float>()
        ), Times.Never);
    }
}