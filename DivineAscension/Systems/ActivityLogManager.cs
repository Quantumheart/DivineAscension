using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;
using DivineAscension.Data;
using DivineAscension.Models.Enum;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;

namespace DivineAscension.Systems;

/// <summary>
///     Manages religion activity logs with bounded storage and efficient querying
/// </summary>
public class ActivityLogManager : IActivityLogManager
{
    private const int MAX_ENTRIES_PER_RELIGION = 100;
    private readonly ILogger _logger;
    private readonly IReligionManager _religionManager;
    private readonly IWorldService _worldService;

    public ActivityLogManager(ILogger logger, IWorldService worldService, IReligionManager religionManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
    }

    /// <summary>
    ///     Initializes the activity log manager and subscribes to cleanup events
    /// </summary>
    public void Initialize()
    {
        // Subscribe to religion deletion for cleanup
        _religionManager.OnReligionDeleted += OnReligionDeleted;

        _logger.Notification("[DivineAscension] ActivityLogManager initialized");
    }

    /// <summary>
    ///     Logs a favor/prestige award to the religion's activity feed.
    ///     Thread-safe with bounded storage (FIFO eviction).
    /// </summary>
    public void LogActivity(string religionUID, string playerUID, string actionType,
        int favorAmount, int prestigeAmount, DeityDomain domain)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
        {
            _logger.Debug(
                $"[ActivityLogManager] Cannot log activity - religion {religionUID} not found");
            return;
        }

        // Get player name (cache for display)
        var player = _worldService.GetPlayerByUID(playerUID);
        var playerName = player?.PlayerName ?? playerUID;

        var entry = new ActivityLogEntry(
            playerUID,
            playerName,
            actionType,
            favorAmount,
            prestigeAmount,
            domain.ToString()
        );

        // Add entry with FIFO eviction (thread-safe)
        religion.AddActivityEntry(entry, MAX_ENTRIES_PER_RELIGION);

        // Trigger save (batched by existing autosave system)
        _religionManager.TriggerSave();

        _logger.Debug(
            $"[ActivityLogManager] Logged activity: {playerName} - {actionType} (+{favorAmount} favor, +{prestigeAmount} prestige) for religion {religion.ReligionName}");
    }

    /// <summary>
    ///     Gets recent activity entries for a religion (newest first)
    /// </summary>
    public List<ActivityLogEntry> GetActivityLog(string religionUID, int limit = 50)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null)
        {
            _logger.Debug(
                $"[ActivityLogManager] Cannot get activity log - religion {religionUID} not found");
            return new List<ActivityLogEntry>();
        }

        return religion.ActivityLog.Take(limit).ToList();
    }

    /// <summary>
    ///     Clears activity log for a religion (called on deletion)
    /// </summary>
    public void ClearActivityLog(string religionUID)
    {
        var religion = _religionManager.GetReligion(religionUID);
        if (religion == null) return;

        religion.ClearActivityLog();
        _religionManager.TriggerSave();

        _logger.Debug(
            $"[ActivityLogManager] Cleared activity log for religion {religion.ReligionName}");
    }

    /// <summary>
    ///     Handles religion deletion event to clean up activity logs
    /// </summary>
    private void OnReligionDeleted(string religionUID)
    {
        ClearActivityLog(religionUID);
    }
}