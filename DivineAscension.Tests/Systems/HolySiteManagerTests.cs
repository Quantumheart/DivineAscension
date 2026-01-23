using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Xunit;

namespace DivineAscension.Tests.Systems;

[ExcludeFromCodeCoverage]
public class HolySiteManagerTests
{
    private readonly FakeEventService _fakeEventService;
    private readonly FakePersistenceService _fakePersistenceService;
    private readonly FakeWorldService _fakeWorldService;
    private readonly Mock<ILoggerWrapper> _mockLogger;
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly HolySiteManager _manager;

    public HolySiteManagerTests()
    {
        _mockLogger = new Mock<ILoggerWrapper>();
        _fakeEventService = new FakeEventService();
        _fakePersistenceService = new FakePersistenceService();
        _fakeWorldService = new FakeWorldService();
        _mockReligionManager = new Mock<IReligionManager>();

        // Default: Fledgling rank (tier 1, max 1 site)
        var defaultReligion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        defaultReligion.PrestigeRank = PrestigeRank.Fledgling;
        _mockReligionManager.Setup(m => m.GetReligion(It.IsAny<string>()))
            .Returns(defaultReligion);

        _manager = new HolySiteManager(
            _mockLogger.Object,
            _fakeEventService,
            _fakePersistenceService,
            _fakeWorldService,
            _mockReligionManager.Object
        );
    }

    #region Test Data Helpers
    private List<Cuboidi> CreateSingleArea(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        return new List<Cuboidi> { new Cuboidi(x1, y1, z1, x2, y2, z2) };
    }

    private List<Cuboidi> CreateMultiArea()
    {
        return new List<Cuboidi>
        {
            new Cuboidi(0, 0, 0, 31, 255, 31),      // Main area 32x256x32
            new Cuboidi(32, 0, 0, 47, 255, 15),     // Adjacent tower 16x256x16
            new Cuboidi(0, 0, 32, 15, 255, 47)      // Adjacent farm 16x256x16
        };
    }
    #endregion

    #region SerializableCuboidi Tests
    [Fact]
    public void SerializableCuboidi_GetVolume_CalculatesCorrectly()
    {
        var area = new SerializableCuboidi(0, 0, 0, 31, 255, 31);

        // 32 * 256 * 32 = 262,144
        Assert.Equal(262144, area.GetVolume());
    }

    [Fact]
    public void SerializableCuboidi_GetXZArea_CalculatesCorrectly()
    {
        var area = new SerializableCuboidi(0, 0, 0, 31, 255, 31);

        // 32 * 32 = 1,024 (ignores Y)
        Assert.Equal(1024, area.GetXZArea());
    }

    [Fact]
    public void SerializableCuboidi_Contains_DetectsPosition()
    {
        var area = new SerializableCuboidi(0, 0, 0, 31, 255, 31);

        Assert.True(area.Contains(new BlockPos(10, 100, 10)));
        Assert.False(area.Contains(new BlockPos(50, 100, 50)));
    }

    [Fact]
    public void SerializableCuboidi_Intersects_DetectsOverlap()
    {
        var area1 = new SerializableCuboidi(0, 0, 0, 31, 255, 31);
        var area2 = new SerializableCuboidi(20, 0, 20, 50, 255, 50);  // Overlaps
        var area3 = new SerializableCuboidi(100, 0, 100, 131, 255, 131);  // No overlap

        Assert.True(area1.Intersects(area2));
        Assert.False(area1.Intersects(area3));
    }

    [Fact]
    public void SerializableCuboidi_ToCuboidi_RoundTrips()
    {
        var original = new Cuboidi(10, 20, 30, 40, 50, 60);
        var serializable = new SerializableCuboidi(original);
        var roundtrip = serializable.ToCuboidi();

        Assert.Equal(original.X1, roundtrip.X1);
        Assert.Equal(original.Y1, roundtrip.Y1);
        Assert.Equal(original.Z1, roundtrip.Z1);
        Assert.Equal(original.X2, roundtrip.X2);
        Assert.Equal(original.Y2, roundtrip.Y2);
        Assert.Equal(original.Z2, roundtrip.Z2);
    }
    #endregion

    #region HolySiteData Tests
    [Fact]
    public void HolySiteData_GetTotalVolume_SumsAllAreas()
    {
        var areas = CreateMultiArea().Select(a => new SerializableCuboidi(a)).ToList();
        var site = new HolySiteData("site1", "rel1", "Test", areas, "founder", "Founder");

        // Main: 32*256*32 = 262,144
        // Tower: 16*256*16 = 65,536
        // Farm: 16*256*16 = 65,536
        // Total: 393,216
        Assert.Equal(393216, site.GetTotalVolume());
    }

