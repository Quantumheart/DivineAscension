using System.Collections.Generic;
using System.Linq;
using ProtoBuf;

namespace DivineAscension.Data;

public partial class ReligionData
{
    /// <summary>
    ///     Backing field for activity log (serialized)
    /// </summary>
    [ProtoMember(19)]
    private List<ActivityLogEntry> _activityLog = new();

    /// <summary>
    ///     Recent activity log entries (last 100 entries, FIFO).
    ///     Returns a thread-safe snapshot copy.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyList<ActivityLogEntry> ActivityLog
    {
        get
        {
            lock (Lock)
            {
                return _activityLog.ToList();
            }
        }
    }

    /// <summary>
    ///     Adds an activity log entry (thread-safe).
    ///     Maintains FIFO with max entries limit.
    /// </summary>
    public void AddActivityEntry(ActivityLogEntry entry, int maxEntries = 100)
    {
        lock (Lock)
        {
            _activityLog.Insert(0, entry);
            if (_activityLog.Count > maxEntries)
            {
                _activityLog.RemoveRange(maxEntries, _activityLog.Count - maxEntries);
            }
        }
    }

    /// <summary>
    ///     Gets recent activity entries (thread-safe).
    /// </summary>
    public List<ActivityLogEntry> GetRecentActivity(int limit)
    {
        lock (Lock)
        {
            return _activityLog.Take(limit).ToList();
        }
    }

    /// <summary>
    ///     Clears the activity log.
    ///     Thread-safe.
    /// </summary>
    public void ClearActivityLog()
    {
        lock (Lock)
        {
            _activityLog.Clear();
        }
    }
}
