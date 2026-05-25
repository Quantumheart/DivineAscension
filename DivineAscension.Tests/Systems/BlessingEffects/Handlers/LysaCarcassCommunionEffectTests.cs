using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Services;
using DivineAscension.Systems.BlessingEffects.Handlers;
using DivineAscension.Tests.Helpers;
using Moq;

namespace DivineAscension.Tests.Systems.BlessingEffects.Handlers;

/// <summary>
/// Tests for the Lysa CarcassCommunion effect handler.
/// The satiety-grant mechanic itself relies on VS' EntityBehaviorHunger and is covered by
/// integration testing; these unit tests focus on the lifecycle contract — effect id,
/// safe (no-op) activation hooks, and clean subscribe/unsubscribe.
/// </summary>
[ExcludeFromCodeCoverage]
public class LysaCarcassCommunionEffectTests
{
    private readonly Mock<IEventService> _mockEventService = new();
    private readonly Mock<ILoggerWrapper> _mockLogger = new();
    private readonly Mock<IWorldService> _mockWorldService = new();

    private LysaEffectHandlers.CarcassCommunionEffect CreateHandler()
    {
        return new LysaEffectHandlers.CarcassCommunionEffect();
    }

    [Fact]
    public void EffectId_ReturnsCarcassCommunion()
    {
        var handler = CreateHandler();

        Assert.Equal(SpecialEffects.CarcassCommunion, handler.EffectId);
    }

    [Fact]
    public void Initialize_DoesNotThrow()
    {
        var handler = CreateHandler();

        var exception = Record.Exception(() =>
            handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object));

        Assert.Null(exception);
        handler.Dispose();
    }

    [Fact]
    public void ActivateAndDeactivate_AreNoOps_DoNotThrow()
    {
        var handler = CreateHandler();
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);
        var player = TestFixtures.CreateMockServerPlayer().Object;

        var exception = Record.Exception(() =>
        {
            handler.ActivateForPlayer(player);
            handler.DeactivateForPlayer(player);
        });

        Assert.Null(exception);
        handler.Dispose();
    }

    [Fact]
    public void OnTick_DoesNotThrow()
    {
        var handler = CreateHandler();
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);

        var exception = Record.Exception(() => handler.OnTick(0.1f));

        Assert.Null(exception);
        handler.Dispose();
    }

    [Fact]
    public void Dispose_AfterInitialize_DoesNotThrow()
    {
        var handler = CreateHandler();
        handler.Initialize(_mockLogger.Object, _mockEventService.Object, _mockWorldService.Object);

        var exception = Record.Exception(() => handler.Dispose());

        Assert.Null(exception);
    }
}
