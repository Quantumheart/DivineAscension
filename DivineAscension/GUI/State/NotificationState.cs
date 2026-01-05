using System.Collections.Generic;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.State;

public class NotificationState
{
    public bool IsVisible { get; set; }
    public NotificationType Type { get; set; }
    public string RankName { get; set; } = string.Empty;
    public string RankDescription { get; set; } = string.Empty;
    public DeityType DeityType { get; set; }
    public float DisplayDuration { get; set; } = 8f;
    public float ElapsedTime { get; set; }
    public Queue<PendingNotification> PendingNotifications { get; set; } = new();

    public void Reset()
    {
        IsVisible = false;
        Type = NotificationType.None;
        RankName = string.Empty;
        RankDescription = string.Empty;
        DeityType = DeityType.None;
        DisplayDuration = 8f;
        ElapsedTime = 0f;
        PendingNotifications.Clear();
    }
}

public record PendingNotification(
    NotificationType Type,
    string RankName,
    string RankDescription,
    DeityType Deity
);