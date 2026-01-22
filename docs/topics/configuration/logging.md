# Logging Configuration Guide

This guide explains how to control Divine Ascension's logging output using the new `LoggingService`.

## Overview

Divine Ascension has extensive logging (~550+ log statements) for debugging and monitoring. The `LoggingService` provides centralized control over what gets logged without modifying individual log statements.

## Quick Start

### 1. Disable ALL Logging (Complete Silence)

```csharp
// In DivineAscensionModSystem.Start()
LoggingService.Instance.Initialize(api.Logger, LoggingConfig.Silent());
```

### 2. Only Log Errors and Warnings

```csharp
LoggingService.Instance.Initialize(api.Logger, LoggingConfig.ErrorsOnly());
```

### 3. Disable Debug Logs Only (Recommended)

```csharp
LoggingService.Instance.Initialize(api.Logger, LoggingConfig.NoDebug());
```

### 4. Custom Configuration

```csharp
var config = new LoggingConfig
{
    EnableDebug = false,           // Disable verbose debug logs
    EnableNotification = true,     // Keep startup/major events
    EnableWarning = true,          // Keep warnings
    EnableError = true,            // Always keep errors (recommended)
    ExcludedCategories = new HashSet<string> { "FavorSystem", "GUI" } // Silence specific systems
};

LoggingService.Instance.Initialize(api.Logger, config);
```

## Integration Steps

### Phase 1: Initialize LoggingService (No Code Changes Required)

**File:** `DivineAscension/DivineAscensionModSystem.cs`

Add to `Start()` method:

```csharp
public override void Start(ICoreAPI api)
{
    base.Start(api);

    // Initialize logging service FIRST (before any logging occurs)
    var loggingConfig = LoggingConfig.NoDebug(); // or LoggingConfig.Silent()
    LoggingService.Instance.Initialize(api.Logger, loggingConfig);

    api.Logger.Notification("[DivineAscension] Mod loaded!");
    // ... rest of Start() method
}
```

**Benefits of Phase 1:**
- ✅ LoggingService is initialized and ready
- ✅ Can be configured via code
- ⚠️ Existing `_logger` and `api.Logger` calls still bypass the service

### Phase 2: Migrate Systems to Use LoggingService (Gradual Refactor)

Replace `ILogger` constructor parameters with `ILoggerWrapper`:

**Before:**
```csharp
public class ReligionManager
{
    private readonly ILogger _logger;

    public ReligionManager(ILogger logger, ...)
    {
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Religion Manager...");
        _logger.Debug($"[DivineAscension] Loaded {count} religions");
    }
}
```

**After:**
```csharp
public class ReligionManager
{
    private readonly ILoggerWrapper _logger;

    public ReligionManager(ILoggerWrapper logger, ...)
    {
        _logger = logger;
    }

    public void Initialize()
    {
        _logger.Notification("[DivineAscension] Initializing Religion Manager...");
        _logger.Debug($"[DivineAscension] Loaded {count} religions");
    }
}
```

**Update initialization in `DivineAscensionSystemInitializer.cs`:**

```csharp
// Create wrapped logger for each manager
var religionLogger = LoggingService.Instance.CreateLogger("ReligionManager");
var favorLogger = LoggingService.Instance.CreateLogger("FavorSystem");
var guiLogger = LoggingService.Instance.CreateLogger("GUI");

var religionManager = new ReligionManager(
    religionLogger,  // ILoggerWrapper instead of ILogger
    eventService,
    persistenceService,
    worldService
);

var favorSystem = new FavorSystem(
    favorLogger,
    eventService,
    worldService,
    // ...
);
```

**Benefits of Phase 2:**
- ✅ Full logging control at category level
- ✅ Zero runtime overhead when logging is disabled
- ✅ Can filter by system (e.g., silence GUI but keep ReligionManager)
- ⚠️ Requires changing ~50 constructor signatures

### Phase 3: Add ConfigLib Integration (Optional)

Make logging configurable via in-game GUI:

**File:** `DivineAscension/Configuration/GameBalanceConfig.cs`

```csharp
public class GameBalanceConfig
{
    // ... existing config fields ...

    [Category("Logging")]
    [Description("Enable debug logs (verbose, may impact performance)")]
    public bool EnableDebugLogs { get; set; } = false;

    [Category("Logging")]
    [Description("Enable notification logs (startup, major events)")]
    public bool EnableNotificationLogs { get; set; } = true;

    [Category("Logging")]
    [Description("Enable warning logs")]
    public bool EnableWarningLogs { get; set; } = true;

    [Category("Logging")]
    [Description("Enable error logs (highly recommended to keep enabled)")]
    public bool EnableErrorLogs { get; set; } = true;
}
```

**Update `OnConfigChanged()` in `DivineAscensionModSystem.cs`:**

```csharp
private void OnConfigChanged(string settingCode)
{
    try
    {
        _gameBalanceConfig.Validate();

        // Update logging configuration dynamically
        if (settingCode.StartsWith("Enable") && settingCode.EndsWith("Logs"))
        {
            var loggingConfig = new LoggingConfig
            {
                EnableDebug = _gameBalanceConfig.EnableDebugLogs,
                EnableNotification = _gameBalanceConfig.EnableNotificationLogs,
                EnableWarning = _gameBalanceConfig.EnableWarningLogs,
                EnableError = _gameBalanceConfig.EnableErrorLogs
            };

            LoggingService.Instance.UpdateConfig(loggingConfig);
            _sapi?.Logger.Notification($"[DivineAscension] Logging configuration updated");
        }

        _sapi?.Logger.Notification($"[DivineAscension] Configuration updated: {settingCode}");
    }
    catch (Exception ex)
    {
        _sapi?.Logger.Error($"[DivineAscension] Config validation failed after update: {ex.Message}");
    }
}
```

