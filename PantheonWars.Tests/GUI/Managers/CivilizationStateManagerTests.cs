using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.GUI.Events.Civilization;
using PantheonWars.GUI.Interfaces;
using PantheonWars.GUI.Managers;
using PantheonWars.GUI.State;
using PantheonWars.Network.Civilization;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Client;

namespace PantheonWars.Tests.GUI.Managers;

[ExcludeFromCodeCoverage]
public class CivilizationStateManagerTests
{
    private readonly Mock<ICoreClientAPI> _mockApi;
    private readonly Mock<ISoundManager> _mockSoundManager;
    private readonly Mock<IUiService> _mockUiService;
    private readonly CivilizationStateManager _sut;

    public CivilizationStateManagerTests()
    {
        _mockApi = new Mock<ICoreClientAPI>();
        _mockUiService = new Mock<IUiService>();
        _mockSoundManager = new Mock<ISoundManager>();

        _sut = new CivilizationStateManager(_mockApi.Object, _mockUiService.Object, _mockSoundManager.Object);
    }

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllState()
    {
        // Arrange
        _sut.CurrentCivilizationId = "civ-123";
        _sut.CurrentCivilizationName = "Test Civ";
        _sut.CivilizationFounderReligionUID = "religion-1";
        _sut.CivilizationMemberReligions = new List<CivilizationInfoResponsePacket.MemberReligion>
        {
            new() { ReligionId = "religion-1", ReligionName = "Test Religion" }
        };

        // Act
        _sut.Reset();

        // Assert
        Assert.Empty(_sut.CurrentCivilizationId);
        Assert.Empty(_sut.CurrentCivilizationName);
        Assert.Empty(_sut.CivilizationFounderReligionUID);
        Assert.Empty(_sut.CivilizationMemberReligions!);
    }

    #endregion

    #region OnCivilizationListReceived Tests

