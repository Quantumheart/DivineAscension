using System.Diagnostics.CodeAnalysis;
using DivineAscension.Commands;
using DivineAscension.Systems.Interfaces;
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
    protected Mock<IServerNetworkChannel> _serverChannel;
    protected BlessingCommands? _sut;

    protected BlessingCommandsTestHelpers()
    {
        _mockSapi = new Mock<ICoreServerAPI>();
        _mockApi = new Mock<ICoreAPI>();

        var mockLogger = new Mock<ILogger>();
        _mockApi.Setup(api => api.Logger).Returns(mockLogger.Object);
        _mockSapi.Setup(sapi => sapi.Logger).Returns(mockLogger.Object);

        _blessingRegistry = new Mock<IBlessingRegistry>();
        _religionManager = new Mock<IReligionManager>();
        _playerReligionDataManager = new Mock<IPlayerProgressionDataManager>();
        _blessingEffectSystem = new Mock<IBlessingEffectSystem>();
        _serverChannel = new Mock<IServerNetworkChannel>();
    }

    protected BlessingCommands InitializeMocksAndSut()
    {
        return new BlessingCommands(
            _mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            _blessingEffectSystem.Object,
            _serverChannel.Object);
    }
}