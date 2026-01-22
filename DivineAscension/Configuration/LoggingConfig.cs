using System.Collections.Generic;

namespace DivineAscension.Configuration;

/// <summary>
///     Configuration for Divine Ascension logging behavior.
///     Allows granular control over log levels and categories.
/// </summary>
public class LoggingConfig
{
    /// <summary>
    ///     Enable/disable Debug level logs (most verbose).
    ///     Default: true
    /// </summary>
    public bool EnableDebug { get; set; } = true;

    /// <summary>
    ///     Enable/disable Notification level logs.
    ///     Default: true
    /// </summary>
    public bool EnableNotification { get; set; } = true;

    /// <summary>
    ///     Enable/disable Warning level logs.
    ///     Default: true
    /// </summary>
    public bool EnableWarning { get; set; } = true;

    /// <summary>
    ///     Enable/disable Error level logs.
    ///     Default: true (strongly recommended to keep enabled)
    /// </summary>
    public bool EnableError { get; set; } = true;

    /// <summary>
    ///     Enable/disable Event level logs.
    ///     Default: true
    /// </summary>
    public bool EnableEvent { get; set; } = true;

    /// <summary>
    ///     Enable/disable Build level logs.
    ///     Default: true
    /// </summary>
    public bool EnableBuild { get; set; } = true;

    /// <summary>
    ///     Enable/disable Chat level logs.
    ///     Default: true
    /// </summary>
    public bool EnableChat { get; set; } = true;

    /// <summary>
    ///     List of categories to exclude from logging (blacklist).
    ///     If a category is in this list, it will be filtered out.
    ///     Examples: "ReligionManager", "FavorSystem", "GUI"
    ///     Default: empty (no exclusions)
    /// </summary>
    public HashSet<string> ExcludedCategories { get; set; } = new();

    /// <summary>
    ///     List of categories to include in logging (whitelist).
    ///     If this list is not empty, ONLY categories in this list will be logged.
    ///     Examples: "ReligionManager", "CivilizationManager"
    ///     Default: empty (all categories allowed)
    /// </summary>
    public HashSet<string> IncludedCategories { get; set; } = new();

    /// <summary>
    ///     Preset: Disable ALL logging (complete silence).
    /// </summary>
    public static LoggingConfig Silent()
    {
        return new LoggingConfig
        {
            EnableDebug = false,
            EnableNotification = false,
            EnableWarning = false,
            EnableError = false,
            EnableEvent = false,
            EnableBuild = false,
            EnableChat = false
        };
    }

    /// <summary>
    ///     Preset: Only errors and warnings (minimal logging).
    /// </summary>
    public static LoggingConfig ErrorsOnly()
    {
        return new LoggingConfig
        {
            EnableDebug = false,
            EnableNotification = false,
            EnableWarning = true,
            EnableError = true,
            EnableEvent = false,
            EnableBuild = false,
            EnableChat = false
        };
    }

    /// <summary>
    ///     Preset: Everything except debug logs (reduced verbosity).
    /// </summary>
    public static LoggingConfig NoDebug()
    {
        return new LoggingConfig
        {
            EnableDebug = false,
            EnableNotification = true,
            EnableWarning = true,
            EnableError = true,
            EnableEvent = true,
            EnableBuild = true,
            EnableChat = true
        };
    }

    /// <summary>
    ///     Preset: Default configuration (all logging enabled).
    /// </summary>
    public static LoggingConfig Default()
    {
        return new LoggingConfig();
    }
}
