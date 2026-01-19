using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Commands;
using DivineAscension.Data;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Systems;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Tests.Helpers;
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
    protected Mock<IPlayerProgressionDataManager> _playerProgressionDataManager;
    protected Mock<IReligionManager> _religionManager;
    protected Mock<IServerNetworkChannel> _serverChannel;
    protected Mock<INetworkService> _mockNetworkService;
    protected Mock<IPlayerMessengerService> _mockMessengerService;
    protected Mock<IWorldService> _mockWorldService;
    protected ReligionCommands? _sut;

    protected ReligionCommandsTestHelpers()
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

        _religionManager = new Mock<IReligionManager>();
        _playerProgressionDataManager = new Mock<IPlayerProgressionDataManager>();
        _serverChannel = new Mock<IServerNetworkChannel>();

        // Initialize service mocks
        _mockNetworkService = new Mock<INetworkService>();
        _mockMessengerService = new Mock<IPlayerMessengerService>();
        _mockWorldService = new Mock<IWorldService>();

        // Default setup: return empty list for GetAllPlayers (tests override this as needed)
        _mockWorldService.Setup(w => w.GetAllPlayers()).Returns(new List<IPlayer>());
    }

    protected ReligionCommands InitializeMocksAndSut()
    {
        var mockPrestigeManager = new Mock<IReligionPrestigeManager>();
        var mockRoleManager = new Mock<IRoleManager>();
        var mockCooldownManager = TestFixtures.CreateMockCooldownManager();

        // Default behavior: allow all operations (no cooldown active)
        mockCooldownManager
            .Setup(m => m.CanPerformOperation(It.IsAny<string>(), It.IsAny<DivineAscension.Models.Enum.CooldownType>(), out It.Ref<string?>.IsAny))
            .Returns(true);

        return new ReligionCommands(
            _mockSapi.Object,
            _religionManager.Object,
            _playerProgressionDataManager.Object,
            mockPrestigeManager.Object,
            _mockNetworkService.Object,
            mockRoleManager.Object,
            mockCooldownManager.Object,
            _mockMessengerService.Object,
            _mockWorldService.Object,
            _mockLogger.Object);
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
    protected PlayerProgressionData CreatePlayerData(string playerUID)
    {
        return new PlayerProgressionData(playerUID)
        {
        };
    }

    /// <summary>
    /// Creates test ReligionData
    /// </summary>
    protected ReligionData CreateReligion(string uid, string name, DeityDomain deity, string founderUID,
        bool isPublic = true)
    {
        var religion = new ReligionData(uid, name, deity, "TestDeity", founderUID, "TestFounder")
        {
            IsPublic = isPublic
        };
        religion.InitializeRoles(RoleDefaults.CreateDefaultRoles());
        religion.AssignMemberRole(founderUID, RoleDefaults.FOUNDER_ROLE_ID);

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