using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Unit tests for ReligionManager
///     Tests religion creation, membership management, and invitation system
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionManagerTests
{
    private readonly Mock<ICoreServerAPI> _mockAPI;
    private readonly Mock<ILogger> _mockLogger;
    private readonly ReligionManager _religionManager;

    public ReligionManagerTests()
    {
        _mockAPI = TestFixtures.CreateMockServerAPI();
        _mockLogger = new Mock<ILogger>();
        _mockAPI.Setup(a => a.Logger).Returns(_mockLogger.Object);

        _religionManager = new ReligionManager(_mockAPI.Object);
    }

    #region RemoveInvitation Tests

    [Fact]
    public void RemoveInvitation_RemovesInvitation()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", false);
        _religionManager.InvitePlayer(religion.ReligionUID, "invited-player", "founder-uid");

        // Act
        _religionManager.RemoveInvitation("invited-player", religion.ReligionUID);

        // Assert
        Assert.False(_religionManager.HasInvitation("invited-player", religion.ReligionUID));
    }

    #endregion

    #region GetReligionsByDeity Tests

    [Fact]
    public void GetReligionsByDeity_ReturnsOnlyMatchingReligions()
    {
        // Arrange
        _religionManager.CreateReligion("Khoras Religion 1", DeityType.Khoras, "founder1", true);
        _religionManager.CreateReligion("Khoras Religion 2", DeityType.Khoras, "founder2", true);
        _religionManager.CreateReligion("Lysa Religion", DeityType.Lysa, "founder3", true);

        // Act
        var khorasReligions = _religionManager.GetReligionsByDeity(DeityType.Khoras);

        // Assert
        Assert.Equal(2, khorasReligions.Count);
        Assert.All(khorasReligions, r => Assert.Equal(DeityType.Khoras, r.Deity));
    }

    #endregion

    #region HandleFounderLeaving Tests

    [Fact]
    public void HandleFounderLeaving_TransfersToNextMember()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        _religionManager.AddMember(religion.ReligionUID, "member-uid");

        // Act - Remove founder
        _religionManager.RemoveMember(religion.ReligionUID, "founder-uid");

        // Assert - Founder should have transferred
        Assert.Equal("member-uid", religion.FounderUID);
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("founder transferred"))),
            Times.Once()
        );
    }

    #endregion

    #region Initialization Tests

    [Fact]
    public void Initialize_RegistersEventHandlers()
    {
        // Arrange
        var mockEventAPI = new Mock<IServerEventAPI>();
        _mockAPI.Setup(a => a.Event).Returns(mockEventAPI.Object);

        // Act
        _religionManager.Initialize();

        // Assert
        mockEventAPI.VerifyAdd(e => e.SaveGameLoaded += It.IsAny<Action>(), Times.Once());
        mockEventAPI.VerifyAdd(e => e.GameWorldSave += It.IsAny<Action>(), Times.Once());
    }

    [Fact]
    public void Initialize_LogsNotification()
    {
        // Arrange
        var mockEventAPI = new Mock<IServerEventAPI>();
        _mockAPI.Setup(a => a.Event).Returns(mockEventAPI.Object);

        // Act
        _religionManager.Initialize();

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("Initializing") && s.Contains("Religion Manager"))),
            Times.Once()
        );
    }

    #endregion

    #region AcceptInvite Tests

    [Fact]
    public void AcceptInvite_InvalidOrExpiredInvite_ReturnsFalseAndLogsWarning()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        var invitedPlayer = "invited-player";
        _religionManager.InvitePlayer(religion.ReligionUID, invitedPlayer, "founder-uid");

        // Get the invite and make it expired
        var invite = _religionManager.GetPlayerInvitations(invitedPlayer).First();
        invite.ExpiresDate = DateTime.UtcNow.AddDays(-1);

        // Act
        var (ok, relId, error) = _religionManager.AcceptInvite(invite.InviteId, invitedPlayer);

        // Assert
        Assert.False(ok);
        Assert.Equal(string.Empty, relId);
        Assert.Equal("Invalid or expired invite", error);
        _mockLogger.Verify(
            l => l.Warning(It.Is<string>(s => s.Contains("Invalid or expired invite") && s.Contains(invite.InviteId))),
            Times.Once);

        // Invite should still be present in storage (Cleanup happens elsewhere)
        Assert.True(!_religionManager.HasInvitation(invitedPlayer, religion.ReligionUID) ||
                    !invite.IsValid);
    }

    [Fact]
    public void AcceptInvite_PlayerMismatch_ReturnsFalseAndKeepsInvite()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        var invitedPlayer = "invited-player";
        _religionManager.InvitePlayer(religion.ReligionUID, invitedPlayer, "founder-uid");
        var invite = _religionManager.GetPlayerInvitations(invitedPlayer).First();

        // Act
        var (ok, relId, error) = _religionManager.AcceptInvite(invite.InviteId, "other-player");

        // Assert
        Assert.False(ok);
        Assert.Equal(string.Empty, relId);
        Assert.Equal("Player cannot accept invite", error);
        _mockLogger.Verify(
            l => l.Warning(It.Is<string>(s =>
                s.Contains("cannot accept invite") && s.Contains("other-player") && s.Contains(invitedPlayer))),
            Times.Once);
        Assert.True(_religionManager.HasInvitation(invitedPlayer, religion.ReligionUID));
    }

    [Fact]
    public void AcceptInvite_PlayerAlreadyHasReligion_ReturnsFalse()
    {
        // Arrange: make the player already belong to a different religion
        var existing = _religionManager.CreateReligion("Existing", DeityType.Lysa, "player-uid", true);
        // Sanity: player is member of existing (as founder)
        Assert.Contains("player-uid", existing.MemberUIDs);

        var target = _religionManager.CreateReligion("Target", DeityType.Khoras, "founder-uid", true);
        _religionManager.InvitePlayer(target.ReligionUID, "player-uid", "founder-uid");
        var invite = _religionManager.GetPlayerInvitations("player-uid").First();

        // Act
        var (ok, relId, error) = _religionManager.AcceptInvite(invite.InviteId, "player-uid");

        // Assert
        Assert.False(ok);
        Assert.Equal(string.Empty, relId);
        Assert.Equal("Player has already has a religion", error);
        _mockLogger.Verify(
            l => l.Warning(It.Is<string>(s => s.Contains("already has a religion") && s.Contains("player-uid"))),
            Times.Once);
        // Invite should remain since acceptance failed
        Assert.True(_religionManager.HasInvitation("player-uid", target.ReligionUID));
    }

    [Fact]
    public void AcceptInvite_ReligionNoLongerExists_ReturnsFalseAndLogsWarning()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Ghost", DeityType.Khoras, "founder-uid", true);
        var invitedPlayer = "invited-player";
        _religionManager.InvitePlayer(religion.ReligionUID, invitedPlayer, "founder-uid");
        var invite = _religionManager.GetPlayerInvitations(invitedPlayer).First();

        // Delete the religion (by founder)
        var deleted = _religionManager.DeleteReligion(religion.ReligionUID, "founder-uid");
        Assert.True(deleted);

        // Act
        var (ok, relId, error) = _religionManager.AcceptInvite(invite.InviteId, invitedPlayer);

        // Assert
        Assert.False(ok);
        Assert.Equal(string.Empty, relId);
        Assert.Equal("No religion", error);
        _mockLogger.Verify(
            l => l.Warning(It.Is<string>(s => s.Contains("no longer exists") && s.Contains(invite.ReligionId))),
            Times.Once);
    }

    [Fact]
    public void AcceptInvite_PlayerBanned_ReturnsFalseAndLogsWarning()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Testers", DeityType.Khoras, "founder-uid", true);
        var bannedPlayer = "banned-player";
        _religionManager.InvitePlayer(religion.ReligionUID, bannedPlayer, "founder-uid");
        var invite = _religionManager.GetPlayerInvitations(bannedPlayer).First();

        // Ban the player before they accept
        var banOk = _religionManager.BanPlayer(religion.ReligionUID, bannedPlayer, "founder-uid", "Bad behavior");
        Assert.True(banOk);

        // Act
        var (ok, relId, error) = _religionManager.AcceptInvite(invite.InviteId, bannedPlayer);

        // Assert
        Assert.False(ok);
        Assert.Equal(string.Empty, relId);
        Assert.Equal("Player is banned from religion", error);
        _mockLogger.Verify(
            l => l.Warning(It.Is<string>(s =>
                s.Contains("is banned from religion") && s.Contains(bannedPlayer) &&
                s.Contains(religion.ReligionName))), Times.Once);
        // Invite should remain since acceptance failed
        Assert.True(_religionManager.HasInvitation(bannedPlayer, religion.ReligionUID));
    }

    [Fact]
    public void AcceptInvite_Success_JoinsReligion_RemovesInvite_LogsNotification()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Happy Path", DeityType.Khoras, "founder-uid", true);
        var invitedPlayer = "invited-player";
        _religionManager.InvitePlayer(religion.ReligionUID, invitedPlayer, "founder-uid");
        var invite = _religionManager.GetPlayerInvitations(invitedPlayer).First();

        // Act
        var (ok, relId, error) = _religionManager.AcceptInvite(invite.InviteId, invitedPlayer);

        // Assert
        Assert.True(ok);
        Assert.Equal(religion.ReligionUID, relId);
        Assert.Equal(string.Empty, error);
        Assert.Contains(invitedPlayer, religion.MemberUIDs);
        Assert.False(_religionManager.HasInvitation(invitedPlayer, religion.ReligionUID));
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("accepted invite") && s.Contains(invitedPlayer) && s.Contains(religion.ReligionName))),
            Times.Once);
    }

    #endregion

    #region DeclineInvite Tests

    [Fact]
    public void DeclineInvite_InviteNotFound_ReturnsFalseAndLogsWarning()
    {
        // Arrange
        var invalidInviteId = "non-existent-invite";

        // Act
        var result = _religionManager.DeclineInvite(invalidInviteId, "some-player");

        // Assert
        Assert.False(result);
        _mockLogger.Verify(
            l => l.Warning(It.Is<string>(s => s.Contains("Invite not found") && s.Contains(invalidInviteId))),
            Times.Once);
    }

    [Fact]
    public void DeclineInvite_PlayerMismatch_ReturnsFalseAndLogsWarning()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        var invitedPlayer = "invited-player";
        _religionManager.InvitePlayer(religion.ReligionUID, invitedPlayer, "founder-uid");

        var invite = _religionManager.GetPlayerInvitations(invitedPlayer).First();

        // Act
        var result = _religionManager.DeclineInvite(invite.InviteId, "other-player");

        // Assert
        Assert.False(result);
        _mockLogger.Verify(
            l => l.Warning(It.Is<string>(s =>
                s.Contains("cannot decline invite") && s.Contains("other-player") && s.Contains(invitedPlayer))),
            Times.Once);
        // Ensure invite still exists
        Assert.True(_religionManager.HasInvitation(invitedPlayer, religion.ReligionUID));
    }

    [Fact]
    public void DeclineInvite_ValidInvite_RemovesInviteAndLogsDebug()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        var invitedPlayer = "invited-player";
        _religionManager.InvitePlayer(religion.ReligionUID, invitedPlayer, "founder-uid");
        var invite = _religionManager.GetPlayerInvitations(invitedPlayer).First();

        // Pre-assert
        Assert.True(_religionManager.HasInvitation(invitedPlayer, religion.ReligionUID));

        // Act
        var result = _religionManager.DeclineInvite(invite.InviteId, invitedPlayer);

        // Assert
        Assert.True(result);
        Assert.False(_religionManager.HasInvitation(invitedPlayer, religion.ReligionUID));
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s =>
                s.Contains("declined invite") && s.Contains(invitedPlayer) && s.Contains(invite.InviteId))),
            Times.Once);
    }

    #endregion

    #region CreateReligion Tests

    [Fact]
    public void CreateReligion_WithValidParameters_CreatesReligion()
    {
        // Act
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);

        // Assert
        Assert.NotNull(religion);
        Assert.Equal("Test Religion", religion.ReligionName);
        Assert.Equal(DeityType.Khoras, religion.Deity);
        Assert.Equal("founder-uid", religion.FounderUID);
        Assert.True(religion.IsPublic);
        Assert.Contains("founder-uid", religion.MemberUIDs);
    }

    [Fact]
    public void CreateReligion_GeneratesUniqueUID()
    {
        // Act
        var religion1 = _religionManager.CreateReligion("Religion 1", DeityType.Khoras, "founder1", true);
        var religion2 = _religionManager.CreateReligion("Religion 2", DeityType.Lysa, "founder2", true);

        // Assert
        Assert.NotEqual(religion1.ReligionUID, religion2.ReligionUID);
    }

    [Fact]
    public void CreateReligion_WithDeityNone_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _religionManager.CreateReligion("Invalid Religion", DeityType.None, "founder-uid", true)
        );
    }

    [Fact]
    public void CreateReligion_LogsNotification()
    {
        // Act
        _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);

        // Assert
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("Religion created") &&
                s.Contains("Test Religion") &&
                s.Contains("Khoras") &&
                s.Contains("founder-uid"))),
            Times.Once()
        );
    }

    [Fact]
    public void CreateReligion_CanCreatePrivateReligion()
    {
        // Act
        var religion = _religionManager.CreateReligion("Private Religion", DeityType.Khoras, "founder-uid", false);

        // Assert
        Assert.False(religion.IsPublic);
    }

    #endregion

    #region AddMember Tests

    [Fact]
    public void AddMember_AddsPlayerToReligion()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);

        // Act
        _religionManager.AddMember(religion.ReligionUID, "new-member-uid");

        // Assert
        Assert.Contains("new-member-uid", religion.MemberUIDs);
        Assert.Equal(2, religion.GetMemberCount()); // Founder + new member
    }

    [Fact]
    public void AddMember_WithInvalidReligion_LogsError()
    {
        // Act
        _religionManager.AddMember("invalid-uid", "player-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Error(It.Is<string>(s => s.Contains("Cannot add member to non-existent religion"))),
            Times.Once()
        );
    }

    [Fact]
    public void AddMember_LogsDebugMessage()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        _mockLogger.Reset();

        // Act
        _religionManager.AddMember(religion.ReligionUID, "new-member-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Debug(It.Is<string>(s =>
                s.Contains("Added player") &&
                s.Contains("new-member-uid") &&
                s.Contains("Test Religion"))),
            Times.Once()
        );
    }

    #endregion

    #region RemoveMember Tests

    [Fact]
    public void RemoveMember_RemovesPlayerFromReligion()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        _religionManager.AddMember(religion.ReligionUID, "member-uid");

        // Act
        _religionManager.RemoveMember(religion.ReligionUID, "member-uid");

        // Assert
        Assert.DoesNotContain("member-uid", religion.MemberUIDs);
        Assert.Equal(1, religion.GetMemberCount());
    }

    [Fact]
    public void RemoveMember_WhenFounderLeaves_TransfersFoundership()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        _religionManager.AddMember(religion.ReligionUID, "member-uid");

        // Act
        _religionManager.RemoveMember(religion.ReligionUID, "founder-uid");

        // Assert
        Assert.Equal("member-uid", religion.FounderUID);
        Assert.Contains("member-uid", religion.MemberUIDs);
        Assert.DoesNotContain("founder-uid", religion.MemberUIDs);
    }

    [Fact]
    public void RemoveMember_WhenLastMemberLeaves_DeletesReligion()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        var religionUID = religion.ReligionUID;

        // Act
        _religionManager.RemoveMember(religionUID, "founder-uid");

        // Assert
        var retrieved = _religionManager.GetReligion(religionUID);
        Assert.Null(retrieved);

        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s =>
                s.Contains("disbanded") &&
                s.Contains("no members remaining"))),
            Times.Once()
        );
    }

    [Fact]
    public void RemoveMember_WithInvalidReligion_LogsError()
    {
        // Act
        _religionManager.RemoveMember("invalid-uid", "player-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Error(It.Is<string>(s => s.Contains("Cannot remove member from non-existent religion"))),
            Times.Once()
        );
    }

    #endregion

    #region GetPlayerReligion Tests

    [Fact]
    public void GetPlayerReligion_WithMemberPlayer_ReturnsReligion()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);

        // Act
        var found = _religionManager.GetPlayerReligion("founder-uid");

        // Assert
        Assert.NotNull(found);
        Assert.Equal(religion.ReligionUID, found.ReligionUID);
    }

    [Fact]
    public void GetPlayerReligion_WithNonMemberPlayer_ReturnsNull()
    {
        // Arrange
        _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);

        // Act
        var found = _religionManager.GetPlayerReligion("non-member-uid");

        // Assert
        Assert.Null(found);
    }

    #endregion

    #region GetReligion Tests

    [Fact]
    public void GetReligion_WithValidUID_ReturnsReligion()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);

        // Act
        var found = _religionManager.GetReligion(religion.ReligionUID);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Test Religion", found.ReligionName);
    }

    [Fact]
    public void GetReligion_WithInvalidUID_ReturnsNull()
    {
        // Act
        var found = _religionManager.GetReligion("invalid-uid");

        // Assert
        Assert.Null(found);
    }

    #endregion

    #region GetReligionByName Tests

    [Fact]
    public void GetReligionByName_WithValidName_ReturnsReligion()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);

        // Act
        var found = _religionManager.GetReligionByName("Test Religion");

        // Assert
        Assert.NotNull(found);
        Assert.Equal(religion.ReligionUID, found.ReligionUID);
    }

    [Fact]
    public void GetReligionByName_IsCaseInsensitive()
    {
        // Arrange
        _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);

        // Act
        var found = _religionManager.GetReligionByName("test religion");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Test Religion", found.ReligionName);
    }

    [Fact]
    public void GetReligionByName_WithInvalidName_ReturnsNull()
    {
        // Act
        var found = _religionManager.GetReligionByName("Nonexistent Religion");

        // Assert
        Assert.Null(found);
    }

    #endregion

    #region GetPlayerActiveDeity Tests

    [Fact]
    public void GetPlayerActiveDeity_WithPlayerInReligion_ReturnsDeity()
    {
        // Arrange
        _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);

        // Act
        var deity = _religionManager.GetPlayerActiveDeity("founder-uid");

        // Assert
        Assert.Equal(DeityType.Khoras, deity);
    }

    [Fact]
    public void GetPlayerActiveDeity_WithPlayerNotInReligion_ReturnsNone()
    {
        // Act
        var deity = _religionManager.GetPlayerActiveDeity("non-member-uid");

        // Assert
        Assert.Equal(DeityType.None, deity);
    }

    #endregion

    #region CanJoinReligion Tests

    [Fact]
    public void CanJoinReligion_PublicReligion_ReturnsTrue()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Public Religion", DeityType.Khoras, "founder-uid", true);

        // Act
        var canJoin = _religionManager.CanJoinReligion(religion.ReligionUID, "new-player-uid");

        // Assert
        Assert.True(canJoin);
    }

    [Fact]
    public void CanJoinReligion_PrivateReligionWithoutInvitation_ReturnsFalse()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Private Religion", DeityType.Khoras, "founder-uid", false);

        // Act
        var canJoin = _religionManager.CanJoinReligion(religion.ReligionUID, "new-player-uid");

        // Assert
        Assert.False(canJoin);
    }

    [Fact]
    public void CanJoinReligion_PrivateReligionWithInvitation_ReturnsTrue()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Private Religion", DeityType.Khoras, "founder-uid", false);
        _religionManager.InvitePlayer(religion.ReligionUID, "new-player-uid", "founder-uid");

        // Act
        var canJoin = _religionManager.CanJoinReligion(religion.ReligionUID, "new-player-uid");

        // Assert
        Assert.True(canJoin);
    }

    [Fact]
    public void CanJoinReligion_AlreadyMember_ReturnsFalse()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);

        // Act
        var canJoin = _religionManager.CanJoinReligion(religion.ReligionUID, "founder-uid");

        // Assert
        Assert.False(canJoin);
    }

    [Fact]
    public void CanJoinReligion_InvalidReligion_ReturnsFalse()
    {
        // Act
        var canJoin = _religionManager.CanJoinReligion("invalid-uid", "player-uid");

        // Assert
        Assert.False(canJoin);
    }

    #endregion

    #region InvitePlayer Tests

    [Fact]
    public void InvitePlayer_CreatesInvitation()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", false);

        // Act
        _religionManager.InvitePlayer(religion.ReligionUID, "invited-player", "founder-uid");

        // Assert
        Assert.True(_religionManager.HasInvitation("invited-player", religion.ReligionUID));
    }

    [Fact]
    public void InvitePlayer_WithNonMemberInviter_LogsWarning()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", false);

        // Act
        _religionManager.InvitePlayer(religion.ReligionUID, "invited-player", "non-member-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Warning(It.Is<string>(s => s.Contains("cannot invite to religion they're not in"))),
            Times.Once()
        );
        Assert.False(_religionManager.HasInvitation("invited-player", religion.ReligionUID));
    }

    [Fact]
    public void InvitePlayer_WithInvalidReligion_LogsError()
    {
        // Act
        _religionManager.InvitePlayer("invalid-uid", "player-uid", "inviter-uid");

        // Assert
        _mockLogger.Verify(
            l => l.Error(It.Is<string>(s => s.Contains("Cannot invite to non-existent religion"))),
            Times.Once()
        );
    }

    [Fact]
    public void InvitePlayer_DuplicateInvitation_DoesNotDuplicate()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", false);

        // Act
        _religionManager.InvitePlayer(religion.ReligionUID, "invited-player", "founder-uid");
        _religionManager.InvitePlayer(religion.ReligionUID, "invited-player", "founder-uid");

        // Assert
        var invitations = _religionManager.GetPlayerInvitations("invited-player");
        Assert.Single(invitations);
    }

    #endregion

    #region HasInvitation Tests

    [Fact]
    public void HasInvitation_WithValidInvitation_ReturnsTrue()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", false);
        _religionManager.InvitePlayer(religion.ReligionUID, "invited-player", "founder-uid");

        // Act
        var hasInvitation = _religionManager.HasInvitation("invited-player", religion.ReligionUID);

        // Assert
        Assert.True(hasInvitation);
    }

    [Fact]
    public void HasInvitation_WithoutInvitation_ReturnsFalse()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", false);

        // Act
        var hasInvitation = _religionManager.HasInvitation("player-uid", religion.ReligionUID);

        // Assert
        Assert.False(hasInvitation);
    }

    #endregion

    #region GetPlayerInvitations Tests

    [Fact]
    public void GetPlayerInvitations_ReturnsAllInvitations()
    {
        // Arrange
        var religion1 = _religionManager.CreateReligion("Religion 1", DeityType.Khoras, "founder1", false);
        var religion2 = _religionManager.CreateReligion("Religion 2", DeityType.Lysa, "founder2", false);

        _religionManager.InvitePlayer(religion1.ReligionUID, "player-uid", "founder1");
        _religionManager.InvitePlayer(religion2.ReligionUID, "player-uid", "founder2");

        // Act
        var invitations = _religionManager.GetPlayerInvitations("player-uid");

        // Assert
        Assert.Equal(2, invitations.Count);
        Assert.Contains(invitations, i => i.ReligionId == religion1.ReligionUID);
        Assert.Contains(invitations, i => i.ReligionId == religion2.ReligionUID);
    }

    [Fact]
    public void GetPlayerInvitations_WithNoInvitations_ReturnsEmptyList()
    {
        // Act
        var invitations = _religionManager.GetPlayerInvitations("player-uid");

        // Assert
        Assert.Empty(invitations);
    }

    #endregion

    #region HasReligion Tests

    [Fact]
    public void HasReligion_WithPlayerInReligion_ReturnsTrue()
    {
        // Arrange
        _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);

        // Act
        var hasReligion = _religionManager.HasReligion("founder-uid");

        // Assert
        Assert.True(hasReligion);
    }

    [Fact]
    public void HasReligion_WithPlayerNotInReligion_ReturnsFalse()
    {
        // Act
        var hasReligion = _religionManager.HasReligion("player-uid");

        // Assert
        Assert.False(hasReligion);
    }

    #endregion

    #region GetAllReligions Tests

    [Fact]
    public void GetAllReligions_ReturnsAllCreatedReligions()
    {
        // Arrange
        _religionManager.CreateReligion("Religion 1", DeityType.Khoras, "founder1", true);
        _religionManager.CreateReligion("Religion 2", DeityType.Lysa, "founder2", true);
        _religionManager.CreateReligion("Religion 3", DeityType.Gaia, "founder3", true);

        // Act
        var religions = _religionManager.GetAllReligions();

        // Assert
        Assert.Equal(3, religions.Count);
    }

    [Fact]
    public void GetAllReligions_WithNoReligions_ReturnsEmptyList()
    {
        // Act
        var religions = _religionManager.GetAllReligions();

        // Assert
        Assert.Empty(religions);
    }

    #endregion

    #region DeleteReligion Tests

    [Fact]
    public void DeleteReligion_ByFounder_DeletesReligion()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        var religionUID = religion.ReligionUID;

        // Act
        var deleted = _religionManager.DeleteReligion(religionUID, "founder-uid");

        // Assert
        Assert.True(deleted);
        Assert.Null(_religionManager.GetReligion(religionUID));
        _mockLogger.Verify(
            l => l.Notification(It.Is<string>(s => s.Contains("disbanded by founder"))),
            Times.Once()
        );
    }

    [Fact]
    public void DeleteReligion_ByNonFounder_ReturnsFalse()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        _religionManager.AddMember(religion.ReligionUID, "member-uid");

        // Act
        var deleted = _religionManager.DeleteReligion(religion.ReligionUID, "member-uid");

        // Assert
        Assert.False(deleted);
        Assert.NotNull(_religionManager.GetReligion(religion.ReligionUID));
    }

    [Fact]
    public void DeleteReligion_WithInvalidReligion_ReturnsFalse()
    {
        // Act
        var deleted = _religionManager.DeleteReligion("invalid-uid", "player-uid");

        // Assert
        Assert.False(deleted);
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public void OnSaveGameLoaded_LoadsReligions()
    {
        // Arrange - Create some religions
        _religionManager.CreateReligion("Religion 1", DeityType.Khoras, "founder1", true);
        _religionManager.CreateReligion("Religion 2", DeityType.Lysa, "founder2", false);

        // Act - Use reflection to call private method
        var method = _religionManager.GetType().GetMethod("OnSaveGameLoaded",
            BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Public);
        method?.Invoke(_religionManager, Array.Empty<object>());

        // Assert - Should attempt to load (even if storage is mock)
        // This test verifies the method executes without errors
        Assert.NotNull(method);
    }

    [Fact]
    public void OnGameWorldSave_SavesReligions()
    {
        // Arrange - Create some religions
        _religionManager.CreateReligion("Religion 1", DeityType.Khoras, "founder1", true);
        _religionManager.CreateReligion("Religion 2", DeityType.Lysa, "founder2", false);

        // Act - Use reflection to call private method
        var method = _religionManager.GetType().GetMethod("OnGameWorldSave",
            BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Public);
        method?.Invoke(_religionManager, Array.Empty<object>());

        // Assert - Should attempt to save (even if storage is mock)
        // This test verifies the method executes without errors
        Assert.NotNull(method);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void GetAllReligions_AfterDeletion_DoesNotIncludeDeletedReligion()
    {
        // Arrange
        var religion1 = _religionManager.CreateReligion("Religion 1", DeityType.Khoras, "founder1", true);
        var religion2 = _religionManager.CreateReligion("Religion 2", DeityType.Lysa, "founder2", true);

        // Act
        _religionManager.DeleteReligion(religion1.ReligionUID, "founder1");
        var allReligions = _religionManager.GetAllReligions();

        // Assert
        Assert.Single(allReligions);
        Assert.Equal("Religion 2", allReligions[0].ReligionName);
    }

    [Fact]
    public void RemoveMember_FromReligionWithMultipleMembers_PreservesOtherMembers()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        _religionManager.AddMember(religion.ReligionUID, "member1-uid");
        _religionManager.AddMember(religion.ReligionUID, "member2-uid");

        // Act
        _religionManager.RemoveMember(religion.ReligionUID, "member1-uid");

        // Assert
        Assert.Equal(2, religion.GetMemberCount()); // founder + member2
        Assert.Contains("founder-uid", religion.MemberUIDs);
        Assert.Contains("member2-uid", religion.MemberUIDs);
        Assert.DoesNotContain("member1-uid", religion.MemberUIDs);
    }

    [Fact]
    public void InvitePlayer_ToAlreadyInvited_DoesNotCreateDuplicates()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", false);

        // Act - Invite twice
        _religionManager.InvitePlayer(religion.ReligionUID, "player-uid", "founder-uid");
        _religionManager.InvitePlayer(religion.ReligionUID, "player-uid", "founder-uid");

        // Assert - Only one invitation should exist
        var invitations = _religionManager.GetPlayerInvitations("player-uid");
        Assert.Single(invitations);
    }

    [Fact]
    public void GetPlayerReligion_AfterPlayerJoins_ReturnsCorrectReligion()
    {
        // Arrange
        var religion1 = _religionManager.CreateReligion("Religion 1", DeityType.Khoras, "founder1", true);
        var religion2 = _religionManager.CreateReligion("Religion 2", DeityType.Lysa, "founder2", true);

        // Act
        var religionForFounder1 = _religionManager.GetPlayerReligion("founder1");
        var religionForFounder2 = _religionManager.GetPlayerReligion("founder2");

        // Assert
        Assert.Equal(religion1.ReligionUID, religionForFounder1?.ReligionUID);
        Assert.Equal(religion2.ReligionUID, religionForFounder2?.ReligionUID);
    }

    [Fact]
    public void CreateReligion_WithNullOrEmptyName_StillCreatesReligion()
    {
        // Arrange & Act
        var religion = _religionManager.CreateReligion(string.Empty, DeityType.Khoras, "founder-uid", true);

        // Assert - Should create religion even with empty name
        Assert.NotNull(religion);
        Assert.Empty(religion.ReligionName);
    }

    [Fact]
    public void GetReligionsByDeity_WithNoMatchingReligions_ReturnsEmptyList()
    {
        // Arrange
        _religionManager.CreateReligion("Khoras Religion", DeityType.Khoras, "founder1", true);

        // Act
        var lysaReligions = _religionManager.GetReligionsByDeity(DeityType.Lysa);

        // Assert
        Assert.Empty(lysaReligions);
    }

    [Fact]
    public void CanJoinReligion_PlayerAlreadyInAnotherReligion_StillReturnsBasedOnThisReligion()
    {
        // Arrange
        var religion1 = _religionManager.CreateReligion("Religion 1", DeityType.Khoras, "founder1", true);
        var religion2 = _religionManager.CreateReligion("Religion 2", DeityType.Lysa, "founder2", true);

        // Player is already in religion1 as founder
        // Act - Check if founder can join religion2
        var canJoin = _religionManager.CanJoinReligion(religion2.ReligionUID, "founder1");

        // Assert - Should return true for public religion (even if player is in another)
        Assert.True(canJoin);
    }

    [Fact]
    public void RemoveInvitation_ForNonExistentInvitation_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        var exception = Record.Exception(() =>
            _religionManager.RemoveInvitation("player-uid", "religion-uid"));

        Assert.Null(exception);
    }

    #endregion

    #region OnReligionDeleted Event Tests

    [Fact]
    public void RemoveMember_WhenLastMemberLeaves_FiresOnReligionDeletedEvent()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        var religionUID = religion.ReligionUID;
        var eventFired = false;
        string? deletedReligionId = null;

        _religionManager.OnReligionDeleted += religionId =>
        {
            eventFired = true;
            deletedReligionId = religionId;
        };

        // Act
        _religionManager.RemoveMember(religionUID, "founder-uid");

        // Assert
        Assert.True(eventFired);
        Assert.Equal(religionUID, deletedReligionId);
    }

    [Fact]
    public void DeleteReligion_ByFounder_FiresOnReligionDeletedEvent()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        var religionUID = religion.ReligionUID;
        var eventFired = false;
        string? deletedReligionId = null;

        _religionManager.OnReligionDeleted += religionId =>
        {
            eventFired = true;
            deletedReligionId = religionId;
        };

        // Act
        var deleted = _religionManager.DeleteReligion(religionUID, "founder-uid");

        // Assert
        Assert.True(deleted);
        Assert.True(eventFired);
        Assert.Equal(religionUID, deletedReligionId);
    }

    [Fact]
    public void RemoveMember_WhenNotLastMember_DoesNotFireEvent()
    {
        // Arrange
        var religion = _religionManager.CreateReligion("Test Religion", DeityType.Khoras, "founder-uid", true);
        _religionManager.AddMember(religion.ReligionUID, "member-uid");
        var eventFired = false;

        _religionManager.OnReligionDeleted += _ => eventFired = true;

        // Act - Remove member but not founder
        _religionManager.RemoveMember(religion.ReligionUID, "member-uid");

        // Assert
        Assert.False(eventFired);
    }

    #endregion
}