using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Models.Civilization.Create;

namespace DivineAscension.Tests.GUI.Models.Civilization.Create;

/// <summary>
/// Unit tests for CivilizationCreateViewModel
/// </summary>
[ExcludeFromCodeCoverage]
public class CivilizationCreateViewModelTests
{
    #region Property Tests

    [Fact]
    public void Properties_AreCorrectlyInitialized()
    {
        // Arrange & Act
        var vm = new CivilizationCreateViewModel(
            civilizationName: "TestCiv",
            selectedIcon: "custom_icon",
            description: "Test description",
            errorMessage: "Error!",
            userIsReligionFounder: true,
            userInCivilization: false,
            profanityMatchedWord: "test",
            profanityMatchedWordInDescription: null,
            x: 100, y: 200, width: 600, height: 500);

        // Assert
        Assert.Equal("TestCiv", vm.CivilizationName);
        Assert.Equal("custom_icon", vm.SelectedIcon);
        Assert.Equal("Error!", vm.ErrorMessage);
        Assert.True(vm.UserIsReligionFounder);
        Assert.False(vm.UserInCivilization);
        Assert.Equal("test", vm.ProfanityMatchedWord);
        Assert.Equal(100, vm.X);
        Assert.Equal(200, vm.Y);
        Assert.Equal(600, vm.Width);
        Assert.Equal(500, vm.Height);
    }

    #endregion

    #region CanCreate Tests

    [Fact]
    public void CanCreate_AllConditionsMet_ReturnsTrue()
    {
        // Arrange
        var vm = new CivilizationCreateViewModel(
            civilizationName: "TestCiv",
            selectedIcon: "default",
            description: "",
            errorMessage: null,
            userIsReligionFounder: true,
            userInCivilization: false,
            profanityMatchedWord: null,
            profanityMatchedWordInDescription: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.True(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_NotReligionFounder_ReturnsFalse()
    {
        // Arrange
        var vm = new CivilizationCreateViewModel(
            civilizationName: "TestCiv",
            selectedIcon: "default",
            description: "",
            errorMessage: null,
            userIsReligionFounder: false,
            userInCivilization: false,
            profanityMatchedWord: null,
            profanityMatchedWordInDescription: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_AlreadyInCivilization_ReturnsFalse()
    {
        // Arrange
        var vm = new CivilizationCreateViewModel(
            civilizationName: "TestCiv",
            selectedIcon: "default",
            description: "",
            errorMessage: null,
            userIsReligionFounder: true,
            userInCivilization: true,
            profanityMatchedWord: null,
            profanityMatchedWordInDescription: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void CanCreate_EmptyOrWhitespaceName_ReturnsFalse(string? name)
    {
        // Arrange
        var vm = new CivilizationCreateViewModel(
            civilizationName: name ?? "",
            selectedIcon: "default",
            description: "",
            errorMessage: null,
            userIsReligionFounder: true,
            userInCivilization: false,
            profanityMatchedWord: null,
            profanityMatchedWordInDescription: null,
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
        var vm = new CivilizationCreateViewModel(
            civilizationName: name,
            selectedIcon: "default",
            description: "",
            errorMessage: null,
            userIsReligionFounder: true,
            userInCivilization: false,
            profanityMatchedWord: null,
            profanityMatchedWordInDescription: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_NameTooLong_ReturnsFalse()
    {
        // Arrange - 33 characters
        var longName = new string('A', 33);
        var vm = new CivilizationCreateViewModel(
            civilizationName: longName,
            selectedIcon: "default",
            description: "",
            errorMessage: null,
            userIsReligionFounder: true,
            userInCivilization: false,
            profanityMatchedWord: null,
            profanityMatchedWordInDescription: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_NameExactly32Characters_ReturnsTrue()
    {
        // Arrange - exactly 32 characters
        var exactName = new string('A', 32);
        var vm = new CivilizationCreateViewModel(
            civilizationName: exactName,
            selectedIcon: "default",
            description: "",
            errorMessage: null,
            userIsReligionFounder: true,
            userInCivilization: false,
            profanityMatchedWord: null,
            profanityMatchedWordInDescription: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.True(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_NameExactly3Characters_ReturnsTrue()
    {
        // Arrange - exactly 3 characters
        var vm = new CivilizationCreateViewModel(
            civilizationName: "Abc",
            selectedIcon: "default",
            description: "",
            errorMessage: null,
            userIsReligionFounder: true,
            userInCivilization: false,
            profanityMatchedWord: null,
            profanityMatchedWordInDescription: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.True(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_HasProfanity_ReturnsFalse()
    {
        // Arrange
        var vm = new CivilizationCreateViewModel(
            civilizationName: "ValidName",
            selectedIcon: "default",
            description: "",
            errorMessage: null,
            userIsReligionFounder: true,
            userInCivilization: false,
            profanityMatchedWord: "badword",
            profanityMatchedWordInDescription: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.CanCreate);
    }

    [Fact]
    public void CanCreate_ValidNameWithErrorMessage_ReturnsTrue()
    {
        // Arrange - error message doesn't affect CanCreate
        var vm = new CivilizationCreateViewModel(
            civilizationName: "ValidName",
            selectedIcon: "default",
            description: "",
            errorMessage: "Some server error",
            userIsReligionFounder: true,
            userInCivilization: false,
            profanityMatchedWord: null,
            profanityMatchedWordInDescription: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.True(vm.CanCreate);
    }

    #endregion

    #region HasProfanity Tests

    [Fact]
    public void HasProfanity_WithMatchedWord_ReturnsTrue()
    {
        // Arrange
        var vm = new CivilizationCreateViewModel(
            civilizationName: "SomeName",
            selectedIcon: "default",
            description: "",
            errorMessage: null,
            userIsReligionFounder: true,
            userInCivilization: false,
            profanityMatchedWord: "offensive",
            profanityMatchedWordInDescription: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.True(vm.HasProfanity);
        Assert.Equal("offensive", vm.ProfanityMatchedWord);
    }

    [Fact]
    public void HasProfanity_WithNullMatchedWord_ReturnsFalse()
    {
        // Arrange
        var vm = new CivilizationCreateViewModel(
            civilizationName: "ValidName",
            selectedIcon: "default",
            description: "",
            errorMessage: null,
            userIsReligionFounder: true,
            userInCivilization: false,
            profanityMatchedWord: null,
            profanityMatchedWordInDescription: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.HasProfanity);
    }

    [Fact]
    public void HasProfanity_WithEmptyMatchedWord_ReturnsFalse()
    {
        // Arrange
        var vm = new CivilizationCreateViewModel(
            civilizationName: "ValidName",
            selectedIcon: "default",
            description: "",
            errorMessage: null,
            userIsReligionFounder: true,
            userInCivilization: false,
            profanityMatchedWord: "",
            profanityMatchedWordInDescription: null,
            x: 0, y: 0, width: 500, height: 400);

        // Assert
        Assert.False(vm.HasProfanity);
    }

    #endregion
}