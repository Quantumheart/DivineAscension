using System.Diagnostics.CodeAnalysis;
using DivineAscension.Configuration;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Services;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Server;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems.Networking.Server;

/// <summary>
///     Tests for PlayerDataNetworkHandler — verifies <see cref="PlayerReligionDataPacket"/>
///     carries the correct <c>MaxBlessingSlots</c> (#445) and that prestige rank-ups
///     trigger a broadcast refresh to every religion member.
/// </summary>
[ExcludeFromCodeCoverage]
public class PlayerDataNetworkHandlerTests
{
    private readonly GameBalanceConfig _config;
    private readonly FakeEventService _eventService;
    private readonly FakePersistenceService _persistenceService;
    private readonly FakeTimeService _timeService;
    private readonly FakeWorldService _worldService;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<ILoggerWrapper> _mockLoggerWrapper;
    private readonly Mock<IReligionManager> _mockReligionManager;
    private readonly SpyNetworkService _networkService;
    private readonly PlayerProgressionDataManager _progression;
    private readonly ReligionPrestigeManager _prestige;
    private readonly PlayerDataNetworkHandler _handler;

    public PlayerDataNetworkHandlerTests()
    {
        _config = new GameBalanceConfig();
        _mockLogger = new Mock<ILogger>();
        _mockLoggerWrapper = new Mock<ILoggerWrapper>();
        _mockReligionManager = new Mock<IReligionManager>();
        _eventService = new FakeEventService();
        _persistenceService = new FakePersistenceService();
        _worldService = new FakeWorldService();
        _timeService = new FakeTimeService();
        _networkService = new SpyNetworkService();

        _progression = new PlayerProgressionDataManager(_mockLoggerWrapper.Object, _eventService,
            _persistenceService, _worldService, _mockReligionManager.Object, _config, _timeService);
        _prestige = new ReligionPrestigeManager(_mockLoggerWrapper.Object, _worldService,
            _mockReligionManager.Object, _config);

        _handler = new PlayerDataNetworkHandler(
            _mockLogger.Object,
            _worldService,
            _eventService,
            _networkService,
            _progression,
            _mockReligionManager.Object,
            _prestige,
            _config);
    }

    /// <summary>
    ///     Favor rank-up triggers <c>OnPlayerDataChanged</c>, which sends a refreshed
    ///     <see cref="PlayerReligionDataPacket"/> whose <c>MaxBlessingSlots</c> reflects
    ///     the new rank (Disciple = 2 slots by default).
    /// </summary>
    [Fact]
    public void AddFavor_RankUp_SendsPacketWithUpdatedSlotCount()
    {
        // Arrange: a Fledgling religion (no prestige bonus), founder is the only member.
        var religion = TestFixtures.CreateTestReligion(domain: DeityDomain.Craft, founderUID: "founder");
        _mockReligionManager.Setup(m => m.GetPlayerReligion("founder")).Returns(religion);
        _mockReligionManager.Setup(m => m.GetReligion(religion.ReligionUID)).Returns(religion);

        var player = _worldService.CreatePlayer("founder", "Founder");

        // Player just under the Disciple threshold (Initiate = 1 slot).
        var data = _progression.GetOrCreatePlayerData("founder");
        data.SetTotalFavorEarned(DeityDomain.Craft, _config.DiscipleThreshold - 10);

        // Act: cross the threshold; PlayerProgressionDataManager fires OnPlayerDataChanged.
        _progression.AddFavor("founder", DeityDomain.Craft, 20);

        // Assert: packet carries Disciple slot count.
        var packet = _networkService.GetLastSentMessage<PlayerReligionDataPacket>();
        Assert.NotNull(packet);
        Assert.Equal(_config.DiscipleActiveBlessingSlots, packet!.MaxBlessingSlots);
    }

