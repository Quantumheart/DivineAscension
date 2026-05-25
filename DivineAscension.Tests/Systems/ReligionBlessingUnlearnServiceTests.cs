using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Configuration;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Xunit;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Unit tests for <see cref="ReligionBlessingUnlearnService" /> (epic #479, slice 5 — #484).
///     Covers refund math, lifetime-prestige invariance, effect refresh, cascade, and
///     ownership/kind/religion rejection.
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionBlessingUnlearnServiceTests
{
    private const string ReligionUid = "test-religion-uid";
    private const string VowId = "craft_vow";

    private readonly BlessingRegistry _registry;
    private readonly Mock<IBlessingEffectSystem> _effectSystem = new();
    private readonly Mock<IReligionManager> _religionManager = new();
    private readonly GameBalanceConfig _config = new(); // UnlearnRefundPercent defaults to 0.5
    private readonly FreeRespecWindow _freeRespecWindow = new(); // closed by default
    private readonly ReligionData _religion;
    private readonly ReligionBlessingUnlearnService _service;

    public ReligionBlessingUnlearnServiceTests()
    {
        var mockApi = TestFixtures.CreateMockCoreAPI();
        _registry = new BlessingRegistry(mockApi.Object);
        _registry.RegisterBlessing(new Blessing(VowId, "Craft Vow", DeityDomain.Craft)
        {
            Kind = BlessingKind.Religion,
            Cost = 100
        });

        // Patron Craft so the vow is paid at 1.0x (refund based on unadjusted cost).
        _religion = TestFixtures.CreateTestReligion(domain: DeityDomain.Craft);
        _religionManager.Setup(m => m.GetReligion(ReligionUid)).Returns(_religion);

        _service = new ReligionBlessingUnlearnService(
            _registry, _effectSystem.Object, _religionManager.Object, _config, _freeRespecWindow);
    }

    [Fact]
    public void Unlearn_OwnedVow_RefundsHalfToSpendablePrestige_LeavesLifetimeUnchanged()
    {
        _religion.AddPrestige(3000);      // spendable 3000, lifetime 3000 (Established rank)
        _religion.RemovePrestige(100);    // simulate having paid the vow cost: spendable 2900
        _religion.UnlockBlessing(VowId);
        var lifetimeBefore = _religion.TotalPrestige;
        var rankBefore = _religion.PrestigeRank;

        var result = _service.UnlearnReligionBlessing(ReligionUid, VowId);

        Assert.True(result.Success);
        Assert.Equal(ReligionUnlearnOutcome.Success, result.Outcome);
        Assert.Equal(50, result.RefundedPrestige);        // 100 cost * 0.5
        Assert.Equal(2950, _religion.Prestige);           // spendable refunded
        Assert.Equal(lifetimeBefore, _religion.TotalPrestige); // lifetime untouched
        Assert.Equal(rankBefore, _religion.PrestigeRank);      // rank cannot flicker
        Assert.False(_religion.UnlockedBlessings.ContainsKey(VowId));
    }

    [Fact]
    public void Unlearn_DuringFreeRespecWindow_RefundsFullCost()
    {
        _freeRespecWindow.SetActive(true);
        _religion.UnlockBlessing(VowId);

        var result = _service.UnlearnReligionBlessing(ReligionUid, VowId);

        Assert.True(result.Success);
        Assert.Equal(100, result.RefundedPrestige); // 100 cost * 1.0 (full)
        Assert.Equal(100, _religion.Prestige);
    }

    [Fact]
    public void Unlearn_NonPatronVow_RefundsOnAdjustedCost()
    {
        // Religion patron Wild, vow is Craft → paid 1.5x (150); 50% refund = 75.
        var wildReligion = TestFixtures.CreateTestReligion(domain: DeityDomain.Wild);
        _religionManager.Setup(m => m.GetReligion(ReligionUid)).Returns(wildReligion);
        wildReligion.UnlockBlessing(VowId);

        var result = _service.UnlearnReligionBlessing(ReligionUid, VowId);

        Assert.Equal(75, result.RefundedPrestige);
        Assert.Equal(75, wildReligion.Prestige);
    }

    [Fact]
    public void Unlearn_OwnedVow_RefreshesEffects()
    {
        _religion.UnlockBlessing(VowId);

        _service.UnlearnReligionBlessing(ReligionUid, VowId);

        _effectSystem.Verify(e => e.RefreshReligionBlessings(ReligionUid), Times.Once);
    }

    [Fact]
    public void Unlearn_NotOwned_IsRejected_WithNoSideEffects()
    {
        _religion.AddPrestige(500);

        var result = _service.UnlearnReligionBlessing(ReligionUid, VowId);

        Assert.Equal(ReligionUnlearnOutcome.NotOwned, result.Outcome);
        Assert.Equal(0, result.RefundedPrestige);
        Assert.Equal(500, _religion.Prestige);
        _effectSystem.Verify(e => e.RefreshReligionBlessings(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Unlearn_PersonalKindBlessing_IsRejected()
    {
        _registry.RegisterBlessing(new Blessing("personal_1", "Personal", DeityDomain.Craft)
        {
            Kind = BlessingKind.Player,
            Cost = 100
        });

        var result = _service.UnlearnReligionBlessing(ReligionUid, "personal_1");

        Assert.Equal(ReligionUnlearnOutcome.NotReligionBlessing, result.Outcome);
    }

    [Fact]
    public void Unlearn_UnknownBlessing_ReturnsBlessingNotFound()
    {
        var result = _service.UnlearnReligionBlessing(ReligionUid, "does_not_exist");

        Assert.Equal(ReligionUnlearnOutcome.BlessingNotFound, result.Outcome);
    }

    [Fact]
    public void Unlearn_UnknownReligion_ReturnsReligionNotFound()
    {
        _religionManager.Setup(m => m.GetReligion("missing")).Returns((ReligionData?)null);
        _religion.UnlockBlessing(VowId); // ownership irrelevant — religion lookup fails first

        var result = _service.UnlearnReligionBlessing("missing", VowId);

        Assert.Equal(ReligionUnlearnOutcome.ReligionNotFound, result.Outcome);
    }

    [Fact]
    public void Unlearn_CascadesToDependentChildren_StripsAll_AndSumsRefund()
    {
        _registry.RegisterBlessing(new Blessing("craft_vow_child", "Craft Vow Child", DeityDomain.Craft)
        {
            Kind = BlessingKind.Religion,
            Cost = 40,
            Branch = "branchA",
            PrerequisiteBlessings = new List<string> { VowId }
        });
        _religion.UnlockBlessing(VowId);
        _religion.UnlockBlessing("craft_vow_child");

        var result = _service.UnlearnReligionBlessing(ReligionUid, VowId);

        Assert.True(result.Success);
        Assert.Equal(2, result.StruckCount);
        Assert.Equal(70, result.RefundedPrestige);   // 100*0.5 + 40*0.5
        Assert.Equal(70, _religion.Prestige);
        Assert.False(_religion.UnlockedBlessings.ContainsKey(VowId));
        Assert.False(_religion.UnlockedBlessings.ContainsKey("craft_vow_child"));
    }

    [Fact]
    public void ResolveUnlearnCascade_ReturnsOrderedKillList_WithoutMutating()
    {
        _registry.RegisterBlessing(new Blessing("craft_vow_child", "Craft Vow Child", DeityDomain.Craft)
        {
            Kind = BlessingKind.Religion,
            Cost = 40,
            Branch = "branchA",
            PrerequisiteBlessings = new List<string> { VowId }
        });
        _religion.UnlockBlessing(VowId);
        _religion.UnlockBlessing("craft_vow_child");

        var cascade = _service.ResolveUnlearnCascade(ReligionUid, VowId);

        Assert.Equal(new[] { VowId, "craft_vow_child" }, cascade);
        Assert.True(_religion.UnlockedBlessings.ContainsKey(VowId));
        Assert.True(_religion.UnlockedBlessings.ContainsKey("craft_vow_child"));
    }

    [Fact]
    public void ResolveUnlearnCascade_NotOwned_ReturnsEmpty()
    {
        Assert.Empty(_service.ResolveUnlearnCascade(ReligionUid, VowId));
    }
}
