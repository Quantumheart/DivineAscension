using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using ProtoBuf;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Systems;

[ExcludeFromCodeCoverage]
public class HolySiteManagerTests
{
    private readonly FakeEventService _fakeEventService;
    private readonly FakePersistenceService _fakePersistenceService;
    private readonly FakeWorldService _fakeWorldService;
    private readonly HolySiteManager _manager;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IReligionManager> _mockReligionManager;

    public HolySiteManagerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _fakeEventService = new FakeEventService();
        _fakePersistenceService = new FakePersistenceService();
        _fakeWorldService = new FakeWorldService();
        _mockReligionManager = new Mock<IReligionManager>();

        // Default: Fledgling rank (tier 1, max 1 site)
        var defaultReligion =
            new ReligionData("rel1", "Test Religion", DeityDomain.Craft, "Test Deity", "founder", "Founder");
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

    #region Initialization Tests

    [Fact]
    public void Initialize_RegistersEventHandlers()
    {
        _manager.Initialize();

        Assert.Equal(1, _fakeEventService.SaveGameLoadedCallbackCount);
        Assert.Equal(1, _fakeEventService.GameWorldSaveCallbackCount);
    }

    #endregion

    #region Disposal Tests

    [Fact]
    public void Dispose_UnsubscribesEvents()
    {
        _manager.Initialize();
        var initialLoadCount = _fakeEventService.SaveGameLoadedCallbackCount;
        var initialSaveCount = _fakeEventService.GameWorldSaveCallbackCount;

        _manager.Dispose();

        // Callbacks should be removed
        Assert.Equal(0, _fakeEventService.SaveGameLoadedCallbackCount);
        Assert.Equal(0, _fakeEventService.GameWorldSaveCallbackCount);
    }

    #endregion

    #region Data Model Tests

    [Fact]
    public void SerializableChunkPos_ToKey_ReturnsCorrectFormat()
    {
        var chunk = new SerializableChunkPos(10, 20);
        Assert.Equal("10,20", chunk.ToKey());
    }

    [Theory]
    [InlineData(1, 1)] // 1 chunk = tier 1
    [InlineData(2, 2)] // 2 chunks = tier 2
    [InlineData(3, 2)] // 3 chunks = tier 2
    [InlineData(4, 3)] // 4 chunks = tier 3
    [InlineData(6, 3)] // 6 chunks = tier 3
    public void HolySiteData_GetTier_ReturnsCorrectTier(int chunkCount, int expectedTier)
    {
        var site = new HolySiteData("site1", "rel1", "Test", new(0, 0), "founder", "Founder");
        for (int i = 1; i < chunkCount; i++)
            site.AddChunk(new SerializableChunkPos(i, 0));

        Assert.Equal(expectedTier, site.GetTier());
    }

    [Theory]
    [InlineData(1, 1.5)]
    [InlineData(2, 2.0)]
    [InlineData(3, 2.5)]
    public void HolySiteData_GetTerritoryMultiplier_ReturnsCorrectValue(int tier, double expected)
    {
        var site = new HolySiteData("site1", "rel1", "Test", new(0, 0), "founder", "Founder");
        // Add chunks to reach desired tier (tier 1 = 1 chunk, tier 2 = 2 chunks, tier 3 = 4 chunks)
        int chunksNeeded = tier == 1 ? 1 : tier == 2 ? 2 : 4;
        for (int i = 1; i < chunksNeeded; i++)
            site.AddChunk(new SerializableChunkPos(i, 0));

        Assert.Equal(expected, site.GetTerritoryMultiplier());
    }

    [Theory]
    [InlineData(1, 2.0)]
    [InlineData(2, 2.5)]
    [InlineData(3, 3.0)]
    public void HolySiteData_GetPrayerMultiplier_ReturnsCorrectValue(int tier, double expected)
    {
        var site = new HolySiteData("site1", "rel1", "Test", new(0, 0), "founder", "Founder");
        // Add chunks to reach desired tier (tier 1 = 1 chunk, tier 2 = 2 chunks, tier 3 = 4 chunks)
        int chunksNeeded = tier == 1 ? 1 : tier == 2 ? 2 : 4;
        for (int i = 1; i < chunksNeeded; i++)
            site.AddChunk(new SerializableChunkPos(i, 0));

        Assert.Equal(expected, site.GetPrayerMultiplier());
    }

    [Fact]
    public void HolySiteData_ContainsChunk_CenterChunk_ReturnsTrue()
    {
        var center = new SerializableChunkPos(10, 20);
        var site = new HolySiteData("site1", "rel1", "Test", center, "founder", "Founder");

        Assert.True(site.ContainsChunk(center));
    }