    /// <summary>
    ///     Slot count uses the player's highest favor rank across all five deities plus
    ///     the religion's prestige bonus — captures "the best you can do" so the toast
    ///     fires whenever any deity rank-up raises the cap.
    /// </summary>
    [Fact]
    public void SendPlayerDataToClient_UsesHighestFavorRank_PlusPrestigeBonus()
    {
        // Arrange: Renowned religion (+1 bonus). Player is Avatar in Wild, Initiate elsewhere.
        var religion = TestFixtures.CreateTestReligion(domain: DeityDomain.Craft, founderUID: "founder");
        religion.PrestigeRank = PrestigeRank.Renowned;
        _mockReligionManager.Setup(m => m.GetPlayerReligion("founder")).Returns(religion);
        _mockReligionManager.Setup(m => m.GetReligion(religion.ReligionUID)).Returns(religion);

        var player = _worldService.CreatePlayer("founder", "Founder");
        var data = _progression.GetOrCreatePlayerData("founder");
        data.SetTotalFavorEarned(DeityDomain.Wild, _config.AvatarThreshold);

        // Act
        _handler.SendPlayerDataToClient(player);

        // Assert: Avatar (5) + Renowned bonus (1) = 6.
        var packet = _networkService.GetLastSentMessage<PlayerReligionDataPacket>();
        Assert.NotNull(packet);
        Assert.Equal(_config.AvatarActiveBlessingSlots + _config.RenownedBonusSlots,
            packet!.MaxBlessingSlots);
    }

    /// <summary>
    ///     Prestige rank-up that crosses a bonus threshold (Established → Renowned grants
    ///     +1 slot by default) triggers a refresh for every religion member, not just the
    ///     player who earned the prestige.
    /// </summary>
    [Fact]
    public void PrestigeRankUp_SendsPacketToAllReligionMembers()
    {
        // Arrange: religion with founder + two extra members, sitting just under Renowned.
        var religion = TestFixtures.CreateTestReligion(domain: DeityDomain.Craft, founderUID: "founder");
        religion.AddMember("member-2", "Member Two");
        religion.AddMember("member-3", "Member Three");
        religion.TotalPrestige = _config.RenownedThreshold - 1;
        religion.PrestigeRank = PrestigeRank.Established;

        _mockReligionManager.Setup(m => m.GetReligion(religion.ReligionUID)).Returns(religion);
        _mockReligionManager.Setup(m => m.GetPlayerReligion(It.IsAny<string>())).Returns(religion);

        // All three members are online and have progression data so a packet is built.
        foreach (var uid in new[] { "founder", "member-2", "member-3" })
        {
            _worldService.CreatePlayer(uid, uid);
            _progression.GetOrCreatePlayerData(uid);
        }

        // Act: cross the Renowned threshold (Established → Renowned grants +1 bonus slot).
        _prestige.AddPrestige(religion.ReligionUID, 1);

        // Assert: all three members got a refreshed packet, each carrying the new bonus.
        var packets = _networkService.GetSentMessages<PlayerReligionDataPacket>().ToList();
        Assert.Equal(3, packets.Count);
        Assert.All(packets, p => Assert.Equal(
            _config.InitiateActiveBlessingSlots + _config.RenownedBonusSlots,
            p.MaxBlessingSlots));
    }

    /// <summary>
    ///     Prestige gain that doesn't cross a rank threshold doesn't trigger a member-wide
    ///     broadcast — the per-member packet only goes out when the rank itself moves.
    /// </summary>
    [Fact]
    public void PrestigeGain_WithoutRankUp_DoesNotBroadcastToMembers()
    {
        // Arrange: religion comfortably within Fledgling, two members.
        var religion = TestFixtures.CreateTestReligion(domain: DeityDomain.Craft, founderUID: "founder");
        religion.AddMember("member-2", "Member Two");
        religion.TotalPrestige = 0;
        religion.PrestigeRank = PrestigeRank.Fledgling;

        _mockReligionManager.Setup(m => m.GetReligion(religion.ReligionUID)).Returns(religion);

        foreach (var uid in new[] { "founder", "member-2" })
        {
            _worldService.CreatePlayer(uid, uid);
            _progression.GetOrCreatePlayerData(uid);
        }

        // Act: small bump well below Established threshold.
        _prestige.AddPrestige(religion.ReligionUID, 10);

        // Assert: handler did not push any packet on behalf of the prestige event.
        Assert.Empty(_networkService.GetSentMessages<PlayerReligionDataPacket>());
    }
}
