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
    ///     Create a logger wrapper for a specific category.
    /// </summary>
    public ILoggerWrapper CreateLogger(string category)
    {
        if (_logger == null)
        {
            throw new InvalidOperationException("LoggingService must be initialized before creating loggers");
        }

        return new LoggerWrapper(_logger, _config, category);
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
    private readonly ILogger _logger;
    private readonly LoggingConfig _config;
    private readonly string _category;

    public LoggerWrapper(ILogger logger, LoggingConfig config, string category)
    {
        _logger = logger;
        _config = config;
        _category = category;
    }

    public void Debug(string message)
    {
        if (!_config.EnableDebug) return;
        if (IsFilteredOut()) return;
        _logger.Debug(message);
    }

    public void Notification(string message)
    {
        if (!_config.EnableNotification) return;
        if (IsFilteredOut()) return;
        _logger.Notification(message);
    }

    public void Warning(string message)
    {
        if (!_config.EnableWarning) return;
        if (IsFilteredOut()) return;
        _logger.Warning(message);
    }

    public void Error(string message)
    {
        if (!_config.EnableError) return;
        if (IsFilteredOut()) return;
        _logger.Error(message);
    }

    public void Event(string message)
    {
        if (!_config.EnableEvent) return;
        if (IsFilteredOut()) return;
        _logger.Event(message);
    }

    public void Build(string message)
    {
        if (!_config.EnableBuild) return;
        if (IsFilteredOut()) return;
        _logger.Build(message);
    }

    public void Chat(string message)
    {
        if (!_config.EnableChat) return;
        if (IsFilteredOut()) return;
        _logger.Chat(message);
    }

    private bool IsFilteredOut()
    {
        // Check if this category is in the excluded categories list
        if (_config.ExcludedCategories.Contains(_category))
        {
            return true;
        }

        // Check if this category is in the included categories list (if list is not empty)
        if (_config.IncludedCategories.Count > 0 && !_config.IncludedCategories.Contains(_category))
        {
            return true;
        }

        return false;
    }
}
