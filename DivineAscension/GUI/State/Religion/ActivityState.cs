using System.Collections.Generic;

namespace DivineAscension.GUI.State.Religion;

public class ActivityState
{
    public List<string> ActivityLog { get; set; } = new(); // Future: activity events
    public float ActivityScrollY { get; set; }

    public void Reset()
    {
        ActivityLog.Clear();
        ActivityScrollY = 0f;
    }
}