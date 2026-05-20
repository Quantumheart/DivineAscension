using System;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.State;

/// <summary>
///     One row in the persistent notification history. Pushed by
///     <c>NotificationManager.QueueRankUpNotification</c> at the moment a toast
///     is queued. The right-rail feed renders these.
/// </summary>
public record NotificationHistoryEntry(
    NotificationType Type,
    string Title,
    string Body,
    DeityDomain Deity,
    DateTime Timestamp,
    bool Read = false
);