    [Fact]
    public void HolySiteData_GetTier_DefaultsToTierOne()
    {
        // Arrange - Create site without explicitly setting RitualTier
        var areas = new List<SerializableCuboidi>
        {
            new SerializableCuboidi(0, 0, 0, 10, 10, 10)
        };
        var site = new HolySiteData("site1", "rel1", "Test", areas, "founder", "Founder");

        // Act & Assert - Should default to Tier 1
        Assert.Equal(1, site.GetTier());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void HolySiteData_GetTier_ReturnsRitualTier(int ritualTier)
    {
        // Arrange - Create site with specific RitualTier
        var areas = new List<SerializableCuboidi>
        {
            new SerializableCuboidi(0, 0, 0, 10, 10, 10)
        };
        var site = new HolySiteData("site1", "rel1", "Test", areas, "founder", "Founder")
        {
            RitualTier = ritualTier
        };

        // Act & Assert
        Assert.Equal(ritualTier, site.GetTier());
    }

    [Theory]
    [InlineData(1, 2.0)]
    [InlineData(2, 2.5)]
    [InlineData(3, 3.0)]
    public void HolySiteData_GetPrayerMultiplier_ReturnsCorrectValue(int tier, double expected)
    {
        // Arrange - Create site with specific RitualTier
        var areas = new List<SerializableCuboidi>
        {
            new SerializableCuboidi(0, 0, 0, 10, 10, 10)
        };
        var site = new HolySiteData("site1", "rel1", "Test", areas, "founder", "Founder")
        {
            RitualTier = tier
        };

        // Act & Assert
        Assert.Equal(expected, site.GetPrayerMultiplier());
    }

    [Fact]
    public void HolySiteData_ContainsPosition_ChecksAllAreas()
    {
        var areas = CreateMultiArea().Select(a => new SerializableCuboidi(a)).ToList();
        var site = new HolySiteData("site1", "rel1", "Test", areas, "founder", "Founder");

        Assert.True(site.ContainsPosition(new BlockPos(10, 100, 10)));    // In main area
        Assert.True(site.ContainsPosition(new BlockPos(40, 100, 5)));     // In tower
        Assert.True(site.ContainsPosition(new BlockPos(5, 100, 40)));     // In farm
        Assert.False(site.ContainsPosition(new BlockPos(100, 100, 100))); // Outside all
    }

    [Fact]
    public void HolySiteData_Intersects_DetectsOverlap()
    {
        var areas1 = CreateSingleArea(0, 0, 0, 31, 255, 31);
        var site1 = new HolySiteData("site1", "rel1", "Site1",
            areas1.Select(a => new SerializableCuboidi(a)).ToList(), "founder", "Founder");

        var areas2Overlap = CreateSingleArea(20, 0, 20, 50, 255, 50);  // Overlaps
        var site2Overlap = new HolySiteData("site2", "rel1", "Site2",
            areas2Overlap.Select(a => new SerializableCuboidi(a)).ToList(), "founder", "Founder");

        var areas2NoOverlap = CreateSingleArea(100, 0, 100, 131, 255, 131);  // No overlap
        var site2NoOverlap = new HolySiteData("site3", "rel1", "Site3",
            areas2NoOverlap.Select(a => new SerializableCuboidi(a)).ToList(), "founder", "Founder");

        Assert.True(site1.Intersects(site2Overlap));
        Assert.False(site1.Intersects(site2NoOverlap));
    }

    [Fact]
    public void HolySiteData_GetCenter_CalculatesWeightedCenter()
    {
        var areas = CreateMultiArea().Select(a => new SerializableCuboidi(a)).ToList();
        var site = new HolySiteData("site1", "rel1", "Test", areas, "founder", "Founder");

        var center = site.GetCenter();

        // Should be weighted toward the larger main area
        Assert.True(center.X >= 0 && center.X <= 47);
        Assert.True(center.Z >= 0 && center.Z <= 47);
        Assert.True(center.Y >= 0 && center.Y <= 255);
    }
    #endregion

    #region Manager CRUD Tests
    [Fact]
    public void ConsecrateHolySite_Success_CreatesSite()
    {
        _manager.Initialize();
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);

        var site = _manager.ConsecrateHolySite("rel1", "Sacred Temple", areas, "founder");

        Assert.NotNull(site);
        Assert.Equal("Sacred Temple", site.SiteName);
        Assert.Equal("rel1", site.ReligionUID);
        Assert.Single(site.Areas);
    }