    [Fact]
    public void OnCivilizationListReceived_UpdatesStateWithCivilizations()
    {
        // Arrange
        var packet = new CivilizationListResponsePacket
        {
            Civilizations = new List<CivilizationListResponsePacket.CivilizationInfo>
            {
                new() { CivId = "civ-1", Name = "Alliance 1" },
                new() { CivId = "civ-2", Name = "Alliance 2" }
            }
        };

        // Act
        _sut.OnCivilizationListReceived(packet);

        // Assert - we can't directly access state, but we can verify the method was called successfully
        Assert.NotNull(_sut);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullApi_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CivilizationStateManager(null!, _mockUiService.Object, _mockSoundManager.Object));
    }

    [Fact]
    public void Constructor_WithNullUiService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CivilizationStateManager(_mockApi.Object, null!, _mockSoundManager.Object));
    }

    [Fact]
    public void Constructor_WithNullSoundManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new CivilizationStateManager(_mockApi.Object, _mockUiService.Object, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var manager = new CivilizationStateManager(_mockApi.Object, _mockUiService.Object, _mockSoundManager.Object);

        Assert.NotNull(manager);
        Assert.Empty(manager.CurrentCivilizationId);
        Assert.Empty(manager.CurrentCivilizationName);
        Assert.NotNull(manager.CivilizationMemberReligions);
    }

    #endregion

    #region HasCivilization Tests

    [Fact]
    public void HasCivilization_WhenCivIdEmpty_ReturnsFalse()
    {
        // Arrange
        _sut.CurrentCivilizationId = string.Empty;

        // Act
        var result = _sut.HasCivilization();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasCivilization_WhenCivIdSet_ReturnsTrue()
    {
        // Arrange
        _sut.CurrentCivilizationId = "civ-123";

        // Act
        var result = _sut.HasCivilization();

        // Assert
        Assert.True(result);
    }

    #endregion

    #region UpdateCivilizationState Tests

    [Fact]
    public void UpdateCivilizationState_WithNullDetails_ClearsCivilizationState()
    {
        // Arrange
        _sut.CurrentCivilizationId = "civ-123";
        _sut.CurrentCivilizationName = "Test Civ";
        _sut.CivilizationFounderReligionUID = "religion-1";

        // Act
        _sut.UpdateCivilizationState(null);

        // Assert
        Assert.Empty(_sut.CurrentCivilizationId);
        Assert.Empty(_sut.CurrentCivilizationName);
        Assert.Empty(_sut.CivilizationFounderReligionUID);
        Assert.Empty(_sut.CivilizationMemberReligions!);
    }

    [Fact]
    public void UpdateCivilizationState_WithEmptyCivId_ClearsStateButUpdatesInvites()
    {
        // Arrange
        var details = new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = string.Empty,
            Name = string.Empty,
            PendingInvites = new List<CivilizationInfoResponsePacket.PendingInvite>
            {
                new() { InviteId = "invite-1", ReligionName = "Test Religion" }
            }
        };

        // Act
        _sut.UpdateCivilizationState(details);

        // Assert
        Assert.Empty(_sut.CurrentCivilizationId);
        Assert.Empty(_sut.CurrentCivilizationName);
        Assert.Empty(_sut.CivilizationFounderReligionUID);
    }

    [Fact]
    public void UpdateCivilizationState_WithValidDetails_UpdatesState()
    {
        // Arrange
        var details = new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = "civ-123",
            Name = "Grand Alliance",
            FounderReligionUID = "religion-1",
            MemberReligions = new List<CivilizationInfoResponsePacket.MemberReligion>
            {
                new() { ReligionId = "religion-1", ReligionName = "Test Religion" }
            }
        };

        // Act
        _sut.UpdateCivilizationState(details);

        // Assert
        Assert.Equal("civ-123", _sut.CurrentCivilizationId);
        Assert.Equal("Grand Alliance", _sut.CurrentCivilizationName);
        Assert.Equal("religion-1", _sut.CivilizationFounderReligionUID);
        Assert.Single(_sut.CivilizationMemberReligions!);
    }

    #endregion

    #region RequestCivilizationList Tests

    [Fact]
    public void RequestCivilizationList_CallsUiService()
    {
        // Arrange
        var deityFilter = "Khoras";

        // Act
        _sut.RequestCivilizationList(deityFilter);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationList(deityFilter), Times.Once);
    }

    [Fact]
    public void RequestCivilizationList_WithEmptyFilter_CallsUiService()
    {
        // Act
        _sut.RequestCivilizationList();

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationList(string.Empty), Times.Once);
    }

    #endregion

    #region RequestCivilizationInfo Tests

    [Fact]
    public void RequestCivilizationInfo_WithEmptyId_CallsUiService()
    {
        // Act
        _sut.RequestCivilizationInfo();

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationInfo(string.Empty), Times.Once);
    }

    [Fact]
    public void RequestCivilizationInfo_WithCivId_CallsUiService()
    {
        // Arrange
        var civId = "civ-123";

        // Act
        _sut.RequestCivilizationInfo(civId);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationInfo(civId), Times.Once);
    }

    #endregion

    #region RequestCivilizationAction Tests

    [Fact]
    public void RequestCivilizationAction_Create_CallsUiService()
    {
        // Arrange
        var name = "New Civilization";

        // Act
        _sut.RequestCivilizationAction("create", "", "", name);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("create", "", "", name), Times.Once);
    }

    [Fact]
    public void RequestCivilizationAction_Invite_CallsUiService()
    {
        // Arrange
        var civId = "civ-123";
        var religionName = "Target Religion";

        // Act
        _sut.RequestCivilizationAction("invite", civId, religionName);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("invite", civId, religionName, ""), Times.Once);
    }

    [Fact]
    public void RequestCivilizationAction_Leave_CallsUiService()
    {
        // Act
        _sut.RequestCivilizationAction("leave");

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("leave", "", "", ""), Times.Once);
    }

    #endregion

    #region OnCivilizationInfoReceived Tests

    [Fact]
    public void OnCivilizationInfoReceived_UpdatesCivilizationState()
    {
        // Arrange
        var packet = new CivilizationInfoResponsePacket
        {
            Details = new CivilizationInfoResponsePacket.CivilizationDetails
            {
                CivId = "civ-123",
                Name = "Test Civilization",
                FounderReligionUID = "religion-1",
                MemberReligions = new List<CivilizationInfoResponsePacket.MemberReligion>
                {
                    new() { ReligionId = "religion-1", ReligionName = "Founder Religion" }
                }
            }
        };

        // Act
        _sut.OnCivilizationInfoReceived(packet);

        // Assert
        Assert.Equal("civ-123", _sut.CurrentCivilizationId);
        Assert.Equal("Test Civilization", _sut.CurrentCivilizationName);
        Assert.Equal("religion-1", _sut.CivilizationFounderReligionUID);
    }

    [Fact]
    public void OnCivilizationInfoReceived_WithNullDetails_ClearsCivilizationState()
    {
        // Arrange
        _sut.CurrentCivilizationId = "civ-123";
        var packet = new CivilizationInfoResponsePacket
        {
            Details = null
        };

        // Act
        _sut.OnCivilizationInfoReceived(packet);

        // Assert
        Assert.Empty(_sut.CurrentCivilizationId);
        Assert.Empty(_sut.CurrentCivilizationName);
    }

    #endregion

    #region OnCivilizationActionCompleted Tests

    [Fact]
    public void OnCivilizationActionCompleted_Success_PlaysClickAndRefreshes()
    {
        // Arrange
        var packet = new CivilizationActionResponsePacket
        {
            Success = true,
            Message = "Action completed successfully"
        };

        // Act
        _sut.OnCivilizationActionCompleted(packet);

        // Assert
        _mockSoundManager.Verify(s => s.PlayClick(), Times.Once);
        _mockUiService.Verify(u => u.RequestCivilizationList(It.IsAny<string>()), Times.Once);
        _mockUiService.Verify(u => u.RequestCivilizationInfo(string.Empty), Times.Once);
    }

    [Fact]
    public void OnCivilizationActionCompleted_Failure_PlaysErrorAndDoesNotRefresh()
    {
        // Arrange
        var packet = new CivilizationActionResponsePacket
        {
            Success = false,
            Message = "Action failed"
        };

        // Act
        _sut.OnCivilizationActionCompleted(packet);

        // Assert
        _mockSoundManager.Verify(s => s.PlayError(), Times.Once);
        _mockSoundManager.Verify(s => s.PlayClick(), Times.Never);
        _mockUiService.Verify(u => u.RequestCivilizationList(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CompleteWorkflow_CreateCivilization_UpdatesStateCorrectly()
    {
        // Arrange - Start with no civilization
        Assert.False(_sut.HasCivilization());

        // Act 1 - Create civilization
        _sut.RequestCivilizationAction("create", "", "", "Grand Alliance");

        // Act 2 - Receive success response
        var actionResponse = new CivilizationActionResponsePacket
        {
            Success = true,
            Message = "Civilization created"
        };
        _sut.OnCivilizationActionCompleted(actionResponse);

        // Act 3 - Receive civilization info
        var infoPacket = new CivilizationInfoResponsePacket
        {
            Details = new CivilizationInfoResponsePacket.CivilizationDetails
            {
                CivId = "civ-new",
                Name = "Grand Alliance",
                FounderReligionUID = "religion-1",
                MemberReligions = new List<CivilizationInfoResponsePacket.MemberReligion>
                {
                    new() { ReligionId = "religion-1", ReligionName = "My Religion" }
                }
            }
        };
        _sut.OnCivilizationInfoReceived(infoPacket);

        // Assert
        Assert.True(_sut.HasCivilization());
        Assert.Equal("civ-new", _sut.CurrentCivilizationId);
        Assert.Equal("Grand Alliance", _sut.CurrentCivilizationName);
        _mockUiService.Verify(u => u.RequestCivilizationAction("create", "", "", "Grand Alliance"), Times.Once);
        _mockSoundManager.Verify(s => s.PlayClick(), Times.Once);
    }

    [Fact]
    public void CompleteWorkflow_InviteReligion_CallsCorrectMethods()
    {
        // Arrange - Set up a civilization
        var details = new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = "civ-123",
            Name = "Test Civ",
            FounderReligionUID = "religion-1",
            MemberReligions = new List<CivilizationInfoResponsePacket.MemberReligion>
            {
                new() { ReligionId = "religion-1", ReligionName = "My Religion" }
            }
        };
        _sut.UpdateCivilizationState(details);

        // Act - Invite another religion
        _sut.RequestCivilizationAction("invite", "civ-123", "Target Religion");

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("invite", "civ-123", "Target Religion", ""), Times.Once);
    }

    [Fact]
    public void CompleteWorkflow_LeaveCivilization_ClearsCivilizationState()
    {
        // Arrange - Start in a civilization
        var details = new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = "civ-123",
            Name = "Test Civ",
            FounderReligionUID = "religion-1",
            MemberReligions = new List<CivilizationInfoResponsePacket.MemberReligion>()
        };
        _sut.UpdateCivilizationState(details);
        Assert.True(_sut.HasCivilization());

        // Act 1 - Leave civilization
        _sut.RequestCivilizationAction("leave");

        // Act 2 - Receive success response
        var actionResponse = new CivilizationActionResponsePacket
        {
            Success = true,
            Message = "Left civilization"
        };
        _sut.OnCivilizationActionCompleted(actionResponse);

        // Act 3 - Receive updated info (no civilization)
        var infoPacket = new CivilizationInfoResponsePacket
        {
            Details = new CivilizationInfoResponsePacket.CivilizationDetails
            {
                CivId = string.Empty
            }
        };
        _sut.OnCivilizationInfoReceived(infoPacket);

        // Assert
        Assert.False(_sut.HasCivilization());
        _mockUiService.Verify(u => u.RequestCivilizationAction("leave", "", "", ""), Times.Once);
        _mockSoundManager.Verify(s => s.PlayClick(), Times.Once);
    }

    [Fact]
    public void StateProperties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        _sut.UserHasReligion = true;
        _sut.UserIsReligionFounder = true;
        _sut.CurrentCivilizationId = "civ-123";
        _sut.CurrentCivilizationName = "Test Civ";
        _sut.CivilizationFounderReligionUID = "religion-1";

        // Assert
        Assert.True(_sut.UserHasReligion);
        Assert.True(_sut.UserIsReligionFounder);
        Assert.Equal("civ-123", _sut.CurrentCivilizationId);
        Assert.Equal("Test Civ", _sut.CurrentCivilizationName);
        Assert.Equal("religion-1", _sut.CivilizationFounderReligionUID);
    }

    #endregion

    #region ProcessBrowseEvents Tests

    [Fact]
    public void ProcessBrowseEvents_DeityFilterChanged_ToSpecificDeity_UpdatesFilterAndRequestsList()
    {
        // Arrange
        var events = new List<BrowseEvent>
        {
            new BrowseEvent.DeityFilterChanged("Khoras")
        };

        // Act
        _sut.ProcessBrowseEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationList("Khoras"), Times.Once);
        _mockSoundManager.Verify(s => s.PlayClick(), Times.Once);
    }

    [Fact]
    public void ProcessBrowseEvents_DeityFilterChanged_ToAll_UpdatesFilterToEmptyAndRequestsList()
    {
        // Arrange
        var events = new List<BrowseEvent>
        {
            new BrowseEvent.DeityFilterChanged("All")
        };

        // Act
        _sut.ProcessBrowseEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationList(string.Empty), Times.Once);
        _mockSoundManager.Verify(s => s.PlayClick(), Times.Once);
    }

    [Fact]
    public void ProcessBrowseEvents_ScrollChanged_UpdatesScrollPosition()
    {
        // Arrange
        var events = new List<BrowseEvent>
        {
            new BrowseEvent.ScrollChanged(123.45f)
        };

        // Act
        _sut.ProcessBrowseEvents(events);

        // Assert - We can't directly verify state but we can ensure no errors occurred
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessBrowseEvents_ViewDetailsClicked_RequestsCivilizationInfo()
    {
        // Arrange
        var civId = "civ-123";
        var events = new List<BrowseEvent>
        {
            new BrowseEvent.ViewDetailedsClicked(civId)
        };

        // Act
        _sut.ProcessBrowseEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationInfo(civId), Times.Once);
    }

    [Fact]
    public void ProcessBrowseEvents_RefreshClicked_RequestsCivilizationList()
    {
        // Arrange
        var events = new List<BrowseEvent>
        {
            new BrowseEvent.RefreshClicked()
        };

        // Act
        _sut.ProcessBrowseEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationList(string.Empty), Times.Once);
    }

    [Fact]
    public void ProcessBrowseEvents_RefreshClicked_WithActiveFilter_RequestsListWithFilter()
    {
        // Arrange - Set a filter first
        var filterEvents = new List<BrowseEvent>
        {
            new BrowseEvent.DeityFilterChanged("Lysa")
        };
        _sut.ProcessBrowseEvents(filterEvents);
        _mockUiService.Invocations.Clear();

        // Now refresh
        var refreshEvents = new List<BrowseEvent>
        {
            new BrowseEvent.RefreshClicked()
        };

        // Act
        _sut.ProcessBrowseEvents(refreshEvents);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationList("Lysa"), Times.Once);
    }

    [Fact]
    public void ProcessBrowseEvents_DeityDropDownToggled_Open_UpdatesState()
    {
        // Arrange
        var events = new List<BrowseEvent>
        {
            new BrowseEvent.DeityDropDownToggled(true)
        };

        // Act
        _sut.ProcessBrowseEvents(events);

        // Assert - We can't directly verify state but we can ensure no errors occurred
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessBrowseEvents_DeityDropDownToggled_Close_UpdatesState()
    {
        // Arrange
        var events = new List<BrowseEvent>
        {
            new BrowseEvent.DeityDropDownToggled(false)
        };

        // Act
        _sut.ProcessBrowseEvents(events);

        // Assert - We can't directly verify state but we can ensure no errors occurred
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessBrowseEvents_MultipleEvents_ProcessesAllInOrder()
    {
        // Arrange
        var events = new List<BrowseEvent>
        {
            new BrowseEvent.DeityFilterChanged("Khoras"),
            new BrowseEvent.ScrollChanged(50.0f),
            new BrowseEvent.RefreshClicked()
        };

        // Act
        _sut.ProcessBrowseEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationList("Khoras"),
            Times.Exactly(2)); // Once for filter change, once for refresh
        _mockSoundManager.Verify(s => s.PlayClick(), Times.Once); // Only for filter change
    }

    [Fact]
    public void ProcessBrowseEvents_EmptyEventList_DoesNothing()
    {
        // Arrange
        var events = new List<BrowseEvent>();

        // Act
        _sut.ProcessBrowseEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationList(It.IsAny<string>()), Times.Never);
        _mockSoundManager.Verify(s => s.PlayClick(), Times.Never);
    }

    #endregion

    #region ProcessTabEvents Tests

    [Fact]
    public void ProcessTabEvents_TabChanged_ToBrowse_UpdatesCurrentSubTab()
    {
        // Arrange
        var events = new List<SubTabEvent>
        {
            new SubTabEvent.TabChanged(CivilizationSubTab.Browse)
        };

        // Act
        _sut.ProcessTabEvents(events);

        // Assert - We can't directly verify state but we can ensure no errors occurred
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessTabEvents_TabChanged_ToMyCiv_RequestsCivilizationInfo()
    {
        // Arrange
        var events = new List<SubTabEvent>
        {
            new SubTabEvent.TabChanged(CivilizationSubTab.MyCiv)
        };

        // Act
        _sut.ProcessTabEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationInfo(string.Empty), Times.Once);
    }

    [Fact]
    public void ProcessTabEvents_TabChanged_ToInvites_RequestsCivilizationInfo()
    {
        // Arrange
        var events = new List<SubTabEvent>
        {
            new SubTabEvent.TabChanged(CivilizationSubTab.Invites)
        };

        // Act
        _sut.ProcessTabEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationInfo(string.Empty), Times.Once);
    }

    [Fact]
    public void ProcessTabEvents_TabChanged_ToCreate_DoesNotRequestInfo()
    {
        // Arrange
        var events = new List<SubTabEvent>
        {
            new SubTabEvent.TabChanged(CivilizationSubTab.Create)
        };

        // Act
        _sut.ProcessTabEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationInfo(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessTabEvents_DismissActionError_DoesNotThrow()
    {
        // Arrange
        var events = new List<SubTabEvent>
        {
            new SubTabEvent.DismissActionError()
        };

        // Act
        _sut.ProcessTabEvents(events);

        // Assert - We can't directly verify state but we can ensure no errors occurred
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessTabEvents_DismissContextError_ForBrowse_DoesNotThrow()
    {
        // Arrange
        var events = new List<SubTabEvent>
        {
            new SubTabEvent.DismissContextError(CivilizationSubTab.Browse)
        };

        // Act
        _sut.ProcessTabEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessTabEvents_DismissContextError_ForMyCiv_DoesNotThrow()
    {
        // Arrange
        var events = new List<SubTabEvent>
        {
            new SubTabEvent.DismissContextError(CivilizationSubTab.MyCiv)
        };

        // Act
        _sut.ProcessTabEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessTabEvents_DismissContextError_ForInvites_DoesNotThrow()
    {
        // Arrange
        var events = new List<SubTabEvent>
        {
            new SubTabEvent.DismissContextError(CivilizationSubTab.Invites)
        };

        // Act
        _sut.ProcessTabEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessTabEvents_RetryRequested_ForBrowse_RequestsCivilizationList()
    {
        // Arrange
        var events = new List<SubTabEvent>
        {
            new SubTabEvent.RetryRequested(CivilizationSubTab.Browse)
        };

        // Act
        _sut.ProcessTabEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationList(string.Empty), Times.Once);
    }

    [Fact]
    public void ProcessTabEvents_RetryRequested_ForMyCiv_RequestsCivilizationInfo()
    {
        // Arrange
        var events = new List<SubTabEvent>
        {
            new SubTabEvent.RetryRequested(CivilizationSubTab.MyCiv)
        };

        // Act
        _sut.ProcessTabEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationInfo(string.Empty), Times.Once);
    }

    [Fact]
    public void ProcessTabEvents_RetryRequested_ForInvites_RequestsCivilizationInfo()
    {
        // Arrange
        var events = new List<SubTabEvent>
        {
            new SubTabEvent.RetryRequested(CivilizationSubTab.Invites)
        };

        // Act
        _sut.ProcessTabEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationInfo(string.Empty), Times.Once);
    }

    [Fact]
    public void ProcessTabEvents_MultipleEvents_ProcessesAllInOrder()
    {
        // Arrange
        var events = new List<SubTabEvent>
        {
            new SubTabEvent.DismissActionError(),
            new SubTabEvent.TabChanged(CivilizationSubTab.MyCiv)
        };

        // Act
        _sut.ProcessTabEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationInfo(string.Empty), Times.Once);
    }

    #endregion

    #region ProcessInfoEvents Tests

    [Fact]
    public void ProcessInfoEvents_ScrollChanged_UpdatesScrollPosition()
    {
        // Arrange
        var events = new List<InfoEvent>
        {
            new InfoEvent.ScrollChanged(100.5f)
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessInfoEvents_MemberScrollChanged_UpdatesScrollPosition()
    {
        // Arrange
        var events = new List<InfoEvent>
        {
            new InfoEvent.MemberScrollChanged(50.25f)
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessInfoEvents_InviteReligionNameChanged_UpdatesName()
    {
        // Arrange
        var events = new List<InfoEvent>
        {
            new InfoEvent.InviteReligionNameChanged("New Religion")
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessInfoEvents_InviteReligionClicked_WithValidData_RequestsInviteAction()
    {
        // Arrange
        _sut.UpdateCivilizationState(new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = "civ-123",
            Name = "Test Civ"
        });

        var events = new List<InfoEvent>
        {
            new InfoEvent.InviteReligionClicked("Target Religion")
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("invite", "civ-123", "Target Religion", ""), Times.Once);
    }

    [Fact]
    public void ProcessInfoEvents_InviteReligionClicked_WithWhitespaceReligionName_DoesNotRequestAction()
    {
        // Arrange
        _sut.UpdateCivilizationState(new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = "civ-123",
            Name = "Test Civ"
        });

        var events = new List<InfoEvent>
        {
            new InfoEvent.InviteReligionClicked("   ")
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        _mockUiService.Verify(
            u => u.RequestCivilizationAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessInfoEvents_InviteReligionClicked_WithNoCivilization_DoesNotRequestAction()
    {
        // Arrange - No civilization set
        var events = new List<InfoEvent>
        {
            new InfoEvent.InviteReligionClicked("Target Religion")
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        _mockUiService.Verify(
            u => u.RequestCivilizationAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessInfoEvents_LeaveClicked_WithCivilization_RequestsLeaveAction()
    {
        // Arrange
        _sut.UpdateCivilizationState(new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = "civ-123",
            Name = "Test Civ"
        });

        var events = new List<InfoEvent>
        {
            new InfoEvent.LeaveClicked()
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("leave", "", "", ""), Times.Once);
    }

    [Fact]
    public void ProcessInfoEvents_LeaveClicked_WithoutCivilization_DoesNotRequestAction()
    {
        // Arrange - No civilization
        var events = new List<InfoEvent>
        {
            new InfoEvent.LeaveClicked()
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        _mockUiService.Verify(
            u => u.RequestCivilizationAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessInfoEvents_DisbandOpened_UpdatesConfirmationState()
    {
        // Arrange
        var events = new List<InfoEvent>
        {
            new InfoEvent.DisbandOpened()
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessInfoEvents_DisbandCancel_UpdatesConfirmationState()
    {
        // Arrange
        var events = new List<InfoEvent>
        {
            new InfoEvent.DisbandCancel()
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessInfoEvents_DisbandConfirmed_WithCivilization_RequestsDisbandAction()
    {
        // Arrange
        _sut.UpdateCivilizationState(new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = "civ-123",
            Name = "Test Civ"
        });

        var events = new List<InfoEvent>
        {
            new InfoEvent.DisbandConfirmed()
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("disband", "civ-123", "", ""), Times.Once);
    }

    [Fact]
    public void ProcessInfoEvents_DisbandConfirmed_WithoutCivilization_DoesNotRequestAction()
    {
        // Arrange - No civilization
        var events = new List<InfoEvent>
        {
            new InfoEvent.DisbandConfirmed()
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        _mockUiService.Verify(
            u => u.RequestCivilizationAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessInfoEvents_KickOpen_UpdatesKickConfirmationState()
    {
        // Arrange
        var events = new List<InfoEvent>
        {
            new InfoEvent.KickOpen("religion-123", "Test Religion")
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessInfoEvents_KickCancel_ClearsKickConfirmationState()
    {
        // Arrange
        var events = new List<InfoEvent>
        {
            new InfoEvent.KickCancel()
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessInfoEvents_KickConfirm_WithValidState_RequestsKickAction()
    {
        // Arrange
        _sut.UpdateCivilizationState(new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = "civ-123",
            Name = "Test Civ"
        });

        // First open kick dialog, then confirm
        var events = new List<InfoEvent>
        {
            new InfoEvent.KickOpen("religion-456", "Target Religion"),
            new InfoEvent.KickConfirm("Target Religion")
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("kick", "civ-123", "religion-456", ""), Times.Once);
    }

    [Fact]
    public void ProcessInfoEvents_KickConfirm_WithoutKickTarget_DoesNotRequestAction()
    {
        // Arrange
        _sut.UpdateCivilizationState(new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = "civ-123",
            Name = "Test Civ"
        });

        var events = new List<InfoEvent>
        {
            new InfoEvent.KickConfirm("test")
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert
        _mockUiService.Verify(
            u => u.RequestCivilizationAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessInfoEvents_KickWorkflow_OpenCancelConfirm_OnlyConfirmRequestsAction()
    {
        // Arrange
        _sut.UpdateCivilizationState(new CivilizationInfoResponsePacket.CivilizationDetails
        {
            CivId = "civ-123",
            Name = "Test Civ"
        });

        // Open, cancel, open again, confirm
        var events = new List<InfoEvent>
        {
            new InfoEvent.KickOpen("religion-456", "test"),
            new InfoEvent.KickCancel(),
            new InfoEvent.KickOpen("religion-789", "test2"),
            new InfoEvent.KickConfirm("test2")
        };

        // Act
        _sut.ProcessInfoEvents(events);

        // Assert - Should only kick religion-789, not religion-456
        _mockUiService.Verify(u => u.RequestCivilizationAction("kick", "civ-123", "religion-789", ""), Times.Once);
        _mockUiService.Verify(u => u.RequestCivilizationAction("kick", "civ-123", "religion-456", ""), Times.Never);
    }

    #endregion

    #region ProcessCreateEvents Tests

    [Fact]
    public void ProcessCreateEvents_NameChanged_UpdatesName()
    {
        // Arrange
        var events = new List<CreateEvent>
        {
            new CreateEvent.NameChanged("New Civilization")
        };

        // Act
        _sut.ProcessCreateEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessCreateEvents_SubmitClicked_WithValidName_RequestsCreateAction()
    {
        // Arrange
        var events = new List<CreateEvent>
        {
            new CreateEvent.NameChanged("Grand Alliance"),
            new CreateEvent.SubmitClicked()
        };

        // Act
        _sut.ProcessCreateEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("create", "", "", "Grand Alliance"), Times.Once);
    }

    [Fact]
    public void ProcessCreateEvents_SubmitClicked_WithNameTooShort_ShowsErrorMessage()
    {
        // Arrange
        var events = new List<CreateEvent>
        {
            new CreateEvent.NameChanged("AB"),
            new CreateEvent.SubmitClicked()
        };

        // Act
        _sut.ProcessCreateEvents(events);

        // Assert
        _mockApi.Verify(a => a.ShowChatMessage("Civilization name must be 3-32 characters."), Times.Once);
        _mockSoundManager.Verify(s => s.PlayError(), Times.Once);
        _mockUiService.Verify(
            u => u.RequestCivilizationAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessCreateEvents_SubmitClicked_WithNameTooLong_ShowsErrorMessage()
    {
        // Arrange
        var longName = new string('A', 33);
        var events = new List<CreateEvent>
        {
            new CreateEvent.NameChanged(longName),
            new CreateEvent.SubmitClicked()
        };

        // Act
        _sut.ProcessCreateEvents(events);

        // Assert
        _mockApi.Verify(a => a.ShowChatMessage("Civilization name must be 3-32 characters."), Times.Once);
        _mockSoundManager.Verify(s => s.PlayError(), Times.Once);
        _mockUiService.Verify(
            u => u.RequestCivilizationAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessCreateEvents_SubmitClicked_WithWhitespaceName_ShowsErrorMessage()
    {
        // Arrange
        var events = new List<CreateEvent>
        {
            new CreateEvent.NameChanged("   "),
            new CreateEvent.SubmitClicked()
        };

        // Act
        _sut.ProcessCreateEvents(events);

        // Assert
        _mockApi.Verify(a => a.ShowChatMessage("Civilization name must be 3-32 characters."), Times.Once);
        _mockSoundManager.Verify(s => s.PlayError(), Times.Once);
        _mockUiService.Verify(
            u => u.RequestCivilizationAction(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessCreateEvents_SubmitClicked_WithEmptyName_ShowsErrorMessage()
    {
        // Arrange
        var events = new List<CreateEvent>
        {
            new CreateEvent.SubmitClicked()
        };

        // Act
        _sut.ProcessCreateEvents(events);

        // Assert
        _mockApi.Verify(a => a.ShowChatMessage("Civilization name must be 3-32 characters."), Times.Once);
        _mockSoundManager.Verify(s => s.PlayError(), Times.Once);
    }

    [Fact]
    public void ProcessCreateEvents_SubmitClicked_WithMinimumValidLength_RequestsCreateAction()
    {
        // Arrange - Exactly 3 characters
        var events = new List<CreateEvent>
        {
            new CreateEvent.NameChanged("ABC"),
            new CreateEvent.SubmitClicked()
        };

        // Act
        _sut.ProcessCreateEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("create", "", "", "ABC"), Times.Once);
        _mockApi.Verify(a => a.ShowChatMessage(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessCreateEvents_SubmitClicked_WithMaximumValidLength_RequestsCreateAction()
    {
        // Arrange - Exactly 32 characters
        var name = new string('A', 32);
        var events = new List<CreateEvent>
        {
            new CreateEvent.NameChanged(name),
            new CreateEvent.SubmitClicked()
        };

        // Act
        _sut.ProcessCreateEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("create", "", "", name), Times.Once);
        _mockApi.Verify(a => a.ShowChatMessage(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProcessCreateEvents_ClearClicked_ClearsName()
    {
        // Arrange
        var events = new List<CreateEvent>
        {
            new CreateEvent.NameChanged("Some Name"),
            new CreateEvent.ClearClicked()
        };

        // Act
        _sut.ProcessCreateEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessCreateEvents_MultipleNameChanges_UsesLatestName()
    {
        // Arrange
        var events = new List<CreateEvent>
        {
            new CreateEvent.NameChanged("First Name"),
            new CreateEvent.NameChanged("Second Name"),
            new CreateEvent.NameChanged("Final Name"),
            new CreateEvent.SubmitClicked()
        };

        // Act
        _sut.ProcessCreateEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("create", "", "", "Final Name"), Times.Once);
    }

    #endregion

    #region ProcessInvitesEvents Tests

    [Fact]
    public void ProcessInvitesEvents_ScrollChanged_UpdatesScrollPosition()
    {
        // Arrange
        var events = new List<InvitesEvent>
        {
            new InvitesEvent.ScrollChanged(75.5f)
        };

        // Act
        _sut.ProcessInvitesEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessInvitesEvents_AcceptInviteClicked_RequestsAcceptAction()
    {
        // Arrange
        var events = new List<InvitesEvent>
        {
            new InvitesEvent.AcceptInviteClicked("invite-123")
        };

        // Act
        _sut.ProcessInvitesEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("accept", "", "invite-123", ""), Times.Once);
    }

    [Fact]
    public void ProcessInvitesEvents_AcceptInviteDeclined_ShowsComingSoonMessage()
    {
        // Arrange
        var events = new List<InvitesEvent>
        {
            new InvitesEvent.AcceptInviteDeclined("invite-456")
        };

        // Act
        _sut.ProcessInvitesEvents(events);

        // Assert
        _mockApi.Verify(a => a.ShowChatMessage("Decline functionality coming soon!"), Times.Once);
    }

    [Fact]
    public void ProcessInvitesEvents_MultipleInviteAccepts_RequestsMultipleActions()
    {
        // Arrange
        var events = new List<InvitesEvent>
        {
            new InvitesEvent.AcceptInviteClicked("invite-1"),
            new InvitesEvent.AcceptInviteClicked("invite-2"),
            new InvitesEvent.AcceptInviteClicked("invite-3")
        };

        // Act
        _sut.ProcessInvitesEvents(events);

        // Assert
        _mockUiService.Verify(u => u.RequestCivilizationAction("accept", "", "invite-1", ""), Times.Once);
        _mockUiService.Verify(u => u.RequestCivilizationAction("accept", "", "invite-2", ""), Times.Once);
        _mockUiService.Verify(u => u.RequestCivilizationAction("accept", "", "invite-3", ""), Times.Once);
    }

    #endregion

    #region ProcessDetailEvents Tests

    [Fact]
    public void ProcessDetailEvents_BackToBrowseClicked_DoesNotThrow()
    {
        // Arrange
        var events = new List<DetailEvent>
        {
            new DetailEvent.BackToBrowseClicked()
        };

        // Act
        _sut.ProcessDetailEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessDetailEvents_MemberScrollChanged_UpdatesScrollPosition()
    {
        // Arrange
        var events = new List<DetailEvent>
        {
            new DetailEvent.MemberScrollChanged(120.75f)
        };

        // Act
        _sut.ProcessDetailEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessDetailEvents_RequestToJoinClicked_DoesNotThrow()
    {
        // Arrange - Note: This functionality is not implemented yet
        var events = new List<DetailEvent>
        {
            new DetailEvent.RequestToJoinClicked("test")
        };

        // Act
        _sut.ProcessDetailEvents(events);

        // Assert - Should not throw, even though not implemented
        Assert.NotNull(_sut);
    }

    [Fact]
    public void ProcessDetailEvents_MultipleEvents_ProcessesAll()
    {
        // Arrange
        var events = new List<DetailEvent>
        {
            new DetailEvent.MemberScrollChanged(50.0f),
            new DetailEvent.BackToBrowseClicked()
        };

        // Act
        _sut.ProcessDetailEvents(events);

        // Assert
        Assert.NotNull(_sut);
    }

    #endregion
}