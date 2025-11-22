using System.Diagnostics.CodeAnalysis;
using PantheonWars.Data;

namespace PantheonWars.Tests.Data;

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
        Assert.Empty(religion.FounderUID);
        Assert.Empty(religion.MemberUIDs);
        Assert.True(religion.IsPublic);
        Assert.Empty(religion.Description);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var religionUID = "test-religion-uid";
        var religionName = "Knights Guild";
        var founderUID = "founder-123";

        // Act
        var religion = new ReligionData(religionUID, religionName, founderUID);

        // Assert
        Assert.Equal(religionUID, religion.ReligionUID);
        Assert.Equal(religionName, religion.ReligionName);
        Assert.Equal(founderUID, religion.FounderUID);
        Assert.Single(religion.MemberUIDs);
        Assert.Contains(founderUID, religion.MemberUIDs);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldSetCreationDate()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var religion = new ReligionData("uid", "name", "founder");
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
        var religion = new ReligionData("uid", "name", "founder");
        var newMemberUID = "member-123";

        // Act
        religion.AddMember(newMemberUID);

        // Assert
        Assert.Equal(2, religion.MemberUIDs.Count);
        Assert.Contains(newMemberUID, religion.MemberUIDs);
    }

    [Fact]
    public void AddMember_ExistingMember_ShouldNotDuplicate()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", "founder");
        var memberUID = "member-123";
        religion.AddMember(memberUID);

        // Act
        religion.AddMember(memberUID); // Try to add again

        // Assert
        Assert.Equal(2, religion.MemberUIDs.Count);
        Assert.Single(religion.MemberUIDs, m => m == memberUID);
    }

    [Fact]
    public void RemoveMember_ExistingMember_ShouldRemoveAndReturnTrue()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", "founder");
        var memberUID = "member-123";
        religion.AddMember(memberUID);

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
        var religion = new ReligionData("uid", "name", "founder");

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
        var religion = new ReligionData("uid", "name", "founder");
        var memberUID = "member-123";
        religion.AddMember(memberUID);

        // Act
        var result = religion.IsMember(memberUID);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsMember_NonExistingMember_ShouldReturnFalse()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", "founder");

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
        var religion = new ReligionData("uid", "name", DeityType.Khoras, founderUID);

        // Act
        var result = religion.IsMember(founderUID);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetMemberCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", "founder");
        religion.AddMember("member-1");
        religion.AddMember("member-2");
        religion.AddMember("member-3");

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
        var religion = new ReligionData("uid", "name", DeityType.Khoras, founderUID);

        // Act
        var result = religion.IsFounder(founderUID);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsFounder_RegularMember_ShouldReturnFalse()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", "founder");
        var memberUID = "member-123";
        religion.AddMember(memberUID);

        // Act
        var result = religion.IsFounder(memberUID);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsFounder_NonMember_ShouldReturnFalse()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", "founder");

        // Act
        var result = religion.IsFounder("random-player");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CompleteWorkflow_CreateGuildAddMembers_ShouldWork()
    {
        // Arrange
        var founderUID = "founder-123";
        var religion = new ReligionData("religion-1", "Divine Order", founderUID);

        // Act - Add members
        religion.AddMember("member-1");
        religion.AddMember("member-2");
        religion.AddMember("member-3");

        // Assert - Verify everything
        Assert.Equal(4, religion.GetMemberCount());
        Assert.True(religion.IsFounder(founderUID));
        Assert.True(religion.IsMember("member-1"));
    }

    [Fact]
    public void MemberManagement_AddRemoveMultiple_ShouldMaintainCorrectState()
    {
        // Arrange
        var religion = new ReligionData("uid", "name", "founder");

        // Act - Add multiple members
        religion.AddMember("member-1");
        religion.AddMember("member-2");
        religion.AddMember("member-3");
        religion.AddMember("member-4");
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