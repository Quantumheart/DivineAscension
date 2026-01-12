using System.Diagnostics.CodeAnalysis;
using DivineAscension.Commands;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Commands.Helpers;

[ExcludeFromCodeCoverage]
public class CivilizationCommandsTestHelpers
{
    protected Mock<ICivilizationManager> _civilizationManager;
    protected Mock<IChatCommandApi> _mockChatCommands;
    protected Mock<ILogger> _mockLogger;
    protected Mock<ICoreServerAPI> _mockSapi;
    protected Mock<IServerWorldAccessor> _mockWorld;
    protected Mock<IPlayerProgressionDataManager> _playerProgressionDataManager;
    protected Mock<IReligionManager> _religionManager;
    protected CivilizationCommands? _sut;

    protected CivilizationCommandsTestHelpers()
    {
        // Initialize localization for tests
        TestFixtures.InitializeLocalizationForTests();

        _mockSapi = new Mock<ICoreServerAPI>();
        _mockLogger = new Mock<ILogger>();
        _mockChatCommands = new Mock<IChatCommandApi>();
        _mockWorld = new Mock<IServerWorldAccessor>();

        _mockSapi.Setup(api => api.Logger).Returns(_mockLogger.Object);
        _mockSapi.Setup(api => api.ChatCommands).Returns(_mockChatCommands.Object);
        _mockSapi.Setup(api => api.World).Returns(_mockWorld.Object);

        _civilizationManager = new Mock<ICivilizationManager>();
        _religionManager = new Mock<IReligionManager>();
        _playerProgressionDataManager = new Mock<IPlayerProgressionDataManager>();
    }

    protected CivilizationCommands InitializeMocksAndSut()
    {
        return new CivilizationCommands(
            _mockSapi.Object,
            _civilizationManager.Object,
            _religionManager.Object,
            _playerProgressionDataManager.Object);
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
    /// Creates test ReligionData
    /// </summary>
    protected ReligionData CreateReligion(string uid, string name, DeityDomain deity, string deityName,
        string founderUID)
    {
        var religion = new ReligionData(uid, name, deity, deityName, founderUID, "TestFounder")
        {
            IsPublic = true,
            MemberUIDs = new List<string> { founderUID }
        };

        return religion;
    }

    /// <summary>
    /// Creates test Civilization
    /// </summary>
    protected DivineAscension.Data.Civilization CreateCivilization(string civId, string name, string founderUID,
        List<string> religionIds)
    {
        return new DivineAscension.Data.Civilization
        {
            CivId = civId,
            Name = name,
            FounderUID = founderUID,
            MemberReligionIds = religionIds,
            MemberCount = 0,
            Icon = "default"
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