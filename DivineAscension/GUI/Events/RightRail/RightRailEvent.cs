namespace DivineAscension.GUI.Events.RightRail;

/// <summary>
///     Events emitted by <c>RightRailRenderer</c> in response to user interaction
///     with the notification feed. <c>MainLayoutCoordinator</c> applies these
///     against <c>NotificationManager</c> and <c>RightRailState</c>.
/// </summary>
public abstract record RightRailEvent
{
    /// <summary>User clicked a notification row to mark it read.</summary>
    public sealed record MarkNotificationRead(int Index) : RightRailEvent;

    /// <summary>User clicked the "clear all" affordance in the feed header.</summary>
    public sealed record ClearNotificationHistory : RightRailEvent;

    /// <summary>User toggled the "unread only" filter in the feed header.</summary>
    public sealed record SetUnreadOnly(bool Enabled) : RightRailEvent;
}
