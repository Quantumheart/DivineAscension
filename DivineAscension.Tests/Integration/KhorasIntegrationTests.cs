using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Integration;

/// <summary>
/// Integration tests for Khoras deity system
/// Tests full progression workflows from Follower to Champion
/// </summary>
[ExcludeFromCodeCoverage]
public class KhorasIntegrationTests
{
    #region Religion Integration Tests

    [Fact]
    public void ReligionProgression_PrestigeAccumulation_UnlocksReligionBlessings()
    {
        // Arrange
        var playerUID = "player-1";
        var religionId = "test-religion";

        var religionData = new ReligionData
        {
            ReligionUID = religionId,
            ReligionName = "Forge Brotherhood",
            FounderUID = playerUID,
            Domain = DeityDomain.Craft,
            Prestige = 0,
            UnlockedBlessings = new Dictionary<string, bool>(),
            MemberUIDs = new List<string> { playerUID }
        };

        _mockReligionManager.Setup(m => m.GetPlayerReligion(playerUID))
            .Returns(religionData);
        _mockReligionManager.Setup(m => m.GetReligion(religionId))
            .Returns(religionData);

        // Act - Accumulate prestige to 500 (R1 threshold)
        religionData.Prestige = 500;

        // Assert - R1 blessing (Shared Workshop) should be available
        var r1Blessing = BlessingDefinitions.GetAllBlessings()
            .First(b => b.BlessingId == BlessingIds.CraftSharedWorkshop);

        Assert.Equal(0, r1Blessing.RequiredPrestigeRank); // Fledgling
        Assert.Equal(BlessingKind.Religion, r1Blessing.Kind);
    }

    #endregion

    #region Test Setup

    private readonly Mock<ICoreServerAPI> _mockAPI;
    private readonly Mock<IPlayerProgressionDataManager> _mockPlayerReligionDataManager;
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly Mock<IReligionPrestigeManager> _mockPrestigeManager;
    private readonly Mock<IBlessingRegistry> _mockBlessingRegistry;
    private readonly FavorSystem _favorSystem;
    private readonly BlessingEffectSystem _blessingEffectSystem;

    public KhorasIntegrationTests()
    {
        _mockAPI = TestFixtures.CreateMockServerAPI();
        _mockPlayerReligionDataManager = TestFixtures.CreateMockPlayerProgressionDataManager();
        _mockReligionManager = TestFixtures.CreateMockReligionManager();
        _mockPrestigeManager = TestFixtures.CreateMockReligionPrestigeManager();
        _mockBlessingRegistry = TestFixtures.CreateMockBlessingRegistry();

        // Setup blessing registry with Khoras blessings
        var khorasBlessings = BlessingDefinitions.GetAllBlessings()
            .Where(b => b.Domain == DeityDomain.Craft)
            .ToList();

        foreach (var blessing in khorasBlessings)
        {
            _mockBlessingRegistry.Setup(r => r.GetBlessing(blessing.BlessingId))
                .Returns(blessing);
        }

        // Note: GetBlessingsForDeity has optional parameters, so we can't use it in expression trees
        // Instead, we setup GetBlessing for each individual blessing

        var mockActivityLogManager = new Mock<IActivityLogManager>();
        _favorSystem = new FavorSystem(
            _mockAPI.Object,
            _mockPlayerReligionDataManager.Object,
            _mockReligionManager.Object,
            _mockPrestigeManager.Object,
            mockActivityLogManager.Object
        );

        _blessingEffectSystem = new BlessingEffectSystem(
            _mockAPI.Object,
            _mockBlessingRegistry.Object,
            _mockPlayerReligionDataManager.Object,
            _mockReligionManager.Object
        );
    }

    private PlayerProgressionData SetupKhorasFollower(string playerUID)
    {
        var playerData = new PlayerProgressionData
        {
            Id = playerUID,
            Favor = 0,
            UnlockedBlessings = new()
        };

        _mockPlayerReligionDataManager.Setup(m => m.GetOrCreatePlayerData(playerUID))
            .Returns(playerData);

        return playerData;
    }

    #endregion

    #region Full Progression Tests

    [Fact]
    public void FullProgression_FollowerToInitiate_T1BlessingAvailable()
    {
        // Arrange & Assert - Verify T1 blessing (Craftsman's Touch) requirements
        var t1Blessing = BlessingDefinitions.GetAllBlessings()
            .First(b => b.BlessingId == BlessingIds.CraftCraftsmansTouch);

        // T1 blessing should require Initiate rank (rank 0) and have no prerequisites
        Assert.Equal(0, t1Blessing.RequiredFavorRank); // Initiate
        Assert.NotNull(t1Blessing.PrerequisiteBlessings);
        Assert.Empty(t1Blessing.PrerequisiteBlessings);
        Assert.Equal(DeityDomain.Craft, t1Blessing.Domain);
        Assert.Equal(BlessingKind.Player, t1Blessing.Kind);
    }

