using System.Diagnostics.CodeAnalysis;
using PantheonWars.Data;

namespace PantheonWars.Tests.Data;

[ExcludeFromCodeCoverage]
public class CivilizationDataTests
{
    #region CivilizationInvite Integration Tests

    [Fact]
    public void CivilizationInvite_CompleteWorkflow_CreateAndCheckValidity()
    {
        // Arrange
        var sentDate = DateTime.UtcNow.AddDays(-5);
        var invite = new CivilizationInvite("invite-1", "civ-1", "religion-1", sentDate);

        // Assert - Invite should be valid (sent 5 days ago, expires after 7 days)
        Assert.True(invite.IsValid);
        Assert.Equal("invite-1", invite.InviteId);
        Assert.Equal("civ-1", invite.CivId);
        Assert.Equal("religion-1", invite.ReligionId);
        Assert.Equal(sentDate.AddDays(7), invite.ExpiresDate);
    }

    #endregion

    #region Civilization Constructor Tests

    [Fact]
    public void Civilization_Constructor_Parameterless_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var civilization = new Civilization();

        // Assert
        Assert.Empty(civilization.CivId);
        Assert.Empty(civilization.Name);
        Assert.Empty(civilization.FounderUID);
        Assert.Empty(civilization.FounderReligionUID);
        Assert.Empty(civilization.MemberReligionIds);
        Assert.Equal(0, civilization.MemberCount);
        Assert.Null(civilization.DisbandedDate);
    }

    [Fact]
    public void Civilization_Constructor_WithParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var civId = "civ-123";
        var name = "The Grand Alliance";
        var founderUID = "founder-456";
        var founderReligionId = "religion-789";

        // Act
        var civilization = new Civilization(civId, name, founderUID, founderReligionId);

        // Assert
        Assert.Equal(civId, civilization.CivId);
        Assert.Equal(name, civilization.Name);
        Assert.Equal(founderUID, civilization.FounderUID);
        Assert.Equal(founderReligionId, civilization.FounderReligionUID);
        Assert.Single(civilization.MemberReligionIds);
        Assert.Contains(founderReligionId, civilization.MemberReligionIds);
    }

    [Fact]
    public void Civilization_Constructor_WithParameters_ShouldSetCreationDate()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var civilization = new Civilization("civ-1", "Alliance", "founder", "religion-1");
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.InRange(civilization.CreatedDate, beforeCreation, afterCreation);
    }

    #endregion

    #region Civilization HasReligion Tests

    [Fact]
    public void HasReligion_FounderReligion_ShouldReturnTrue()
    {
        // Arrange
        var founderReligionId = "religion-1";
        var civilization = new Civilization("civ-1", "Alliance", "founder", founderReligionId);

        // Act
        var result = civilization.HasReligion(founderReligionId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasReligion_AddedReligion_ShouldReturnTrue()
    {
        // Arrange
        var civilization = new Civilization("civ-1", "Alliance", "founder", "religion-1");
        var newReligionId = "religion-2";
        civilization.AddReligion(newReligionId);

        // Act
        var result = civilization.HasReligion(newReligionId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasReligion_NonExistentReligion_ShouldReturnFalse()
    {
        // Arrange
        var civilization = new Civilization("civ-1", "Alliance", "founder", "religion-1");

        // Act
        var result = civilization.HasReligion("non-existent-religion");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Civilization AddReligion Tests

    [Fact]
    public void AddReligion_NewReligion_ShouldAddSuccessfully()
    {
        // Arrange
        var civilization = new Civilization("civ-1", "Alliance", "founder", "religion-1");
        var newReligionId = "religion-2";

        // Act
        var result = civilization.AddReligion(newReligionId);

        // Assert
        Assert.True(result);
        Assert.Equal(2, civilization.MemberReligionIds.Count);
        Assert.Contains(newReligionId, civilization.MemberReligionIds);
    }

    [Fact]
    public void AddReligion_DuplicateReligion_ShouldReturnFalse()
    {
        // Arrange
        var civilization = new Civilization("civ-1", "Alliance", "founder", "religion-1");

        // Act
        var result = civilization.AddReligion("religion-1");

        // Assert
        Assert.False(result);
        Assert.Single(civilization.MemberReligionIds);
    }

    [Fact]
    public void AddReligion_WhenAtMaximumCapacity_ShouldReturnFalse()
    {
        // Arrange
        var civilization = new Civilization("civ-1", "Alliance", "founder", "religion-1");
        civilization.AddReligion("religion-2");
        civilization.AddReligion("religion-3");
        civilization.AddReligion("religion-4");

        // Act
        var result = civilization.AddReligion("religion-5");

        // Assert
        Assert.False(result);
        Assert.Equal(4, civilization.MemberReligionIds.Count);
        Assert.DoesNotContain("religion-5", civilization.MemberReligionIds);
    }

    [Fact]
    public void AddReligion_UpToFourReligions_ShouldAllSucceed()
    {
        // Arrange
        var civilization = new Civilization("civ-1", "Alliance", "founder", "religion-1");

        // Act
        var result2 = civilization.AddReligion("religion-2");
        var result3 = civilization.AddReligion("religion-3");
        var result4 = civilization.AddReligion("religion-4");

        // Assert
        Assert.True(result2);
        Assert.True(result3);
        Assert.True(result4);
        Assert.Equal(4, civilization.MemberReligionIds.Count);
    }

    #endregion

    #region Civilization RemoveReligion Tests

    [Fact]
    public void RemoveReligion_ExistingReligion_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var civilization = new Civilization("civ-1", "Alliance", "founder", "religion-1");
        civilization.AddReligion("religion-2");

        // Act
        var result = civilization.RemoveReligion("religion-2");

        // Assert
        Assert.True(result);
        Assert.Single(civilization.MemberReligionIds);
        Assert.DoesNotContain("religion-2", civilization.MemberReligionIds);
    }

    [Fact]
    public void RemoveReligion_NonExistentReligion_ShouldReturnFalse()
    {
        // Arrange
        var civilization = new Civilization("civ-1", "Alliance", "founder", "religion-1");

        // Act
        var result = civilization.RemoveReligion("non-existent-religion");

        // Assert
        Assert.False(result);
        Assert.Single(civilization.MemberReligionIds);
    }

    [Fact]
    public void RemoveReligion_FounderReligion_ShouldRemoveSuccessfully()
    {
        // Arrange
        var founderReligionId = "religion-1";
        var civilization = new Civilization("civ-1", "Alliance", "founder", founderReligionId);

        // Act
        var result = civilization.RemoveReligion(founderReligionId);

        // Assert
        Assert.True(result);
        Assert.Empty(civilization.MemberReligionIds);
    }

    #endregion

    #region Civilization Integration Tests

    [Fact]
    public void Civilization_CompleteWorkflow_AddAndRemoveMultipleReligions_ShouldWork()
    {
        // Arrange
        var civilization = new Civilization("civ-1", "Grand Alliance", "founder", "religion-1");

        // Act - Add religions
        civilization.AddReligion("religion-2");
        civilization.AddReligion("religion-3");
        Assert.Equal(3, civilization.MemberReligionIds.Count);

        // Act - Remove one
        civilization.RemoveReligion("religion-2");
        Assert.Equal(2, civilization.MemberReligionIds.Count);

        // Act - Add another
        civilization.AddReligion("religion-4");
        Assert.Equal(3, civilization.MemberReligionIds.Count);

        // Assert - Verify state
        Assert.True(civilization.HasReligion("religion-1"));
        Assert.False(civilization.HasReligion("religion-2"));
        Assert.True(civilization.HasReligion("religion-3"));
        Assert.True(civilization.HasReligion("religion-4"));
    }

    [Fact]
    public void Civilization_MaxCapacityManagement_ShouldEnforceLimit()
    {
        // Arrange
        var civilization = new Civilization("civ-1", "Alliance", "founder", "religion-1");
        civilization.AddReligion("religion-2");
        civilization.AddReligion("religion-3");
        civilization.AddReligion("religion-4");

        // Act - Try to exceed limit
        var failedAdd = civilization.AddReligion("religion-5");
        Assert.False(failedAdd);

        // Act - Remove one and add another
        civilization.RemoveReligion("religion-2");
        var successfulAdd = civilization.AddReligion("religion-5");

        // Assert
        Assert.True(successfulAdd);
        Assert.Equal(4, civilization.MemberReligionIds.Count);
        Assert.False(civilization.HasReligion("religion-2"));
        Assert.True(civilization.HasReligion("religion-5"));
    }

    #endregion

    #region CivilizationInvite Constructor Tests

    [Fact]
    public void CivilizationInvite_Constructor_Parameterless_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var invite = new CivilizationInvite();

        // Assert
        Assert.Empty(invite.InviteId);
        Assert.Empty(invite.CivId);
        Assert.Empty(invite.ReligionId);
        Assert.Equal(default, invite.SentDate);
        Assert.Equal(default, invite.ExpiresDate);
    }

    [Fact]
    public void CivilizationInvite_Constructor_WithParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var inviteId = "invite-123";
        var civId = "civ-456";
        var religionId = "religion-789";
        var sentDate = DateTime.UtcNow;

        // Act
        var invite = new CivilizationInvite(inviteId, civId, religionId, sentDate);

        // Assert
        Assert.Equal(inviteId, invite.InviteId);
        Assert.Equal(civId, invite.CivId);
        Assert.Equal(religionId, invite.ReligionId);
        Assert.Equal(sentDate, invite.SentDate);
        Assert.Equal(sentDate.AddDays(7), invite.ExpiresDate);
    }

    [Fact]
    public void CivilizationInvite_Constructor_WithParameters_ShouldSetExpirationSevenDaysLater()
    {
        // Arrange
        var sentDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var invite = new CivilizationInvite("invite-1", "civ-1", "religion-1", sentDate);

        // Assert
        Assert.Equal(new DateTime(2024, 1, 8, 12, 0, 0, DateTimeKind.Utc), invite.ExpiresDate);
    }

    #endregion

    #region CivilizationInvite IsValid Tests

    [Fact]
    public void IsValid_FreshInvite_ShouldReturnTrue()
    {
        // Arrange
        var sentDate = DateTime.UtcNow;
        var invite = new CivilizationInvite("invite-1", "civ-1", "religion-1", sentDate);

        // Act
        var result = invite.IsValid;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_InviteWithinSevenDays_ShouldReturnTrue()
    {
        // Arrange
        var sentDate = DateTime.UtcNow.AddDays(-3);
        var invite = new CivilizationInvite("invite-1", "civ-1", "religion-1", sentDate);

        // Act
        var result = invite.IsValid;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_ExpiredInvite_ShouldReturnFalse()
    {
        // Arrange
        var sentDate = DateTime.UtcNow.AddDays(-8);
        var invite = new CivilizationInvite("invite-1", "civ-1", "religion-1", sentDate);

        // Act
        var result = invite.IsValid;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_InviteAtExactExpirationTime_ShouldReturnFalse()
    {
        // Arrange
        var sentDate = DateTime.UtcNow.AddDays(-7);
        var invite = new CivilizationInvite("invite-1", "civ-1", "religion-1", sentDate);

        // Act
        var result = invite.IsValid;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_InviteJustBeforeExpiration_ShouldReturnTrue()
    {
        // Arrange
        var sentDate = DateTime.UtcNow.AddDays(-7).AddMinutes(1);
        var invite = new CivilizationInvite("invite-1", "civ-1", "religion-1", sentDate);

        // Act
        var result = invite.IsValid;

        // Assert
        Assert.True(result);
    }

    #endregion
}