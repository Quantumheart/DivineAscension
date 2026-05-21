using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.PlayerInfo;
using DivineAscension.GUI.Models.PlayerInfo;
using DivineAscension.GUI.Models.Religion.Header;
using DivineAscension.GUI.UI.Renderers.Blessing;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.PlayerInfo;

/// <summary>
///     Content page renderer for the "You" sidebar destination.
///     Draws the player identity card (former right-rail top block) followed
///     by the notification feed (former right-rail bottom block). Page chrome
///     mirrors Religion Browse / Info so all destinations read as one family.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class PlayerInfoRenderer
{
    private const float TopPadding = 8f;
    private const float IdentityToFeedGap = 40f;

    public static IReadOnlyList<PlayerInfoEvent> Draw(PlayerInfoViewModel vm)
    {
        var events = new List<PlayerInfoEvent>();
        if (vm.Width <= 0f || vm.Height <= 0f) return events;

        var drawList = ImGui.GetWindowDrawList();
        var currentY = vm.Y + TopPadding;

        // === PANE HEADER ===
        currentY = PaneHeaderRenderer.Draw(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_TAB_PLAYER_INFO),
            vm.X, currentY, vm.Width);

        // === IDENTITY CARD ===
        var headerBounded = WithBounds(vm.Header, vm.X, currentY, vm.Width);
        var identityHeight = ReligionHeaderRenderer.Draw(headerBounded);
        currentY += identityHeight + IdentityToFeedGap;

        // === NOTIFICATION FEED ===
        var feedHeight = vm.Y + vm.Height - currentY;
        if (feedHeight > 0f)
        {
            NotificationFeedRenderer.Draw(
                vm.X, currentY, vm.Width, feedHeight,
                vm.Notifications, vm.ShowUnreadOnly, events);
        }

        return events;
    }

    private static ReligionHeaderViewModel WithBounds(ReligionHeaderViewModel src,
        float x, float y, float width)
    {
        return new ReligionHeaderViewModel(
            src.HasReligion,
            src.HasCivilization,
            src.CurrentCivilizationName,
            src.CivilizationMemberReligions,
            src.CurrentDeity,
            src.CurrentDeityName,
            src.CurrentReligionName,
            src.ReligionMemberCount,
            src.PlayerRoleInReligion,
            src.PlayerFavorProgress,
            src.ReligionPrestigeProgress,
            src.IsCivilizationFounder,
            src.CivilizationIcon,
            src.CivilizationRank,
            x,
            y,
            width);
    }
}