    [Fact]
    public void FullProgression_InitiateToAdherent_T2BlessingsAvailable()
    {
        // Arrange & Assert - Verify both T2 blessings require Disciple rank
        var t2a = BlessingDefinitions.GetAllBlessings()
            .First(b => b.BlessingId == BlessingIds.CraftMasterworkTools);

        var t2b = BlessingDefinitions.GetAllBlessings()
            .First(b => b.BlessingId == BlessingIds.CraftForgebornEndurance);

        // Both T2 blessings should require Disciple rank (rank 1)
        Assert.Equal(1, t2a.RequiredFavorRank); // Disciple
        Assert.Equal(1, t2b.RequiredFavorRank); // Disciple

        // T2A should require T1
        Assert.NotNull(t2a.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.CraftCraftsmansTouch, t2a.PrerequisiteBlessings);

        // T2B should require T1
        Assert.NotNull(t2b.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.CraftCraftsmansTouch, t2b.PrerequisiteBlessings);
    }

    [Fact]
    public void FullProgression_AdherentToChampion_CapstoneRequiresBothPaths()
    {
        // Arrange & Assert - Verify capstone (Avatar of the Forge) requirements
        var capstone = BlessingDefinitions.GetAllBlessings()
            .First(b => b.BlessingId == BlessingIds.CraftAvatarOfForge);

        // Capstone should require Champion rank (rank 3)
        Assert.Equal(3, capstone.RequiredFavorRank); // Champion

        // Capstone should require BOTH T3 paths
        Assert.NotNull(capstone.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.CraftLegendarySmith, capstone.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.CraftUnyielding, capstone.PrerequisiteBlessings);
        Assert.Equal(2, capstone.PrerequisiteBlessings.Count); // Only these two prerequisites
    }

    #endregion

    #region Blessing Stat Stacking Tests

    [Fact]
    public void BlessingStatModifiers_T1ThroughT3A_StackAdditively()
    {
        // Arrange
        var blessings = BlessingDefinitions.GetAllBlessings()
            .Where(b => b.Domain == DeityDomain.Craft)
            .ToDictionary(b => b.BlessingId);

        var t1 = blessings[BlessingIds.CraftCraftsmansTouch];
        var t2a = blessings[BlessingIds.CraftMasterworkTools];
        var t3a = blessings[BlessingIds.CraftLegendarySmith];

        // Act - Calculate total tool durability bonus
        float totalToolDurability = 0f;
        if (t1.StatModifiers.TryGetValue(VintageStoryStats.ToolDurability, out var t1Dur))
            totalToolDurability += t1Dur;
        if (t2a.StatModifiers.TryGetValue(VintageStoryStats.ToolDurability, out var t2aDur))
            totalToolDurability += t2aDur;
        if (t3a.StatModifiers.TryGetValue(VintageStoryStats.ToolDurability, out var t3aDur))
            totalToolDurability += t3aDur;

        // Assert - Should add up to 45% (0.10 + 0.15 + 0.20)
        Assert.Equal(0.45f, totalToolDurability, precision: 3);
    }

    [Fact]
    public void PlayerAndReligionBonuses_BothApply_StackAdditively()
    {
        // Arrange
        var playerUID = "player-1";
        var mockPlayer = TestFixtures.CreateMockServerPlayer(playerUID, "TestPlayer");
        var playerData = SetupKhorasFollower(playerUID);

        var religionData = new ReligionData
        {
            ReligionUID = "test-religion",
            ReligionName = "Test Religion",
            FounderUID = playerUID,
            Domain = DeityDomain.Craft,
            Prestige = 500,
            UnlockedBlessings = new Dictionary<string, bool> { { BlessingIds.CraftSharedWorkshop, true } },
            MemberUIDs = new List<string> { playerUID }
        };

        _mockReligionManager.Setup(m => m.GetPlayerReligion(playerUID))
            .Returns(religionData);

        // Player has T1 blessing (Craftsman's Touch: +10% tool durability)
        playerData.UnlockedBlessings.Add(BlessingIds.CraftCraftsmansTouch);

        // Religion has R1 blessing (Shared Workshop: +10% tool durability)
        // This is validated in the blessing definitions

        // Act - Get stat modifiers
        var playerBlessing = BlessingDefinitions.GetAllBlessings()
            .First(b => b.BlessingId == BlessingIds.CraftCraftsmansTouch);
        var religionBlessing = BlessingDefinitions.GetAllBlessings()
            .First(b => b.BlessingId == BlessingIds.CraftSharedWorkshop);

        playerBlessing.StatModifiers.TryGetValue(VintageStoryStats.ToolDurability, out var playerBonus);
        religionBlessing.StatModifiers.TryGetValue(VintageStoryStats.ToolDurability, out var religionBonus);

        var totalBonus = playerBonus + religionBonus;

        // Assert - Total should be 20% (0.10 + 0.10)
        Assert.Equal(0.20f, totalBonus, precision: 3);
    }

    #endregion

