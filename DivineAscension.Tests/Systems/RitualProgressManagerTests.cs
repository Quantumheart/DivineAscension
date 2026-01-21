using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services.Interfaces;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using Moq;
using Vintagestory.API.Common;
using Xunit;

namespace DivineAscension.Tests.Systems;

/// <summary>
/// Tests for RitualProgressManager ritual tracking and tier upgrades.
/// </summary>
[ExcludeFromCodeCoverage]
public class RitualProgressManagerTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IRitualLoader> _mockRitualLoader;
    private readonly Mock<IHolySiteManager> _mockHolySiteManager;
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly RitualProgressManager _manager;

    private const string TestSiteUID = "site1";
    private const string TestRitualId = "craft_tier2_ritual";
    private const string TestPlayerUID = "player1";
    private const string TestReligionUID = "religion1";

    public RitualProgressManagerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockRitualLoader = new Mock<IRitualLoader>();
        _mockHolySiteManager = new Mock<IHolySiteManager>();
        _mockReligionManager = new Mock<IReligionManager>();

        _manager = new RitualProgressManager(
            _mockLogger.Object,
            _mockRitualLoader.Object,
            _mockHolySiteManager.Object,
            _mockReligionManager.Object);
    }

    #region StartRitual Tests

    [Fact]
    public void StartRitual_Success_InitializesRitualProgress()
    {
        // Arrange
        var site = CreateTestSite();
        var religion = CreateTestReligion();
        var ritual = CreateTestRitual();

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockReligionManager.Setup(m => m.GetReligion(TestReligionUID)).Returns(religion);
        _mockRitualLoader.Setup(m => m.GetRitualById(TestRitualId)).Returns(ritual);

        // Act
        var result = _manager.StartRitual(TestSiteUID, TestRitualId, TestPlayerUID);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Started ritual", result.Message);
        Assert.NotNull(result.Ritual);
        Assert.NotNull(site.ActiveRitual);
        Assert.Equal(TestRitualId, site.ActiveRitual.RitualId);
        Assert.Equal(2, site.ActiveRitual.Progress.Count); // 2 requirements in test ritual
    }

    [Fact]
    public void StartRitual_SiteNotFound_ReturnsFalse()
    {
        // Arrange
        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns((HolySiteData?)null);

        // Act
        var result = _manager.StartRitual(TestSiteUID, TestRitualId, TestPlayerUID);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Holy site not found", result.Message);
    }

    [Fact]
    public void StartRitual_ReligionNotFound_ReturnsFalse()
    {
        // Arrange
        var site = CreateTestSite();
        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockReligionManager.Setup(m => m.GetReligion(TestReligionUID)).Returns((ReligionData?)null);

        // Act
        var result = _manager.StartRitual(TestSiteUID, TestRitualId, TestPlayerUID);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Religion not found", result.Message);
    }

    [Fact]
    public void StartRitual_NotConsecrator_ReturnsFalse()
    {
        // Arrange
        var site = CreateTestSite();
        var religion = CreateTestReligion();

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockReligionManager.Setup(m => m.GetReligion(TestReligionUID)).Returns(religion);

        // Act - Different player trying to start ritual
        var result = _manager.StartRitual(TestSiteUID, TestRitualId, "different_player");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Only the site consecrator", result.Message);
    }

    [Fact]
    public void StartRitual_RitualAlreadyActive_ReturnsFalse()
    {
        // Arrange
        var site = CreateTestSite();
        site.ActiveRitual = new RitualProgressData
        {
            RitualId = "existing_ritual",
            StartedAt = DateTime.UtcNow,
            Progress = new()
        };

        var religion = CreateTestReligion();

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockReligionManager.Setup(m => m.GetReligion(TestReligionUID)).Returns(religion);

        // Act
        var result = _manager.StartRitual(TestSiteUID, TestRitualId, TestPlayerUID);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already in progress", result.Message);
    }

    [Fact]
    public void StartRitual_RitualNotFound_ReturnsFalse()
    {
        // Arrange
        var site = CreateTestSite();
        var religion = CreateTestReligion();

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockReligionManager.Setup(m => m.GetReligion(TestReligionUID)).Returns(religion);
        _mockRitualLoader.Setup(m => m.GetRitualById(TestRitualId)).Returns((Ritual?)null);

        // Act
        var result = _manager.StartRitual(TestSiteUID, TestRitualId, TestPlayerUID);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Ritual not found", result.Message);
    }

    [Fact]
    public void StartRitual_DomainMismatch_ReturnsFalse()
    {
        // Arrange
        var site = CreateTestSite();
        var religion = CreateTestReligion(DeityDomain.Wild); // Different domain
        var ritual = CreateTestRitual(DeityDomain.Craft); // Craft domain ritual

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockReligionManager.Setup(m => m.GetReligion(TestReligionUID)).Returns(religion);
        _mockRitualLoader.Setup(m => m.GetRitualById(TestRitualId)).Returns(ritual);

        // Act
        var result = _manager.StartRitual(TestSiteUID, TestRitualId, TestPlayerUID);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("domain", result.Message.ToLower());
    }

    [Fact]
    public void StartRitual_TierMismatch_ReturnsFalse()
    {
        // Arrange
        var site = CreateTestSite();
        site.RitualTier = 2; // Site is Tier 2

        var religion = CreateTestReligion();
        var ritual = CreateTestRitual(sourceTier: 1); // Ritual requires Tier 1

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockReligionManager.Setup(m => m.GetReligion(TestReligionUID)).Returns(religion);
        _mockRitualLoader.Setup(m => m.GetRitualById(TestRitualId)).Returns(ritual);

        // Act
        var result = _manager.StartRitual(TestSiteUID, TestRitualId, TestPlayerUID);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Tier", result.Message);
    }

    #endregion

    #region ContributeToRitual Tests

    [Fact]
    public void ContributeToRitual_Success_UpdatesProgress()
    {
        // Arrange
        var site = CreateTestSiteWithActiveRitual();
        var ritual = CreateTestRitual();
        var offering = CreateItemStack("game:ingot-copper", stackSize: 10);

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockRitualLoader.Setup(m => m.GetRitualById(TestRitualId)).Returns(ritual);

        // Act
        var result = _manager.ContributeToRitual(TestSiteUID, offering, TestPlayerUID);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("copper_ingots", result.RequirementId);
        Assert.Equal(10, result.QuantityContributed);
        Assert.Equal(50, result.QuantityRequired);
        Assert.False(result.RequirementCompleted); // Only 10/50
        Assert.False(result.RitualCompleted);
    }

    [Fact]
    public void ContributeToRitual_SiteNotFound_ReturnsFalse()
    {
        // Arrange
        var offering = CreateItemStack("game:ingot-copper");
        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns((HolySiteData?)null);

        // Act
        var result = _manager.ContributeToRitual(TestSiteUID, offering, TestPlayerUID);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Holy site not found", result.Message);
    }

    [Fact]
    public void ContributeToRitual_NoActiveRitual_ReturnsFalse()
    {
        // Arrange
        var site = CreateTestSite(); // No active ritual
        var offering = CreateItemStack("game:ingot-copper");

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);

        // Act
        var result = _manager.ContributeToRitual(TestSiteUID, offering, TestPlayerUID);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No ritual in progress", result.Message);
    }

    [Fact]
    public void ContributeToRitual_RitualDefinitionNotFound_ReturnsFalse()
    {
        // Arrange
        var site = CreateTestSiteWithActiveRitual();
        var offering = CreateItemStack("game:ingot-copper");

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockRitualLoader.Setup(m => m.GetRitualById(TestRitualId)).Returns((Ritual?)null);

        // Act
        var result = _manager.ContributeToRitual(TestSiteUID, offering, TestPlayerUID);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Ritual definition not found", result.Message);
    }

    [Fact]
    public void ContributeToRitual_ItemNotNeeded_ReturnsFalse()
    {
        // Arrange
        var site = CreateTestSiteWithActiveRitual();
        var ritual = CreateTestRitual();
        var offering = CreateItemStack("game:stone-granite"); // Not needed for ritual

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockRitualLoader.Setup(m => m.GetRitualById(TestRitualId)).Returns(ritual);

        // Act
        var result = _manager.ContributeToRitual(TestSiteUID, offering, TestPlayerUID);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not needed", result.Message);
    }

    [Fact]
    public void ContributeToRitual_RequirementAlreadyCompleted_ReturnsFalse()
    {
        // Arrange
        var site = CreateTestSiteWithActiveRitual();
        // Mark copper requirement as complete
        site.ActiveRitual!.Progress["copper_ingots"].QuantityContributed = 50;

        var ritual = CreateTestRitual();
        var offering = CreateItemStack("game:ingot-copper");

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockRitualLoader.Setup(m => m.GetRitualById(TestRitualId)).Returns(ritual);

        // Act
        var result = _manager.ContributeToRitual(TestSiteUID, offering, TestPlayerUID);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("already completed", result.Message);
    }

    [Fact]
    public void ContributeToRitual_PartialStack_ContributesOnlyNeeded()
    {
        // Arrange
        var site = CreateTestSiteWithActiveRitual();
        // Set progress to 45/50 (only 5 more needed)
        site.ActiveRitual!.Progress["copper_ingots"].QuantityContributed = 45;

        var ritual = CreateTestRitual();
        var offering = CreateItemStack("game:ingot-copper", stackSize: 20); // Offering 20, but only 5 needed

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockRitualLoader.Setup(m => m.GetRitualById(TestRitualId)).Returns(ritual);

        // Act
        var result = _manager.ContributeToRitual(TestSiteUID, offering, TestPlayerUID);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(50, result.QuantityContributed); // Should be capped at 50
        Assert.True(result.RequirementCompleted);
    }

    [Fact]
    public void ContributeToRitual_MultipleContributors_TracksIndividually()
    {
        // Arrange
        var site = CreateTestSiteWithActiveRitual();
        var ritual = CreateTestRitual();

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockRitualLoader.Setup(m => m.GetRitualById(TestRitualId)).Returns(ritual);

        // Act - First player contributes
        var offering1 = CreateItemStack("game:ingot-copper", stackSize: 20);
        var result1 = _manager.ContributeToRitual(TestSiteUID, offering1, "player1");

        // Act - Second player contributes
        var offering2 = CreateItemStack("game:ingot-copper", stackSize: 15);
        var result2 = _manager.ContributeToRitual(TestSiteUID, offering2, "player2");

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(35, result2.QuantityContributed); // Total

        var progress = site.ActiveRitual!.Progress["copper_ingots"];
        Assert.Equal(2, progress.Contributors.Count);
        Assert.Equal(20, progress.Contributors["player1"]);
        Assert.Equal(15, progress.Contributors["player2"]);
    }

    [Fact]
    public void ContributeToRitual_CompletesRitual_UpgradesTier()
    {
        // Arrange
        var site = CreateTestSiteWithActiveRitual();
        // Set first requirement to complete
        site.ActiveRitual!.Progress["copper_ingots"].QuantityContributed = 50;

        var ritual = CreateTestRitual();
        var offering = CreateItemStack("game:ingot-bronze", stackSize: 25); // Complete second requirement

        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);
        _mockRitualLoader.Setup(m => m.GetRitualById(TestRitualId)).Returns(ritual);

        // Act
        var result = _manager.ContributeToRitual(TestSiteUID, offering, TestPlayerUID);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.RitualCompleted);
        Assert.Equal(2, site.RitualTier); // Upgraded to Tier 2
        Assert.Null(site.ActiveRitual); // Ritual cleared
    }

    #endregion

    #region CancelRitual Tests

    [Fact]
    public void CancelRitual_Success_ClearsRitual()
    {
        // Arrange
        var site = CreateTestSiteWithActiveRitual();
        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);

        // Act
        var result = _manager.CancelRitual(TestSiteUID, TestPlayerUID);

        // Assert
        Assert.True(result);
        Assert.Null(site.ActiveRitual);
    }

    [Fact]
    public void CancelRitual_SiteNotFound_ReturnsFalse()
    {
        // Arrange
        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns((HolySiteData?)null);

        // Act
        var result = _manager.CancelRitual(TestSiteUID, TestPlayerUID);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CancelRitual_NoActiveRitual_ReturnsFalse()
    {
        // Arrange
        var site = CreateTestSite(); // No active ritual
        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);

        // Act
        var result = _manager.CancelRitual(TestSiteUID, TestPlayerUID);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CancelRitual_NotConsecrator_ReturnsFalse()
    {
        // Arrange
        var site = CreateTestSiteWithActiveRitual();
        _mockHolySiteManager.Setup(m => m.GetHolySite(TestSiteUID)).Returns(site);

        // Act - Different player trying to cancel
        var result = _manager.CancelRitual(TestSiteUID, "different_player");

        // Assert
        Assert.False(result);
        Assert.NotNull(site.ActiveRitual); // Ritual should still be active
    }

    #endregion

    #region Helper Methods

    private static HolySiteData CreateTestSite()
    {
        return new HolySiteData(
            TestSiteUID,
            TestReligionUID,
            "Test Holy Site",
            new List<SerializableCuboidi>
            {
                new SerializableCuboidi(0, 0, 0, 10, 10, 10)
            },
            TestPlayerUID,
            "Test Player")
        {
            RitualTier = 1
        };
    }

    private static HolySiteData CreateTestSiteWithActiveRitual()
    {
        var site = CreateTestSite();
        site.ActiveRitual = new RitualProgressData
        {
            RitualId = TestRitualId,
            StartedAt = DateTime.UtcNow,
            Progress = new Dictionary<string, ItemProgress>
            {
                ["copper_ingots"] = new ItemProgress
                {
                    QuantityContributed = 0,
                    QuantityRequired = 50,
                    Contributors = new Dictionary<string, int>()
                },
                ["bronze_ingots"] = new ItemProgress
                {
                    QuantityContributed = 0,
                    QuantityRequired = 25,
                    Contributors = new Dictionary<string, int>()
                }
            }
        };
        return site;
    }

    private static ReligionData CreateTestReligion(DeityDomain domain = DeityDomain.Craft)
    {
        return new ReligionData(
            TestReligionUID,
            "Test Religion",
            domain,
            "Aethra",
            TestPlayerUID,
            "Test Player");
    }

    private static Ritual CreateTestRitual(DeityDomain domain = DeityDomain.Craft, int sourceTier = 1)
    {
        return new Ritual(
            RitualId: TestRitualId,
            Name: "Test Ritual",
            Domain: domain,
            SourceTier: sourceTier,
            TargetTier: 2,
            Requirements: new List<RitualRequirement>
            {
                new RitualRequirement(
                    RequirementId: "copper_ingots",
                    DisplayName: "Copper Ingots",
                    Quantity: 50,
                    Type: RequirementType.Exact,
                    ItemCodes: new[] { "game:ingot-copper" }),
                new RitualRequirement(
                    RequirementId: "bronze_ingots",
                    DisplayName: "Bronze Ingots",
                    Quantity: 25,
                    Type: RequirementType.Exact,
                    ItemCodes: new[] { "game:ingot-bronze" })
            },
            Description: "Test ritual description");
    }

    private static ItemStack CreateItemStack(string code, int stackSize = 1)
    {
        var parts = code.Split(':');
        var domain = parts.Length > 1 ? parts[0] : "game";
        var path = parts.Length > 1 ? parts[1] : parts[0];

        var item = new Item
        {
            Code = new AssetLocation(domain, path)
        };
        return new ItemStack(item, stackSize);
    }

    #endregion
}