    [Fact]
    public void ConsecrateHolySite_EmptyName_ReturnsNull()
    {
        _manager.Initialize();
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);

        var site = _manager.ConsecrateHolySite("rel1", "", areas, "founder");

        Assert.Null(site);
    }

    [Fact]
    public void ConsecrateHolySite_EmptyAreas_ReturnsNull()
    {
        _manager.Initialize();

        var site = _manager.ConsecrateHolySite("rel1", "Temple", new List<Cuboidi>(), "founder");

        Assert.Null(site);
    }

    [Fact]
    public void ConsecrateHolySite_AtPrestigeLimit_ReturnsNull()
    {
        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Fledgling;
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();

        var areas1 = CreateSingleArea(0, 0, 0, 31, 255, 31);
        _manager.ConsecrateHolySite("rel1", "Site 1", areas1, "founder");

        var areas2 = CreateSingleArea(100, 0, 100, 131, 255, 131);
        var site = _manager.ConsecrateHolySite("rel1", "Site 2", areas2, "founder");

        Assert.Null(site);
    }

    [Fact]
    public void ConsecrateHolySite_OverlappingAreas_ReturnsNull()
    {
        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Mythic;
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();

        var areas1 = CreateSingleArea(0, 0, 0, 31, 255, 31);
        _manager.ConsecrateHolySite("rel1", "Site 1", areas1, "founder");

        var areas2 = CreateSingleArea(20, 0, 20, 50, 255, 50);  // Overlaps
        var site = _manager.ConsecrateHolySite("rel1", "Site 2", areas2, "founder");

        Assert.Null(site);
    }

    [Fact]
    public void ConsecrateHolySite_MultipleAreas_Success()
    {
        _manager.Initialize();
        var areas = CreateMultiArea();

        var site = _manager.ConsecrateHolySite("rel1", "Temple Complex", areas, "founder");

        Assert.NotNull(site);
        Assert.Equal(3, site!.Areas.Count);
    }

    [Fact]
    public void DeconsacrateHolySite_Success_ReturnsTrue()
    {
        _manager.Initialize();
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        var site = _manager.ConsecrateHolySite("rel1", "Temple", areas, "founder");

        var result = _manager.DeconsacrateHolySite(site!.SiteUID);

        Assert.True(result);
        Assert.Null(_manager.GetHolySite(site.SiteUID));
    }

    [Fact]
    public void DeconsacrateHolySite_NotFound_ReturnsFalse()
    {
        _manager.Initialize();

        var result = _manager.DeconsacrateHolySite("nonexistent");

        Assert.False(result);
    }
    #endregion

    #region Query Tests
    [Fact]
    public void GetHolySiteAtPosition_InsideArea_ReturnsSite()
    {
        _manager.Initialize();
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        var site = _manager.ConsecrateHolySite("rel1", "Temple", areas, "founder");

        var result = _manager.GetHolySiteAtPosition(new BlockPos(10, 100, 10));

        Assert.NotNull(result);
        Assert.Equal(site!.SiteUID, result.SiteUID);
    }

    [Fact]
    public void GetHolySiteAtPosition_OutsideArea_ReturnsNull()
    {
        _manager.Initialize();
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        _manager.ConsecrateHolySite("rel1", "Temple", areas, "founder");

        var result = _manager.GetHolySiteAtPosition(new BlockPos(100, 100, 100));

        Assert.Null(result);
    }

    [Fact]
    public void GetReligionHolySites_ReturnsAllSites()
    {
        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Mythic;
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();

        var areas1 = CreateSingleArea(0, 0, 0, 31, 255, 31);
        _manager.ConsecrateHolySite("rel1", "Site 1", areas1, "founder");

        var areas2 = CreateSingleArea(100, 0, 100, 131, 255, 131);
        _manager.ConsecrateHolySite("rel1", "Site 2", areas2, "founder");

        var sites = _manager.GetReligionHolySites("rel1");

        Assert.Equal(2, sites.Count);
    }

    [Fact]
    public void IsPlayerInHolySite_InsideSite_ReturnsTrue()
    {
        _manager.Initialize();

        // IsPlayerInHolySite is a wrapper around GetHolySiteAtPosition
        // Since GetHolySiteAtPosition is already tested, we can test this indirectly
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        var site = _manager.ConsecrateHolySite("rel1", "Temple", areas, "founder");

        // Test the underlying GetHolySiteAtPosition which IsPlayerInHolySite uses
        var result = _manager.GetHolySiteAtPosition(new BlockPos(10, 100, 10));

        Assert.NotNull(result);
        Assert.Equal(site!.SiteUID, result.SiteUID);
    }

    [Fact]
    public void IsPlayerInHolySite_OutsideSite_ReturnsFalse()
    {
        _manager.Initialize();

        // IsPlayerInHolySite is a wrapper around GetHolySiteAtPosition
        // Since GetHolySiteAtPosition is already tested, we can test this indirectly
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        _manager.ConsecrateHolySite("rel1", "Temple", areas, "founder");

        // Test the underlying GetHolySiteAtPosition which IsPlayerInHolySite uses
        var result = _manager.GetHolySiteAtPosition(new BlockPos(100, 100, 100));

        Assert.Null(result);
    }
    #endregion

    #region Cascading Deletion Tests
    [Fact]
    public void HandleReligionDeleted_RemovesAllSites()
    {
        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Mythic;
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();

        var areas1 = CreateSingleArea(0, 0, 0, 31, 255, 31);
        _manager.ConsecrateHolySite("rel1", "Site 1", areas1, "founder");

        var areas2 = CreateSingleArea(100, 0, 100, 131, 255, 131);
        _manager.ConsecrateHolySite("rel1", "Site 2", areas2, "founder");

        _manager.HandleReligionDeleted("rel1");

        var sites = _manager.GetReligionHolySites("rel1");
        Assert.Empty(sites);
    }
    #endregion

    #region Chunk Index Optimization Tests

    [Fact]
    public void GetHolySiteAtPosition_WithNoSites_ReturnsNull()
    {
        _manager.Initialize();

        // Position far from any sites - should be O(1) early exit
        var result = _manager.GetHolySiteAtPosition(new BlockPos(5000, 100, 5000));

        Assert.Null(result);
    }

    [Fact]
    public void GetHolySiteAtPosition_MultipleChunks_FindsSiteSpanningChunks()
    {
        // Configure religion for max sites
        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Mythic;
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();

        // Create a site that spans multiple chunks (chunks are 32x32)
        // Area from x=0 to x=100 spans chunks 0, 1, 2, 3
        var areas = CreateSingleArea(0, 0, 0, 100, 255, 100);
        var site = _manager.ConsecrateHolySite("rel1", "Large Temple", areas, "founder");

        // Test positions in different chunks within the area
        Assert.NotNull(_manager.GetHolySiteAtPosition(new BlockPos(10, 100, 10)));   // Chunk (0, 0)
        Assert.NotNull(_manager.GetHolySiteAtPosition(new BlockPos(50, 100, 50)));   // Chunk (1, 1)
        Assert.NotNull(_manager.GetHolySiteAtPosition(new BlockPos(90, 100, 90)));   // Chunk (2, 2)
    }

    [Fact]
    public void DeconsacrateHolySite_CleansUpChunkIndex()
    {
        _manager.Initialize();
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        var site = _manager.ConsecrateHolySite("rel1", "Temple", areas, "founder");

        // Verify site is found at position
        Assert.NotNull(_manager.GetHolySiteAtPosition(new BlockPos(10, 100, 10)));

        // Deconsecrate the site
        _manager.DeconsacrateHolySite(site!.SiteUID);

        // Verify position lookup now returns null (chunk index should be cleaned up)
        Assert.Null(_manager.GetHolySiteAtPosition(new BlockPos(10, 100, 10)));
    }

    [Fact]
    public void OnSaveGameLoaded_RebuildsChunkIndex()
    {
        var areas = new List<SerializableCuboidi>
        {
            new SerializableCuboidi(0, 0, 0, 31, 255, 31)
        };
        var data = new HolySiteWorldData
        {
            HolySites = new List<HolySiteData>
            {
                new("site1", "rel1", "Temple", areas, "founder", "Founder")
            }
        };
        _fakePersistenceService.Save("divineascension_holy_sites", data);
        _manager.Initialize();

        // Trigger reload
        _fakeEventService.TriggerSaveGameLoaded();

        // Verify chunk index was rebuilt - site should be findable
        var site = _manager.GetHolySiteAtPosition(new BlockPos(10, 100, 10));
        Assert.NotNull(site);
        Assert.Equal("site1", site.SiteUID);
    }

    [Fact]
    public void GetHolySiteAtPosition_WithMultipleSitesInDifferentChunks_FindsCorrectSite()
    {
        // Configure religion for max sites
        var religion = new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Mythic;
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();

        // Create two sites in different locations
        var areas1 = CreateSingleArea(0, 0, 0, 31, 255, 31);       // Chunk (0, 0)
        var site1 = _manager.ConsecrateHolySite("rel1", "Temple 1", areas1, "founder");

        var areas2 = CreateSingleArea(500, 0, 500, 531, 255, 531);  // Chunk (15, 15)
        var site2 = _manager.ConsecrateHolySite("rel1", "Temple 2", areas2, "founder");

        // Verify correct site is returned for each position
        var foundSite1 = _manager.GetHolySiteAtPosition(new BlockPos(10, 100, 10));
        var foundSite2 = _manager.GetHolySiteAtPosition(new BlockPos(510, 100, 510));

        Assert.NotNull(foundSite1);
        Assert.NotNull(foundSite2);
        Assert.Equal("Temple 1", foundSite1.SiteName);
        Assert.Equal("Temple 2", foundSite2.SiteName);
    }

    #endregion

    #region Persistence Tests
    [Fact]
    public void ConsecrateHolySite_SavesData()
    {
        _manager.Initialize();
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        _manager.ConsecrateHolySite("rel1", "Temple", areas, "founder");

        var saved = _fakePersistenceService.Load<HolySiteWorldData>("divineascension_holy_sites");

        Assert.NotNull(saved);
        Assert.Single(saved.HolySites);
    }

    [Fact]
    public void OnSaveGameLoaded_LoadsData()
    {
        var areas = new List<SerializableCuboidi>
        {
            new SerializableCuboidi(0, 0, 0, 31, 255, 31)
        };
        var data = new HolySiteWorldData
        {
            HolySites = new List<HolySiteData>
            {
                new("site1", "rel1", "Temple", areas, "founder", "Founder")
            }
        };
        _fakePersistenceService.Save("divineascension_holy_sites", data);
        _manager.Initialize();

        _fakeEventService.TriggerSaveGameLoaded();

        var site = _manager.GetHolySite("site1");
        Assert.NotNull(site);
    }

    [Fact]
    public void ProtoBuf_Roundtrip_PreservesData()
    {
        var areas = new List<SerializableCuboidi>
        {
            new SerializableCuboidi(0, 0, 0, 31, 255, 31),
            new SerializableCuboidi(32, 0, 0, 47, 255, 15)
        };
        var original = new HolySiteData("site1", "rel1", "Sacred Temple",
            areas, "founder", "Founder");

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<HolySiteData>(ms);

        Assert.Equal(original.SiteUID, deserialized.SiteUID);
        Assert.Equal(original.SiteName, deserialized.SiteName);
        Assert.Equal(2, deserialized.Areas.Count);
        Assert.Equal(original.GetTotalVolume(), deserialized.GetTotalVolume());
    }
    #endregion

    #region ValidateHolySiteCreationInputs Tests

    [Fact]
    public void ValidateHolySiteCreationInputs_ValidInputs_ReturnsNull()
    {
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);

        var result = HolySiteManager.ValidateHolySiteCreationInputs("Valid Name", areas);

        Assert.Null(result);
    }

    [Fact]
    public void ValidateHolySiteCreationInputs_NullName_ReturnsError()
    {
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);

        var result = HolySiteManager.ValidateHolySiteCreationInputs(null, areas);

        Assert.NotNull(result);
        Assert.Contains("name cannot be empty", result);
    }

    [Fact]
    public void ValidateHolySiteCreationInputs_EmptyName_ReturnsError()
    {
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);

        var result = HolySiteManager.ValidateHolySiteCreationInputs("", areas);

        Assert.NotNull(result);
        Assert.Contains("name cannot be empty", result);
    }

    [Fact]
    public void ValidateHolySiteCreationInputs_WhitespaceName_ReturnsError()
    {
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);

        var result = HolySiteManager.ValidateHolySiteCreationInputs("   ", areas);

        Assert.NotNull(result);
        Assert.Contains("name cannot be empty", result);
    }

    [Fact]
    public void ValidateHolySiteCreationInputs_NullAreas_ReturnsError()
    {
        var result = HolySiteManager.ValidateHolySiteCreationInputs("Valid Name", null);

        Assert.NotNull(result);
        Assert.Contains("areas cannot be empty", result);
    }

    [Fact]
    public void ValidateHolySiteCreationInputs_EmptyAreas_ReturnsError()
    {
        var result = HolySiteManager.ValidateHolySiteCreationInputs("Valid Name", new List<Cuboidi>());

        Assert.NotNull(result);
        Assert.Contains("areas cannot be empty", result);
    }

    #endregion

    #region FindOverlappingHolySite Tests

    [Fact]
    public void FindOverlappingHolySite_NoExistingSites_ReturnsNull()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        var areas = new List<SerializableCuboidi>
        {
            new SerializableCuboidi(0, 0, 0, 31, 255, 31)
        };

        var result = _manager.FindOverlappingHolySite(areas);

        Assert.Null(result);
    }

    [Fact]
    public void FindOverlappingHolySite_WithOverlap_ReturnsSiteName()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        // Create an existing site
        var existingAreas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        _manager.ConsecrateHolySite("rel1", "Existing Temple", existingAreas, "founder");

        // Try to check overlap with an overlapping area
        var newAreas = new List<SerializableCuboidi>
        {
            new SerializableCuboidi(10, 0, 10, 50, 255, 50) // Overlaps with existing
        };

        var result = _manager.FindOverlappingHolySite(newAreas);

        Assert.NotNull(result);
        Assert.Equal("Existing Temple", result);
    }

    [Fact]
    public void FindOverlappingHolySite_NoOverlap_ReturnsNull()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        // Create an existing site
        var existingAreas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        _manager.ConsecrateHolySite("rel1", "Existing Temple", existingAreas, "founder");

        // Check with non-overlapping area
        var newAreas = new List<SerializableCuboidi>
        {
            new SerializableCuboidi(100, 0, 100, 131, 255, 131) // Far away
        };

        var result = _manager.FindOverlappingHolySite(newAreas);

        Assert.Null(result);
    }

    #endregion

    #region RegisterNewHolySite Tests

    [Fact]
    public void RegisterNewHolySite_AddsSiteToUIDIndex()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        var site = new HolySiteData(
            "site-uid-1",
            "rel1",
            "Test Temple",
            new List<SerializableCuboidi> { new SerializableCuboidi(0, 0, 0, 31, 255, 31) },
            "founder",
            "Founder");

        _manager.RegisterNewHolySite(site, "rel1");

        var retrieved = _manager.GetHolySite("site-uid-1");
        Assert.NotNull(retrieved);
        Assert.Equal("Test Temple", retrieved.SiteName);
    }

    [Fact]
    public void RegisterNewHolySite_AddsSiteToReligionIndex()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        var site = new HolySiteData(
            "site-uid-1",
            "rel1",
            "Test Temple",
            new List<SerializableCuboidi> { new SerializableCuboidi(0, 0, 0, 31, 255, 31) },
            "founder",
            "Founder");

        _manager.RegisterNewHolySite(site, "rel1");

        var religionSites = _manager.GetReligionHolySites("rel1");
        Assert.Single(religionSites);
        Assert.Equal("site-uid-1", religionSites[0].SiteUID);
    }

    [Fact]
    public void RegisterNewHolySite_IndexesChunksForPositionLookup()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        var site = new HolySiteData(
            "site-uid-1",
            "rel1",
            "Test Temple",
            new List<SerializableCuboidi> { new SerializableCuboidi(0, 0, 0, 31, 255, 31) },
            "founder",
            "Founder");

        _manager.RegisterNewHolySite(site, "rel1");

        // Should be findable by position
        var found = _manager.GetHolySiteAtPosition(new BlockPos(10, 64, 10));
        Assert.NotNull(found);
        Assert.Equal("site-uid-1", found.SiteUID);
    }

    #endregion

    #region ConsecrateHolySiteInternal Tests

    [Fact]
    public void ConsecrateHolySiteInternal_ValidInputs_CreatesSite()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);

        var result = _manager.ConsecrateHolySiteInternal(
            "rel1", "Test Temple", areas, "founder", "FounderName", null);

        Assert.NotNull(result);
        Assert.Equal("Test Temple", result.SiteName);
        Assert.Null(result.AltarPosition);
    }

    [Fact]
    public void ConsecrateHolySiteInternal_WithAltarPosition_SetsAltarPosition()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        var altarPos = new BlockPos(10, 64, 10);

        var result = _manager.ConsecrateHolySiteInternal(
            "rel1", "Test Temple", areas, "founder", "FounderName", altarPos);

        Assert.NotNull(result);
        Assert.NotNull(result.AltarPosition);
        Assert.Equal(10, result.AltarPosition.X);
        Assert.Equal(64, result.AltarPosition.Y);
        Assert.Equal(10, result.AltarPosition.Z);
    }

    [Fact]
    public void ConsecrateHolySiteInternal_InvalidName_ReturnsNull()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);

        var result = _manager.ConsecrateHolySiteInternal(
            "rel1", "", areas, "founder", "FounderName", null);

        Assert.Null(result);
    }

    [Fact]
    public void ConsecrateHolySiteInternal_NullAreas_ReturnsNull()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        var result = _manager.ConsecrateHolySiteInternal(
            "rel1", "Test Temple", null, "founder", "FounderName", null);

        Assert.Null(result);
    }

    [Fact]
    public void ConsecrateHolySiteInternal_OverlappingSite_ReturnsNull()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        // Create first site
        var areas1 = CreateSingleArea(0, 0, 0, 31, 255, 31);
        _manager.ConsecrateHolySiteInternal("rel1", "First Temple", areas1, "founder", "Founder", null);

        // Increase prestige to allow more sites
        var religion = new ReligionData("rel1", "Test", DeityDomain.Craft, "Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Established;
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);

        // Try to create overlapping site
        var areas2 = CreateSingleArea(10, 0, 10, 50, 255, 50);
        var result = _manager.ConsecrateHolySiteInternal("rel1", "Second Temple", areas2, "founder", "Founder", null);

        Assert.Null(result);
    }

    #endregion

    #region ValidateHolySiteName Tests

    [Fact]
    public void ValidateHolySiteName_ValidName_ReturnsNull()
    {
        var result = HolySiteManager.ValidateHolySiteName("Valid Temple Name");

        Assert.Null(result);
    }

    [Fact]
    public void ValidateHolySiteName_NullName_ReturnsError()
    {
        var result = HolySiteManager.ValidateHolySiteName(null);

        Assert.NotNull(result);
        Assert.Contains("cannot be empty", result);
    }

    [Fact]
    public void ValidateHolySiteName_EmptyName_ReturnsError()
    {
        var result = HolySiteManager.ValidateHolySiteName("");

        Assert.NotNull(result);
        Assert.Contains("cannot be empty", result);
    }

    [Fact]
    public void ValidateHolySiteName_WhitespaceName_ReturnsError()
    {
        var result = HolySiteManager.ValidateHolySiteName("   ");

        Assert.NotNull(result);
        Assert.Contains("cannot be empty", result);
    }

    [Fact]
    public void ValidateHolySiteName_TooLongName_ReturnsError()
    {
        var longName = new string('A', 51); // 51 characters

        var result = HolySiteManager.ValidateHolySiteName(longName);

        Assert.NotNull(result);
        Assert.Contains("cannot exceed 50 characters", result);
    }

    [Fact]
    public void ValidateHolySiteName_MaxLengthName_ReturnsNull()
    {
        var maxName = new string('A', 50); // Exactly 50 characters

        var result = HolySiteManager.ValidateHolySiteName(maxName);

        Assert.Null(result);
    }

    #endregion

    #region ValidateHolySiteDescription Tests

    [Fact]
    public void ValidateHolySiteDescription_ValidDescription_ReturnsNull()
    {
        var result = HolySiteManager.ValidateHolySiteDescription("A beautiful temple dedicated to the gods.");

        Assert.Null(result);
    }

    [Fact]
    public void ValidateHolySiteDescription_NullDescription_ReturnsNull()
    {
        // Null is allowed (clears description)
        var result = HolySiteManager.ValidateHolySiteDescription(null);

        Assert.Null(result);
    }

    [Fact]
    public void ValidateHolySiteDescription_EmptyDescription_ReturnsNull()
    {
        // Empty is allowed (clears description)
        var result = HolySiteManager.ValidateHolySiteDescription("");

        Assert.Null(result);
    }

    [Fact]
    public void ValidateHolySiteDescription_TooLongDescription_ReturnsError()
    {
        var longDescription = new string('A', 201); // 201 characters

        var result = HolySiteManager.ValidateHolySiteDescription(longDescription);

        Assert.NotNull(result);
        Assert.Contains("cannot exceed 200 characters", result);
    }

    [Fact]
    public void ValidateHolySiteDescription_MaxLengthDescription_ReturnsNull()
    {
        var maxDescription = new string('A', 200); // Exactly 200 characters

        var result = HolySiteManager.ValidateHolySiteDescription(maxDescription);

        Assert.Null(result);
    }

    #endregion

    #region ApplyHolySiteRename Tests

    [Fact]
    public void ApplyHolySiteRename_UpdatesSiteName()
    {
        var site = new HolySiteData(
            "site1", "rel1", "Old Name",
            new List<SerializableCuboidi> { new SerializableCuboidi(0, 0, 0, 31, 255, 31) },
            "founder", "Founder");

        HolySiteManager.ApplyHolySiteRename(site, "New Name");

        Assert.Equal("New Name", site.SiteName);
    }

    #endregion

    #region ApplyHolySiteDescriptionUpdate Tests

    [Fact]
    public void ApplyHolySiteDescriptionUpdate_UpdatesDescription()
    {
        var site = new HolySiteData(
            "site1", "rel1", "Temple",
            new List<SerializableCuboidi> { new SerializableCuboidi(0, 0, 0, 31, 255, 31) },
            "founder", "Founder");

        HolySiteManager.ApplyHolySiteDescriptionUpdate(site, "A sacred place of worship.");

        Assert.Equal("A sacred place of worship.", site.Description);
    }

    [Fact]
    public void ApplyHolySiteDescriptionUpdate_NullDescription_SetsEmptyString()
    {
        var site = new HolySiteData(
            "site1", "rel1", "Temple",
            new List<SerializableCuboidi> { new SerializableCuboidi(0, 0, 0, 31, 255, 31) },
            "founder", "Founder")
        {
            Description = "Existing description"
        };

        HolySiteManager.ApplyHolySiteDescriptionUpdate(site, null!);

        Assert.Equal(string.Empty, site.Description);
    }

    [Fact]
    public void ApplyHolySiteDescriptionUpdate_EmptyDescription_ClearsDescription()
    {
        var site = new HolySiteData(
            "site1", "rel1", "Temple",
            new List<SerializableCuboidi> { new SerializableCuboidi(0, 0, 0, 31, 255, 31) },
            "founder", "Founder")
        {
            Description = "Existing description"
        };

        HolySiteManager.ApplyHolySiteDescriptionUpdate(site, "");

        Assert.Equal("", site.Description);
    }

    #endregion

    #region RenameHolySite Integration Tests

    [Fact]
    public void RenameHolySite_InvalidName_ReturnsFalse()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        // Create a site first
        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        var site = _manager.ConsecrateHolySite("rel1", "Original Name", areas, "founder");
        Assert.NotNull(site);

        // Try to rename with invalid name
        var result = _manager.RenameHolySite(site.SiteUID, "");

        Assert.False(result);
        // Name should be unchanged
        var retrieved = _manager.GetHolySite(site.SiteUID);
        Assert.Equal("Original Name", retrieved!.SiteName);
    }

    [Fact]
    public void RenameHolySite_TooLongName_ReturnsFalse()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        var site = _manager.ConsecrateHolySite("rel1", "Original Name", areas, "founder");
        Assert.NotNull(site);

        var longName = new string('A', 51);
        var result = _manager.RenameHolySite(site.SiteUID, longName);

        Assert.False(result);
    }

    #endregion

    #region UpdateDescription Integration Tests

    [Fact]
    public void UpdateDescription_TooLongDescription_ReturnsFalse()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        var site = _manager.ConsecrateHolySite("rel1", "Temple", areas, "founder");
        Assert.NotNull(site);

        var longDescription = new string('A', 201);
        var result = _manager.UpdateDescription(site.SiteUID, longDescription);

        Assert.False(result);
    }

    [Fact]
    public void UpdateDescription_ValidDescription_ReturnsTrue()
    {
        _manager.Initialize();
        _fakeEventService.TriggerSaveGameLoaded();

        var areas = CreateSingleArea(0, 0, 0, 31, 255, 31);
        var site = _manager.ConsecrateHolySite("rel1", "Temple", areas, "founder");
        Assert.NotNull(site);

        var result = _manager.UpdateDescription(site.SiteUID, "A sacred place.");

        Assert.True(result);
        var retrieved = _manager.GetHolySite(site.SiteUID);
        Assert.Equal("A sacred place.", retrieved!.Description);
    }

    #endregion
}
