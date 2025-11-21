using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Constants;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Tests.Commands.Helpers;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace PantheonWars.Tests.Commands.Blessing;

[ExcludeFromCodeCoverage]
public class BlessingCommandListTests : BlessingCommandsTestHelpers
{
    public BlessingCommandListTests()
    {
        _sut = InitializeMocksAndSut();
    }


    [Fact]
    public void OnBlessingsList_PlayerNotFound_ReturnsError()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Mock<Caller>().Object
        };

        // Act
        var result = _sut!.OnList(args);

        // Assert
        Assert.Equal(ErrorMessageConstants.ErrorPlayerNotFound, result.StatusMessage);
    }

    [Fact]
    public void OnBlessingsList_PlayerNotInReligion_StillShowsUniversalBlessings()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(new PlayerReligionData { ActiveDeity = DeityType.None });

        // Act
        var result = _sut!.OnList(args);

        // Assert - no longer returns error, shows available blessings header
        Assert.Equal(EnumCommandStatus.Success, result.Status);
        Assert.Contains("Available Blessings", result.StatusMessage);
    }




    [Fact]
    public void OnBlessingsList_PlayerInReligionWithReligionBlessings_ReturnsFormattedList()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerData = new PlayerReligionData
        {
            ActiveDeity = DeityType.Aethra,
            ReligionUID = "religion-uid"
        };

        var religionBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                BlessingId = "religionblessing1",
                Name = "Sacred Flame",
                Description = "Inflicts divine fire on enemies.",
                RequiredPrestigeRank = (int)PrestigeRank.Fledgling,
            }
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(playerData);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra))
            .Returns(religionBlessings);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra))
            .Returns(new List<PantheonWars.Models.Blessing>());

        var religion = new ReligionData
        {
            ReligionUID = "religion-uid",
            UnlockedBlessings = new Dictionary<string, bool>
            {
                { "religionblessing1", true }
            }
        };

        _religionManager.Setup(rm => rm.GetReligion("religion-uid"))
            .Returns(religion);

        // Act
        var result = _sut!.OnList(args);

        // Assert - With religion-only system, we deleted deity-specific blessings
        // The output now shows "Universal Blessings" section
        Assert.Contains("Available Blessings", result.StatusMessage);
        // Note: "Aethra Blessings" header only shows if GetBlessingsForDeity returns blessings
        // Since we deleted all deity-specific blessings, this test needs updated expectations
        Assert.Contains("Sacred Flame", result.StatusMessage);
    }


    [Fact]
    public void OnBlessingsList_DisplaysPrestigeRankCorrectly()
    {
        // Arrange
        var args = new TextCommandCallingArgs
        {
            Caller = new Caller
            {
                Player = new Mock<IServerPlayer>().Object
            }
        };

        var playerData = new PlayerReligionData
        {
            ActiveDeity = DeityType.Aethra,
            ReligionUID = "religion-uid"
        };

        var religionBlessings = new List<PantheonWars.Models.Blessing>
        {
            new()
            {
                BlessingId = "blessing_established",
                Name = "Established Blessing",
                Description = "A blessing for established religions.",
                RequiredPrestigeRank = (int)PrestigeRank.Established,
            }
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(playerData);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra))
            .Returns(new List<PantheonWars.Models.Blessing>());
        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra))
            .Returns(religionBlessings);

        var religion = new ReligionData
        {
            ReligionUID = "religion-uid",
            UnlockedBlessings = new Dictionary<string, bool>()
        };

        _religionManager.Setup(rm => rm.GetReligion("religion-uid"))
            .Returns(religion);

        // Act
        var result = _sut!.OnList(args);

        // Assert
        Assert.Contains("Established Blessing", result.StatusMessage);
        Assert.Contains("Established", result.StatusMessage);
    }


}