    [Fact]
    public void HolySiteData_ContainsChunk_ExpandedChunk_ReturnsTrue()
    {
        var site = new HolySiteData("site1", "rel1", "Test", new(0, 0), "founder", "Founder");
        var expanded = new SerializableChunkPos(1, 0);
        site.AddChunk(expanded);

        Assert.True(site.ContainsChunk(expanded));
    }

    [Fact]
    public void HolySiteData_ContainsChunk_OtherChunk_ReturnsFalse()
    {
        var site = new HolySiteData("site1", "rel1", "Test", new(0, 0), "founder", "Founder");

        Assert.False(site.ContainsChunk(new SerializableChunkPos(10, 10)));
    }

    [Fact]
    public void HolySiteData_GetAllChunks_IncludesCenterAndExpanded()
    {
        var site = new HolySiteData("site1", "rel1", "Test", new(0, 0), "founder", "Founder");
        site.AddChunk(new SerializableChunkPos(1, 0));
        site.AddChunk(new SerializableChunkPos(2, 0));

        var chunks = site.GetAllChunks();

        Assert.Equal(3, chunks.Count);
        Assert.Contains(new SerializableChunkPos(0, 0), chunks);
        Assert.Contains(new SerializableChunkPos(1, 0), chunks);
        Assert.Contains(new SerializableChunkPos(2, 0), chunks);
    }

    #endregion

    #region Prestige Limits Tests

