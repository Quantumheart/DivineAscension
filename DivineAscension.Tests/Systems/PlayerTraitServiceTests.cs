using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using DivineAscension.Configuration;
using DivineAscension.Services;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using ProtoBuf;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Tests for <see cref="PlayerTraitService"/> — issue #559 foundation.
///     Verifies grant/revoke updates the granted-codes store, idempotency,
///     event-handler registration, and ProtoBuf round-trip for the new field.
/// </summary>
[ExcludeFromCodeCoverage]
public class PlayerTraitServiceTests
{
    private readonly FakeEventService _eventService;
    private readonly FakePersistenceService _persistenceService;
    private readonly Mock<ILoggerWrapper> _logger;
    private readonly Mock<IReligionManager> _religionManager;
    private readonly PlayerProgressionDataManager _ppdm;
    private readonly PlayerTraitService _sut;

    public PlayerTraitServiceTests()
    {
        _logger = new Mock<ILoggerWrapper>();
        _eventService = new FakeEventService();
        _persistenceService = new FakePersistenceService();
        _religionManager = new Mock<IReligionManager>();
        var worldService = new FakeWorldService();
        var timeService = new FakeTimeService();
        var config = new GameBalanceConfig();

        _ppdm = new PlayerProgressionDataManager(_logger.Object, _eventService, _persistenceService,
            worldService, _religionManager.Object, config, timeService);

        _sut = new PlayerTraitService(_logger.Object, _eventService, worldService, _ppdm, sapi: null);
    }

    private static Mock<IServerPlayer> MakePlayer(string uid)
    {
        var mock = new Mock<IServerPlayer>();
        mock.Setup(p => p.PlayerUID).Returns(uid);
        // Entity left null — SyncExtraTraitsAttribute and ReapplyCharacterClass early-return,
        // which is acceptable: the data-layer behavior is what's tested here.
        return mock;
    }

    [Fact]
    public void Initialize_RegistersPlayerJoinAndSaveGameLoadedHandlers()
    {
        _sut.Initialize();

        Assert.Equal(1, _eventService.PlayerJoinCallbackCount);
        Assert.Equal(1, _eventService.SaveGameLoadedCallbackCount);
    }

    [Fact]
    public void GrantTrait_AddsCodeToPlayerData()
    {
        var player = MakePlayer("uid-1");

        var added = _sut.GrantTrait(player.Object, "da_member");

        Assert.True(added);
        Assert.Contains("da_member", _ppdm.GetOrCreatePlayerData("uid-1").GrantedTraitCodes);
    }

    [Fact]
    public void GrantTrait_IsIdempotent()
    {
        var player = MakePlayer("uid-2");

        var first = _sut.GrantTrait(player.Object, "da_blessed");
        var second = _sut.GrantTrait(player.Object, "da_blessed");

        Assert.True(first);
        Assert.False(second);
        Assert.Single(_ppdm.GetOrCreatePlayerData("uid-2").GrantedTraitCodes);
    }

    [Fact]
    public void RevokeTrait_RemovesCodeFromPlayerData()
    {
        var player = MakePlayer("uid-3");
        _sut.GrantTrait(player.Object, "da_favored");

        var removed = _sut.RevokeTrait(player.Object, "da_favored");

        Assert.True(removed);
        Assert.Empty(_ppdm.GetOrCreatePlayerData("uid-3").GrantedTraitCodes);
    }

    [Fact]
    public void RevokeTrait_AbsentCode_ReturnsFalse()
    {
        var player = MakePlayer("uid-4");

        var removed = _sut.RevokeTrait(player.Object, "da_member");

        Assert.False(removed);
    }

    [Fact]
    public void HasGrantedTrait_ReflectsGrantState()
    {
        var player = MakePlayer("uid-5");

        Assert.False(_sut.HasGrantedTrait("uid-5", "da_member"));

        _sut.GrantTrait(player.Object, "da_member");
        Assert.True(_sut.HasGrantedTrait("uid-5", "da_member"));

        _sut.RevokeTrait(player.Object, "da_member");
        Assert.False(_sut.HasGrantedTrait("uid-5", "da_member"));
    }

    [Fact]
    public void GrantTrait_NullOrEmptyCode_NoOps()
    {
        var player = MakePlayer("uid-6");

        Assert.False(_sut.GrantTrait(player.Object, ""));
        Assert.False(_sut.GrantTrait(player.Object, "   "));
        Assert.Empty(_ppdm.GetOrCreatePlayerData("uid-6").GrantedTraitCodes);
    }

    [Fact]
    public void Dispose_UnsubscribesEventHandlers()
    {
        _sut.Initialize();
        Assert.Equal(1, _eventService.PlayerJoinCallbackCount);
        Assert.Equal(1, _eventService.SaveGameLoadedCallbackCount);

        _sut.Dispose();

        Assert.Equal(0, _eventService.PlayerJoinCallbackCount);
        Assert.Equal(0, _eventService.SaveGameLoadedCallbackCount);
    }

    /// <summary>
    ///     ProtoBuf round-trip — adding a new tagged field must not break loading older
    ///     payloads (they simply deserialize with an empty granted-codes set).
    /// </summary>
    [Fact]
    public void PlayerProgressionData_GrantedTraitCodes_RoundTripViaProtoBuf()
    {
        var original = new PlayerProgressionData("uid-7");
        original.AddGrantedTraitCode("da_member");
        original.AddGrantedTraitCode("da_blessed");

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, original);
        ms.Position = 0;
        var restored = Serializer.Deserialize<PlayerProgressionData>(ms);

        Assert.Equal(9, restored.DataVersion);
        Assert.Equal(2, restored.GrantedTraitCodes.Count);
        Assert.Contains("da_member", restored.GrantedTraitCodes);
        Assert.Contains("da_blessed", restored.GrantedTraitCodes);
    }

    /// <summary>
    ///     Round-trip via the persistence layer (mirrors how the manager saves/loads).
    ///     Granted codes survive a save → load cycle.
    /// </summary>
    [Fact]
    public void GrantedTraitCodes_SurvivePersistenceRoundTrip()
    {
        var player = MakePlayer("uid-8");
        _sut.GrantTrait(player.Object, "da_member");
        _sut.GrantTrait(player.Object, "da_favored");
        _ppdm.SaveAllPlayerData();

        // Drop in-memory state and reload from the (fake) persistence store.
        var reloaded = new PlayerProgressionDataManager(_logger.Object, _eventService, _persistenceService,
            new FakeWorldService(), _religionManager.Object, new GameBalanceConfig(), new FakeTimeService());
        reloaded.LoadPlayerData("uid-8");

        var data = reloaded.GetOrCreatePlayerData("uid-8");
        Assert.Contains("da_member", data.GrantedTraitCodes);
        Assert.Contains("da_favored", data.GrantedTraitCodes);
    }
}
