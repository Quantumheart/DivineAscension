using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Favor;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Patches;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems.Favor;

[ExcludeFromCodeCoverage]
public class TraderTransactionFavorTrackerTests
{
    private static TraderTransactionFavorTracker CreateTracker(
        Mock<IWorldService> mockWorldService,
        Mock<IFavorSystem> mockFavor)
    {
        var mockLogger = new Mock<ILoggerWrapper>();
        return new TraderTransactionFavorTracker(mockLogger.Object, mockWorldService.Object, mockFavor.Object);
    }

    [Fact]
    public void DeityDomain_IsCaravan()
    {
        var tracker = CreateTracker(new Mock<IWorldService>(), TestFixtures.CreateMockFavorSystem());

        Assert.Equal(DeityDomain.Caravan, tracker.DeityDomain);
    }

    [Theory]
    [InlineData(0, 0)]      // empty trade — no favor
    [InlineData(5, 2)]      // sub-divisor — base only
    [InlineData(10, 3)]     // exactly divisor — base + 1
    [InlineData(50, 7)]     // 2 + 5
    [InlineData(180, 20)]   // 2 + 18 = 20 (at cap boundary)
    [InlineData(200, 20)]   // would be 22 — clamped to cap
    [InlineData(10000, 20)] // whale trade — clamped
    public void ComputeFavor_FollowsBaseAndCap(int valueInGears, int expected)
    {
        Assert.Equal(expected, TraderTransactionFavorTracker.ComputeFavor(valueInGears));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ComputeFavor_NegativeValue_ReturnsZero(int valueInGears)
    {
        Assert.Equal(0, TraderTransactionFavorTracker.ComputeFavor(valueInGears));
    }

    [Fact]
    public void HandleTraderTransaction_AwardsCaravanFavorWithTradeAction()
    {
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(new Mock<IWorldService>(), mockFavor);
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.SetupGet(p => p.PlayerName).Returns("Tester");

        tracker.HandleTraderTransaction(mockPlayer.Object, valueInGears: 50);

        mockFavor.Verify(f => f.AwardFavorForAction(mockPlayer.Object, "trade",
            7f /* base 2 + 50/10 */, DeityDomain.Caravan), Times.Once);
    }

    [Fact]
    public void HandleTraderTransaction_WhaleTrade_ClampedToCap()
    {
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(new Mock<IWorldService>(), mockFavor);
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.SetupGet(p => p.PlayerName).Returns("Whale");

        tracker.HandleTraderTransaction(mockPlayer.Object, valueInGears: 9_999);

        mockFavor.Verify(f => f.AwardFavorForAction(mockPlayer.Object, "trade",
            (float)TraderTransactionFavorTracker.MaxFavorPerTrade, DeityDomain.Caravan), Times.Once);
    }

    [Fact]
    public void HandleTraderTransaction_ZeroValue_NoAward()
    {
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(new Mock<IWorldService>(), mockFavor);
        var mockPlayer = new Mock<IServerPlayer>();

        tracker.HandleTraderTransaction(mockPlayer.Object, valueInGears: 0);

        mockFavor.Verify(f => f.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(),
            It.IsAny<float>(), It.IsAny<DeityDomain>()), Times.Never);
    }

    [Fact]
    public void HandleTraderTransaction_NonServerPlayer_NoAward()
    {
        var mockFavor = TestFixtures.CreateMockFavorSystem();
        var tracker = CreateTracker(new Mock<IWorldService>(), mockFavor);
        // IPlayer that is *not* IServerPlayer — guards the server-authoritative gate.
        var mockClient = new Mock<IPlayer>();

        tracker.HandleTraderTransaction(mockClient.Object, valueInGears: 100);

        mockFavor.Verify(f => f.AwardFavorForAction(It.IsAny<IServerPlayer>(), It.IsAny<string>(),
            It.IsAny<float>(), It.IsAny<DeityDomain>()), Times.Never);
    }

    // Note: Initialize/Dispose subscribe/unsubscribe from the static TraderPatches event,
    // which can't be invoked from outside the declaring type. The HandleTraderTransaction
    // tests above cover the favor-award path the subscription routes to.
}
