using System.Diagnostics.CodeAnalysis;
using DivineAscension.Configuration;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Systems;

/// <summary>
///     Integration tests for the religion-membership ↔ <see cref="PlayerTraitService"/>
///     wiring introduced in #560. Exercises the real <see cref="ReligionManager"/> and
///     <see cref="PlayerTraitService"/> end-to-end through fakes; verifies that
///     <c>PlayerProgressionData.GrantedTraitCodes</c> tracks <c>da_member</c> across
///     join, leave, disband, and cascade-revoke on religion deletion.
/// </summary>
[ExcludeFromCodeCoverage]
public class ReligionMembershipTraitTests
{
    private readonly FakeEventService _eventService;
    private readonly FakeWorldService _worldService;
    private readonly PlayerProgressionDataManager _ppdm;
    private readonly PlayerTraitService _traits;
    private readonly ReligionManager _religionManager;

    public ReligionMembershipTraitTests()
    {
        var logger = new Mock<ILogger>();
        var loggerWrapper = new Mock<ILoggerWrapper>();
        _eventService = new FakeEventService();
        var persistence = new FakePersistenceService();
        _worldService = new FakeWorldService();

        _religionManager = new ReligionManager(logger.Object, _eventService, persistence, _worldService);

        var religionManagerInterface = new Mock<IReligionManager>();
        _ppdm = new PlayerProgressionDataManager(loggerWrapper.Object, _eventService, persistence,
            _worldService, religionManagerInterface.Object, new GameBalanceConfig(), new FakeTimeService());

        _traits = new PlayerTraitService(loggerWrapper.Object, _eventService, _worldService, _ppdm, sapi: null);
        _religionManager.SetPlayerTraitService(_traits);
    }

    private static Mock<IServerPlayer> MakePlayer(string uid, string name = "TestPlayer")
    {
        var mock = new Mock<IServerPlayer>();
        mock.Setup(p => p.PlayerUID).Returns(uid);
        mock.Setup(p => p.PlayerName).Returns(name);
        return mock;
    }

    [Fact]
    public void SetPlayerTraitService_Null_Throws()
    {
        var fresh = new ReligionManager(new Mock<ILogger>().Object, new FakeEventService(),
            new FakePersistenceService(), new FakeWorldService());
        Assert.Throws<System.ArgumentNullException>(() => fresh.SetPlayerTraitService(null!));
    }

    [Fact]
    public void CreateReligion_GrantsMembershipTraitToFounder()
    {
        var founder = MakePlayer("founder-uid");
        _worldService.AddPlayer(founder.Object);

        _religionManager.CreateReligion("Test", DeityDomain.Craft, "Deity", "founder-uid", true);

        Assert.Contains(TraitCodes.Member, _ppdm.GetOrCreatePlayerData("founder-uid").GrantedTraitCodes);
    }

    [Fact]
    public void AddMember_GrantsMembershipTrait()
    {
        var p = MakePlayer("joiner-uid");
        _worldService.AddPlayer(p.Object);
        var religion = _religionManager.CreateReligion("Test", DeityDomain.Craft, "Deity", "founder", true);

        _religionManager.AddMember(religion.ReligionUID, "joiner-uid");

        Assert.Contains(TraitCodes.Member, _ppdm.GetOrCreatePlayerData("joiner-uid").GrantedTraitCodes);
    }

    [Fact]
    public void AddMember_Idempotent_DoesNotDuplicateTrait()
    {
        var p = MakePlayer("p-uid");
        _worldService.AddPlayer(p.Object);
        var religion = _religionManager.CreateReligion("Test", DeityDomain.Craft, "Deity", "founder", true);

        _religionManager.AddMember(religion.ReligionUID, "p-uid");
        _religionManager.AddMember(religion.ReligionUID, "p-uid");

        var codes = _ppdm.GetOrCreatePlayerData("p-uid").GrantedTraitCodes;
        Assert.Single(codes, TraitCodes.Member);
    }

    [Fact]
    public void RemoveMember_RevokesMembershipTrait()
    {
        var p = MakePlayer("p-uid");
        _worldService.AddPlayer(p.Object);
        var religion = _religionManager.CreateReligion("Test", DeityDomain.Craft, "Deity", "founder", true);
        _religionManager.AddMember(religion.ReligionUID, "p-uid");

        _religionManager.RemoveMember(religion.ReligionUID, "p-uid");

        Assert.DoesNotContain(TraitCodes.Member, _ppdm.GetOrCreatePlayerData("p-uid").GrantedTraitCodes);
    }

    [Fact]
    public void DeleteReligion_CascadeRevokesTraitFromEveryMember()
    {
        var founder = MakePlayer("founder-uid", "Founder");
        var m1 = MakePlayer("m1-uid", "Member1");
        var m2 = MakePlayer("m2-uid", "Member2");
        _worldService.AddPlayer(founder.Object);
        _worldService.AddPlayer(m1.Object);
        _worldService.AddPlayer(m2.Object);

        var religion = _religionManager.CreateReligion("Test", DeityDomain.Craft, "Deity", "founder-uid", true);
        _religionManager.AddMember(religion.ReligionUID, "founder-uid");
        _religionManager.AddMember(religion.ReligionUID, "m1-uid");
        _religionManager.AddMember(religion.ReligionUID, "m2-uid");

        Assert.Contains(TraitCodes.Member, _ppdm.GetOrCreatePlayerData("founder-uid").GrantedTraitCodes);
        Assert.Contains(TraitCodes.Member, _ppdm.GetOrCreatePlayerData("m1-uid").GrantedTraitCodes);
        Assert.Contains(TraitCodes.Member, _ppdm.GetOrCreatePlayerData("m2-uid").GrantedTraitCodes);

        var deleted = _religionManager.DeleteReligion(religion.ReligionUID, "founder-uid");

        Assert.True(deleted);
        Assert.DoesNotContain(TraitCodes.Member, _ppdm.GetOrCreatePlayerData("founder-uid").GrantedTraitCodes);
        Assert.DoesNotContain(TraitCodes.Member, _ppdm.GetOrCreatePlayerData("m1-uid").GrantedTraitCodes);
        Assert.DoesNotContain(TraitCodes.Member, _ppdm.GetOrCreatePlayerData("m2-uid").GrantedTraitCodes);
    }

    [Fact]
    public void RemoveMember_OfflinePlayer_StillRevokesTrait()
    {
        // Player never added to FakeWorldService → GetPlayerByUID returns null → offline path.
        var religion = _religionManager.CreateReligion("Test", DeityDomain.Craft, "Deity", "founder", true);
        _religionManager.AddMember(religion.ReligionUID, "offline-uid");
        Assert.Contains(TraitCodes.Member, _ppdm.GetOrCreatePlayerData("offline-uid").GrantedTraitCodes);

        _religionManager.RemoveMember(religion.ReligionUID, "offline-uid");

        Assert.DoesNotContain(TraitCodes.Member, _ppdm.GetOrCreatePlayerData("offline-uid").GrantedTraitCodes);
    }

    [Fact]
    public void ReligionManager_WithoutTraitServiceWired_DoesNotCrashOnAddMember()
    {
        var bare = new ReligionManager(new Mock<ILogger>().Object, new FakeEventService(),
            new FakePersistenceService(), _worldService);
        var p = MakePlayer("solo-uid");
        _worldService.AddPlayer(p.Object);
        var religion = bare.CreateReligion("Test", DeityDomain.Craft, "Deity", "founder", true);

        var ex = Record.Exception(() => bare.AddMember(religion.ReligionUID, "solo-uid"));

        Assert.Null(ex);
    }
}
