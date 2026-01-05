using System;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.State;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.Managers;

public class NotificationManager(ISoundManager soundManager) : INotificationManager
{
    private const int MaxQueueSize = 5;
    private Action? _showImGuiCallback;

    /// <summary>
    /// Exposes the current notification State for rendering.
    /// </summary>
    public NotificationState State { get; } = new();

    /// <summary>
    /// Sets the callback to trigger ImGui showing when notifications are queued.
    /// </summary>
    public void SetShowImGuiCallback(Action showCallback)
    {
        _showImGuiCallback = showCallback;
    }

    /// <summary>
    /// Queues a rank-up notification. If no notification is currently visible, shows it immediately.
    /// </summary>
    public void QueueRankUpNotification(NotificationType type, string rankName, string rankDescription, DeityType deity)
    {
        var notification = new PendingNotification(type, rankName, rankDescription, deity);

        // If queue is at max size, remove the oldest notification
        if (State.PendingNotifications.Count >= MaxQueueSize)
        {
            State.PendingNotifications.Dequeue();
        }

        State.PendingNotifications.Enqueue(notification);

        // Show immediately if no active notification
        if (!State.IsVisible)
        {
            ShowNextNotification();
        }
    }

    /// <summary>
    /// Shows the next pending notification from the queue.
    /// </summary>
    public void ShowNextNotification()
    {
        if (State.PendingNotifications.Count == 0)
        {
            return;
        }

        var notification = State.PendingNotifications.Dequeue();


        State.Type = notification.Type;
        State.RankName = notification.RankName;
        State.RankDescription = notification.RankDescription;
        State.DeityType = notification.Deity;
        State.ElapsedTime = 0f;
        State.IsVisible = true;

        // Trigger ImGui to show so notification can be rendered
        _showImGuiCallback?.Invoke();

        // Play deity-specific sound
        soundManager.PlayDeityUnlock(notification.Deity);
    }

    /// <summary>
    /// Updates the notification timer and auto-dismisses after the display duration.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!State.IsVisible)
        {
            return;
        }

        State.ElapsedTime += deltaTime;

        if (State.ElapsedTime >= State.DisplayDuration)
        {
            DismissCurrentNotification();
        }
    }

    /// <summary>
    /// Dismisses the current notification and shows the next one if available.
    /// </summary>
    public void DismissCurrentNotification()
    {
        State.IsVisible = false;
        State.ElapsedTime = 0f;

        // Show next notification if available
        if (State.PendingNotifications.Count > 0)
        {
            ShowNextNotification();
        }
    }

    /// <summary>
    /// Handles the "View Blessings" button click.
    /// </summary>
    /// <param name="openCallback">Callback to open the main dialog if it's closed</param>
    /// <param name="setTabCallback">Callback to set the active tab to Blessings</param>
    public void OnViewBlessingsClicked(Action openCallback, Action setTabCallback)
    {
        DismissCurrentNotification();
        openCallback();
        setTabCallback();
    }

    /// <summary>
    /// Gets the description for a rank based on the notification type and rank name.
    /// Note: This is a placeholder. Tasks 1.3 and 1.4 will create the utility classes.
    /// </summary>
    private string GetRankDescription(NotificationType type, string rankName)
    {
        // This will be implemented when FavorRankDescriptions and PrestigeRankDescriptions utilities are created
        // For now, return a placeholder or the rank name itself
        return type switch
        {
            NotificationType.FavorRankUp => GetFavorRankDescription(rankName),
            NotificationType.PrestigeRankUp => GetPrestigeRankDescription(rankName),
            _ => rankName
        };
    }

    private string GetFavorRankDescription(string rankName)
    {
        // Placeholder until Task 1.3 is complete
        return Enum.TryParse<FavorRank>(rankName, out var rank)
            ? rank switch
            {
                FavorRank.Initiate => "You have begun your spiritual journey.",
                FavorRank.Disciple => "Your devotion grows stronger.",
                FavorRank.Zealot => "Your faith burns with intensity.",
                FavorRank.Champion => "You are a paragon of your deity.",
                FavorRank.Avatar => "You embody the divine will.",
                _ => rankName
            }
            : rankName;
    }

    private string GetPrestigeRankDescription(string rankName)
    {
        // Placeholder until Task 1.4 is complete
        return Enum.TryParse<PrestigeRank>(rankName, out var rank)
            ? rank switch
            {
                PrestigeRank.Fledgling => "Your religion takes its first steps.",
                PrestigeRank.Established => "Your religion gains recognition.",
                PrestigeRank.Renowned => "Your religion is widely known.",
                PrestigeRank.Legendary => "Your religion is legendary.",
                PrestigeRank.Mythic => "Your religion has achieved mythic status.",
                _ => rankName
            }
            : rankName;
    }
}

public interface INotificationManager
{
    NotificationState State { get; }
    void SetShowImGuiCallback(Action showCallback);
    void QueueRankUpNotification(NotificationType type, string rankName, string rankDescription, DeityType deity);
    void ShowNextNotification();
    void Update(float deltaTime);
    void DismissCurrentNotification();
    void OnViewBlessingsClicked(Action openCallback, Action setTabCallback);
}