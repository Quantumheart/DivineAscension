using System.Collections.Generic;
using DivineAscension.GUI.State;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Network.Civilization;

namespace DivineAscension.GUI.UI.Renderers.RightRail;

/// <summary>
///     Data needed to render the right-rail religion + civilization status blocks
///     and the notification feed. Fields mirror what <c>ReligionHeaderViewModel</c>
///     already carries today (Phase 5b's expanded two-column dataset) so the rail
///     can stand in for the existing top banner once Phase 3b flips the layout.
/// </summary>
public sealed record RightRailViewModel(
    // Religion block --------------------------------------------------------
    bool HasReligion,
    string? CurrentReligionName,
    string? CurrentDeityName,
    DeityDomain CurrentDeity,
    int ReligionMemberCount,
    string? PlayerRoleInReligion,
    PlayerFavorProgress PlayerFavorProgress,
    ReligionPrestigeProgress ReligionPrestigeProgress,
    // Civilization block ---------------------------------------------------
    bool HasCivilization,
    string? CurrentCivilizationName,
    string? CivilizationIcon,
    int CivilizationRank,
    IReadOnlyList<CivilizationInfoResponsePacket.MemberReligion> CivilizationMemberReligions,
    bool IsCivilizationFounder,
    // Notification feed -----------------------------------------------------
    IReadOnlyList<NotificationHistoryEntry> Notifications,
    bool ShowUnreadOnly
);
