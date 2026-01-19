using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using DivineAscension.Commands;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Networking.Interfaces;
using DivineAscension.Tests.Helpers;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Commands.Helpers;

[ExcludeFromCodeCoverage]
public class BlessingCommandsTestHelpers
{
    protected Mock<IBlessingEffectSystem> _blessingEffectSystem;
    protected Mock<IBlessingRegistry> _blessingRegistry;
    protected Mock<ICoreAPI> _mockApi;
    protected Mock<ICoreServerAPI> _mockSapi;
    protected Mock<IPlayerProgressionDataManager> _playerReligionDataManager;
    protected Mock<IReligionManager> _religionManager;
    protected Mock<INetworkService> _networkService;
    protected Mock<IPlayerMessengerService> _messengerService;
    protected BlessingCommands? _sut;

    protected BlessingCommandsTestHelpers()
    {
        // Initialize localization for tests
        TestFixtures.InitializeLocalizationForTests();

        _mockSapi = new Mock<ICoreServerAPI>();
        _mockApi = new Mock<ICoreAPI>();

        var mockLogger = new Mock<ILogger>();
        _mockApi.Setup(api => api.Logger).Returns(mockLogger.Object);
        _mockSapi.Setup(sapi => sapi.Logger).Returns(mockLogger.Object);

        _blessingRegistry = new Mock<IBlessingRegistry>();
        _religionManager = new Mock<IReligionManager>();
        _playerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        _blessingEffectSystem = new Mock<IBlessingEffectSystem>();
        _networkService = new Mock<INetworkService>();
        _messengerService = new Mock<IPlayerMessengerService>();
    }

    protected BlessingCommands InitializeMocksAndSut()
    {
        return new BlessingCommands(
            _mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            _blessingEffectSystem.Object,
            _networkService.Object,
            _messengerService.Object);
    }
}