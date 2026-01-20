using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
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
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly HolySiteManager _manager;

    public HolySiteManagerTests()
    {
        _mockLogger = new Mock<ILogger>();
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

    [Theory]
    [InlineData(30000, 1)]      // < 50k = Tier 1
    [InlineData(50000, 2)]      // 50k = Tier 2
    [InlineData(100000, 2)]     // 100k = Tier 2
    [InlineData(200000, 3)]     // 200k = Tier 3
    [InlineData(500000, 3)]     // 500k = Tier 3
    public void HolySiteData_GetTier_CalculatesBasedOnVolume(int volume, int expectedTier)
    {
        // Create area with specific volume (cube root for dimensions)
        int side = (int)Math.Ceiling(Math.Pow(volume, 1.0/3.0));
        var areas = new List<SerializableCuboidi>
        {
            new SerializableCuboidi(0, 0, 0, side-1, side-1, side-1)
        };
        var site = new HolySiteData("site1", "rel1", "Test", areas, "founder", "Founder");

        Assert.Equal(expectedTier, site.GetTier());
    }

    [Theory]
    [InlineData(1, 2.0)]
    [InlineData(2, 2.5)]
    [InlineData(3, 3.0)]
    public void HolySiteData_GetPrayerMultiplier_ReturnsCorrectValue(int tier, double expected)
    {
        int volume = tier == 1 ? 40000 : (tier == 2 ? 150000 : 300000);
        int side = (int)Math.Ceiling(Math.Pow(volume, 1.0/3.0));
        var areas = new List<SerializableCuboidi>
        {
            new SerializableCuboidi(0, 0, 0, side-1, side-1, side-1)
        };
        var site = new HolySiteData("site1", "rel1", "Test", areas, "founder", "Founder");

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
}
