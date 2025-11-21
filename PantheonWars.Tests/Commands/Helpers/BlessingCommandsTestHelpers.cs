using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Moq;
using PantheonWars.Commands;
using PantheonWars.Models.Enum;
using PantheonWars.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Blessing = PantheonWars.Models.Blessing;

namespace PantheonWars.Tests.Commands.Helpers;

[ExcludeFromCodeCoverage]
public class BlessingCommandsTestHelpers
{
    protected Mock<ICoreAPI> _mockApi;
    protected Mock<ICoreServerAPI> _mockSapi;
    protected Mock<IBlessingEffectSystem> _blessingEffectSystem;
    protected Mock<IBlessingRegistry> _blessingRegistry;
    protected Mock<IPlayerReligionDataManager> _playerReligionDataManager;
    protected Mock<IReligionManager> _religionManager;
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
        _playerReligionDataManager = new Mock<IPlayerReligionDataManager>();
        _blessingEffectSystem = new Mock<IBlessingEffectSystem>();

        // Default setup for GetUniversalBlessings to return empty list
        _blessingRegistry.Setup(br => br.GetUniversalBlessings())
            .Returns(new List<PantheonWars.Models.Blessing>());
    }

    protected BlessingCommands InitializeMocksAndSut()
    {
        return new BlessingCommands(
            _mockSapi.Object,
            _blessingRegistry.Object,
            _playerReligionDataManager.Object,
            _religionManager.Object,
            _blessingEffectSystem.Object);
    }
}