using System.Collections.Generic;
using DivineAscension.GUI.Models.Religion.Header;
using DivineAscension.GUI.State;

namespace DivineAscension.GUI.Models.PlayerInfo;

/// <summary>
///     "You" content page composition: religion + civilization status (via
///     <see cref="ReligionHeaderViewModel" />) plus the notification feed.
/// </summary>
public sealed record PlayerInfoViewModel(
    ReligionHeaderViewModel Header,
    IReadOnlyList<NotificationHistoryEntry> Notifications,
    bool ShowUnreadOnly,
    float ScrollY,
    float X,
    float Y,
    float Width,
    float Height
);
