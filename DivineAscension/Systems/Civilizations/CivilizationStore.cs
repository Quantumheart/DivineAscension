using System;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Services;

namespace DivineAscension.Systems.Civilizations;

/// <summary>
///     Persistence boundary for civilization world data. Owns the save key and the
///     legacy capital-name backfill performed on load.
/// </summary>
internal sealed class CivilizationStore
{
    private const string DATA_KEY = "divineascension_civilizations";
    private readonly ILoggerWrapper _logger;
    private readonly IPersistenceService _persistenceService;

    public CivilizationStore(IPersistenceService persistenceService, ILoggerWrapper logger)
    {
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Loads persisted data, repairing legacy saves with empty capital names.
    ///     Never throws — returns a fresh container on any failure.
    /// </summary>
    public CivilizationWorldData Load()
    {
        try
        {
            var loadedData = _persistenceService.Load<CivilizationWorldData>(DATA_KEY);
            if (loadedData != null)
            {
                foreach (var civ in loadedData.Civilizations.Values)
                {
                    if (string.IsNullOrEmpty(civ.CapitalName))
                        civ.CapitalName = $"{civ.Name} Seat";
                }

                _logger.Notification($"[DivineAscension] Loaded {loadedData.Civilizations.Count} civilizations");
                return loadedData;
            }

            _logger.Debug("[DivineAscension] No civilization data found, starting fresh");
            return new CivilizationWorldData();
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Failed to load civilizations: {ex.Message}");
            return new CivilizationWorldData();
        }
    }

    public void Save(CivilizationWorldData data)
    {
        try
        {
            _persistenceService.Save(DATA_KEY, data);
            _logger.Debug($"[DivineAscension] Saved {data.Civilizations.Count} civilizations");
        }
        catch (Exception ex)
        {
            _logger.Error($"[DivineAscension] Failed to save civilizations: {ex.Message}");
        }
    }
}
