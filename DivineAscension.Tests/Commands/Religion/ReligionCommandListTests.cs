using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Commands.Religion;

/// <summary>
/// Tests for the /religion list command handler
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCommandListTests : ReligionCommandsTestHelpers
{
    public ReligionCommandListTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region Success Cases - No Filter

    [Fact]
    public void OnListReligions_WithNoFilter_ListsAllReligions()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        var religions = new List<ReligionData>
        {
            CreateReligion("religion-1", "FirstReligion", DeityDomain.Craft, "founder-1"),
            CreateReligion("religion-2", "SecondReligion", DeityDomain.Wild, "founder-2")
        };

        _religionManager.Setup(m => m.GetAllReligions()).Returns(religions);

        // Act
        var result = _sut!.OnListReligions(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("FirstReligion", result.StatusMessage);
        Assert.Contains("SecondReligion", result.StatusMessage);
        Assert.Contains("Khoras", result.StatusMessage);
        Assert.Contains("Lysa", result.StatusMessage);
    }

    [Fact]
    public void OnListReligions_ShowsPublicPrivateStatus()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        var religions = new List<ReligionData>
        {
            CreateReligion("religion-1", "PublicReligion", DeityDomain.Craft, "founder-1", isPublic: true),
            CreateReligion("religion-2", "PrivateReligion", DeityDomain.Wild, "founder-2", isPublic: false)
        };

        _religionManager.Setup(m => m.GetAllReligions()).Returns(religions);

        // Act
        var result = _sut!.OnListReligions(args);

        // Assert
        Assert.Contains("Public", result.StatusMessage);
        Assert.Contains("Private", result.StatusMessage);
    }

    [Fact]
    public void OnListReligions_ShowsMemberCount()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        var religion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "founder-1");
        religion.MemberUIDs.Add("member-1");
        religion.MemberUIDs.Add("member-2");

        var religions = new List<ReligionData> { religion };
        _religionManager.Setup(m => m.GetAllReligions()).Returns(religions);

        // Act
        var result = _sut!.OnListReligions(args);

        // Assert
        Assert.Contains("3 members", result.StatusMessage);
    }

    [Fact]
    public void OnListReligions_ShowsPrestigeRank()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        var religion = CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "founder-1");
        religion.PrestigeRank = PrestigeRank.Renowned;

        var religions = new List<ReligionData> { religion };
        _religionManager.Setup(m => m.GetAllReligions()).Returns(religions);

        // Act
        var result = _sut!.OnListReligions(args);

        // Assert
        Assert.Contains("Renowned", result.StatusMessage);
    }

    [Fact]
    public void OnListReligions_OrdersByPrestige()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        var lowPrestige = CreateReligion("religion-1", "LowPrestige", DeityDomain.Craft, "founder-1");
        lowPrestige.TotalPrestige = 100;

        var highPrestige = CreateReligion("religion-2", "HighPrestige", DeityDomain.Wild, "founder-2");
        highPrestige.TotalPrestige = 1000;

        var religions = new List<ReligionData> { lowPrestige, highPrestige };
        _religionManager.Setup(m => m.GetAllReligions()).Returns(religions);

        // Act
        var result = _sut!.OnListReligions(args);

        // Assert
        var highIndex = result.StatusMessage.IndexOf("HighPrestige");
        var lowIndex = result.StatusMessage.IndexOf("LowPrestige");
        Assert.True(highIndex < lowIndex, "Religions should be ordered by prestige (descending)");
    }

    #endregion

    #region Success Cases - With Deity Filter

    [Fact]
    public void OnListReligions_WithDeityFilter_ListsOnlyMatchingReligions()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, nameof(DeityDomain.Craft));

        var khorasReligions = new List<ReligionData>
        {
            CreateReligion("religion-1", "KhorasReligion", DeityDomain.Craft, "founder-1")
        };

        _religionManager.Setup(m => m.GetReligionsByDeity(DeityDomain.Craft)).Returns(khorasReligions);

        // Act
        var result = _sut!.OnListReligions(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("KhorasReligion", result.StatusMessage);
        _religionManager.Verify(m => m.GetReligionsByDeity(DeityDomain.Craft), Times.Once);
    }

    [Fact]
    public void OnListReligions_WithInvalidDeity_ReturnsError()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, "InvalidDeity");

        // Act
        var result = _sut!.OnListReligions(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Invalid deity filter: InvalidDeity", result.StatusMessage);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void OnListReligions_WhenNoReligionsExist_ReturnsEmptyMessage()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args);

        _religionManager.Setup(m => m.GetAllReligions()).Returns(new List<ReligionData>());

        // Act
        var result = _sut!.OnListReligions(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("No religions found", result.StatusMessage);
    }

    [Fact]
    public void OnListReligions_WhenFilterReturnsNone_ReturnsEmptyMessage()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, nameof(DeityDomain.Wild));

        _religionManager.Setup(m => m.GetReligionsByDeity(DeityDomain.Wild)).Returns(new List<ReligionData>());

        // Act
        var result = _sut!.OnListReligions(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("No religions found", result.StatusMessage);
    }

    [Fact]
    public void OnListReligions_CaseInsensitiveDeityFilter()
    {
        // Arrange
        var mockPlayer = CreateMockPlayer("player-1", "TestPlayer");
        var args = CreateCommandArgs(mockPlayer.Object);
        SetupParsers(args, nameof(DeityDomain.Craft)); // lowercase

        var religions = new List<ReligionData>
        {
            CreateReligion("religion-1", "TestReligion", DeityDomain.Craft, "founder-1")
        };

        _religionManager.Setup(m => m.GetReligionsByDeity(DeityDomain.Craft)).Returns(religions);

        // Act
        var result = _sut!.OnListReligions(args);

        // Assert
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("TestReligion", result.StatusMessage);
    }

    #endregion
}