# Logging Configuration Guide

Divine Ascension routes its logging through a central `LoggingService` so server
admins can dial verbosity up or down without editing source. This guide covers
the admin-facing knob and the developer internals behind it.

## For server admins

### The logging toggles

Logging is controlled by four per-level checkboxes in the mod's
ConfigLib-managed config (`ModConfig/divineascension.yaml`):

| Setting                  | What it controls                         |
|--------------------------|------------------------------------------|
| `EnableDebugLogs`        | Verbose debug logs (highest volume)      |
| `EnableNotificationLogs` | Startup and major events                 |
| `EnableWarningLogs`      | Warnings                                 |
| `EnableErrorLogs`        | Errors                                   |

All default to `true`, so existing worlds and no-config setups keep full
logging. **Uncheck all four for complete silence.** Common combos:

- **Drop debug noise:** `EnableDebugLogs: false`, rest `true`.
- **Errors only:** only `EnableWarningLogs` + `EnableErrorLogs` `true`.
- **Silent:** all four `false`.

(The rarely-used Event/Build/Chat levels follow `EnableNotificationLogs`.)

### Changing it

- **With [ConfigLib](https://mods.vintagestory.at/configlib) installed:** toggle
  the checkboxes in the in-game config GUI, or edit
  `ModConfig/divineascension.yaml`. GUI changes apply **live** — no restart.
- **Without ConfigLib:** the mod uses the all-on defaults. ConfigLib owns the
  YAML file and GUI, so it's required to change the values.

```yaml
# ModConfig/divineascension.yaml — errors and warnings only
EnableDebugLogs: false
EnableNotificationLogs: false
EnableWarningLogs: true
EnableErrorLogs: true
```

> Note: a handful of bootstrap lines (mod loaded, Harmony patches, ConfigLib
> status) are emitted very early in startup, before the toggles have been read,
> and always print. Everything logged after initialization honors the toggles.

## How it works

`LoggingService` wraps Vintage Story's `ILogger`. Each system is handed an
`ILoggerWrapper` (created via `LoggingService.Instance.CreateLogger("Category")`)
that checks a shared `LoggingConfig` before forwarding to the underlying logger,
so disabled levels short-circuit before any string formatting or I/O.

`DivineAscensionModSystem.Start()` initializes the service with full logging (so
early bootstrap works), then calls
`LoggingService.Instance.ApplyConfig(_gameBalanceConfig.BuildLoggingConfig())`
once ConfigLib has populated the config. `ApplyConfig` **mutates the shared
`LoggingConfig` in place** rather than swapping the reference, so loggers already
handed out pick up the change — this is what makes the live update in
`OnConfigChanged` work.

> The toggles are plain booleans on purpose: ConfigLib renders them as
> checkboxes and round-trips them through YAML reliably. (An earlier enum-based
> "verbosity preset" field didn't get an editable control in the ConfigLib GUI,
> so live changes never reached the config.)

### Category filtering (code-level)

`LoggingConfig` also supports per-category whitelist/blacklist filtering via
`IncludedCategories` / `ExcludedCategories`. These are not yet surfaced in the
ConfigLib GUI (presets only); they're available to code and tests:

```csharp
var config = new LoggingConfig
{
    EnableDebug = true,
    ExcludedCategories = new HashSet<string> { "GUI", "FavorSystem" } // silence the noisiest
};
LoggingService.Instance.UpdateConfig(config);
```

## Performance

When a level is disabled the wrapper returns before formatting the message, so a
silenced log statement costs almost nothing (no string allocation, no I/O).

| Configuration | Log statements/sec | Log file growth |
|---------------|--------------------|-----------------|
| `Default`     | ~50–200            | ~1–5 MB/hour    |
| `NoDebug`     | ~10–30             | ~100–500 KB/hour|
| `ErrorsOnly`  | ~0–5               | ~10–50 KB/hour  |
| `Silent`      | 0                  | 0               |

## Category names reference

Useful category names for `IncludedCategories` / `ExcludedCategories` filtering:

| Category | Description | Typical volume |
|----------|-------------|----------------|
| `ReligionManager` | Religion CRUD operations | Medium |
| `FavorSystem` | Favor tracking and rewards | **Very high** |
| `BlessingEffectSystem` | Blessing stat modifiers | Medium |
| `HolySiteManager` | Holy site management | Low–medium |
| `CivilizationManager` | Civilization operations | Low |
| `ReligionNetworkHandler` | Religion network packets | High |
| `AltarPrayerHandler` | Prayer/offering events | Medium |
| `RitualProgressManager` | Ritual tracking | Low–medium |

## Troubleshooting

**Q: I unchecked everything but a few `[DivineAscension]` lines still print at startup.**
A: Expected. The earliest bootstrap lines log through the raw `api.Logger` before
the toggles are read; they're intentionally not gated. All category-logger output
past initialization is suppressed.

**Q: My toggle changes aren't taking effect.**
A: Confirm ConfigLib is installed — without it the mod always uses the all-on
defaults and there is no YAML/GUI to change. With ConfigLib, GUI checkbox changes
apply live; manual YAML edits apply on next load.

**Q: Does this affect Vintage Story's own logging?**
A: No — only Divine Ascension's log statements. VS core logging is unaffected.

## Best practices

1. Keep `EnableErrorLogs` on in production — errors are critical for triage.
2. Turn off `EnableDebugLogs` to cut the bulk of the noise while keeping
   startup/major events, warnings, and errors.
3. Reach for code-level category filtering when one subsystem is drowning out the
   rest.

## Future enhancements

- Surface the category whitelist/blacklist filters in the ConfigLib GUI (the GUI
  exposes the per-level toggles today; category filtering is code-only).
- Chat command to change levels at runtime (`/da logging set debug off`).
- Per-player debug mode for admins.
