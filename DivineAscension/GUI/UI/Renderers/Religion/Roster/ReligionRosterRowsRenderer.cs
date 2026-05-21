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

[ExcludeFromCodeCoverage]
internal static class ReligionRosterRowsRenderer
{
    public const float RowHeight = 22f;
    public const float ActionStripHeight = 30f;
    public const float CounterHeight = 22f;
    private const float MarkerInset = 4f;
    private const float MarkerSize = 4f;
    private const float NameOffset = 18f;

    public static float Draw(
        ReligionRosterViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width,
        List<RosterEvent> events)
    {
        var loc = LocalizationService.Instance;
        var currentY = y;

        if (!vm.HasMembers)
        {
            TextRenderer.DrawInfoText(drawList, loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_EMPTY),
                x, currentY, width, Body, ColorPalette.Grey);
            return currentY + RowHeight;
        }

        var founderTag = loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_FOUNDER_TAG);

        foreach (var member in vm.Members)
        {
            // ✦ marker — gold diamond at row left.
            ChromeRenderer.DrawDiamond(drawList,
                x + MarkerInset, currentY + RowHeight / 2f, MarkerSize, ColorPalette.Gold);

            var nameColor = member.IsFounder ? ColorPalette.Vermilion : ColorPalette.White;
            var roleLabel = member.IsFounder ? founderTag : member.RoleName;

            ChromeRenderer.DrawLeader(drawList,
                member.PlayerName, roleLabel,
                x + NameOffset, currentY + 3f, width - NameOffset,
                labelColor: nameColor,
                valueColor: ColorPalette.White);

            // Click anywhere in the row toggles expansion (founder-only meaning).
            var rowMin = new Vector2(x, currentY);
            var rowMax = new Vector2(x + width, currentY + RowHeight);
            if (IsClicked(rowMin, rowMax))
                events.Add(new RosterEvent.RowToggled(member.PlayerUID));

            currentY += RowHeight;

            if (vm.IsFounder
                && vm.ExpandedMemberUID == member.PlayerUID
                && !member.IsFounder
                && member.PlayerUID != vm.CurrentPlayerUID)
            {
                currentY = DrawActionStrip(drawList, vm, member, x, currentY, width, events);
            }
        }

        // Counter line — `─── N of N names ───` styled grey, centered.
        var counter = loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_COUNTER, vm.MemberCount);
        var counterSize = ImGui.CalcTextSize(counter);
        var counterX = x + (width - counterSize.X) / 2f;
        var counterColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(new Vector2(counterX, currentY + 4f), counterColor, counter);
        currentY += CounterHeight;

        return currentY;
    }

    private static float DrawActionStrip(
        ImDrawListPtr drawList,
        ReligionRosterViewModel vm,
        Network.PlayerReligionInfoResponsePacket.MemberInfo member,
        float x, float y, float width,
        List<RosterEvent> events)
    {
        var loc = LocalizationService.Instance;
        const float buttonW = 80f;
        const float buttonH = 24f;
        const float gap = 8f;
        const float rightPad = 8f;

        var btnY = y + 3f;
        var strikeX = x + width - buttonW - rightPad;
        var kickX = strikeX - buttonW - gap;

        if (ButtonRenderer.DrawButton(drawList,
                loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_KICK_BUTTON),
                kickX, btnY, buttonW, buttonH))
        {
            events.Add(new RosterEvent.KickClicked(member.PlayerUID, member.PlayerName));
        }

        // Strike — empty-label button with dagger primitive painted on top.
        if (ButtonRenderer.DrawButton(drawList,
                loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_STRIKE_BUTTON),
                strikeX, btnY, buttonW, buttonH))
        {
            events.Add(new RosterEvent.StrikeClicked(member.PlayerUID, member.PlayerName));
        }
        var labelWidth = ImGui.CalcTextSize(loc.Get(LocalizationKeys.UI_RELIGION_ROSTER_STRIKE_BUTTON)).X;
        var daggerCx = strikeX + buttonW / 2f + labelWidth / 2f + 8f;
        var daggerCy = btnY + buttonH / 2f;
        ChromeRenderer.DrawDagger(drawList, daggerCx, daggerCy, 10f, ColorPalette.LightText);

        return y + ActionStripHeight;
    }

    private static bool IsClicked(Vector2 min, Vector2 max)
    {
        var mp = ImGui.GetMousePos();
        var over = mp.X >= min.X && mp.X <= max.X && mp.Y >= min.Y && mp.Y <= max.Y;
        return over && ImGui.IsMouseClicked(ImGuiMouseButton.Left);
    }
}
