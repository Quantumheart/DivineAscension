using System.Collections.Generic;
using DivineAscension.GUI.Models.Religion.Header;
using DivineAscension.GUI.State;

namespace DivineAscension.GUI.UI.Renderers.RightRail;

/// <summary>
///     Right-rail composition: religion + civilization status (via
///     <see cref="ReligionHeaderViewModel" />) plus the notification feed.
/// </summary>
public sealed record RightRailViewModel(
    ReligionHeaderViewModel Header,
    IReadOnlyList<NotificationHistoryEntry> Notifications,
    bool ShowUnreadOnly
);
