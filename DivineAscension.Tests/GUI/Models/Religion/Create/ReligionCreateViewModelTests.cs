using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Models.Religion.Create;

namespace DivineAscension.Tests.GUI.Models.Religion.Create;

/// <summary>
/// Unit tests for ReligionCreateViewModel
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionCreateViewModelTests
{
    private static readonly string[] DefaultDomains = ["Craft", "Wild", "Harvest", "Stone"];

    #region CanCreate Tests

    [Fact]
    public void CanCreate_ValidName_ReturnsTrue()
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: "TestReligion",
            domain: "Craft",
            deityName: "Khoras",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.True(vm.CanCreate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void CanCreate_EmptyOrWhitespaceName_ReturnsFalse(string? name)
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: name ?? "",
            domain: "Craft",
            deityName: "Khoras",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Theory]
    [InlineData("Ab")]
    [InlineData("X")]
    public void CanCreate_NameTooShort_ReturnsFalse(string name)
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: name,
            domain: "Craft",
            deityName: "Khoras",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_NameTooLong_ReturnsFalse()
    {
        // Arrange - 33 characters
        var longName = new string('A', 33);
        var vm = new ReligionCreateViewModel(
            religionName: longName,
            domain: "Craft",
            deityName: "Khoras",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_NameExactly32Characters_ReturnsTrue()
    {
        // Arrange - exactly 32 characters
        var exactName = new string('A', 32);
        var vm = new ReligionCreateViewModel(
            religionName: exactName,
            domain: "Craft",
            deityName: "Khoras",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.True(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_NameExactly3Characters_ReturnsTrue()
    {
        // Arrange - exactly 3 characters
        var vm = new ReligionCreateViewModel(
            religionName: "Abc",
            domain: "Craft",
            deityName: "Khoras",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.True(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_ReligionNameHasProfanity_ReturnsFalse()
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: "ValidName",
            domain: "Craft",
            deityName: "Khoras",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: "badword",
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_DeityNameHasProfanity_ReturnsFalse()
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: "ValidName",
            domain: "Craft",
            deityName: "BadDeity",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: "badword",
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_BothFieldsHaveProfanity_ReturnsFalse()
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: "BadReligion",
            domain: "Craft",
            deityName: "BadDeity",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: "badword1",
            deityNameProfanityWord: "badword2",
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_ValidNameWithErrorMessage_ReturnsTrue()
    {
        // Arrange - error message doesn't affect CanCreate
        var vm = new ReligionCreateViewModel(
            religionName: "ValidName",
            domain: "Craft",
            deityName: "Khoras",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: "Some server error",
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.True(vm.CanCreate);
    }

    #endregion

    #region HasProfanity Tests

    [Fact]
    public void ReligionNameHasProfanity_WithMatchedWord_ReturnsTrue()
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: "SomeName",
            domain: "Craft",
            deityName: "Khoras",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: "offensive",
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.True(vm.ReligionNameHasProfanity);
        Assert.False(vm.DeityNameHasProfanity);
        Assert.True(vm.HasProfanity);
        Assert.Equal("offensive", vm.ReligionNameProfanityWord);
    }

    [Fact]
    public void DeityNameHasProfanity_WithMatchedWord_ReturnsTrue()
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: "ValidName",
            domain: "Craft",
            deityName: "BadDeity",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: "offensive",
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.ReligionNameHasProfanity);
        Assert.True(vm.DeityNameHasProfanity);
        Assert.True(vm.HasProfanity);
        Assert.Equal("offensive", vm.DeityNameProfanityWord);
    }

    [Fact]
    public void HasProfanity_WithNullMatchedWords_ReturnsFalse()
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: "ValidName",
            domain: "Craft",
            deityName: "Khoras",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.ReligionNameHasProfanity);
        Assert.False(vm.DeityNameHasProfanity);
        Assert.False(vm.HasProfanity);
    }

    [Fact]
    public void HasProfanity_WithEmptyMatchedWords_ReturnsFalse()
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: "ValidName",
            domain: "Craft",
            deityName: "Khoras",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: "",
            deityNameProfanityWord: "",
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.ReligionNameHasProfanity);
        Assert.False(vm.DeityNameHasProfanity);
        Assert.False(vm.HasProfanity);
    }

    #endregion

    #region GetCurrentDomainIndex Tests

    [Theory]
    [InlineData("Craft", 0)]
    [InlineData("Wild", 1)]
    [InlineData("Harvest", 2)]
    [InlineData("Stone", 3)]
    public void GetCurrentDomainIndex_ValidDomain_ReturnsCorrectIndex(string domain, int expectedIndex)
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: "TestReligion",
            domain: domain,
            deityName: "TestDeity",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Act
        var index = vm.GetCurrentDomainIndex();

        // Assert
        Assert.Equal(expectedIndex, index);
    }

    [Fact]
    public void GetCurrentDomainIndex_UnknownDomain_ReturnsZero()
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: "TestReligion",
            domain: "UnknownDomain",
            deityName: "TestDeity",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Act
        var index = vm.GetCurrentDomainIndex();

        // Assert
        Assert.Equal(0, index);
    }

    #endregion

    #region InfoText Tests

    [Fact]
    public void InfoText_IsPublic_ReturnsPublicText()
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: "TestReligion",
            domain: "Craft",
            deityName: "Khoras",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.Contains("Public", vm.InfoText);
        Assert.Contains("anyone can join", vm.InfoText);
    }

    [Fact]
    public void InfoText_IsPrivate_ReturnsPrivateText()
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: "TestReligion",
            domain: "Craft",
            deityName: "Khoras",
            isPublic: false,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.Contains("Private", vm.InfoText);
        Assert.Contains("invitation", vm.InfoText);
    }

    #endregion

    #region DeityName Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void CanCreate_EmptyOrWhitespaceDeityName_ReturnsFalse(string? deityName)
    {
        // Arrange
        var vm = new ReligionCreateViewModel(
            religionName: "ValidReligion",
            domain: "Craft",
            deityName: deityName ?? "",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_DeityNameTooShort_ReturnsFalse()
    {
        // Arrange - 1 character (minimum is 2)
        var vm = new ReligionCreateViewModel(
            religionName: "ValidReligion",
            domain: "Craft",
            deityName: "A",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_DeityNameTooLong_ReturnsFalse()
    {
        // Arrange - 49 characters (maximum is 48)
        var longDeityName = new string('A', 49);
        var vm = new ReligionCreateViewModel(
            religionName: "ValidReligion",
            domain: "Craft",
            deityName: longDeityName,
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_DeityNameExactly2Characters_ReturnsTrue()
    {
        // Arrange - exactly 2 characters (minimum)
        var vm = new ReligionCreateViewModel(
            religionName: "ValidReligion",
            domain: "Craft",
            deityName: "Ab",
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.True(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_DeityNameExactly48Characters_ReturnsTrue()
    {
        // Arrange - exactly 48 characters (maximum)
        var exactDeityName = new string('A', 48);
        var vm = new ReligionCreateViewModel(
            religionName: "ValidReligion",
            domain: "Craft",
            deityName: exactDeityName,
            isPublic: true,
            availableDomains: DefaultDomains,
            errorMessage: null,
            religionNameProfanityWord: null,
            deityNameProfanityWord: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.True(vm.CanCreate);
    }

    #endregion
}