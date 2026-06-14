using System.Diagnostics.CodeAnalysis;
using DivineAscension.Configuration;
using DivineAscension.Services;
using Moq;
using Vintagestory.API.Common;

namespace DivineAscension.Tests.Services;

/// <summary>
///     Verifies the admin-facing logging toggles (#620): ApplyConfig mutates the shared config in
///     place so loggers already handed out honor the new levels without a restart — this is the
///     path the live ConfigLib GUI change drives via OnConfigChanged.
/// </summary>
[ExcludeFromCodeCoverage]
public class LoggingServiceTests
{
    [Fact]
    public void ApplyConfig_Silent_SuppressesAlreadyCreatedLogger()
    {
        var underlying = new Mock<ILogger>();
        LoggingService.Instance.Initialize(underlying.Object, LoggingConfig.Default());

        // Logger handed out BEFORE the config change (mirrors systems built at startup).
        var logger = LoggingService.Instance.CreateLogger("FavorSystem");
        logger.Notification("before");
        underlying.Verify(l => l.Notification("before"), Times.Once);

        LoggingService.Instance.ApplyConfig(LoggingConfig.Silent());

        logger.Notification("after");
        logger.Debug("after");
        logger.Error("after");

        underlying.Verify(l => l.Notification("after"), Times.Never);
        underlying.Verify(l => l.Debug("after"), Times.Never);
        underlying.Verify(l => l.Error("after"), Times.Never);
    }

    [Fact]
    public void ApplyConfig_FromGameBalanceToggles_TakesEffectLive()
    {
        var underlying = new Mock<ILogger>();
        LoggingService.Instance.Initialize(underlying.Object, LoggingConfig.Default());
        var logger = LoggingService.Instance.CreateLogger("FavorSystem");

        // Admin unchecks debug + notification in the ConfigLib GUI, keeps warning + error.
        var balance = new GameBalanceConfig
        {
            EnableDebugLogs = false,
            EnableNotificationLogs = false,
            EnableWarningLogs = true,
            EnableErrorLogs = true
        };
        LoggingService.Instance.ApplyConfig(balance.BuildLoggingConfig());

        logger.Debug("d");
        logger.Error("e");

        underlying.Verify(l => l.Debug("d"), Times.Never);
        underlying.Verify(l => l.Error("e"), Times.Once);
    }

    [Fact]
    public void ApplyConfig_AfterReInitialize_StillSuppressesExistingLogger()
    {
        // Single-player runs client AND server ModSystems in one process, both calling Initialize on
        // this shared singleton. The second Initialize must not strand loggers handed out after the
        // first — otherwise a Silent applied later never reaches them (the actual #620 field bug).
        var underlying = new Mock<ILogger>();
        LoggingService.Instance.Initialize(underlying.Object, LoggingConfig.Default());
        var logger = LoggingService.Instance.CreateLogger("FavorSystem");

        // Other game side re-initializes the singleton with a fresh Default config.
        LoggingService.Instance.Initialize(underlying.Object, LoggingConfig.Default());

        LoggingService.Instance.ApplyConfig(LoggingConfig.Silent());

        logger.Debug("after");
        logger.Notification("after");
        underlying.Verify(l => l.Debug("after"), Times.Never);
        underlying.Verify(l => l.Notification("after"), Times.Never);
    }

    [Fact]
    public void ApplyConfig_BackToDefault_RestoresLogging()
    {
        var underlying = new Mock<ILogger>();
        LoggingService.Instance.Initialize(underlying.Object, LoggingConfig.Default());
        var logger = LoggingService.Instance.CreateLogger("FavorSystem");

        LoggingService.Instance.ApplyConfig(LoggingConfig.Silent());
        LoggingService.Instance.ApplyConfig(LoggingConfig.Default());

        logger.Notification("restored");

        underlying.Verify(l => l.Notification("restored"), Times.Once);
    }
}
