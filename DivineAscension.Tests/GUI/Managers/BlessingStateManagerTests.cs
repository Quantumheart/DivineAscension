using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Managers;
using DivineAscension.GUI.Models.Blessing.Tab;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Moq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace DivineAscension.Tests.GUI.Managers;

[ExcludeFromCodeCoverage]
public class BlessingStateManagerTests
{
    private readonly Mock<ICoreClientAPI> _mockApi;
    private readonly Mock<Entity> _mockEntity;
    private readonly Mock<ISoundManager> _mockSoundManager;
    private readonly Mock<IUiService> _mockUiService;
    private readonly Mock<IWorldAccessor> _mockWorld;
    private readonly BlessingStateManager _sut;

    public BlessingStateManagerTests()
    {
        _mockApi = new Mock<ICoreClientAPI>();
        _mockUiService = new Mock<IUiService>();
        _mockWorld = new Mock<IWorldAccessor>();
        _mockEntity = new Mock<Entity>();
        _mockSoundManager = new Mock<ISoundManager>();


        _sut = new BlessingStateManager(_mockApi.Object, _mockUiService.Object, _mockSoundManager.Object);
    }

    #region Helper Methods

    private static Blessing CreateBlessing(
        string id,
        BlessingKind kind,
        int requiredFavorRank = 0,
        int requiredPrestigeRank = 0,
        List<string>? prerequisites = null,
        string? branch = null)
    {
        return new Blessing
        {
            BlessingId = id,
            Kind = kind,
            RequiredFavorRank = requiredFavorRank,
            RequiredPrestigeRank = requiredPrestigeRank,
            PrerequisiteBlessings = prerequisites ?? new List<string>(),
            Branch = branch
        };
    }

    /// <summary>
    ///     Backward-compatible refresh helper for tests written before Phase 3's per-deity rank dict.
    ///     Treats <paramref name="favorRank"/> as the rank for every deity, patron = Craft (default Blessing.Domain).
    /// </summary>
    private void Refresh(int favorRank, int prestigeRank)
    {
        // Seed every DeityDomain (incl. None, which is the default for blessings created without an explicit Domain)
        // with the same rank so legacy tests keep their single-rank semantics.
        var dict = new Dictionary<DeityDomain, int>
        {
            [DeityDomain.None] = favorRank,
            [DeityDomain.Craft] = favorRank,
            [DeityDomain.Wild] = favorRank,
            [DeityDomain.Harvest] = favorRank,
            [DeityDomain.Stone] = favorRank,
            [DeityDomain.Conquest] = favorRank
        };
        _sut.RefreshAllBlessingStates(dict, prestigeRank, DeityDomain.None);
    }

