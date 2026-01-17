using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Tests.Commands.Helpers;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Commands.Civilization;

/// <summary>
/// Tests for admin civilization commands (create, dissolve, cleanup)
/// </summary>
[ExcludeFromCodeCoverage]
public class CivilizationCommandAdminTests : CivilizationCommandsTestHelpers
{
    public CivilizationCommandAdminTests()
    {
        _sut = InitializeMocksAndSut();
    }

    #region /civ admin create tests

    [Fact]
    public void OnAdminCreate_SingleReligion_CreatesSuccessfully()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var religion1 = CreateReligion("religion-1", "TestReligion1", DeityDomain.Craft, "test-deity", "founder-1");
        var civilization = CreateCivilization("civ-1", "TestCiv", "founder-1", new List<string> { "religion-1" });

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "TestCiv", "TestReligion1");

        _religionManager.Setup(m => m.GetReligionByName("TestReligion1")).Returns(religion1);
        _civilizationManager.Setup(m => m.GetCivilizationByReligion("religion-1"))
            .Returns((DivineAscension.Data.Civilization?)null);
        _civilizationManager.Setup(m => m.CreateCivilization("TestCiv", "founder-1", "religion-1", "default", ""))
            .Returns(civilization);
        _religionManager.Setup(m => m.GetReligion("religion-1")).Returns(religion1);

        // Act
        var result = _sut!.OnAdminCreate(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Created civilization 'TestCiv' with 1 religion(s)", result.StatusMessage);
        _civilizationManager.Verify(m => m.CreateCivilization("TestCiv", "founder-1", "religion-1", "default", ""),
            Times.Once);
    }

    [Fact]
    public void OnAdminCreate_MultipleReligions_CreatesSuccessfully()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var religion1 = CreateReligion("religion-1", "Religion1", DeityDomain.Craft, "test-deity", "founder-1");
        var religion2 = CreateReligion("religion-2", "Religion2", DeityDomain.Wild, "test-deity", "founder-2");
        var religion3 = CreateReligion("religion-3", "Religion3", DeityDomain.Harvest, "test-deity", "founder-3");
        var civilization = CreateCivilization("civ-1", "TestCiv", "founder-1", new List<string> { "religion-1" });

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "TestCiv", "Religion1", "Religion2", "Religion3");

        _religionManager.Setup(m => m.GetReligionByName("Religion1")).Returns(religion1);
        _religionManager.Setup(m => m.GetReligionByName("Religion2")).Returns(religion2);
        _religionManager.Setup(m => m.GetReligionByName("Religion3")).Returns(religion3);
        _civilizationManager.Setup(m => m.GetCivilizationByReligion(It.IsAny<string>()))
            .Returns((DivineAscension.Data.Civilization?)null);
        _civilizationManager.Setup(m => m.CreateCivilization("TestCiv", "founder-1", "religion-1", "default", ""))
            .Returns(civilization);
        _religionManager.Setup(m => m.GetReligion(It.IsAny<string>()))
            .Returns((string id) =>
            {
                if (id == "religion-1") return religion1;
                if (id == "religion-2") return religion2;
                if (id == "religion-3") return religion3;
                return null;
            });

        // Act
        var result = _sut!.OnAdminCreate(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Created civilization 'TestCiv' with 3 religion(s)", result.StatusMessage);
        Assert.Contains("religion-2", civilization.MemberReligionIds);
        Assert.Contains("religion-3", civilization.MemberReligionIds);
    }

    [Fact]
    public void OnAdminCreate_ReligionNotFound_ReturnsError()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "TestCiv", "NonExistentReligion");

        _religionManager.Setup(m => m.GetReligionByName("NonExistentReligion")).Returns((ReligionData?)null);

        // Act
        var result = _sut!.OnAdminCreate(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Religion 'NonExistentReligion' not found", result.StatusMessage);
    }

    [Fact]
    public void OnAdminCreate_DuplicateDeities_ReturnsError()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var religion1 = CreateReligion("religion-1", "Religion1", DeityDomain.Craft, "test-deity", "founder-1");
        var religion2 = CreateReligion("religion-2", "Religion2", DeityDomain.Craft, "test-deity", "founder-2");

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "TestCiv", "Religion1", "Religion2");

        _religionManager.Setup(m => m.GetReligionByName("Religion1")).Returns(religion1);
        _religionManager.Setup(m => m.GetReligionByName("Religion2")).Returns(religion2);

        // Act
        var result = _sut!.OnAdminCreate(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Duplicate deity/deities found", result.StatusMessage);
        Assert.Contains("Craft", result.StatusMessage);
    }

    [Fact]
    public void OnAdminCreate_ReligionAlreadyInCivilization_ReturnsError()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var religion1 = CreateReligion("religion-1", "Religion1", DeityDomain.Craft, "test-deity", "founder-1");
        var existingCiv =
            CreateCivilization("existing-civ", "ExistingCiv", "founder-1", new List<string> { "religion-1" });

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "TestCiv", "Religion1");

        _religionManager.Setup(m => m.GetReligionByName("Religion1")).Returns(religion1);
        _civilizationManager.Setup(m => m.GetCivilizationByReligion("religion-1")).Returns(existingCiv);

        // Act
        var result = _sut!.OnAdminCreate(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Religion 'Religion1' is already part of civilization 'ExistingCiv'", result.StatusMessage);
    }

    [Fact]
    public void OnAdminCreate_CreateCivilizationFails_ReturnsError()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var religion1 = CreateReligion("religion-1", "Religion1", DeityDomain.Craft, "test-deity", "founder-1");

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "TestCiv", "Religion1");

        _religionManager.Setup(m => m.GetReligionByName("Religion1")).Returns(religion1);
        _civilizationManager.Setup(m => m.GetCivilizationByReligion("religion-1"))
            .Returns((DivineAscension.Data.Civilization?)null);
        _civilizationManager.Setup(m => m.CreateCivilization("TestCiv", "founder-1", "religion-1", "default", ""))
            .Returns((DivineAscension.Data.Civilization?)null);

        // Act
        var result = _sut!.OnAdminCreate(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Failed to create civilization", result.StatusMessage);
    }

    #endregion

    #region /civ admin dissolve tests

    [Fact]
    public void OnAdminDissolve_ValidCivilization_DissolvesSuccessfully()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var civilization = CreateCivilization("civ-1", "TestCiv", "founder-1",
            new List<string> { "religion-1", "religion-2" });

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "TestCiv");

        _civilizationManager.Setup(m => m.GetAllCivilizations())
            .Returns(new List<DivineAscension.Data.Civilization> { civilization });
        _civilizationManager.Setup(m => m.DisbandCivilization("civ-1", "founder-1"));

        // Act
        var result = _sut!.OnAdminDissolve(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Civilization 'TestCiv' has been disbanded", result.StatusMessage);
        _civilizationManager.Verify(m => m.DisbandCivilization("civ-1", "founder-1"), Times.Once);
    }

    [Fact]
    public void OnAdminDissolve_CivilizationNotFound_ReturnsError()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "NonExistentCiv");

        _civilizationManager.Setup(m => m.GetAllCivilizations()).Returns(new List<DivineAscension.Data.Civilization>());

        // Act
        var result = _sut!.OnAdminDissolve(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Error, result.Status);
        Assert.Contains("Civilization 'NonExistentCiv' not found", result.StatusMessage);
    }

    [Fact]
    public void OnAdminDissolve_CaseInsensitiveSearch_DissolvesSuccessfully()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var civilization = CreateCivilization("civ-1", "TestCiv", "founder-1", new List<string> { "religion-1" });

        var args = CreateCommandArgs(admin.Object);
        SetupParsers(args, "testciv"); // lowercase

        _civilizationManager.Setup(m => m.GetAllCivilizations())
            .Returns(new List<DivineAscension.Data.Civilization> { civilization });
        _civilizationManager.Setup(m => m.DisbandCivilization("civ-1", "founder-1"));

        // Act
        var result = _sut!.OnAdminDissolve(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Civilization 'testciv' has been disbanded", result.StatusMessage);
        _civilizationManager.Verify(m => m.DisbandCivilization("civ-1", "founder-1"), Times.Once);
    }

    #endregion

    #region /civ admin cleanup tests

    [Fact]
    public void OnCleanupOrphanedData_NoOrphans_ReturnsSuccessMessage()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        var civilization = CreateCivilization("civ-1", "TestCiv", "founder-1", new List<string> { "religion-1" });

        var args = CreateCommandArgs(admin.Object);

        _civilizationManager.Setup(m => m.GetAllCivilizations())
            .Returns(new List<DivineAscension.Data.Civilization> { civilization });

        // Act
        var result = _sut!.OnCleanupOrphanedData(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("No orphaned civilizations found", result.StatusMessage);
    }

    [Fact]
    public void OnCleanupOrphanedData_WithOrphans_CleansUpSuccessfully()
    {
        // Arrange
        var admin = CreateMockPlayer("admin-1", "Admin");
        // Create orphaned civs by removing their only religion after creation
        var orphanedCiv1 = CreateCivilization("civ-1", "OrphanedCiv1", "founder-1", new List<string> { "religion-orphan-1" });
        orphanedCiv1.RemoveReligion("religion-orphan-1");
        var orphanedCiv2 = CreateCivilization("civ-2", "OrphanedCiv2", "founder-2", new List<string> { "religion-orphan-2" });
        orphanedCiv2.RemoveReligion("religion-orphan-2");
        var validCiv = CreateCivilization("civ-3", "ValidCiv", "founder-3", new List<string> { "religion-2" });

        var args = CreateCommandArgs(admin.Object);

        _civilizationManager.Setup(m => m.GetAllCivilizations())
            .Returns(new List<DivineAscension.Data.Civilization> { orphanedCiv1, orphanedCiv2, validCiv });
        _civilizationManager.Setup(m => m.GetCivilization("civ-1")).Returns(orphanedCiv1);
        _civilizationManager.Setup(m => m.GetCivilization("civ-2")).Returns(orphanedCiv2);
        _civilizationManager.Setup(m => m.DisbandCivilization(It.IsAny<string>(), It.IsAny<string>()));

        // Act
        var result = _sut!.OnCleanupOrphanedData(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Cleaned up 2 orphaned civilization(s)", result.StatusMessage);
        _civilizationManager.Verify(m => m.DisbandCivilization("civ-1", "founder-1"), Times.Once);
        _civilizationManager.Verify(m => m.DisbandCivilization("civ-2", "founder-2"), Times.Once);
        _civilizationManager.Verify(m => m.DisbandCivilization("civ-3", It.IsAny<string>()), Times.Never);
    }

    #endregion
}