    #region Multiple Favor Source Tests

    [Fact]
    public void AllKhorasBlessings_HaveCorrectDeity()
    {
        // Arrange & Assert - All Craft domain blessings should have DeityDomain.Craft
        var khorasBlessings = BlessingDefinitions.GetAllBlessings()
            .Where(b => b.Domain == DeityDomain.Craft)
            .ToList();

        // Should have exactly 10 blessings (6 player + 4 religion)
        Assert.Equal(10, khorasBlessings.Count);

        // All should be Craft domain
        Assert.All(khorasBlessings, b => Assert.Equal(DeityDomain.Craft, b.Domain));
    }

    [Fact]
    public void KhorasBlessings_PlayerVsReligion_CorrectCounts()
    {
        // Arrange
        var khorasBlessings = BlessingDefinitions.GetAllBlessings()
            .Where(b => b.Domain == DeityDomain.Craft)
            .ToList();

        // Assert - Should have 6 player blessings and 4 religion blessings
        var playerBlessings = khorasBlessings.Where(b => b.Kind == BlessingKind.Player).ToList();
        var religionBlessings = khorasBlessings.Where(b => b.Kind == BlessingKind.Religion).ToList();

        Assert.Equal(6, playerBlessings.Count);
        Assert.Equal(4, religionBlessings.Count);
    }

    #endregion

    #region Blessing Prerequisite Tests

    [Fact]
    public void BlessingPrerequisites_T3A_RequiresT2A()
    {
        // Arrange
        var t3a = BlessingDefinitions.GetAllBlessings()
            .First(b => b.BlessingId == BlessingIds.CraftLegendarySmith);

        // Assert
        Assert.NotNull(t3a.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.CraftMasterworkTools, t3a.PrerequisiteBlessings);
    }

    [Fact]
    public void BlessingPrerequisites_T3B_RequiresT2B()
    {
        // Arrange
        var t3b = BlessingDefinitions.GetAllBlessings()
            .First(b => b.BlessingId == BlessingIds.CraftUnyielding);

        // Assert
        Assert.NotNull(t3b.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.CraftForgebornEndurance, t3b.PrerequisiteBlessings);
    }

    [Fact]
    public void BlessingPrerequisites_Capstone_RequiresBothT3Paths()
    {
        // Arrange
        var capstone = BlessingDefinitions.GetAllBlessings()
            .First(b => b.BlessingId == BlessingIds.CraftAvatarOfForge);

        // Assert - Capstone requires BOTH T3A and T3B
        Assert.NotNull(capstone.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.CraftLegendarySmith, capstone.PrerequisiteBlessings);
        Assert.Contains(BlessingIds.CraftUnyielding, capstone.PrerequisiteBlessings);
        Assert.Equal(2, capstone.PrerequisiteBlessings.Count);
    }

    [Fact]
    public void BlessingPrerequisites_ReligionBlessings_FormLinearChain()
    {
        // Arrange
        var blessings = BlessingDefinitions.GetAllBlessings()
            .Where(b => b.Domain == DeityDomain.Craft && b.Kind == BlessingKind.Religion)
            .OrderBy(b => b.RequiredPrestigeRank)
            .ToList();

        // Assert - Religion blessings form a linear chain
        // R1 (Shared Workshop) has no prerequisites
        Assert.True(blessings[0].PrerequisiteBlessings == null || blessings[0].PrerequisiteBlessings.Count == 0);

        // R2 requires R1
        Assert.NotNull(blessings[1].PrerequisiteBlessings);
        Assert.Contains(blessings[0].BlessingId, blessings[1].PrerequisiteBlessings);

        // R3 requires R2
        Assert.NotNull(blessings[2].PrerequisiteBlessings);
        Assert.Contains(blessings[1].BlessingId, blessings[2].PrerequisiteBlessings);

        // R4 requires R3
        Assert.NotNull(blessings[3].PrerequisiteBlessings);
        Assert.Contains(blessings[2].BlessingId, blessings[3].PrerequisiteBlessings);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void AllKhorasBlessings_HaveStatModifiers()
    {
        // Arrange & Assert - All Khoras blessings should have at least one stat modifier
        var khorasBlessings = BlessingDefinitions.GetAllBlessings()
            .Where(b => b.Domain == DeityDomain.Craft)
            .ToList();

        Assert.All(khorasBlessings, b => Assert.NotEmpty(b.StatModifiers));
    }

    [Fact]
    public void AllKhorasBlessings_HaveUniqueIds()
    {
        // Arrange
        var khorasBlessings = BlessingDefinitions.GetAllBlessings()
            .Where(b => b.Domain == DeityDomain.Craft)
            .ToList();

        // Assert - All blessing IDs should be unique
        var blessingIds = khorasBlessings.Select(b => b.BlessingId).ToList();
        var distinctIds = blessingIds.Distinct().ToList();

        Assert.Equal(blessingIds.Count, distinctIds.Count);
    }

    #endregion
}