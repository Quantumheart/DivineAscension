using System.Diagnostics.CodeAnalysis;
using System.IO;
using DivineAscension.API.Interfaces;
using DivineAscension.Configuration;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Xunit;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Unit tests for the single-blessing unlearn slice (#459): refund math, cooldown enforcement,
///     effect strip, persistence round-trip, and server-authoritative ownership/cooldown rejection.
/// </summary>
[ExcludeFromCodeCoverage]
public class PlayerProgressionServiceUnlearnTests
{
    private const string PlayerUid = "player-1";

    private readonly GameBalanceConfig _config = new();
    private readonly FakeTimeService _time = new();
    private readonly Mock<IPlayerProgressionDataManager> _progression = new();
    private readonly Mock<IReligionManager> _religion = new();
    private readonly Mock<IBlessingRegistry> _registry = new();
    private readonly Mock<IBlessingEffectSystem> _effects = new();
    private readonly CooldownManager _cooldowns;
    private readonly PlayerProgressionData _data = new(PlayerUid);

    public PlayerProgressionServiceUnlearnTests()
    {
        TestFixtures.InitializeLocalizationForTests();

        _cooldowns = new CooldownManager(new Mock<ILogger>().Object, new FakeEventService(),
            new FakeWorldService(), new ModConfigData(), _config);

        // The data manager always hands back our single test player record.
        _progression.Setup(m => m.GetOrCreatePlayerData(PlayerUid)).Returns(_data);
        PlayerProgressionData? outData = _data;
        _progression.Setup(m => m.TryGetPlayerData(PlayerUid, out outData)).Returns(true);
    }

    private PlayerProgressionService BuildService(IBlessingEffectSystem? effectSystem = null)
    {
        var service = new PlayerProgressionService(
            new Mock<IFavorSystem>().Object,
            new Mock<IReligionPrestigeManager>().Object,
            new Mock<IActivityLogManager>().Object,
            _progression.Object,
            _religion.Object,
            _cooldowns,
            _config,
            _time);
        service.SetBlessingSystems(_registry.Object, effectSystem ?? _effects.Object);
        return service;
    }

    private static Blessing PlayerBlessing(string id, DeityDomain domain = DeityDomain.Craft, int cost = 100)
    {
        var b = TestFixtures.CreateTestBlessing(id, id, domain);
        b.Cost = cost;
        return b;
    }

    [Fact]
    public void UnlearnBlessing_RefundsHalfToSpendableFavor_LifetimeUnchanged()
    {
        var blessing = PlayerBlessing("b1");
        _data.UnlockBlessing("b1");
        _registry.Setup(r => r.GetBlessing("b1")).Returns(blessing);
        _religion.Setup(r => r.GetPlayerReligion(PlayerUid))
            .Returns(TestFixtures.CreateTestReligion(domain: DeityDomain.Craft));

        var result = BuildService().UnlearnBlessing(PlayerUid, "b1");

        Assert.True(result.Success);
        Assert.Equal(50, result.RefundedFavor); // 100 * 0.5
        Assert.Equal(50, _data.GetFavor(DeityDomain.Craft)); // credited to spendable favor
        Assert.Equal(0, _data.GetTotalFavorEarned(DeityDomain.Craft)); // lifetime untouched
        Assert.False(_data.IsBlessingUnlocked("b1")); // stripped from unlocked set
    }

    [Fact]
    public void UnlearnBlessing_SecondUnlearnWhileOnCooldown_IsRejected()
    {
        _data.UnlockBlessing("b1");
        _data.UnlockBlessing("b2");
        _registry.Setup(r => r.GetBlessing("b1")).Returns(PlayerBlessing("b1"));
        _registry.Setup(r => r.GetBlessing("b2")).Returns(PlayerBlessing("b2"));
        _religion.Setup(r => r.GetPlayerReligion(PlayerUid))
            .Returns(TestFixtures.CreateTestReligion(domain: DeityDomain.Craft));
        var service = BuildService();

        var first = service.UnlearnBlessing(PlayerUid, "b1");
        var second = service.UnlearnBlessing(PlayerUid, "b2");

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal(UnlearnFailureReason.OnCooldown, second.Reason);
        Assert.True(second.RemainingCooldownSeconds > 0);
        Assert.True(_data.IsBlessingUnlocked("b2")); // not stripped — rejected before mutation
    }

    [Fact]
    public void UnlearnBlessing_StripsBlessingEffects()
    {
        var blessing = PlayerBlessing("b1");
        blessing.StatModifiers["walkspeed"] = 0.1f;
        _data.UnlockBlessing("b1");
        _registry.Setup(r => r.GetBlessing("b1")).Returns(blessing);
        _religion.Setup(r => r.GetPlayerReligion(PlayerUid))
            .Returns(TestFixtures.CreateTestReligion(domain: DeityDomain.Craft));

        var realEffects = new BlessingEffectSystem(
            new Mock<ILoggerWrapper>().Object,
            new Mock<IEventService>().Object,
            new FakeWorldService(),
            _registry.Object,
            _progression.Object,
            _religion.Object);

        // Before: the blessing's modifier is present.
        Assert.True(realEffects.GetPlayerStatModifiers(PlayerUid).ContainsKey("walkspeed"));

        BuildService(realEffects).UnlearnBlessing(PlayerUid, "b1");

        // After: recomputed from the now-empty unlocked set.
        Assert.False(realEffects.GetPlayerStatModifiers(PlayerUid).ContainsKey("walkspeed"));
    }

    [Fact]
    public void UnlearnBlessing_PersistenceRoundTrip_PreservesCooldownStampAndUnlockedSet()
    {
        _data.UnlockBlessing("b1");
        _data.UnlockBlessing("keep");
        _registry.Setup(r => r.GetBlessing("b1")).Returns(PlayerBlessing("b1"));
        _religion.Setup(r => r.GetPlayerReligion(PlayerUid))
            .Returns(TestFixtures.CreateTestReligion(domain: DeityDomain.Craft));

        BuildService().UnlearnBlessing(PlayerUid, "b1");

        using var ms = new MemoryStream();
        ProtoBuf.Serializer.Serialize(ms, _data);
        ms.Position = 0;
        var roundTripped = ProtoBuf.Serializer.Deserialize<PlayerProgressionData>(ms);

        Assert.NotNull(roundTripped.NextUnlearnAllowedTimeUtc);
        Assert.Equal(_data.NextUnlearnAllowedTimeUtc, roundTripped.NextUnlearnAllowedTimeUtc);
        Assert.DoesNotContain("b1", roundTripped.UnlockedBlessings);
        Assert.Contains("keep", roundTripped.UnlockedBlessings);
    }

    [Fact]
    public void UnlearnBlessing_NotOwned_IsRejected()
    {
        _registry.Setup(r => r.GetBlessing("b1")).Returns(PlayerBlessing("b1"));
        _religion.Setup(r => r.GetPlayerReligion(PlayerUid))
            .Returns(TestFixtures.CreateTestReligion(domain: DeityDomain.Craft));

        var result = BuildService().UnlearnBlessing(PlayerUid, "b1");

        Assert.False(result.Success);
        Assert.Equal(UnlearnFailureReason.NotOwned, result.Reason);
    }

    [Fact]
    public void UnlearnBlessing_BlessingNotFound_IsRejected()
    {
        _registry.Setup(r => r.GetBlessing(It.IsAny<string>())).Returns((Blessing?)null);

        var result = BuildService().UnlearnBlessing(PlayerUid, "missing");

        Assert.False(result.Success);
        Assert.Equal(UnlearnFailureReason.BlessingNotFound, result.Reason);
    }

    [Fact]
    public void UnlearnBlessing_ReligionBlessing_IsRejectedAsUnsupported()
    {
        var religionBlessing = TestFixtures.CreateTestBlessing("rb", "rb", DeityDomain.Craft, BlessingKind.Religion);
        _data.UnlockBlessing("rb");
        _registry.Setup(r => r.GetBlessing("rb")).Returns(religionBlessing);

        var result = BuildService().UnlearnBlessing(PlayerUid, "rb");

        Assert.False(result.Success);
        Assert.Equal(UnlearnFailureReason.NotPlayerBlessing, result.Reason);
    }

    [Fact]
    public void UnlearnBlessing_NotInReligion_IsRejected()
    {
        _data.UnlockBlessing("b1");
        _registry.Setup(r => r.GetBlessing("b1")).Returns(PlayerBlessing("b1"));
        _religion.Setup(r => r.GetPlayerReligion(PlayerUid)).Returns((ReligionData?)null);

        var result = BuildService().UnlearnBlessing(PlayerUid, "b1");

        Assert.False(result.Success);
        Assert.Equal(UnlearnFailureReason.NotInReligion, result.Reason);
        Assert.True(_data.IsBlessingUnlocked("b1"));
    }
}
