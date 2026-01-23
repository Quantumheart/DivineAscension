using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;

namespace DivineAscension.Tests.Data;

[ExcludeFromCodeCoverage]
public class ReligionDataTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Parameterless_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var religion = new ReligionData();

        // Assert
        Assert.Empty(religion.ReligionUID);
        Assert.Empty(religion.ReligionName);
        Assert.Equal(DeityDomain.None, religion.Domain);
        Assert.Empty(religion.FounderUID);
        Assert.Empty(religion.MemberUIDs);
        Assert.Equal(PrestigeRank.Fledgling, religion.PrestigeRank);
        Assert.Equal(0, religion.Prestige);
        Assert.Equal(0, religion.TotalPrestige);
        Assert.Empty(religion.UnlockedBlessings);
        Assert.True(religion.IsPublic);
        Assert.Empty(religion.Description);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var religionUID = "test-religion-uid";
        var religionName = "Knights of Khoras";
        var deity = DeityDomain.Craft;
        var founderUID = "founder-123";

        // Act
        var religion = new ReligionData(religionUID, religionName, deity, "TestDeity", founderUID, "TestFounder");

        // Assert
        Assert.Equal(religionUID, religion.ReligionUID);
        Assert.Equal(religionName, religion.ReligionName);
        Assert.Equal(deity, religion.Domain);
        Assert.Equal(founderUID, religion.FounderUID);
        Assert.Single(religion.MemberUIDs);
        Assert.Contains(founderUID, religion.MemberUIDs);
        Assert.Equal(PrestigeRank.Fledgling, religion.PrestigeRank);
        Assert.Equal(0, religion.Prestige);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldSetCreationDate()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.InRange(religion.CreationDate, beforeCreation, afterCreation);
    }

    #endregion

    #region Member Management Tests

    [Fact]
    public void AddMember_NewMember_ShouldAddToMemberList()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        var newMemberUID = "member-123";

        // Act
        religion.AddMember(newMemberUID, "Member 123");

        // Assert
        Assert.Equal(2, religion.MemberUIDs.Count);
        Assert.Contains(newMemberUID, religion.MemberUIDs);
    }

    [Fact]
    public void AddMember_ExistingMember_ShouldNotDuplicate()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        var memberUID = "member-123";
        religion.AddMember(memberUID, "Member 123");

        // Act
        religion.AddMember(memberUID, "Member 123"); // Try to add again

        // Assert
        Assert.Equal(2, religion.MemberUIDs.Count);
        Assert.Single(religion.MemberUIDs, m => m == memberUID);
    }

    [Fact]
    public void RemoveMember_ExistingMember_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        var memberUID = "member-123";
        religion.AddMember(memberUID, "Member 123");

        // Act
        var result = religion.RemoveMember(memberUID);

        // Assert
        Assert.True(result);
        Assert.Single(religion.MemberUIDs);
        Assert.DoesNotContain(memberUID, religion.MemberUIDs);
    }

    [Fact]
    public void RemoveMember_NonExistingMember_ShouldReturnFalse()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");

        // Act
        var result = religion.RemoveMember("non-existing-member");

        // Assert
        Assert.False(result);
        Assert.Single(religion.MemberUIDs);
    }

    [Fact]
    public void IsMember_ExistingMember_ShouldReturnTrue()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        var memberUID = "member-123";
        religion.AddMember(memberUID, "Member 123");

        // Act
        var result = religion.IsMember(memberUID);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMember_NonExistingMember_ShouldReturnFalse()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");

        // Act
        var result = religion.IsMember("non-existing-member");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsMember_Founder_ShouldReturnTrue()
    {
        // Arrange
        var founderUID = "founder-123";
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", founderUID, "TestFounder");

        // Act
        var result = religion.IsMember(founderUID);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetMemberCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        religion.AddMember("member-1", "Member 1");
        religion.AddMember("member-2", "Member 2");
        religion.AddMember("member-3", "Member 3");

        // Act
        var count = religion.GetMemberCount();

        // Assert
        Assert.Equal(4, count); // Founder + 3 members
    }

    #endregion

    #region Founder Tests

    [Fact]
    public void IsFounder_Founder_ShouldReturnTrue()
    {
        // Arrange
        var founderUID = "founder-123";
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", founderUID, "TestFounder");

        // Act
        var result = religion.IsFounder(founderUID);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFounder_RegularMember_ShouldReturnFalse()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        var memberUID = "member-123";
        religion.AddMember(memberUID, "Member 123");

        // Act
        var result = religion.IsFounder(memberUID);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFounder_NonMember_ShouldReturnFalse()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");

        // Act
        var result = religion.IsFounder("random-player");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Prestige Tests

    [Fact]
    public void AddPrestige_PositiveAmount_ShouldIncreasePrestige()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");

        // Act
        religion.AddPrestige(100);

        // Assert
        Assert.Equal(100, religion.Prestige);
        Assert.Equal(100, religion.TotalPrestige);
    }

    [Fact]
    public void AddPrestige_MultipleAdditions_ShouldAccumulate()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");

        // Act
        religion.AddPrestige(100);
        religion.AddPrestige(200);
        religion.AddPrestige(150);

        // Assert
        Assert.Equal(450, religion.Prestige);
        Assert.Equal(450, religion.TotalPrestige);
    }

    [Fact]
    public void AddPrestige_ZeroAmount_ShouldNotChange()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        religion.AddPrestige(100);

        // Act
        religion.AddPrestige(0);

        // Assert
        Assert.Equal(100, religion.Prestige);
        Assert.Equal(100, religion.TotalPrestige);
    }

    [Fact]
    public void AddPrestige_NegativeAmount_ShouldNotChange()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        religion.AddPrestige(100);

        // Act
        religion.AddPrestige(-50);

        // Assert
        Assert.Equal(100, religion.Prestige);
        Assert.Equal(100, religion.TotalPrestige);
    }

    [Theory]
    [InlineData(0, PrestigeRank.Fledgling)]
    [InlineData(100, PrestigeRank.Fledgling)]
    [InlineData(2499, PrestigeRank.Fledgling)]
    [InlineData(2500, PrestigeRank.Established)]
    [InlineData(5000, PrestigeRank.Established)]
    [InlineData(9999, PrestigeRank.Established)]
    [InlineData(10000, PrestigeRank.Renowned)]
    [InlineData(15000, PrestigeRank.Renowned)]
    [InlineData(24999, PrestigeRank.Renowned)]
    [InlineData(25000, PrestigeRank.Legendary)]
    [InlineData(35000, PrestigeRank.Legendary)]
    [InlineData(49999, PrestigeRank.Legendary)]
    [InlineData(50000, PrestigeRank.Mythic)]
    [InlineData(75000, PrestigeRank.Mythic)]
    [InlineData(99999, PrestigeRank.Mythic)]
    public void UpdatePrestigeRank_ShouldSetCorrectRankBasedOnTotalPrestige(int totalPrestige,
        PrestigeRank expectedRank)
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        religion.AddPrestige(totalPrestige);

        // Act
        religion.UpdatePrestigeRank();

        // Assert
        Assert.Equal(expectedRank, religion.PrestigeRank);
    }

    [Fact]
    public void AddPrestige_ShouldAutomaticallyUpdateRank()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");

        // Act
        religion.AddPrestige(12500);

        // Assert
        Assert.Equal(PrestigeRank.Renowned, religion.PrestigeRank);
    }

    [Fact]
    public void RemovePrestige_SufficientPrestige_DeductsAndReturnsTrue()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        religion.AddPrestige(500);

        // Act
        var result = religion.RemovePrestige(200);

        // Assert
        Assert.True(result);
        Assert.Equal(300, religion.Prestige);
    }

    [Fact]
    public void RemovePrestige_InsufficientPrestige_ReturnsFalseAndDoesNotDeduct()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        religion.AddPrestige(100);

        // Act
        var result = religion.RemovePrestige(200);

        // Assert
        Assert.False(result);
        Assert.Equal(100, religion.Prestige); // Prestige unchanged
    }

    [Fact]
    public void RemovePrestige_ExactAmount_DeductsAndReturnsTrue()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        religion.AddPrestige(500);

        // Act
        var result = religion.RemovePrestige(500);

        // Assert
        Assert.True(result);
        Assert.Equal(0, religion.Prestige);
    }

    #endregion

    #region Blessing Tests

    [Fact]
    public void UnlockBlessing_NewBlessing_ShouldAddToUnlockedBlessings()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        var blessingId = "test-blessing-1";

        // Act
        religion.UnlockBlessing(blessingId);

        // Assert
        Assert.Single(religion.UnlockedBlessings);
        Assert.True(religion.UnlockedBlessings[blessingId]);
    }

    [Fact]
    public void UnlockBlessing_MultipleDifferentBlessings_ShouldAddAll()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");

        // Act
        religion.UnlockBlessing("blessing-1");
        religion.UnlockBlessing("blessing-2");
        religion.UnlockBlessing("blessing-3");

        // Assert
        Assert.Equal(3, religion.UnlockedBlessings.Count);
        Assert.True(religion.UnlockedBlessings["blessing-1"]);
        Assert.True(religion.UnlockedBlessings["blessing-2"]);
        Assert.True(religion.UnlockedBlessings["blessing-3"]);
    }

    [Fact]
    public void UnlockBlessing_SameBlessingTwice_ShouldRemainUnlocked()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        var blessingId = "test-blessing";

        // Act
        religion.UnlockBlessing(blessingId);
        religion.UnlockBlessing(blessingId); // Unlock again

        // Assert
        Assert.Single(religion.UnlockedBlessings);
        Assert.True(religion.UnlockedBlessings[blessingId]);
    }

    [Fact]
    public void IsBlessingUnlocked_UnlockedBlessing_ShouldReturnTrue()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        var blessingId = "test-blessing";
        religion.UnlockBlessing(blessingId);

        // Act
        var result = religion.IsBlessingUnlocked(blessingId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsBlessingUnlocked_LockedBlessing_ShouldReturnFalse()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");

        // Act
        var result = religion.IsBlessingUnlocked("non-existent-blessing");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsBlessingUnlocked_BlessingNotUnlocked_ShouldReturnFalse()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");
        var blessingId = "test-blessing";
        // Don't unlock the blessing - it should return false by default

        // Act
        var result = religion.IsBlessingUnlocked(blessingId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CompleteWorkflow_CreateReligionAddMembersAndBlessings_ShouldWork()
    {
        // Arrange
        var founderUID = "founder-123";
        var religion = new ReligionData("religion-1", "Divine Order", DeityDomain.Craft, "TestDeity", founderUID,
            "TestFounder");

        // Act - Add members
        religion.AddMember("member-1", "Member 1");
        religion.AddMember("member-2", "Member 2");
        religion.AddMember("member-3", "Member 3");

        // Act - Gain prestige
        religion.AddPrestige(3000); // Should reach Established rank

        // Act - Unlock blessings
        religion.UnlockBlessing("blessing-1");
        religion.UnlockBlessing("blessing-2");

        // Assert - Verify everything
        Assert.Equal(4, religion.GetMemberCount());
        Assert.Equal(PrestigeRank.Established, religion.PrestigeRank);
        Assert.Equal(2, religion.UnlockedBlessings.Count);
        Assert.True(religion.IsFounder(founderUID));
        Assert.True(religion.IsMember("member-1"));
        Assert.True(religion.IsBlessingUnlocked("blessing-1"));
    }

    [Fact]
    public void PrestigeProgression_FromFledglingToMythic_ShouldUpdateRanks()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");

        // Act & Assert - Progress through all ranks
        Assert.Equal(PrestigeRank.Fledgling, religion.PrestigeRank);

        religion.AddPrestige(2500);
        Assert.Equal(PrestigeRank.Established, religion.PrestigeRank);

        religion.AddPrestige(7500); // Total: 10000
        Assert.Equal(PrestigeRank.Renowned, religion.PrestigeRank);

        religion.AddPrestige(15000); // Total: 25000
        Assert.Equal(PrestigeRank.Legendary, religion.PrestigeRank);

        religion.AddPrestige(25000); // Total: 50000
        Assert.Equal(PrestigeRank.Mythic, religion.PrestigeRank);

        Assert.Equal(50000, religion.TotalPrestige);
    }

    [Fact]
    public void MemberManagement_AddRemoveMultiple_ShouldMaintainCorrectState()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", DeityDomain.Craft, "TestDeity", "founder", "TestFounder");

        // Act - Add multiple members
        religion.AddMember("member-1", "Member 1");
        religion.AddMember("member-2", "Member 2");
        religion.AddMember("member-3", "Member 3");
        religion.AddMember("member-4", "Member 4");
        Assert.Equal(5, religion.GetMemberCount());

        // Act - Remove some members
        religion.RemoveMember("member-2");
        Assert.Equal(4, religion.GetMemberCount());
        Assert.False(religion.IsMember("member-2"));

        religion.RemoveMember("member-4");
        Assert.Equal(3, religion.GetMemberCount());

        // Assert - Verify remaining members
        Assert.True(religion.IsMember("founder"));
        Assert.True(religion.IsMember("member-1"));
        Assert.True(religion.IsMember("member-3"));
        Assert.False(religion.IsMember("member-2"));
        Assert.False(religion.IsMember("member-4"));
    }

    #endregion
}