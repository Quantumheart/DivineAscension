namespace DivineAscension.GUI.Events.PlayerInfo;

/// <summary>
///     Events emitted by <c>PlayerInfoRenderer</c> in response to user
///     interaction with the notification feed. <c>MainLayoutCoordinator</c>
///     applies these against <c>NotificationManager</c> and
///     <c>PlayerInfoState</c>.
/// </summary>
public abstract record PlayerInfoEvent
{
    /// <summary>User clicked a notification row to mark it read.</summary>
    public sealed record MarkNotificationRead(int Index) : PlayerInfoEvent;

    /// <summary>User clicked the "clear all" affordance in the feed header.</summary>
    public sealed record ClearNotificationHistory : PlayerInfoEvent;

    /// <summary>User toggled the "unread only" filter in the feed header.</summary>
    public sealed record SetUnreadOnly(bool Enabled) : PlayerInfoEvent;
}
