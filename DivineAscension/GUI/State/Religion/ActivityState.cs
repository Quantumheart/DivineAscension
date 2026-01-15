using System;
using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.State.Religion;

public class ActivityState
{
    public List<ActivityLogResponsePacket.ActivityEntry> ActivityEntries { get; set; } = new();
    public float ActivityScrollY { get; set; }
    public bool IsLoading { get; set; } = false;
    public string? ErrorMessage { get; set; }
    public DateTime LastRefresh { get; set; } = DateTime.MinValue;

    public void Reset()
    {
        ActivityEntries.Clear();
        ActivityScrollY = 0f;
        IsLoading = true;
        ErrorMessage = null;
        LastRefresh = DateTime.MinValue;
    }

    public void UpdateEntries(List<ActivityLogResponsePacket.ActivityEntry> entries)
    {
        ActivityEntries = entries;
        IsLoading = false;
        LastRefresh = DateTime.UtcNow;
    }
}