using DivineAscension.API.Interfaces;
using DivineAscension.Services;
using DivineAscension.Systems.Altar;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Systems.Altar;

public class CaravanShrinePlacementHandlerTests
{
    private readonly Mock<AltarEventEmitter> _emitter = new();
    private readonly CaravanShrinePlacementHandler _handler;

    public CaravanShrinePlacementHandlerTests()
    {
        _handler = new CaravanShrinePlacementHandler(
            new Mock<ILoggerWrapper>().Object,
            _emitter.Object,
            new Mock<IPlayerProgressionDataManager>().Object,
            new Mock<IReligionManager>().Object,
            new Mock<IHolySiteManager>().Object,
            new Mock<IWorldService>().Object,
            new SpyPlayerMessenger());
    }

    [Fact]
    public void IsCaravanShrineItem_TrueForShrineCodePath()
    {
        var stack = new ItemStack(new Item { Code = new AssetLocation("divineascension", "caravanshrine") });
        Assert.True(CaravanShrinePlacementHandler.IsCaravanShrineItem(stack));
    }

    [Fact]
    public void IsCaravanShrineItem_FalseForOtherPaths()
    {
        var altar = new ItemStack(new Item { Code = new AssetLocation("game", "altar-stone") });
        Assert.False(CaravanShrinePlacementHandler.IsCaravanShrineItem(altar));
    }

    [Fact]
    public void IsCaravanShrineItem_FalseForNull()
    {
        Assert.False(CaravanShrinePlacementHandler.IsCaravanShrineItem(null));
    }

    [Fact]
    public void IsCaravanShrineCode_TrueForShrine()
    {
        Assert.True(CaravanShrinePlacementHandler.IsCaravanShrineCode(
            new AssetLocation("divineascension", "caravanshrine")));
    }

    [Fact]
    public void IsCaravanShrineCode_FalseForOther()
    {
        Assert.False(CaravanShrinePlacementHandler.IsCaravanShrineCode(
            new AssetLocation("game", "stone")));
    }

    [Fact]
    public void Initialize_SubscribesAndDisposeUnsubscribes()
    {
        _handler.Initialize();
        _handler.Dispose();
    }
}
