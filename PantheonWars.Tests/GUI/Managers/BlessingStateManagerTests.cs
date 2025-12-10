using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.GUI.Events;
using PantheonWars.GUI.Managers;
using PantheonWars.GUI.Models.Blessing.Tab;
using PantheonWars.Models;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace PantheonWars.Tests.GUI.Managers;

[ExcludeFromCodeCoverage]
public class BlessingStateManagerTests
{
    private readonly Mock<ICoreClientAPI> _mockApi;
    private readonly Mock<IUiService> _mockUiService;
    private readonly Mock<IWorldAccessor> _mockWorld;
    private readonly Mock<Entity> _mockEntity;
    private readonly BlessingStateManager _sut;

    public BlessingStateManagerTests()
    {
        _mockApi = new Mock<ICoreClientAPI>();
        _mockUiService = new Mock<IUiService>();
        _mockWorld = new Mock<IWorldAccessor>();
        _mockEntity = new Mock<Entity>();


        _sut = new BlessingStateManager(_mockApi.Object, _mockUiService.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullApi_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BlessingStateManager(null!, _mockUiService.Object));
    }

    [Fact]
    public void Constructor_WithNullUiService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new BlessingStateManager(_mockApi.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var manager = new BlessingStateManager(_mockApi.Object, _mockUiService.Object);

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
            new List<BlessingTreeEvent>(),
            new List<BlessingActionsEvent>(),
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
            new List<BlessingTreeEvent>
            {
                new BlessingTreeEvent.PlayerTreeScrollChanged(3.25f, -1.5f)
            },
            new List<BlessingActionsEvent>(),
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
            new List<BlessingTreeEvent>
            {
                new BlessingTreeEvent.ReligionTreeScrollChanged(-2f, 5f)
            },
            new List<BlessingActionsEvent>(),
            null,
            100f);

        // Act
        _sut.ProcessBlessingTabEvents(result);

        // Assert
        Assert.Equal(-2f, _sut.State.TreeState.ReligionScrollState.X);
        Assert.Equal(5f, _sut.State.TreeState.ReligionScrollState.Y);
    }

    #endregion

    #region HandleUnlockClicked via ActionsEvent

    [Fact]
    public void UnlockClicked_WithNoSelection_DoesNothing()
    {
        // Arrange - no SelectedBlessingId
        var result = new BlessingTabRenderResult(
            new List<BlessingTreeEvent>(),
            new List<BlessingActionsEvent> { new BlessingActionsEvent.UnlockClicked() },
            null,
            100f);

        // Act
        _sut.ProcessBlessingTabEvents(result);

        // Assert
        _mockUiService.Verify(u => u.RequestBlessingUnlock(It.IsAny<string>()), Times.Never);
        _mockApi.Verify(a => a.ShowChatMessage(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void UnlockClicked_WithInvalidBlessingId_ShowsChatError_AndNoSoundOrRequest()
    {
        // Arrange
        var invalid = CreateBlessing(string.Empty, BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { invalid }, new List<Blessing>());
        _sut.State.TreeState.SelectedBlessingId = string.Empty; // select the invalid one
        // Make it eligible so HandleUnlockClicked proceeds to client-side validation
        _sut.State.PlayerBlessingStates[string.Empty].CanUnlock = true;
        _sut.State.PlayerBlessingStates[string.Empty].IsUnlocked = false;

        var result = new BlessingTabRenderResult(
            new List<BlessingTreeEvent>(),
            new List<BlessingActionsEvent> { new BlessingActionsEvent.UnlockClicked() },
            null,
            100f);

        // Act
        _sut.ProcessBlessingTabEvents(result);

        // Assert
        _mockApi.Verify(a => a.ShowChatMessage(It.Is<string>(s => s.Contains("Invalid blessing ID"))), Times.Once);
        _mockUiService.Verify(u => u.RequestBlessingUnlock(It.IsAny<string>()), Times.Never);
        _mockWorld.Verify(w => w.PlaySoundAt(
            It.IsAny<AssetLocation>(),
            It.IsAny<Entity>(),
            It.IsAny<IPlayer?>(),
            It.IsAny<bool>(),
            It.IsAny<float>(),
            It.IsAny<float>()
        ), Times.Never);
    }

    [Fact]
    public void UnlockClicked_WithValidSelection_PlaysClickAndSendsRequest()
    {
        // Arrange
        var blessing = CreateBlessing("bless-1", BlessingKind.Player);
        _sut.LoadBlessingStates(new List<Blessing> { blessing }, new List<Blessing>());
        _sut.State.TreeState.SelectedBlessingId = "bless-1";
        var node = _sut.State.PlayerBlessingStates["bless-1"];
        node.CanUnlock = true;
        node.IsUnlocked = false;

        var result = new BlessingTabRenderResult(
            new List<BlessingTreeEvent>(),
            new List<BlessingActionsEvent> { new BlessingActionsEvent.UnlockClicked() },
            null,
            100f);

        // Act
        _sut.ProcessBlessingTabEvents(result);

        // Assert
        _mockUiService.Verify(u => u.RequestBlessingUnlock("bless-1"), Times.Once);
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
        _sut.RefreshAllBlessingStates(3, 0);

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
        _sut.RefreshAllBlessingStates(2, 0);

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
        _sut.RefreshAllBlessingStates(0, 3);

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
        _sut.RefreshAllBlessingStates(0, 2);

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
        _sut.RefreshAllBlessingStates(5, 5);

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
        _sut.RefreshAllBlessingStates(10, 10);

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
        _sut.RefreshAllBlessingStates(10, 10);

        // Assert
        Assert.True(_sut.State.PlayerBlessingStates["dependent"].CanUnlock);
    }

    [Fact]
    public void RefreshAllBlessingStates_WithMultiplePrerequisites_AllMustBeUnlocked()
    {
        // Arrange
        var prereq1 = CreateBlessing("prereq1", BlessingKind.Player);
        var prereq2 = CreateBlessing("prereq2", BlessingKind.Player);
        var dependent = CreateBlessing("dependent", BlessingKind.Player,
            prerequisites: new List<string> { "prereq1", "prereq2" });

        _sut.LoadBlessingStates(
            new List<Blessing> { prereq1, prereq2, dependent },
            new List<Blessing>());

        // Only unlock one prerequisite
        _sut.SetBlessingUnlocked("prereq1", true);

        // Act
        _sut.RefreshAllBlessingStates(10, 10);

        // Assert - should be false because prereq2 is not unlocked
        Assert.False(_sut.State.PlayerBlessingStates["dependent"].CanUnlock);
    }

    #endregion

    #region Helper Methods

    private static Blessing CreateBlessing(
        string id,
        BlessingKind kind,
        int requiredFavorRank = 0,
        int requiredPrestigeRank = 0,
        List<string>? prerequisites = null)
    {
        return new Blessing
        {
            BlessingId = id,
            Kind = kind,
            RequiredFavorRank = requiredFavorRank,
            RequiredPrestigeRank = requiredPrestigeRank,
            PrerequisiteBlessings = prerequisites ?? new List<string>()
        };
    }

    #endregion
}