**Benefits of Phase 3:**
- ✅ In-game configuration via ConfigLib GUI
- ✅ Runtime changes without restart
- ✅ Per-world settings
- ⚠️ Requires ConfigLib mod installed

## Configuration Examples

### Example 1: Production Server (Minimal Logging)

```csharp
var config = new LoggingConfig
{
    EnableDebug = false,       // No verbose logs
    EnableNotification = false, // No startup spam
    EnableWarning = true,      // Keep warnings
    EnableError = true         // Always log errors
};
```

### Example 2: Debug Specific System

```csharp
var config = new LoggingConfig
{
    EnableDebug = true,
    IncludedCategories = new HashSet<string> { "ReligionManager", "HolySiteManager" }
    // Only these two systems will produce debug logs
};
```

### Example 3: Silence Noisy Systems

```csharp
var config = new LoggingConfig
{
    EnableDebug = true,
    ExcludedCategories = new HashSet<string> { "GUI", "FavorSystem", "NetworkHandler" }
    // Everything except these will log
};
```

## Performance Impact

### Without LoggingService
- All log statements execute string formatting and I/O
- ~550 log statements active every session
- Debug logs can spam console (especially favor tracking)

### With LoggingService (Logging Disabled)
- Zero overhead: early return before string formatting
- No I/O operations
- No string allocations
- Estimated performance gain: 2-5% CPU reduction on busy servers

### Benchmarks (Estimated)

| Configuration | Log Statements/sec | CPU Impact | Log File Growth |
|--------------|-------------------|------------|-----------------|
| All Enabled | ~50-200 | Moderate | ~1-5 MB/hour |
| NoDebug | ~10-30 | Low | ~100-500 KB/hour |
| ErrorsOnly | ~0-5 | Minimal | ~10-50 KB/hour |
| Silent | 0 | None | 0 |

## Migration Checklist

- [ ] Phase 1: Initialize `LoggingService` in `DivineAscensionModSystem.Start()`
- [ ] Choose initial configuration (`Silent()`, `NoDebug()`, etc.)
- [ ] Test that mod still loads and functions
- [ ] (Optional) Phase 2: Migrate high-volume systems first (FavorSystem, GUI)
- [ ] (Optional) Phase 2: Migrate remaining systems
- [ ] (Optional) Phase 3: Add ConfigLib integration for in-game config

## Category Names Reference

Common category names for filtering:

| Category | Description | Typical Volume |
|----------|-------------|----------------|
| `ReligionManager` | Religion CRUD operations | Medium |
| `FavorSystem` | Favor tracking and rewards | **Very High** |
| `FavorTracker` | Individual favor trackers | **Very High** |
| `BlessingEffectSystem` | Blessing stat modifiers | Medium |
| `HolySiteManager` | Holy site management | Low-Medium |
| `CivilizationManager` | Civilization operations | Low |
| `GUI` | ImGui UI rendering | **Very High** |
| `NetworkHandler` | Network packet handling | High |
| `AltarPrayerHandler` | Prayer/offering events | Medium |
| `RitualProgressManager` | Ritual tracking | Low-Medium |

**Recommendation:** Start by excluding `FavorSystem`, `FavorTracker`, and `GUI` categories to see the biggest reduction in log spam.

## Troubleshooting

### Q: Logging still occurs after setting `Silent()`

**A:** Phase 1 only initializes the service. Existing code still uses `ILogger` directly. You need to either:
1. Wait for Phase 2 migration, or
2. Temporarily wrap the base `api.Logger` (advanced, requires Vintage Story API knowledge)

### Q: How to completely disable logging without code changes?

**A:** Not possible without Phase 2 migration. The cleanest approach is:
1. Set `LoggingConfig.Silent()` in Phase 1
2. Gradually migrate high-volume systems in Phase 2

### Q: Can I disable logging for specific methods?

**A:** Not directly. Filtering is per-category (class-level). You could:
1. Create more granular categories (e.g., `ReligionManager.CreateReligion`)
2. Use conditional compilation (`#if DEBUG`)
3. Manually comment out specific log statements

### Q: Does this affect Vintage Story's own logging?

**A:** No. This only affects Divine Ascension's log statements. Vintage Story's core logging is unaffected.

## Best Practices

1. **Always keep error logging enabled** - Critical for troubleshooting
2. **Disable debug logs in production** - Reduces noise and improves performance
3. **Use category filtering for debugging** - More precise than disabling all logs
4. **Test configuration changes** - Ensure critical logs still appear when needed
5. **Document your configuration** - Help other admins understand logging settings

## Future Enhancements

Potential improvements for future versions:

- [ ] Per-player logging levels (debug mode for specific admins)
- [ ] Log level inheritance (e.g., "FavorSystem.*" matches all favor trackers)
- [ ] Regex category patterns
- [ ] Log message sampling (e.g., log only 1 in 10 debug messages)
- [ ] Performance metrics (track time spent in logging code)
- [ ] Chat command to change logging at runtime (`/da logging set debug off`)
