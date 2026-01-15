using System.Collections.Generic;
using DivineAscension.Data;
using DivineAscension.Models.Enum;

namespace DivineAscension.Systems.Interfaces;

/// <summary>
///     Interface for managing religion activity logs with bounded storage and efficient querying
/// </summary>
public interface IActivityLogManager
{
    /// <summary>
    ///     Logs a favor/prestige award to the religion's activity feed
    /// </summary>
    void LogActivity(string religionUID, string playerUID, string actionType,
        int favorAmount, int prestigeAmount, DeityDomain domain);

    /// <summary>
    ///     Gets recent activity entries for a religion (newest first)
    /// </summary>
    List<ActivityLogEntry> GetActivityLog(string religionUID, int limit = 50);

    /// <summary>
    ///     Clears activity log for a religion (called on deletion)
    /// </summary>
    void ClearActivityLog(string religionUID);

    /// <summary>
    ///     Initializes the activity log manager and subscribes to cleanup events
    /// </summary>
    void Initialize();
}