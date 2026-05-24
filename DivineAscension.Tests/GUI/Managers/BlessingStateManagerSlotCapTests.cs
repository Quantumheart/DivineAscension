using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Managers;
using DivineAscension.GUI.Models.Blessing.Tab;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Moq;
using Vintagestory.API.Client;

namespace DivineAscension.Tests.GUI.Managers;

/// <summary>
///     Covers the personal blessing unlock-slot cap affordance (#446): the client-side mirror
///     of the server-authoritative cap (#444). At the cap, otherwise-eligible player blessings
///     flip to blocked-by-cap; religion blessings are unaffected; an unknown cap (0) is open.
/// </summary>
[ExcludeFromCodeCoverage]
public class BlessingStateManagerSlotCapTests
{
    private readonly Mock<ICoreClientAPI> _mockApi = new();
    private readonly Mock<ISoundManager> _mockSoundManager = new();
    private readonly Mock<IUiService> _mockUiService = new();
    private readonly BlessingStateManager _sut;

    public BlessingStateManagerSlotCapTests()
    {
        _sut = new BlessingStateManager(_mockApi.Object, _mockUiService.Object, _mockSoundManager.Object);
    }

    private static Blessing CreatePlayerBlessing(string id) => new()
    {
        BlessingId = id,
        Kind = BlessingKind.Player,
        RequiredFavorRank = 0,
        PrerequisiteBlessings = new List<string>()
    };

    private static Blessing CreateReligionBlessing(string id) => new()
    {
        BlessingId = id,
        Kind = BlessingKind.Religion,
        RequiredPrestigeRank = 0,
        PrerequisiteBlessings = new List<string>()
    };

    private void Refresh()
    {
        var ranks = new Dictionary<DeityDomain, int>
        {
            [DeityDomain.None] = 4,
            [DeityDomain.Craft] = 4,
            [DeityDomain.Wild] = 4,
            [DeityDomain.Harvest] = 4,
            [DeityDomain.Stone] = 4,
            [DeityDomain.Conquest] = 4
        };
        _sut.RefreshAllBlessingStates(ranks, 4, DeityDomain.None);
    }

    [Fact]
    public void UnlockedPlayerBlessingCount_CountsOnlyUnlockedPlayerBlessings()
    {
        _sut.LoadBlessingStates(
            new List<Blessing> { CreatePlayerBlessing("p1"), CreatePlayerBlessing("p2") },
            new List<Blessing> { CreateReligionBlessing("r1") });
        _sut.SetBlessingUnlocked("p1", true);
        _sut.SetBlessingUnlocked("r1", true);

        Assert.Equal(1, _sut.UnlockedPlayerBlessingCount);
    }

    [Fact]
    public void RefreshAllBlessingStates_AtCap_EligiblePlayerBlessing_BlockedByCap()
    {
        _sut.LoadBlessingStates(
            new List<Blessing> { CreatePlayerBlessing("p1"), CreatePlayerBlessing("p2") },
            new List<Blessing>());
        _sut.SetBlessingUnlocked("p1", true);
        _sut.MaxPlayerBlessingSlots = 1; // one slot, one filled → at cap

        Refresh();

        var p2 = _sut.State.PlayerBlessingStates["p2"];
        Assert.False(p2.CanUnlock);
        Assert.True(p2.BlockedByCap);
        Assert.Equal(BlessingNodeVisualState.Locked, p2.VisualState);
    }

    [Fact]
    public void RefreshAllBlessingStates_UnderCap_EligiblePlayerBlessing_CanUnlock()
    {
        _sut.LoadBlessingStates(
            new List<Blessing> { CreatePlayerBlessing("p1"), CreatePlayerBlessing("p2") },
            new List<Blessing>());
        _sut.SetBlessingUnlocked("p1", true);
        _sut.MaxPlayerBlessingSlots = 5; // plenty of room

        Refresh();

        var p2 = _sut.State.PlayerBlessingStates["p2"];
        Assert.True(p2.CanUnlock);
        Assert.False(p2.BlockedByCap);
    }

    [Fact]
    public void RefreshAllBlessingStates_MaxSlotsUnknown_DoesNotEnforceCap()
    {
        _sut.LoadBlessingStates(
            new List<Blessing> { CreatePlayerBlessing("p1"), CreatePlayerBlessing("p2") },
            new List<Blessing>());
        _sut.SetBlessingUnlocked("p1", true);
        _sut.MaxPlayerBlessingSlots = 0; // not yet synced from server

        Refresh();

        var p2 = _sut.State.PlayerBlessingStates["p2"];
        Assert.True(p2.CanUnlock);
        Assert.False(p2.BlockedByCap);
    }

    [Fact]
    public void RefreshAllBlessingStates_AtCap_ReligionBlessing_NotBlockedByCap()
    {
        // The personal cap is player-only; communal vows must stay unlockable regardless.
        _sut.LoadBlessingStates(
            new List<Blessing> { CreatePlayerBlessing("p1") },
            new List<Blessing> { CreateReligionBlessing("r1") });
        _sut.SetBlessingUnlocked("p1", true);
        _sut.MaxPlayerBlessingSlots = 1; // player at cap

        Refresh();

        var r1 = _sut.State.ReligionBlessingStates["r1"];
        Assert.True(r1.CanUnlock);
        Assert.False(r1.BlockedByCap);
    }

    [Fact]
    public void UnlockClicked_WhenBlockedByCap_ShowsCapMessage_AndSendsNoRequest()
    {
        _sut.LoadBlessingStates(
            new List<Blessing> { CreatePlayerBlessing("p1"), CreatePlayerBlessing("p2") },
            new List<Blessing>());
        _sut.SetBlessingUnlocked("p1", true);
        _sut.MaxPlayerBlessingSlots = 1;
        Refresh();
        _sut.State.TreeState.SelectedBlessingId = "p2";

        var result = new BlessingTabRenderResult(
            new List<TreeEvent>(),
            new List<ActionsEvent> { new ActionsEvent.UnlockClicked() },
            null,
            100f);

        _sut.ProcessBlessingTabEvents(result);

        _mockApi.Verify(a => a.ShowChatMessage(
            It.Is<string>(s => s.Contains(LocalizationKeys.UI_BLESSING_TOOLTIP_SLOT_CAP))), Times.Once);
        _mockUiService.Verify(u => u.RequestBlessingUnlock(It.IsAny<string>()), Times.Never);
    }
}
