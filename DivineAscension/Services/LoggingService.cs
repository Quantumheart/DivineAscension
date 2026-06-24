using System;
using DivineAscension.Configuration;
using Vintagestory.API.Common;

namespace DivineAscension.Services;

/// <summary>
///     Centralized logging service with configurable log levels.
///     Wraps ILogger to provide runtime control over logging output.
/// </summary>
public class LoggingService
{
    /// <summary>
    ///     True only when the mod is compiled in a Debug configuration. In Release builds the logger
    ///     wrappers suppress the noisy levels (Debug/Notification/Event/Build/Chat) regardless of the
    ///     ConfigLib toggles, so a shipped install stays quiet. Warning and Error always evaluate (in
    ///     both builds) so a production server keeps a repro trail; they still honor their config
    ///     toggles. In Debug builds every level's config toggle applies normally.
    /// </summary>
    internal static readonly bool DebugBuild =
#if DEBUG
        true;
#else
        false;
#endif

    private static readonly Lazy<LoggingService> _instance = new(() => new LoggingService());

    private ILogger? _logger;
    private LoggingConfig _config = new();

    private LoggingService()
    {
    }

    public static LoggingService Instance => _instance.Value;

    /// <summary>
    ///     Initialize the logging service with an ILogger instance.
    /// </summary>
    public void Initialize(ILogger logger, LoggingConfig? config = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? new LoggingConfig();
    }

    /// <summary>
    ///     Update the logging configuration at runtime.
    /// </summary>
    public void UpdateConfig(LoggingConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    ///     The current logging configuration. Wrappers read this live on every call, so config
    ///     changes (or a re-Initialize from the other game side) take effect immediately.
    /// </summary>
    internal LoggingConfig CurrentConfig => _config;

    /// <summary>
    ///     Applies new logging settings to the live configuration, mutating the current config
    ///     instance in place so loggers already handed out reflect the change without a restart.
    /// </summary>
    public void ApplyConfig(LoggingConfig source)
    {
        _config.CopyFrom(source ?? throw new ArgumentNullException(nameof(source)));
    }

    /// <summary>
    ///     Create a logger wrapper for a specific category.
    /// </summary>
    public ILoggerWrapper CreateLogger(string category)
    {
        if (_logger == null)
        {
            throw new InvalidOperationException("LoggingService must be initialized before creating loggers");
        }

        return new LoggerWrapper(this, _logger, category);
    }

    /// <summary>
    ///     Get the underlying ILogger (for cases where direct access is needed).
    /// </summary>
    public ILogger GetUnderlyingLogger()
    {
        return _logger ?? throw new InvalidOperationException("LoggingService not initialized");
    }
}

/// <summary>
///     Interface for wrapped loggers (allows easy testing/mocking).
/// </summary>
public interface ILoggerWrapper
{
    void Debug(string message);
    void Notification(string message);
    void Warning(string message);
    void Error(string message);
    void Event(string message);
    void Build(string message);
    void Chat(string message);
}

/// <summary>
///     Logger wrapper that respects LoggingConfig settings.
/// </summary>
internal class LoggerWrapper : ILoggerWrapper
{
    private readonly LoggingService _service;
    private readonly ILogger _logger;
    private readonly string _category;

    public LoggerWrapper(LoggingService service, ILogger logger, string category)
    {
        _service = service;
        _logger = logger;
        _category = category;
    }

    // Read the config LIVE on every call instead of snapshotting it at construction. The service
    // is a process-wide singleton that both the client and server ModSystem initialize, and runtime
    // config changes mutate it — a captured reference would go stale and ignore those updates.
    private LoggingConfig Config => _service.CurrentConfig;

    // In Release the noisy levels (Debug/Notification/Event/Build/Chat) are build-gated off
    // regardless of the ConfigLib toggles, keeping a shipped install quiet. Warning and Error are
    // NOT build-gated — they always evaluate so a production server keeps a repro trail — but they
    // still honor their config toggles. In Debug builds every level's config toggle applies.

    public void Debug(string message)
    {
        if (!LoggingService.DebugBuild) return;
        var config = Config;
        if (!config.EnableDebug) return;
        if (IsFilteredOut(config)) return;
        _logger.Debug(message);
    }

    public void Notification(string message)
    {
        if (!LoggingService.DebugBuild) return;
        var config = Config;
        if (!config.EnableNotification) return;
        if (IsFilteredOut(config)) return;
        _logger.Notification(message);
    }

    public void Warning(string message)
    {
        var config = Config;
        if (!config.EnableWarning) return;
        if (IsFilteredOut(config)) return;
        _logger.Warning(message);
    }

    public void Error(string message)
    {
        var config = Config;
        if (!config.EnableError) return;
        if (IsFilteredOut(config)) return;
        _logger.Error(message);
    }

    public void Event(string message)
    {
        if (!LoggingService.DebugBuild) return;
        var config = Config;
        if (!config.EnableEvent) return;
        if (IsFilteredOut(config)) return;
        _logger.Event(message);
    }

    public void Build(string message)
    {
        if (!LoggingService.DebugBuild) return;
        var config = Config;
        if (!config.EnableBuild) return;
        if (IsFilteredOut(config)) return;
        _logger.Build(message);
    }

    public void Chat(string message)
    {
        if (!LoggingService.DebugBuild) return;
        var config = Config;
        if (!config.EnableChat) return;
        if (IsFilteredOut(config)) return;
        _logger.Chat(message);
    }

    private bool IsFilteredOut(LoggingConfig config)
    {
        // Check if this category is in the excluded categories list
        if (config.ExcludedCategories.Contains(_category))
        {
            return true;
        }

        // Check if this category is in the included categories list (if list is not empty)
        if (config.IncludedCategories.Count > 0 && !config.IncludedCategories.Contains(_category))
        {
            return true;
        }

        return false;
    }
}
