using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Roster;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion.Roster;

/// <summary>
/// Renders the founder-only "Stricken from the Ledger" subsection as ledger
/// rows that match the member roster: a dagger mark at the row left, a dotted
/// leader of name → ban reason, and a click-to-expand strip carrying the ban
/// dates and an Unban action. Collapses to a single italic line when empty.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionRosterStrickenRenderer
{
    public static float RowHeight => UiScale.Scaled(22f);
    public static float ActionStripHeight => UiScale.Scaled(30f);
    private static float MarkerInset => UiScale.Scaled(4f);
    private static float MarkerSize => UiScale.Scaled(9f);
    private static float NameOffset => UiScale.Scaled(18f);

    public static float Draw(
        ReligionRosterViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<RosterEvent> events)
    {
        var loc = LocalizationService.Instance;
        var currentY = y;

        if (!vm.HasBannedPlayers)
        {
            // Collapsed-when-empty: single italic-feeling line, no panel reserved.
            TextRenderer.DrawInfoText(drawList,
                loc.Get(LocalizationKeys.UI_RELIGION_INFO_STRICKEN_EMPTY),
                x, currentY, width, Secondary, ColorPalette.Grey);
            return currentY + RowHeight;
        }

        foreach (var ban in vm.BannedPlayers)
        {
            // † mark — gold dagger at row left, signifying the stricken.
            ChromeRenderer.DrawDagger(drawList,
                x + MarkerInset + MarkerSize / 2f, currentY + RowHeight / 2f, MarkerSize, ColorPalette.Gold);

            ChromeRenderer.DrawLeader(drawList,
                ban.PlayerName, ban.Reason,
                x + NameOffset, currentY + UiScale.Scaled(3f), width - NameOffset,
                labelColor: ColorPalette.White,
                valueColor: ColorPalette.Grey);

            // Click anywhere in the row toggles the dates + Unban strip.
            var rowMin = new Vector2(x, currentY);
            var rowMax = new Vector2(x + width, currentY + RowHeight);
            if (IsClicked(rowMin, rowMax))
                events.Add(new RosterEvent.BanRowToggled(ban.PlayerUID));

            currentY += RowHeight;

            if (vm.ExpandedBanUID == ban.PlayerUID)
                currentY = DrawActionStrip(drawList, ban, x, currentY, width, events);
        }

        return currentY;
    }

    private static float DrawActionStrip(
        ImDrawListPtr drawList,
        Network.PlayerReligionInfoResponsePacket.BanInfo ban,
        float x, float y, float width,
        List<RosterEvent> events)
    {
        var loc = LocalizationService.Instance;
        var buttonW = UiScale.Scaled(80f);
        var buttonH = UiScale.Scaled(24f);
        var rightPad = UiScale.Scaled(8f);

        // Ban dates as a leader line, indented under the name.
        var expiry = ban.IsPermanent
            ? loc.Get(LocalizationKeys.UI_RELIGION_INFO_BANNED_NEVER)
            : ban.ExpiresAt;
        var dates = $"{loc.Get(LocalizationKeys.UI_RELIGION_INFO_BANNED_AT_LABEL)} {ban.BannedAt}   " +
                    $"{loc.Get(LocalizationKeys.UI_RELIGION_INFO_BANNED_EXPIRES_LABEL)} {expiry}";
        TextRenderer.DrawInfoText(drawList, dates, x + NameOffset, y + UiScale.Scaled(5f), width - NameOffset - buttonW - rightPad,
            Secondary, ColorPalette.Grey);

        var btnY = y + UiScale.Scaled(3f);
        var unbanX = x + width - buttonW - rightPad;
        if (ButtonRenderer.DrawButton(drawList,
                loc.Get(LocalizationKeys.UI_RELIGION_INFO_UNBAN_BUTTON),
                unbanX, btnY, buttonW, buttonH))
        {
            events.Add(new RosterEvent.UnbanClicked(ban.PlayerUID));
        }

        return y + ActionStripHeight;
    }

    private static bool IsClicked(Vector2 min, Vector2 max)
    {
        var mp = ImGui.GetMousePos();
        var over = mp.X >= min.X && mp.X <= max.X && mp.Y >= min.Y && mp.Y <= max.Y;
        return over && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
    }
}
