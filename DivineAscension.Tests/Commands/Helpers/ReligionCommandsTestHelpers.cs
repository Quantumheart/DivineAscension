using System.Diagnostics.CodeAnalysis;
using DivineAscension.Commands;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Commands.Helpers;

[ExcludeFromCodeCoverage]
public class ReligionCommandsTestHelpers
{
    protected Mock<IChatCommandApi> _mockChatCommands;
    protected Mock<ILogger> _mockLogger;
    protected Mock<ICoreServerAPI> _mockSapi;
    protected Mock<IServerWorldAccessor> _mockWorld;
    protected Mock<IPlayerReligionDataManager> _playerReligionDataManager;
    protected Mock<IReligionManager> _religionManager;
    protected Mock<IServerNetworkChannel> _serverChannel;
    protected ReligionCommands? _sut;

    protected ReligionCommandsTestHelpers()
    {
        _mockSapi = new Mock<ICoreServerAPI>();
        _mockLogger = new Mock<ILogger>();
        _mockChatCommands = new Mock<IChatCommandApi>();
        _mockWorld = new Mock<IServerWorldAccessor>();

        _mockSapi.Setup(api => api.Logger).Returns(_mockLogger.Object);
        _mockSapi.Setup(api => api.ChatCommands).Returns(_mockChatCommands.Object);
        _mockSapi.Setup(api => api.World).Returns(_mockWorld.Object);

        _religionManager = new Mock<IReligionManager>();

        _playerReligionDataManager = new Mock<IPlayerReligionDataManager>();
        _serverChannel = new Mock<IServerNetworkChannel>();
    }

    protected ReligionCommands InitializeMocksAndSut()
    {
        var mockPrestigeManager = new Mock<IReligionPrestigeManager>();
        return new ReligionCommands(
            _mockSapi.Object,
            _religionManager.Object,
            _playerReligionDataManager.Object,
            mockPrestigeManager.Object,
            _serverChannel.Object);
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
    protected PlayerReligionData CreatePlayerData(string playerUID, string? religionUID = null,
        DeityType deity = DeityType.None)
    {
        return new PlayerReligionData(playerUID)
        {
            ReligionUID = religionUID,
            ActiveDeity = deity
        };
    }

    /// <summary>
    /// Creates test ReligionData
    /// </summary>
    protected ReligionData CreateReligion(string uid, string name, DeityType deity, string founderUID,
        bool isPublic = true)
    {
        var religion = new ReligionData(uid, name, deity, founderUID, "TestFounder")
        {
            IsPublic = isPublic,
            Roles = RoleDefaults.CreateDefaultRoles(),
            MemberRoles = new Dictionary<string, string>
            {
                [founderUID] = RoleDefaults.FOUNDER_ROLE_ID
            }
        };

        return religion;
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