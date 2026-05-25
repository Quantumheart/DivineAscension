using System.Diagnostics.CodeAnalysis;
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
///     Unit tests for <see cref="BlessingUnlearnService" /> (epic #425, slice 1 — #459).
///     Covers refund math, lifetime invariance, effect strip, and ownership/kind rejection.
/// </summary>
[ExcludeFromCodeCoverage]
public class BlessingUnlearnServiceTests
{
    private const string PlayerUid = "player-1";
    private const string BlessingId = "craft_blessing";

    private readonly BlessingRegistry _registry;
    private readonly Mock<IBlessingEffectSystem> _effectSystem = new();
    private readonly Mock<IPlayerProgressionDataManager> _dataManager = new();
    private readonly Mock<IReligionManager> _religionManager = new();
    private readonly GameBalanceConfig _config = new(); // UnlearnRefundPercent defaults to 0.5
    private readonly PlayerProgressionData _playerData = new(PlayerUid);
    private readonly FreeRespecWindow _freeRespecWindow = new(); // closed by default
    private readonly BlessingUnlearnService _service;

    public BlessingUnlearnServiceTests()
    {
        var mockApi = TestFixtures.CreateMockCoreAPI();
        _registry = new BlessingRegistry(mockApi.Object);
        _registry.RegisterBlessing(new Blessing(BlessingId, "Craft Blessing", DeityDomain.Craft)
        {
            Kind = BlessingKind.Player,
            Cost = 100
        });

        _dataManager.Setup(m => m.GetOrCreatePlayerData(PlayerUid)).Returns(_playerData);
        // No religion by default — refund is then based on the unadjusted blessing cost.
        _religionManager.Setup(m => m.GetPlayerReligion(PlayerUid)).Returns((ReligionData?)null);

        _service = new BlessingUnlearnService(
            _registry, _effectSystem.Object, _dataManager.Object, _religionManager.Object, _config,
            _freeRespecWindow);
    }

    [Fact]
    public void UnlearnBlessing_OwnedBlessing_RefundsHalfToSpendableFavor_LeavesLifetimeUnchanged()
    {
        _playerData.AddFavor(DeityDomain.Craft, 200); // spendable 200, lifetime 200
        _playerData.UnlockBlessing(BlessingId);

        var result = _service.UnlearnBlessing(PlayerUid, BlessingId);

        Assert.True(result.Success);
        Assert.Equal(UnlearnOutcome.Success, result.Outcome);
        Assert.Equal(50, result.RefundedFavor);                       // 100 cost * 0.5
        Assert.Equal(250, _playerData.GetFavor(DeityDomain.Craft));   // spendable refunded
        Assert.Equal(200, _playerData.GetTotalFavorEarned(DeityDomain.Craft)); // lifetime untouched
        Assert.False(_playerData.IsBlessingUnlocked(BlessingId));      // stripped from unlocked set
    }

    [Fact]
    public void UnlearnBlessing_DuringFreeRespecWindow_RefundsFullCost()
    {
        _freeRespecWindow.SetActive(true);
        _playerData.AddFavor(DeityDomain.Craft, 200);
        _playerData.UnlockBlessing(BlessingId);

        var result = _service.UnlearnBlessing(PlayerUid, BlessingId);

        Assert.True(result.Success);
        Assert.Equal(100, result.RefundedFavor);                      // 100 cost * 1.0 (full)
        Assert.Equal(300, _playerData.GetFavor(DeityDomain.Craft));   // spendable refunded in full
        Assert.Equal(200, _playerData.GetTotalFavorEarned(DeityDomain.Craft)); // lifetime untouched
        Assert.False(_playerData.IsBlessingUnlocked(BlessingId));
    }

    [Fact]
    public void UnlearnBlessing_AfterWindowCloses_RefundsHalfAgain()
    {
        _freeRespecWindow.SetActive(true);
        _freeRespecWindow.SetActive(false); // window opened then closed → back to normal rules
        _playerData.AddFavor(DeityDomain.Craft, 200);
        _playerData.UnlockBlessing(BlessingId);

        var result = _service.UnlearnBlessing(PlayerUid, BlessingId);

        Assert.Equal(50, result.RefundedFavor); // 100 cost * 0.5
    }