    /// <summary>
    ///     Mirrors what <see cref="BlessingStateManager.DrawVowsTab"/> captures each frame: the
    ///     viewing player's founder status, which gates communal-vow unlocks (#453).
    /// </summary>
    private void SetReligionFounder(bool value) => _sut.IsReligionFounder = value;

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullApi_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BlessingStateManager(null!, _mockUiService.Object, _mockSoundManager.Object));
    }

    [Fact]
    public void Constructor_WithNullUiService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BlessingStateManager(_mockApi.Object, null!, _mockSoundManager.Object));
    }

    [Fact]
    public void Constructor_WithNullSoundManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BlessingStateManager(_mockApi.Object, _mockUiService.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var manager = new BlessingStateManager(_mockApi.Object, _mockUiService.Object, _mockSoundManager.Object!);

        Assert.NotNull(manager);
        Assert.NotNull(manager.State);
    }

    #endregion

    #region ProcessBlessingTabEvents (render-result events)

    [Fact]
    public void ProcessBlessingTabEvents_SetsHoveringBlessingId_FromResult()
    {
        // Arrange
        var result = new BlessingTabRenderResult(
            new List<TreeEvent>(),
            new List<ActionsEvent>(),
            "hovered-id",
            100f);

        // Act
        _sut.ProcessBlessingTabEvents(result);
        // InvokeProcessEvents(result);

        // Assert
        Assert.Equal("hovered-id", _sut.State.TreeState.HoveringBlessingId);
    }

    [Fact]
    public void ProcessBlessingTabEvents_OnPlayerTreeScrollChanged_UpdatesPlayerScrollState()
    {
        // Arrange
        var result = new BlessingTabRenderResult(
            new List<TreeEvent>
            {
                new TreeEvent.PlayerTreeScrollChanged(3.25f, -1.5f)
            },
            new List<ActionsEvent>(),
            null,
            100f);

        // Act
        _sut.ProcessBlessingTabEvents(result);

        // Assert
        Assert.Equal(3.25f, _sut.State.TreeState.PlayerScrollState.X);
        Assert.Equal(-1.5f, _sut.State.TreeState.PlayerScrollState.Y);
    }

    [Fact]
    public void ProcessBlessingTabEvents_OnReligionTreeScrollChanged_UpdatesReligionScrollState()
    {
        // Arrange
        var result = new BlessingTabRenderResult(
            new List<TreeEvent>
            {
                new TreeEvent.ReligionTreeScrollChanged(-2f, 5f)
            },
            new List<ActionsEvent>(),
            null,
            100f);

        // Act
        _sut.ProcessBlessingTabEvents(result);

        // Assert
        Assert.Equal(-2f, _sut.State.TreeState.ReligionScrollState.X);
        Assert.Equal(5f, _sut.State.TreeState.ReligionScrollState.Y);
    }

    [Fact]
    public void ProcessBlessingTabEvents_OnRequestedPageScrollY_UpdatesBlessingsPageScrollY()
    {
        // Arrange
        var result = new BlessingTabRenderResult(
            new List<TreeEvent>(),
            new List<ActionsEvent>(),
            null,
            100f,
            requestedActiveDeity: null,
            requestedVowsScrollY: null,
            requestedPageScrollY: 42.5f);

        // Act
        _sut.ProcessBlessingTabEvents(result);

        // Assert — wheel-scroll on III.ii commits to the blessings-page scroll, not vows.
        Assert.Equal(42.5f, _sut.State.BlessingsPageScrollY);
        Assert.Equal(0f, _sut.State.VowsPageScrollY);
    }

    #endregion

    #region Unlock flow: double-click stages confirmation, confirm/cancel resolve it

    private static BlessingTabRenderResult DoubleClick(string blessingId) =>
        new(
            new List<TreeEvent> { new TreeEvent.DoubleClicked(blessingId) },
            new List<ActionsEvent>(),
            null,
            100f);

    private static BlessingTabRenderResult Actions(params ActionsEvent[] events) =>
        new(
            new List<TreeEvent>(),
            events,
            null,
            100f);

    [Fact]
    public void DoubleClick_WithNonExistentBlessing_DoesNothing()
    {
        // Act — double-click an id that isn't loaded.
        _sut.ProcessBlessingTabEvents(DoubleClick("ghost"));

        // Assert
        Assert.Null(_sut.State.PendingUnlockBlessingId);
        _mockUiService.Verify(u => u.RequestBlessingUnlock(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void DoubleClick_WithInvalidBlessingId_ShowsChatError_AndNoRequest()
    {
        // Arrange — eligible but empty-id blessing reaches the invalid-id guard.
        var invalid = CreateBlessing(string.Empty, BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { invalid }, new List<Blessing>());
        _sut.State.PlayerBlessingStates[string.Empty].CanUnlock = true;
        _sut.State.PlayerBlessingStates[string.Empty].IsUnlocked = false;

        // Act
        _sut.ProcessBlessingTabEvents(DoubleClick(string.Empty));

        // Assert
        _mockApi.Verify(a => a.ShowChatMessage(It.Is<string>(s => s.Contains("Invalid blessing ID"))), Times.Once);
        _mockUiService.Verify(u => u.RequestBlessingUnlock(It.IsAny<string>()), Times.Never);
        Assert.Null(_sut.State.PendingUnlockBlessingId);
    }

    [Fact]
    public void DoubleClick_WithValidSelection_OpensConfirmation_DoesNotSendRequest()
    {
        // Arrange
        var blessing = CreateBlessing("bless-1", BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        var node = _sut.State.PlayerBlessingStates["bless-1"];
        node.CanUnlock = true;
        node.IsUnlocked = false;

        // Act
        _sut.ProcessBlessingTabEvents(DoubleClick("bless-1"));

        // Assert — confirmation is staged, but no favor is spent until the player confirms (#453).
        Assert.Equal("bless-1", _sut.State.PendingUnlockBlessingId);
        _mockUiService.Verify(u => u.RequestBlessingUnlock(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void UnlockConfirmed_AfterDoubleClick_SendsRequest_AndClearsPending()
    {
        // Arrange
        var blessing = CreateBlessing("bless-1", BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        var node = _sut.State.PlayerBlessingStates["bless-1"];
        node.CanUnlock = true;
        node.IsUnlocked = false;

        _sut.ProcessBlessingTabEvents(DoubleClick("bless-1"));

        // Act — player confirms.
        _sut.ProcessBlessingTabEvents(Actions(new ActionsEvent.UnlockConfirmed()));

        // Assert — request dispatched once and pending cleared.
        _mockUiService.Verify(u => u.RequestBlessingUnlock("bless-1"), Times.Once);
        Assert.Null(_sut.State.PendingUnlockBlessingId);
    }

    [Fact]
    public void UnlockCanceled_AfterDoubleClick_DoesNotSendRequest_AndClearsPending()
    {
        // Arrange
        var blessing = CreateBlessing("bless-1", BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        var node = _sut.State.PlayerBlessingStates["bless-1"];
        node.CanUnlock = true;
        node.IsUnlocked = false;

        _sut.ProcessBlessingTabEvents(DoubleClick("bless-1"));

        // Act — player cancels.
        _sut.ProcessBlessingTabEvents(Actions(new ActionsEvent.UnlockCanceled()));

        // Assert — nothing dispatched, pending cleared with no side effects.
        _mockUiService.Verify(u => u.RequestBlessingUnlock(It.IsAny<string>()), Times.Never);
        Assert.Null(_sut.State.PendingUnlockBlessingId);
    }

    [Fact]
    public void DoubleClick_OnOwnedBlessing_OpensUnlearnConfirmation_DoesNotSendRequest()
    {
        // Arrange — an already-unlocked personal blessing.
        var blessing = CreateBlessing("bless-1", BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        _sut.State.PlayerBlessingStates["bless-1"].IsUnlocked = true;

        // Act
        _sut.ProcessBlessingTabEvents(DoubleClick("bless-1"));

        // Assert — unlearn confirmation staged; no unlearn dispatched until confirm (#459).
        Assert.Equal("bless-1", _sut.State.PendingUnlearnBlessingId);
        Assert.Null(_sut.State.PendingUnlockBlessingId);
        _mockUiService.Verify(u => u.RequestBlessingUnlearn(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void UnlearnConfirmed_AfterDoubleClick_SendsRequest_AndClearsPending()
    {
        // Arrange
        var blessing = CreateBlessing("bless-1", BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        _sut.State.PlayerBlessingStates["bless-1"].IsUnlocked = true;
        _sut.ProcessBlessingTabEvents(DoubleClick("bless-1"));

        // Act — player confirms.
        _sut.ProcessBlessingTabEvents(Actions(new ActionsEvent.UnlearnConfirmed()));

        // Assert
        _mockUiService.Verify(u => u.RequestBlessingUnlearn("bless-1"), Times.Once);
        Assert.Null(_sut.State.PendingUnlearnBlessingId);
    }

    [Fact]
    public void UnlearnCanceled_AfterDoubleClick_DoesNotSendRequest_AndClearsPending()
    {
        // Arrange
        var blessing = CreateBlessing("bless-1", BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        _sut.State.PlayerBlessingStates["bless-1"].IsUnlocked = true;
        _sut.ProcessBlessingTabEvents(DoubleClick("bless-1"));

        // Act — player cancels.
        _sut.ProcessBlessingTabEvents(Actions(new ActionsEvent.UnlearnCanceled()));

        // Assert
        _mockUiService.Verify(u => u.RequestBlessingUnlearn(It.IsAny<string>()), Times.Never);
        Assert.Null(_sut.State.PendingUnlearnBlessingId);
    }

    [Fact]
    public void DoubleClick_OnOwnedReligionBlessing_DoesNotOpenUnlearnConfirmation()
    {
        // Arrange — owned religion vow; unlearn is personal-only in slice 1.
        var vow = CreateBlessing("vow-1", BlessingKind.Religion);
        _sut.LoadBlessingStates(new List<Blessing>(), new List<Blessing> { vow });
        _sut.State.ReligionBlessingStates["vow-1"].IsUnlocked = true;

        // Act
        _sut.ProcessBlessingTabEvents(DoubleClick("vow-1"));

        // Assert
        Assert.Null(_sut.State.PendingUnlearnBlessingId);
        _mockUiService.Verify(u => u.RequestBlessingUnlearn(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void WhileConfirmationOpen_BackgroundTreeAndDeityEventsAreIgnored()
    {
        // Arrange — stage a confirmation for bless-1.
        var b1 = CreateBlessing("bless-1", BlessingKind.Player);
        var b2 = CreateBlessing("bless-2", BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { b1, b2 }, new List<Blessing>());
        _sut.State.PlayerBlessingStates["bless-1"].CanUnlock = true;
        _sut.State.PlayerBlessingStates["bless-2"].CanUnlock = true;
        _sut.ProcessBlessingTabEvents(DoubleClick("bless-1"));
        Assert.Equal("bless-1", _sut.State.PendingUnlockBlessingId);

        // Act — clicks "behind" the modal: select another node, switch deity, double-click.
        _sut.ProcessBlessingTabEvents(new BlessingTabRenderResult(
            new List<TreeEvent>
            {
                new TreeEvent.Selected("bless-2"),
                new TreeEvent.DoubleClicked("bless-2")
            },
            new List<ActionsEvent>(),
            null,
            100f,
            requestedActiveDeity: DeityDomain.Wild));

        // Assert — nothing behind the dialog took effect; the pending unlock is untouched.
        Assert.Equal("bless-1", _sut.State.PendingUnlockBlessingId);
        Assert.Equal("bless-1", _sut.State.TreeState.SelectedBlessingId);
        Assert.Equal(DeityDomain.Craft, _sut.State.ActiveDeity);
        _mockUiService.Verify(u => u.RequestBlessingUnlock(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void UnlockConfirmed_WithNoPending_DoesNothing()
    {
        // Act — confirm with no staged unlock (e.g. stale event).
        _sut.ProcessBlessingTabEvents(Actions(new ActionsEvent.UnlockConfirmed()));

        // Assert
        _mockUiService.Verify(u => u.RequestBlessingUnlock(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void DoubleClick_ReligionKind_AsFounder_StagesConfirmation_RequestSentOnConfirm()
    {
        // Arrange — a founder may swear communal vows. DrawVowsTab captures founder status;
        // simulate that the player founded the order.
        var vow = CreateBlessing("vow-1", BlessingKind.Religion);
        _sut.LoadBlessingStates(new List<Blessing>(), new List<Blessing> { vow });
        var node = _sut.State.ReligionBlessingStates["vow-1"];
        node.CanUnlock = true;
        node.IsUnlocked = false;
        SetReligionFounder(true);

        // Act — double-click stages, no request yet.
        _sut.ProcessBlessingTabEvents(DoubleClick("vow-1"));

        Assert.Equal("vow-1", _sut.State.PendingUnlockBlessingId);
        _mockUiService.Verify(u => u.RequestBlessingUnlock(It.IsAny<string>()), Times.Never);

        // Confirm dispatches the vow.
        _sut.ProcessBlessingTabEvents(Actions(new ActionsEvent.UnlockConfirmed()));

        _mockUiService.Verify(u => u.RequestBlessingUnlock("vow-1"), Times.Once);
    }

    [Fact]
    public void DoubleClick_ReligionKind_AsNonFounder_IsBlocked_NoConfirmationNoRequest()
    {
        // Arrange — non-founders cannot swear communal vows; the gate mirrors the server.
        var vow = CreateBlessing("vow-1", BlessingKind.Religion);
        _sut.LoadBlessingStates(new List<Blessing>(), new List<Blessing> { vow });
        var node = _sut.State.ReligionBlessingStates["vow-1"];
        node.CanUnlock = true;
        node.IsUnlocked = false;
        SetReligionFounder(false);

        // Act
        _sut.ProcessBlessingTabEvents(DoubleClick("vow-1"));

        // Assert — no confirmation staged, no request, founder-only message shown.
        Assert.Null(_sut.State.PendingUnlockBlessingId);
        _mockUiService.Verify(u => u.RequestBlessingUnlock(It.IsAny<string>()), Times.Never);
        _mockApi.Verify(a => a.ShowChatMessage(It.IsAny<string>()), Times.Once);
    }

    #endregion

    #region LoadBlessingStates Tests

    [Fact]
    public void LoadBlessingStates_WithEmptyLists_ClearsExistingStates()
    {
        // Arrange - add some existing states
        var existingBlessing = CreateBlessing("existing", BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { existingBlessing }, new List<Blessing>());

        // Act
        _sut.LoadBlessingStates(new List<Blessing>(), new List<Blessing>());

        // Assert
        Assert.Empty(_sut.State.PlayerBlessingStates);
        Assert.Empty(_sut.State.ReligionBlessingStates);
    }

    [Fact]
    public void LoadBlessingStates_WithPlayerBlessings_PopulatesPlayerStates()
    {
        // Arrange
        var blessing1 = CreateBlessing("player1", BlessingKind.Player);
        var blessing2 = CreateBlessing("player2", BlessingKind.Player);
        var playerBlessings = new List<Blessing> { blessing1, blessing2 };

        // Act
        _sut.LoadBlessingStates(playerBlessings, new List<Blessing>());

        // Assert
        Assert.Equal(2, _sut.State.PlayerBlessingStates.Count);
        Assert.True(_sut.State.PlayerBlessingStates.ContainsKey("player1"));
        Assert.True(_sut.State.PlayerBlessingStates.ContainsKey("player2"));
    }

    [Fact]
    public void LoadBlessingStates_WithReligionBlessings_PopulatesReligionStates()
    {
        // Arrange
        var blessing1 = CreateBlessing("religion1", BlessingKind.Religion);
        var blessing2 = CreateBlessing("religion2", BlessingKind.Religion);
        var religionBlessings = new List<Blessing> { blessing1, blessing2 };

        // Act
        _sut.LoadBlessingStates(new List<Blessing>(), religionBlessings);

        // Assert
        Assert.Equal(2, _sut.State.ReligionBlessingStates.Count);
        Assert.True(_sut.State.ReligionBlessingStates.ContainsKey("religion1"));
        Assert.True(_sut.State.ReligionBlessingStates.ContainsKey("religion2"));
    }

    [Fact]
    public void LoadBlessingStates_WithMixedBlessings_PopulatesBothStates()
    {
        // Arrange
        var playerBlessing = CreateBlessing("player1", BlessingKind.Player);
        var religionBlessing = CreateBlessing("religion1", BlessingKind.Religion);

        // Act
        _sut.LoadBlessingStates(
            new List<Blessing> { playerBlessing },
            new List<Blessing> { religionBlessing });

        // Assert
        Assert.Single(_sut.State.PlayerBlessingStates);
        Assert.Single(_sut.State.ReligionBlessingStates);
    }

    #endregion

    #region SetBlessingUnlocked Tests

    [Fact]
    public void SetBlessingUnlocked_WithValidPlayerBlessing_UpdatesUnlockedState()
    {
        // Arrange
        var blessing = CreateBlessing("test", BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());

        // Act
        _sut.SetBlessingUnlocked("test", true);

        // Assert
        Assert.True(_sut.State.PlayerBlessingStates["test"].IsUnlocked);
    }

    [Fact]
    public void SetBlessingUnlocked_WithValidReligionBlessing_UpdatesUnlockedState()
    {
        // Arrange
        var blessing = CreateBlessing("test", BlessingKind.Religion);
        _sut.LoadBlessingStates(new List<Blessing>(), new List<Blessing> { blessing });

        // Act
        _sut.SetBlessingUnlocked("test", true);

        // Assert
        Assert.True(_sut.State.ReligionBlessingStates["test"].IsUnlocked);
    }

    [Fact]
    public void SetBlessingUnlocked_WithNonExistentBlessing_DoesNotThrow()
    {
        // Arrange
        _sut.LoadBlessingStates(new List<Blessing>(), new List<Blessing>());

        // Act & Assert - should not throw
        var exception = Record.Exception(() => _sut.SetBlessingUnlocked("nonexistent", true));
        Assert.Null(exception);
    }

    [Fact]
    public void SetBlessingUnlocked_SetToFalse_UpdatesState()
    {
        // Arrange
        var blessing = CreateBlessing("test", BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        _sut.SetBlessingUnlocked("test", true);

        // Act
        _sut.SetBlessingUnlocked("test", false);

        // Assert
        Assert.False(_sut.State.PlayerBlessingStates["test"].IsUnlocked);
    }

    #endregion

    #region Selection Tests

    [Fact]
    public void SelectBlessing_SetsSelectedBlessingId()
    {
        // Act
        _sut.SelectBlessing("blessing123");

        // Assert
        Assert.Equal("blessing123", _sut.State.TreeState.SelectedBlessingId);
    }

    [Fact]
    public void ClearSelection_ClearsSelectedBlessingId()
    {
        // Arrange
        _sut.SelectBlessing("blessing123");

        // Act
        _sut.ClearSelection();

        // Assert
        Assert.Null(_sut.State.TreeState.SelectedBlessingId);
    }

    [Fact]
    public void GetSelectedBlessingState_WithNoSelection_ReturnsNull()
    {
        // Act
        var result = _sut.GetSelectedBlessingState();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetSelectedBlessingState_WithEmptySelection_ReturnsNull()
    {
        // Arrange
        _sut.State.TreeState.SelectedBlessingId = string.Empty;

        // Act
        var result = _sut.GetSelectedBlessingState();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetSelectedBlessingState_WithValidPlayerSelection_ReturnsState()
    {
        // Arrange
        var blessing = CreateBlessing("test", BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        _sut.SelectBlessing("test");

        // Act
        var result = _sut.GetSelectedBlessingState();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Blessing.BlessingId);
    }

    [Fact]
    public void GetSelectedBlessingState_WithValidReligionSelection_ReturnsState()
    {
        // Arrange
        var blessing = CreateBlessing("test", BlessingKind.Religion);
        _sut.LoadBlessingStates(new List<Blessing>(), new List<Blessing> { blessing });
        _sut.SelectBlessing("test");

        // Act
        var result = _sut.GetSelectedBlessingState();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Blessing.BlessingId);
    }

    [Fact]
    public void GetSelectedBlessingState_WithNonExistentSelection_ReturnsNull()
    {
        // Arrange
        _sut.LoadBlessingStates(new List<Blessing>(), new List<Blessing>());
        _sut.SelectBlessing("nonexistent");

        // Act
        var result = _sut.GetSelectedBlessingState();

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region RefreshAllBlessingStates Tests

    [Fact]
    public void RefreshAllBlessingStates_UpdatesCanUnlockForPlayerBlessings()
    {
        // Arrange
        var blessing = CreateBlessing("test", BlessingKind.Player, 2);
        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());

        // Act - with sufficient favor rank
        Refresh(3, 0);

        // Assert
        Assert.True(_sut.State.PlayerBlessingStates["test"].CanUnlock);
    }

    [Fact]
    public void RefreshAllBlessingStates_WithInsufficientFavorRank_SetsCanUnlockFalse()
    {
        // Arrange
        var blessing = CreateBlessing("test", BlessingKind.Player, 5);
        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());

        // Act - with insufficient favor rank
        Refresh(2, 0);

        // Assert
        Assert.False(_sut.State.PlayerBlessingStates["test"].CanUnlock);
    }

    [Fact]
    public void RefreshAllBlessingStates_UpdatesCanUnlockForReligionBlessings()
    {
        // Arrange
        var blessing = CreateBlessing("test", BlessingKind.Religion, requiredPrestigeRank: 2);
        _sut.LoadBlessingStates(new List<Blessing>(), new List<Blessing> { blessing });

        // Act - with sufficient prestige rank
        Refresh(0, 3);

        // Assert
        Assert.True(_sut.State.ReligionBlessingStates["test"].CanUnlock);
    }

    [Fact]
    public void RefreshAllBlessingStates_WithInsufficientPrestigeRank_SetsCanUnlockFalse()
    {
        // Arrange
        var blessing = CreateBlessing("test", BlessingKind.Religion, requiredPrestigeRank: 5);
        _sut.LoadBlessingStates(new List<Blessing>(), new List<Blessing> { blessing });

        // Act - with insufficient prestige rank
        Refresh(0, 2);

        // Assert
        Assert.False(_sut.State.ReligionBlessingStates["test"].CanUnlock);
    }

    [Fact]
    public void RefreshAllBlessingStates_WithUnlockedBlessing_SetsCanUnlockFalse()
    {
        // Arrange
        var blessing = CreateBlessing("test", BlessingKind.Player, 1);
        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        _sut.SetBlessingUnlocked("test", true);

        // Act
        Refresh(5, 5);

        // Assert - already unlocked, so CanUnlock should be false
        Assert.False(_sut.State.PlayerBlessingStates["test"].CanUnlock);
    }

    [Fact]
    public void RefreshAllBlessingStates_WithUnmetPrerequisite_SetsCanUnlockFalse()
    {
        // Arrange
        var prereqBlessing = CreateBlessing("prereq", BlessingKind.Player);
        var dependentBlessing = CreateBlessing("dependent", BlessingKind.Player,
            prerequisites: new List<string> { "prereq" });

        _sut.LoadBlessingStates(
            new List<Blessing> { prereqBlessing, dependentBlessing },
            new List<Blessing>());

        // Act - prereq not unlocked
        Refresh(10, 10);

        // Assert
        Assert.False(_sut.State.PlayerBlessingStates["dependent"].CanUnlock);
    }

    [Fact]
    public void RefreshAllBlessingStates_WithMetPrerequisite_SetsCanUnlockTrue()
    {
        // Arrange
        var prereqBlessing = CreateBlessing("prereq", BlessingKind.Player);
        var dependentBlessing = CreateBlessing("dependent", BlessingKind.Player,
            prerequisites: new List<string> { "prereq" });

        _sut.LoadBlessingStates(
            new List<Blessing> { prereqBlessing, dependentBlessing },
            new List<Blessing>());
        _sut.SetBlessingUnlocked("prereq", true);

        // Act
        Refresh(10, 10);

        // Assert
        Assert.True(_sut.State.PlayerBlessingStates["dependent"].CanUnlock);
    }

    [Fact]
    public void RefreshAllBlessingStates_WithMultiplePrerequisites_BranchBlessing_AllMustBeUnlocked()
    {
        // Arrange - blessing with a branch uses AND logic (all prerequisites required)
        var prereq1 = CreateBlessing("prereq1", BlessingKind.Player);
        var prereq2 = CreateBlessing("prereq2", BlessingKind.Player);
        var dependent = CreateBlessing("dependent", BlessingKind.Player,
            prerequisites: new List<string> { "prereq1", "prereq2" },
            branch: "TestBranch"); // Non-null branch = AND logic

        _sut.LoadBlessingStates(
            new List<Blessing> { prereq1, prereq2, dependent },
            new List<Blessing>());

        // Only unlock one prerequisite
        _sut.SetBlessingUnlocked("prereq1", true);

        // Act
        Refresh(10, 10);

        // Assert - should be false because prereq2 is not unlocked (AND logic)
        Assert.False(_sut.State.PlayerBlessingStates["dependent"].CanUnlock);
    }

    [Fact]
    public void RefreshAllBlessingStates_CapstoneRequiresPatron_NonPatronBlocked()
    {
        // Phase 3: a RequiresPatron blessing in a non-patron domain stays locked.
        var capstone = CreateBlessing("avatar_of_wild", BlessingKind.Player);
        capstone.Domain = DeityDomain.Wild;
        capstone.RequiresPatron = true;

        _sut.LoadBlessingStates(new List<Blessing> { capstone }, new List<Blessing>());

        var dict = new Dictionary<DeityDomain, int> { [DeityDomain.Wild] = 4 };
        _sut.RefreshAllBlessingStates(dict, 4, DeityDomain.Craft);

        Assert.False(_sut.State.PlayerBlessingStates["avatar_of_wild"].CanUnlock);
    }

    [Fact]
    public void RefreshAllBlessingStates_CapstoneRequiresPatron_PatronCanUnlock()
    {
        var capstone = CreateBlessing("avatar_of_wild", BlessingKind.Player);
        capstone.Domain = DeityDomain.Wild;
        capstone.RequiresPatron = true;

        _sut.LoadBlessingStates(new List<Blessing> { capstone }, new List<Blessing>());

        var dict = new Dictionary<DeityDomain, int> { [DeityDomain.Wild] = 4 };
        _sut.RefreshAllBlessingStates(dict, 4, DeityDomain.Wild);

        Assert.True(_sut.State.PlayerBlessingStates["avatar_of_wild"].CanUnlock);
    }

    [Fact]
    public void RefreshAllBlessingStates_NonPatron_SetsCostMultiplier1_5()
    {
        var blessing = CreateBlessing("test", BlessingKind.Player);
        blessing.Domain = DeityDomain.Wild;

        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());

        var dict = new Dictionary<DeityDomain, int> { [DeityDomain.Wild] = 4 };
        _sut.RefreshAllBlessingStates(dict, 0, DeityDomain.Craft);

        Assert.Equal(1.5f, _sut.State.PlayerBlessingStates["test"].NonPatronCostMultiplier);
    }

    [Fact]
    public void RefreshAllBlessingStates_Patron_SetsCostMultiplier1_0()
    {
        var blessing = CreateBlessing("test", BlessingKind.Player);
        blessing.Domain = DeityDomain.Wild;

        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());

        var dict = new Dictionary<DeityDomain, int> { [DeityDomain.Wild] = 4 };
        _sut.RefreshAllBlessingStates(dict, 0, DeityDomain.Wild);

        Assert.Equal(1.0f, _sut.State.PlayerBlessingStates["test"].NonPatronCostMultiplier);
    }

    [Fact]
    public void RefreshAllBlessingStates_PerDeityFavorRank_GatesByDomain()
    {
        // Stone-patron player with no Conquest favor is blocked from a Conquest tier-3 blessing.
        var blessing = CreateBlessing("conquest_t3", BlessingKind.Player, requiredFavorRank: 3);
        blessing.Domain = DeityDomain.Conquest;

        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());

        var dict = new Dictionary<DeityDomain, int>
        {
            [DeityDomain.Stone] = 4,
            [DeityDomain.Conquest] = 0
        };
        _sut.RefreshAllBlessingStates(dict, 0, DeityDomain.Stone);

        Assert.False(_sut.State.PlayerBlessingStates["conquest_t3"].CanUnlock);
    }

    [Fact]
    public void RefreshAllBlessingStates_WithMultiplePrerequisites_CapstoneBlessing_OnlyOneRequired()
    {
        // Arrange - capstone blessing (branch = null) uses OR logic (one prerequisite sufficient)
        var prereq1 = CreateBlessing("prereq1", BlessingKind.Player);
        var prereq2 = CreateBlessing("prereq2", BlessingKind.Player);
        var capstone = CreateBlessing("capstone", BlessingKind.Player,
            prerequisites: new List<string> { "prereq1", "prereq2" },
            branch: null); // Null branch = OR logic (capstone)

        _sut.LoadBlessingStates(
            new List<Blessing> { prereq1, prereq2, capstone },
            new List<Blessing>());

        // Only unlock one prerequisite
        _sut.SetBlessingUnlocked("prereq1", true);

        // Act
        Refresh(10, 10);

        // Assert - should be true because one prerequisite is unlocked (OR logic)
        Assert.True(_sut.State.PlayerBlessingStates["capstone"].CanUnlock);
    }

    #endregion
}