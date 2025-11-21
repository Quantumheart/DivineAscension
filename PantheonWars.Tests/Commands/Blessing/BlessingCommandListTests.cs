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
    public void OnBlessingsList_PlayerNotInReligion_ReturnsError()
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

        // Assert
        Assert.Equal(ErrorMessageConstants.ErrorMustJoinReligion, result.StatusMessage);
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
                Kind = BlessingKind.Religion
            }
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(playerData);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Religion))
            .Returns(religionBlessings);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Player))
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

        // Assert
        Assert.Contains(string.Format(FormatStringConstants.HeaderBlessingsForDeity, DeityType.Aethra),
            result.StatusMessage);
        Assert.Contains(FormatStringConstants.HeaderReligionBlessings, result.StatusMessage);
        Assert.Contains("Sacred Flame [UNLOCKED]", result.StatusMessage);
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
                Kind = BlessingKind.Religion
            }
        };

        _playerReligionDataManager.Setup(prdm => prdm.GetOrCreatePlayerData(It.IsAny<string>()))
            .Returns(playerData);

        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Player))
            .Returns(new List<PantheonWars.Models.Blessing>());
        _blessingRegistry.Setup(pr => pr.GetBlessingsForDeity(DeityType.Aethra, BlessingKind.Religion))
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
        Assert.Contains(string.Format(FormatStringConstants.FormatRequiredRank, PrestigeRank.Established),
            result.StatusMessage);
    }


}