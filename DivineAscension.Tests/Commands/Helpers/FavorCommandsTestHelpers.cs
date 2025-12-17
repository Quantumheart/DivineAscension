using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Commands;
using PantheonWars.Data;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace PantheonWars.Tests.Commands.Helpers;

[ExcludeFromCodeCoverage]
public class FavorCommandsTestHelpers
{
    protected Mock<IDeityRegistry> _deityRegistry;
    protected Mock<IChatCommandApi> _mockChatCommands;
    protected Mock<ILogger> _mockLogger;
    protected Mock<ICoreServerAPI> _mockSapi;
    protected Mock<IServerWorldAccessor> _mockWorld;
    protected Mock<IPlayerReligionDataManager> _playerReligionDataManager;
    protected FavorCommands? _sut;

    protected FavorCommandsTestHelpers()
    {
        _mockSapi = new Mock<ICoreServerAPI>();
        _mockLogger = new Mock<ILogger>();
        _mockChatCommands = new Mock<IChatCommandApi>();
        _mockWorld = new Mock<IServerWorldAccessor>();

        _mockSapi.Setup(api => api.Logger).Returns(_mockLogger.Object);
        _mockSapi.Setup(api => api.ChatCommands).Returns(_mockChatCommands.Object);
        _mockSapi.Setup(api => api.World).Returns(_mockWorld.Object);

        _deityRegistry = new Mock<IDeityRegistry>();
        _playerReligionDataManager = new Mock<IPlayerReligionDataManager>();
    }

    protected FavorCommands InitializeMocksAndSut()
    {
        return new FavorCommands(
            _mockSapi.Object,
            _deityRegistry.Object,
            _playerReligionDataManager.Object);
    }

    /// <summary>
    /// Creates a test TextCommandCallingArgs instance with a player caller
    /// </summary>
    protected TextCommandCallingArgs CreateCommandArgs(IServerPlayer player, params string[] args)
    {
        return new TextCommandCallingArgs
        {
            LanguageCode = "en",
            Caller = new Caller
            {
                Type = EnumCallerType.Player,
                Player = player,
                CallerPrivileges = new[] { "chat" },
                CallerRole = "player",
                Pos = new Vec3d(0, 0, 0)
            },
            RawArgs = new CmdArgs(args),
            Parsers = new List<ICommandArgumentParser>()
        };
    }

    /// <summary>
    /// Creates a test TextCommandCallingArgs instance with admin privileges
    /// </summary>
    protected TextCommandCallingArgs CreateAdminCommandArgs(IServerPlayer player, params string[] args)
    {
        return new TextCommandCallingArgs
        {
            LanguageCode = "en",
            Caller = new Caller
            {
                Type = EnumCallerType.Player,
                Player = player,
                CallerPrivileges = new[] { "chat", "root" },
                CallerRole = "admin",
                Pos = new Vec3d(0, 0, 0)
            },
            RawArgs = new CmdArgs(args),
            Parsers = new List<ICommandArgumentParser>()
        };
    }

    /// <summary>
    /// Creates a mock IServerPlayer with the specified UID and name
    /// </summary>
    protected Mock<IServerPlayer> CreateMockPlayer(string playerUID, string playerName)
    {
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(p => p.PlayerUID).Returns(playerUID);
        mockPlayer.Setup(p => p.PlayerName).Returns(playerName);

        var mockPlayerData = new Mock<IServerPlayerData>();
        mockPlayer.Setup(p => p.ServerData).Returns(mockPlayerData.Object);

        return mockPlayer;
    }

    /// <summary>
    /// Creates test PlayerReligionData
    /// </summary>
    protected PlayerReligionData CreatePlayerData(
        string playerUID,
        DeityType deity = DeityType.Khoras,
        int favor = 0,
        int totalFavor = 0,
        FavorRank rank = FavorRank.Initiate)
    {
        return new PlayerReligionData(playerUID)
        {
            ActiveDeity = deity,
            Favor = favor,
            TotalFavorEarned = totalFavor,
            FavorRank = rank,
            ReligionUID = "test-religion-uid"
        };
    }

    /// <summary>
    /// Sets up the CommandArgumentParsers with arguments
    /// </summary>
    protected void SetupParsers(TextCommandCallingArgs args, params object[] parsedValues)
    {
        args.Parsers.Clear();
        foreach (var value in parsedValues)
        {
            var mockParser = new Mock<ICommandArgumentParser>();
            mockParser.Setup(p => p.GetValue()).Returns(value);
            mockParser.Setup(p => p.ArgCount).Returns(1);
            args.Parsers.Add(mockParser.Object);
        }
    }
}