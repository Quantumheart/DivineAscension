using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.Models.Religion.Header;
using DivineAscension.GUI.UI.Layout;
using DivineAscension.GUI.UI.Renderers.Blessing;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.RightRail;

/// <summary>
///     Vertical 340px column: religion + civilization status (via
///     <see cref="ReligionHeaderRenderer" />), then a scrollable notification feed.
///     Owns the outer panel chrome; delegates status content to the header renderer.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class RightRailRenderer
{
    private const float Padding = 8f;
    private const float HeaderBottomGap = 8f;

    public static void Draw(UiRect rect, RightRailViewModel vm)
    {
        if (rect.W <= 0f || rect.H <= 0f) return;

        var drawList = ImGui.GetWindowDrawList();

        // Outer panel chrome.
        var bg = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        var border = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.5f);
        var topLeft = new Vector2(rect.X, rect.Y);
        var botRight = new Vector2(rect.Right, rect.Bottom);
        drawList.AddRectFilled(topLeft, botRight, bg, 4f);
        drawList.AddRect(topLeft, botRight, border, 4f, ImDrawFlags.None, 2f);

        // Status block content is positioned within the rail rect; the
        // ReligionHeaderViewModel carries its own X/Y/Width.
        var headerVm = WithBounds(vm.Header, rect.X + Padding, rect.Y + Padding,
            rect.W - Padding * 2f);
        var headerHeight = ReligionHeaderRenderer.Draw(headerVm);

        var feedTop = rect.Y + Padding + headerHeight + HeaderBottomGap;
        var feedHeight = rect.Bottom - Padding - feedTop;
        if (feedHeight > 0f)
        {
            DrawNotificationFeed(
                new UiRect(rect.X + Padding, feedTop, rect.W - Padding * 2f, feedHeight),
                vm);
        }
    }

    private static ReligionHeaderViewModel WithBounds(ReligionHeaderViewModel src,
        float x, float y, float width)
    {
        // ReligionHeaderViewModel is a readonly struct; create a copy with new
        // bounds while preserving the data fields.
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

    private static void DrawNotificationFeed(UiRect rect, RightRailViewModel vm)
    {
        ImGui.SetCursorScreenPos(new Vector2(rect.X, rect.Y));
        ImGui.BeginChild("##da-rightrail-feed", new Vector2(rect.W, rect.H), false,
            ImGuiWindowFlags.None);

        var visible = 0;
        for (var i = vm.Notifications.Count - 1; i >= 0; i--)
        {
            var entry = vm.Notifications[i];
            if (vm.ShowUnreadOnly && entry.Read) continue;
            DrawNotificationRow(entry);
            visible++;
        }

        if (visible == 0)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPalette.Grey);
            ImGui.TextWrapped("(no notifications)");
            ImGui.PopStyleColor();
        }

        ImGui.EndChild();
    }

    private static void DrawNotificationRow(GUI.State.NotificationHistoryEntry entry)
    {
        var color = entry.Read ? ColorPalette.Grey : ColorPalette.White;
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextWrapped($"{entry.Timestamp:HH:mm}  {entry.Title}");
        ImGui.PopStyleColor();
        if (!string.IsNullOrEmpty(entry.Body))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ColorPalette.Grey);
            ImGui.TextWrapped($"  {entry.Body}");
            ImGui.PopStyleColor();
        }
        ImGui.Spacing();
    }
}