    [Theory]
    [InlineData(PrestigeRank.Fledgling, 1)] // Rank 0 = Tier 1
    [InlineData(PrestigeRank.Established, 2)] // Rank 1 = Tier 2
    [InlineData(PrestigeRank.Renowned, 3)] // Rank 2 = Tier 3
    [InlineData(PrestigeRank.Legendary, 4)] // Rank 3 = Tier 4
    [InlineData(PrestigeRank.Mythic, 5)] // Rank 4 = Tier 5 (capped)
    public void GetMaxSitesForReligion_ReturnsCorrectLimit(PrestigeRank rank, int expected)
    {
        var religion = new ReligionData("rel1", "Test", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = rank;
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);

        var result = _manager.GetMaxSitesForReligion("rel1");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanCreateHolySite_BelowLimit_ReturnsTrue()
    {
        var religion = new ReligionData("rel1", "Test", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Established; // Tier 2, max 2 sites
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();

        var result = _manager.CanCreateHolySite("rel1");

        Assert.True(result);
    }

    [Fact]
    public void CanCreateHolySite_AtLimit_ReturnsFalse()
    {
        var religion = new ReligionData("rel1", "Test", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Fledgling; // Tier 1, max 1 site
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();
        _manager.ConsecrateHolySite("rel1", "Site 1", new(0, 0), "founder");

        var result = _manager.CanCreateHolySite("rel1");

        Assert.False(result);
    }

    #endregion

    #region CRUD Tests

    [Fact]
    public void ConsecrateHolySite_Success_ReturnsSite()
    {
        _manager.Initialize();

        var site = _manager.ConsecrateHolySite("rel1", "Sacred Temple", new(10, 20), "founder");

        Assert.NotNull(site);
        Assert.Equal("Sacred Temple", site.SiteName);
        Assert.Equal("rel1", site.ReligionUID);
        Assert.Equal(new SerializableChunkPos(10, 20), site.CenterChunk);
    }

    [Fact]
    public void ConsecrateHolySite_EmptyName_ReturnsNull()
    {
        _manager.Initialize();

        var site = _manager.ConsecrateHolySite("rel1", "", new(10, 20), "founder");

        Assert.Null(site);
    }

    [Fact]
    public void ConsecrateHolySite_WhitespaceName_ReturnsNull()
    {
        _manager.Initialize();

        var site = _manager.ConsecrateHolySite("rel1", "   ", new(10, 20), "founder");

        Assert.Null(site);
    }

    [Fact]
    public void ConsecrateHolySite_AtPrestigeLimit_ReturnsNull()
    {
        var religion = new ReligionData("rel1", "Test", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Fledgling; // Tier 1, max 1 site
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();
        _manager.ConsecrateHolySite("rel1", "Site 1", new(0, 0), "founder");

        var site = _manager.ConsecrateHolySite("rel1", "Site 2", new(1, 1), "founder");

        Assert.Null(site);
    }

    [Fact]
    public void ConsecrateHolySite_DuplicateChunk_ReturnsNull()
    {
        var religion = new ReligionData("rel1", "Test", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Mythic; // Tier 5, max 5 sites
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();
        _manager.ConsecrateHolySite("rel1", "Site 1", new(10, 20), "founder");

        var site = _manager.ConsecrateHolySite("rel1", "Site 2", new(10, 20), "founder");

        Assert.Null(site);
    }

    [Fact]
    public void ConsecrateHolySite_UsesFounderName()
    {
        var player = TestFixtures.CreateMockServerPlayer("founder", "TestFounder").Object;
        _fakeWorldService.AddPlayer(player);
        _manager.Initialize();

        var site = _manager.ConsecrateHolySite("rel1", "Temple", new(0, 0), "founder");

        Assert.NotNull(site);
        Assert.Equal("founder", site.FounderUID);
        Assert.Equal("TestFounder", site.FounderName);
    }

    [Fact]
    public void ConsecrateHolySite_NoPlayerFound_UsesUID()
    {
        _manager.Initialize();

        var site = _manager.ConsecrateHolySite("rel1", "Temple", new(0, 0), "unknown_player");

        Assert.NotNull(site);
        Assert.Equal("unknown_player", site.FounderUID);
        Assert.Equal("unknown_player", site.FounderName);
    }

    [Fact]
    public void ExpandHolySite_Success_ReturnsTrue()
    {
        _manager.Initialize();
        var site = _manager.ConsecrateHolySite("rel1", "Temple", new(0, 0), "founder");

        var result = _manager.ExpandHolySite(site!.SiteUID, new(1, 0));

        Assert.True(result);
        Assert.Equal(2, site.GetAllChunks().Count);
    }

    [Fact]
    public void ExpandHolySite_ChunkAlreadyClaimed_ReturnsFalse()
    {
        var religion = new ReligionData("rel1", "Test", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Mythic; // Tier 5, max 5 sites
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();
        var site1 = _manager.ConsecrateHolySite("rel1", "Site 1", new(0, 0), "founder");
        var site2 = _manager.ConsecrateHolySite("rel1", "Site 2", new(10, 10), "founder");

        var result = _manager.ExpandHolySite(site1!.SiteUID, new(10, 10));

        Assert.False(result);
    }

    [Fact]
    public void ExpandHolySite_MaxSize_ReturnsFalse()
    {
        _manager.Initialize();
        var site = _manager.ConsecrateHolySite("rel1", "Temple", new(0, 0), "founder");

        // Expand to max (6 chunks total)
        for (int i = 1; i < 6; i++)
            _manager.ExpandHolySite(site!.SiteUID, new(i, 0));

        var result = _manager.ExpandHolySite(site!.SiteUID, new(10, 10));

        Assert.False(result);
        Assert.Equal(6, site.GetAllChunks().Count);
    }

    [Fact]
    public void ExpandHolySite_SiteNotFound_ReturnsFalse()
    {
        _manager.Initialize();

        var result = _manager.ExpandHolySite("nonexistent", new(1, 0));

        Assert.False(result);
    }

    [Fact]
    public void DeconsacrateHolySite_Success_ReturnsTrue()
    {
        _manager.Initialize();
        var site = _manager.ConsecrateHolySite("rel1", "Temple", new(0, 0), "founder");

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

    [Fact]
    public void DeconsacrateHolySite_RemovesAllChunkMappings()
    {
        _manager.Initialize();
        var site = _manager.ConsecrateHolySite("rel1", "Temple", new(0, 0), "founder");
        _manager.ExpandHolySite(site!.SiteUID, new(1, 0));
        _manager.ExpandHolySite(site.SiteUID, new(2, 0));

        _manager.DeconsacrateHolySite(site.SiteUID);

        Assert.Null(_manager.GetHolySiteAtChunk(new(0, 0)));
        Assert.Null(_manager.GetHolySiteAtChunk(new(1, 0)));
        Assert.Null(_manager.GetHolySiteAtChunk(new(2, 0)));
    }

    #endregion

    #region Query Tests

    [Fact]
    public void GetHolySite_Exists_ReturnsSite()
    {
        _manager.Initialize();
        var created = _manager.ConsecrateHolySite("rel1", "Temple", new(10, 20), "founder");

        var site = _manager.GetHolySite(created!.SiteUID);

        Assert.NotNull(site);
        Assert.Equal(created.SiteUID, site.SiteUID);
    }

    [Fact]
    public void GetHolySite_NotExists_ReturnsNull()
    {
        _manager.Initialize();

        var site = _manager.GetHolySite("nonexistent");

        Assert.Null(site);
    }

    [Fact]
    public void GetHolySiteAtChunk_CenterChunk_ReturnsSite()
    {
        _manager.Initialize();
        var site = _manager.ConsecrateHolySite("rel1", "Temple", new(10, 20), "founder");

        var result = _manager.GetHolySiteAtChunk(new(10, 20));

        Assert.NotNull(result);
        Assert.Equal(site!.SiteUID, result.SiteUID);
    }

    [Fact]
    public void GetHolySiteAtChunk_ExpandedChunk_ReturnsSite()
    {
        _manager.Initialize();
        var site = _manager.ConsecrateHolySite("rel1", "Temple", new(10, 20), "founder");
        _manager.ExpandHolySite(site!.SiteUID, new(11, 20));

        var result = _manager.GetHolySiteAtChunk(new(11, 20));

        Assert.NotNull(result);
        Assert.Equal(site.SiteUID, result.SiteUID);
    }

    [Fact]
    public void GetHolySiteAtChunk_NotExists_ReturnsNull()
    {
        _manager.Initialize();

        var result = _manager.GetHolySiteAtChunk(new(99, 99));

        Assert.Null(result);
    }

    [Fact]
    public void GetReligionHolySites_ReturnsAllSites()
    {
        var religion = new ReligionData("rel1", "Test", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Mythic; // Tier 5, max 5 sites
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();
        _manager.ConsecrateHolySite("rel1", "Site 1", new(0, 0), "founder");
        _manager.ConsecrateHolySite("rel1", "Site 2", new(10, 10), "founder");

        var sites = _manager.GetReligionHolySites("rel1");

        Assert.Equal(2, sites.Count);
    }

    [Fact]
    public void GetReligionHolySites_NoSites_ReturnsEmptyList()
    {
        _manager.Initialize();

        var sites = _manager.GetReligionHolySites("rel1");

        Assert.Empty(sites);
    }

    [Fact]
    public void GetAllHolySites_ReturnsAllSites()
    {
        var religion1 = new ReligionData("rel1", "Test 1", DeityDomain.Craft, "Test Deity 1", "founder", "Founder");
        religion1.PrestigeRank = PrestigeRank.Mythic;
        var religion2 = new ReligionData("rel2", "Test 2", DeityDomain.Wild, "Test Deity 2", "founder", "Founder");
        religion2.PrestigeRank = PrestigeRank.Mythic;
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion1);
        _mockReligionManager.Setup(m => m.GetReligion("rel2")).Returns(religion2);
        _manager.Initialize();
        _manager.ConsecrateHolySite("rel1", "Site 1", new(0, 0), "founder");
        _manager.ConsecrateHolySite("rel2", "Site 2", new(10, 10), "founder");

        var sites = _manager.GetAllHolySites();

        Assert.Equal(2, sites.Count);
    }

    [Fact]
    public void IsPlayerInHolySite_PlayerNotFound_ReturnsFalse()
    {
        _manager.Initialize();

        var result = _manager.IsPlayerInHolySite("nonexistent", out var site);

        Assert.False(result);
        Assert.Null(site);
    }

    #endregion

    #region Cascading Deletion Tests

    [Fact]
    public void HandleReligionDeleted_RemovesAllSites()
    {
        var religion = new ReligionData("rel1", "Test", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Mythic; // Tier 5, max 5 sites
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();
        _manager.ConsecrateHolySite("rel1", "Site 1", new(0, 0), "founder");
        _manager.ConsecrateHolySite("rel1", "Site 2", new(10, 10), "founder");

        _manager.HandleReligionDeleted("rel1");

        var sites = _manager.GetReligionHolySites("rel1");
        Assert.Empty(sites);
    }

    [Fact]
    public void HandleReligionDeleted_NoSites_NoError()
    {
        _manager.Initialize();

        _manager.HandleReligionDeleted("rel1");

        // Should not throw
        var sites = _manager.GetReligionHolySites("rel1");
        Assert.Empty(sites);
    }

    [Fact]
    public void HandleReligionDeleted_RemovesChunkMappings()
    {
        var religion = new ReligionData("rel1", "Test", DeityDomain.Craft, "Test Deity", "founder", "Founder");
        religion.PrestigeRank = PrestigeRank.Mythic; // Tier 5, max 5 sites
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);
        _manager.Initialize();
        var site1 = _manager.ConsecrateHolySite("rel1", "Site 1", new(0, 0), "founder");
        var site2 = _manager.ConsecrateHolySite("rel1", "Site 2", new(10, 10), "founder");

        _manager.HandleReligionDeleted("rel1");

        Assert.Null(_manager.GetHolySiteAtChunk(new(0, 0)));
        Assert.Null(_manager.GetHolySiteAtChunk(new(10, 10)));
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public void ConsecrateHolySite_SavesData()
    {
        _manager.Initialize();
        _manager.ConsecrateHolySite("rel1", "Temple", new(10, 20), "founder");

        var saved = _fakePersistenceService.Load<HolySiteWorldData>("divineascension_holy_sites");

        Assert.NotNull(saved);
        Assert.Single(saved.HolySites);
        Assert.Equal("Temple", saved.HolySites[0].SiteName);
    }

    [Fact]
    public void OnSaveGameLoaded_LoadsData()
    {
        var data = new HolySiteWorldData
        {
            HolySites = new List<HolySiteData>
            {
                new("site1", "rel1", "Temple", new(10, 20), "founder", "Founder")
            }
        };
        _fakePersistenceService.Save("divineascension_holy_sites", data);
        _manager.Initialize();

        _fakeEventService.TriggerSaveGameLoaded();

        var site = _manager.GetHolySite("site1");
        Assert.NotNull(site);
        Assert.Equal("Temple", site.SiteName);
    }

    [Fact]
    public void OnSaveGameLoaded_RebuildsIndexes()
    {
        var site1 = new HolySiteData("site1", "rel1", "Temple 1", new(10, 20), "founder", "Founder");
        site1.AddChunk(new(11, 20));
        var site2 = new HolySiteData("site2", "rel1", "Temple 2", new(30, 40), "founder", "Founder");

        var data = new HolySiteWorldData
        {
            HolySites = new List<HolySiteData> { site1, site2 }
        };
        _fakePersistenceService.Save("divineascension_holy_sites", data);
        _manager.Initialize();

        _fakeEventService.TriggerSaveGameLoaded();

        // Test chunk-to-site index
        var foundSite1 = _manager.GetHolySiteAtChunk(new(10, 20));
        var foundSite1Expanded = _manager.GetHolySiteAtChunk(new(11, 20));
        var foundSite2 = _manager.GetHolySiteAtChunk(new(30, 40));

        Assert.Equal("site1", foundSite1!.SiteUID);
        Assert.Equal("site1", foundSite1Expanded!.SiteUID);
        Assert.Equal("site2", foundSite2!.SiteUID);

        // Test religion-to-sites index
        var religionSites = _manager.GetReligionHolySites("rel1");
        Assert.Equal(2, religionSites.Count);
    }

    [Fact]
    public void OnGameWorldSave_SavesData()
    {
        _manager.Initialize();
        _manager.ConsecrateHolySite("rel1", "Temple", new(10, 20), "founder");

        _fakeEventService.TriggerGameWorldSave();

        var saved = _fakePersistenceService.Load<HolySiteWorldData>("divineascension_holy_sites");
        Assert.NotNull(saved);
        Assert.Single(saved.HolySites);
    }

    [Fact]
    public void ProtoBuf_Roundtrip_PreservesData()
    {
        var original = new HolySiteData("site1", "rel1", "Sacred Temple",
            new(10, 20), "founder", "Founder");
        original.AddChunk(new(11, 20));
        original.AddChunk(new(12, 20));

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<HolySiteData>(ms);

        Assert.Equal(original.SiteUID, deserialized.SiteUID);
        Assert.Equal(original.ReligionUID, deserialized.ReligionUID);
        Assert.Equal(original.SiteName, deserialized.SiteName);
        Assert.Equal(original.CenterChunk, deserialized.CenterChunk);
        Assert.Equal(original.FounderUID, deserialized.FounderUID);
        Assert.Equal(original.FounderName, deserialized.FounderName);
        Assert.Equal(3, deserialized.GetAllChunks().Count);
    }

    [Fact]
    public void ProtoBuf_WorldData_Roundtrip()
    {
        var data = new HolySiteWorldData
        {
            HolySites = new List<HolySiteData>
            {
                new("site1", "rel1", "Temple 1", new(0, 0), "founder1", "Founder1"),
                new("site2", "rel2", "Temple 2", new(10, 10), "founder2", "Founder2")
            }
        };

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, data);
        ms.Position = 0;
        var deserialized = Serializer.Deserialize<HolySiteWorldData>(ms);

        Assert.Equal(2, deserialized.HolySites.Count);
        Assert.Equal("Temple 1", deserialized.HolySites[0].SiteName);
        Assert.Equal("Temple 2", deserialized.HolySites[1].SiteName);
    }

    #endregion
}