    [Fact]
    public void UnlearnBlessing_OwnedBlessing_RefreshesEffects_And_NotifiesDataChanged()
    {
        _playerData.UnlockBlessing(BlessingId);

        _service.UnlearnBlessing(PlayerUid, BlessingId);

        _effectSystem.Verify(e => e.RefreshPlayerBlessings(PlayerUid), Times.Once);
        _dataManager.Verify(m => m.NotifyPlayerDataChanged(PlayerUid), Times.Once);
    }

    [Fact]
    public void UnlearnBlessing_NonPatronReligion_RefundsOnAdjustedCost()
    {
        // Religion patron is Wild but the blessing is Craft, so the player paid 1.5x (150);
        // the 50% refund is therefore 75.
        var religion = TestFixtures.CreateTestReligion(domain: DeityDomain.Wild);
        _religionManager.Setup(m => m.GetPlayerReligion(PlayerUid)).Returns(religion);
        _playerData.UnlockBlessing(BlessingId);

        var result = _service.UnlearnBlessing(PlayerUid, BlessingId);

        Assert.Equal(75, result.RefundedFavor);
        Assert.Equal(75, _playerData.GetFavor(DeityDomain.Craft));
    }

    [Fact]
    public void UnlearnBlessing_NotOwned_IsRejected_WithNoSideEffects()
    {
        _playerData.AddFavor(DeityDomain.Craft, 200);

        var result = _service.UnlearnBlessing(PlayerUid, BlessingId);

        Assert.Equal(UnlearnOutcome.NotOwned, result.Outcome);
        Assert.Equal(0, result.RefundedFavor);
        Assert.Equal(200, _playerData.GetFavor(DeityDomain.Craft)); // no refund credited
        _effectSystem.Verify(e => e.RefreshPlayerBlessings(It.IsAny<string>()), Times.Never);
        _dataManager.Verify(m => m.NotifyPlayerDataChanged(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void UnlearnBlessing_ReligionKindBlessing_IsRejected()
    {
        _registry.RegisterBlessing(new Blessing("vow_1", "Vow", DeityDomain.Craft)
        {
            Kind = BlessingKind.Religion,
            Cost = 100
        });

        var result = _service.UnlearnBlessing(PlayerUid, "vow_1");

        Assert.Equal(UnlearnOutcome.NotPlayerBlessing, result.Outcome);
    }

    [Fact]
    public void UnlearnBlessing_UnknownBlessing_ReturnsBlessingNotFound()
    {
        var result = _service.UnlearnBlessing(PlayerUid, "does_not_exist");

        Assert.Equal(UnlearnOutcome.BlessingNotFound, result.Outcome);
    }

    [Fact]
    public void UnlearnBlessing_CascadesToDependentChildren_StripsAll_AndSumsRefund()
    {
        // craft_blessing (Cost 100) is the prerequisite of a branch child (Cost 40, AND).
        _registry.RegisterBlessing(new Blessing("craft_child", "Craft Child", DeityDomain.Craft)
        {
            Kind = BlessingKind.Player,
            Cost = 40,
            Branch = "branchA",
            PrerequisiteBlessings = new List<string> { BlessingId }
        });
        _playerData.UnlockBlessing(BlessingId);
        _playerData.UnlockBlessing("craft_child");

        var result = _service.UnlearnBlessing(PlayerUid, BlessingId);

        Assert.True(result.Success);
        Assert.Equal(2, result.StruckCount);
        Assert.Equal(70, result.RefundedFavor);                  // 100*0.5 + 40*0.5
        Assert.Equal(70, _playerData.GetFavor(DeityDomain.Craft));
        Assert.False(_playerData.IsBlessingUnlocked(BlessingId));
        Assert.False(_playerData.IsBlessingUnlocked("craft_child")); // orphaned child cascaded
    }

    [Fact]
    public void ResolveUnlearnCascade_ReturnsOrderedKillList_WithoutMutating()
    {
        _registry.RegisterBlessing(new Blessing("craft_child", "Craft Child", DeityDomain.Craft)
        {
            Kind = BlessingKind.Player,
            Cost = 40,
            Branch = "branchA",
            PrerequisiteBlessings = new List<string> { BlessingId }
        });
        _playerData.UnlockBlessing(BlessingId);
        _playerData.UnlockBlessing("craft_child");

        var cascade = _service.ResolveUnlearnCascade(PlayerUid, BlessingId);

        Assert.Equal(new[] { BlessingId, "craft_child" }, cascade);
        // Pure query — nothing stripped.
        Assert.True(_playerData.IsBlessingUnlocked(BlessingId));
        Assert.True(_playerData.IsBlessingUnlocked("craft_child"));
    }

    [Fact]
    public void ResolveUnlearnCascade_NotOwned_ReturnsEmpty()
    {
        Assert.Empty(_service.ResolveUnlearnCascade(PlayerUid, BlessingId));
    }

    // --- Apostasy penalty (epic #425, slice 3 — #461) ---------------------------------------

    [Fact]
    public void StripDomainLockedForApostasy_StripsDomainLocked_KeepsGeneric_ZeroRefund()
    {
        // BlessingId (craft_blessing) is generic; add a domain-locked capstone the player owns.
        _registry.RegisterBlessing(new Blessing("craft_patron", "Craft Patron", DeityDomain.Craft)
        {
            Kind = BlessingKind.Player,
            Cost = 200,
            RequiresPatron = true
        });
        _playerData.AddFavor(DeityDomain.Craft, 300); // spendable 300, lifetime 300
        _playerData.UnlockBlessing(BlessingId);        // generic — kept
        _playerData.UnlockBlessing("craft_patron");    // domain-locked — stripped

        var stripped = _service.StripDomainLockedForApostasy(PlayerUid);

        Assert.Equal(new[] { "craft_patron" }, stripped);
        Assert.False(_playerData.IsBlessingUnlocked("craft_patron"));
        Assert.True(_playerData.IsBlessingUnlocked(BlessingId));                 // generic kept
        Assert.Equal(300, _playerData.GetFavor(DeityDomain.Craft));             // zero refund
        Assert.Equal(300, _playerData.GetTotalFavorEarned(DeityDomain.Craft));  // lifetime untouched
    }

    [Fact]
    public void StripDomainLockedForApostasy_CascadesDependentsOfDomainLockedParent()
    {
        // Domain-locked parent with a generic branch child that depends solely on it; the child
        // is orphaned and cascades even though it is not itself domain-locked.
        _registry.RegisterBlessing(new Blessing("craft_patron", "Craft Patron", DeityDomain.Craft)
        {
            Kind = BlessingKind.Player,
            Cost = 200,
            RequiresPatron = true
        });
        _registry.RegisterBlessing(new Blessing("patron_child", "Patron Child", DeityDomain.Craft)
        {
            Kind = BlessingKind.Player,
            Cost = 40,
            Branch = "branchA",
            PrerequisiteBlessings = new List<string> { "craft_patron" }
        });
        _playerData.UnlockBlessing("craft_patron");
        _playerData.UnlockBlessing("patron_child");

        var stripped = _service.StripDomainLockedForApostasy(PlayerUid);

        Assert.Contains("craft_patron", stripped);
        Assert.Contains("patron_child", stripped);
        Assert.False(_playerData.IsBlessingUnlocked("craft_patron"));
        Assert.False(_playerData.IsBlessingUnlocked("patron_child"));
    }

    [Fact]
    public void StripDomainLockedForApostasy_RefreshesEffects_AndNotifies_OnlyWhenStripped()
    {
        _registry.RegisterBlessing(new Blessing("craft_patron", "Craft Patron", DeityDomain.Craft)
        {
            Kind = BlessingKind.Player,
            Cost = 200,
            RequiresPatron = true
        });
        _playerData.UnlockBlessing("craft_patron");

        _service.StripDomainLockedForApostasy(PlayerUid);

        _effectSystem.Verify(e => e.RefreshPlayerBlessings(PlayerUid), Times.Once);
        _dataManager.Verify(m => m.NotifyPlayerDataChanged(PlayerUid), Times.Once);
    }

    [Fact]
    public void StripDomainLockedForApostasy_NoDomainLockedOwned_NoSideEffects()
    {
        _playerData.UnlockBlessing(BlessingId); // generic only

        var stripped = _service.StripDomainLockedForApostasy(PlayerUid);

        Assert.Empty(stripped);
        Assert.True(_playerData.IsBlessingUnlocked(BlessingId));
        _effectSystem.Verify(e => e.RefreshPlayerBlessings(It.IsAny<string>()), Times.Never);
        _dataManager.Verify(m => m.NotifyPlayerDataChanged(It.IsAny<string>()), Times.Never);
    }
}
