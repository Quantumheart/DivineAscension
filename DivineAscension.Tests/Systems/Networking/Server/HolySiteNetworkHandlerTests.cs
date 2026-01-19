using System.Diagnostics.CodeAnalysis;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Network.HolySite;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Server;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems.Networking.Server;

/// <summary>
/// Tests for HolySiteNetworkHandler packet handling and data transformation.
/// </summary>
[ExcludeFromCodeCoverage]
public class HolySiteNetworkHandlerTests
{
    private readonly HolySiteNetworkHandler _handler;
    private readonly Mock<IHolySiteManager> _mockHolySiteManager;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IServerPlayer> _mockPlayer;
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly SpyNetworkService _networkService;

    public HolySiteNetworkHandlerTests()
    {
        _networkService = new SpyNetworkService();
        _mockPlayer = new Mock<IServerPlayer>();
        _mockPlayer.Setup(p => p.PlayerUID).Returns("player1");
        _mockPlayer.Setup(p => p.PlayerName).Returns("Player One");

        _mockLogger = new Mock<ILogger>();
        _mockHolySiteManager = new Mock<IHolySiteManager>();
        _mockReligionManager = new Mock<IReligionManager>();

        _handler = new HolySiteNetworkHandler(
            _mockLogger.Object,
            _mockHolySiteManager.Object,
            _mockReligionManager.Object,
            _networkService);

        _handler.RegisterHandlers();
    }

    [Fact]
    public void ListAction_ReturnsAllSites_WhenNoFilter()
    {
        // Arrange
        var religion1 = new ReligionData("rel1", "Religion One", DeityDomain.Craft, "Aethra", "player1", "Player One");
        var religion2 = new ReligionData("rel2", "Religion Two", DeityDomain.Wild, "Gaia", "player2", "Player Two");

        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion1);
        _mockReligionManager.Setup(m => m.GetReligion("rel2")).Returns(religion2);

        var site1 = CreateTestSite("site1", "rel1", "Site One", 100000);
        var site2 = CreateTestSite("site2", "rel2", "Site Two", 150000);

        _mockHolySiteManager.Setup(m => m.GetAllHolySites()).Returns(new List<HolySiteData> { site1, site2 });

        var request = new HolySiteRequestPacket("list");

        // Act
        _networkService.SimulateReceive(_mockPlayer.Object, request);

        // Assert
        var response = _networkService.GetLastSentMessage<HolySiteResponsePacket>();
        Assert.NotNull(response);
        Assert.Equal(2, response.Sites.Count);
        Assert.Contains(response.Sites, s => s.SiteName == "Site One" && s.Domain == "Craft");
        Assert.Contains(response.Sites, s => s.SiteName == "Site Two" && s.Domain == "Wild");
    }

    [Fact]
    public void ReligionSitesAction_ReturnsOnlyReligionSites()
    {
        // Arrange
        var religion = new ReligionData("rel1", "Religion One", DeityDomain.Craft, "Aethra", "player1", "Player One");
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);

        var site1 = CreateTestSite("site1", "rel1", "Site One", 100000);
        var site2 = CreateTestSite("site2", "rel1", "Site Two", 150000);

        _mockHolySiteManager.Setup(m => m.GetReligionHolySites("rel1"))
            .Returns(new List<HolySiteData> { site1, site2 });

        var request = new HolySiteRequestPacket("religion_sites", religionUID: "rel1");

        // Act
        _networkService.SimulateReceive(_mockPlayer.Object, request);

        // Assert
        var response = _networkService.GetLastSentMessage<HolySiteResponsePacket>();
        Assert.NotNull(response);
        Assert.Equal(2, response.Sites.Count);
        Assert.All(response.Sites, s => Assert.Equal("rel1", s.ReligionUID));
    }

    [Fact]
    public void DetailAction_ReturnsFullSiteInfo_WhenSiteExists()
    {
        // Arrange
        var religion = new ReligionData("rel1", "Religion One", DeityDomain.Craft, "Aethra", "player1", "Player One");
        _mockReligionManager.Setup(m => m.GetReligion("rel1")).Returns(religion);

        var site = CreateTestSite("site1", "rel1", "Test Site", 100000);
        _mockHolySiteManager.Setup(m => m.GetHolySite("site1")).Returns(site);

        var request = new HolySiteRequestPacket("detail", siteUID: "site1");

        // Act
        _networkService.SimulateReceive(_mockPlayer.Object, request);

        // Assert
        var response = _networkService.GetLastSentMessage<HolySiteResponsePacket>();
        Assert.NotNull(response);
        Assert.NotNull(response.DetailInfo);
        Assert.Equal("site1", response.DetailInfo.SiteUID);
        Assert.Equal("Test Site", response.DetailInfo.SiteName);
        Assert.Equal("Craft", response.DetailInfo.Domain);
    }

    [Fact]
    public void UnknownAction_ReturnsEmptyResponse()
    {
        // Arrange
        var request = new HolySiteRequestPacket("invalid_action");

        // Act
        _networkService.SimulateReceive(_mockPlayer.Object, request);

        // Assert
        var response = _networkService.GetLastSentMessage<HolySiteResponsePacket>();
        Assert.NotNull(response);
        Assert.Empty(response.Sites);
        Assert.Null(response.DetailInfo);
    }

    private HolySiteData CreateTestSite(string siteUID, string religionUID, string siteName, int targetVolume)
    {
        // Create a single area with the target volume
        int sideLength = (int)Math.Pow(targetVolume, 1.0 / 3.0);
        var area = new SerializableCuboidi(0, 0, 0, sideLength, sideLength, sideLength);

        return new HolySiteData(
            siteUID,
            religionUID,
            siteName,
            new List<SerializableCuboidi> { area },
            "player1",
            "Player One");
    }
}