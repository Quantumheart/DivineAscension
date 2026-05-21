using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Detail;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Renders the detail chapter of another Order, opened from the "Other
///     Orders" browse page (#314). Follows the ledger framing of the "This
///     Order" chapter (#309): serif title strip with a per-domain primitive
///     glyph, prose intro, dotted-leader stat block (with Lapis prestige
///     bar), prose description, and a read-only roster styled as ledger
///     rows. No kick / ban / invite affordances — the viewer is not a
///     member of this Order.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionDetailRenderer
{
    private const float TopPadding = 8f;
    private const float NavRowHeight = 32f;
    private const float NavRowBottomPadding = 12f;
    private const float NavBackWidth = 36f;
    private const float NavJoinWidth = 130f;
    private const float BackGlyphSize = 14f;
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float StatRowHeight = 22f;
    private const float StatBlockBottomSpacing = 8f;
    private const float ProseBottomSpacing = 12f;
    private const float PrestigeBarHeight = 12f;
    private const float PrestigeBarMaxWidth = 180f;
    private const float MemberRowHeight = 26f;
    private const float MemberRowGap = 2f;
    private const float SectionBottomSpacing = 8f;

    public static ReligionDetailRendererResult Draw(
        ReligionDetailViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<DetailEvent>();
        var currentY = vm.Y + TopPadding;
        var contentWidth = vm.Width - ChapterStripRenderer.ScrollbarGutter;

        // Nav row sits above the chapter frame: ◂ back on the left, Join on
        // the right when joinable. Always rendered (even while loading) so
        // the viewer can back out without waiting on the detail packet.
        currentY = DrawNavRow(vm, drawList, events, currentY, contentWidth);

        if (vm.IsLoading)
        {
            TextRenderer.DrawInfoText(
                drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DETAIL_LOADING),
                vm.X, currentY + 8f, contentWidth);
            return new ReligionDetailRendererResult(events, vm.Height);
        }

        // === LEDGER CHAPTER HEADER ===
        (currentY, contentWidth) = DrawChapterHeader(vm, drawList, currentY);

        // === PROSE INTRO ===
        currentY = DrawProseIntro(vm, drawList, currentY, contentWidth);

        // === STAT BLOCK ===
        currentY = DrawStatBlock(vm, drawList, currentY, contentWidth);

        // === DIVIDER ===
        currentY = DrawDivider(drawList, vm.X, currentY, contentWidth);

        // === OF THE ORDER'S PURPOSE ===
        currentY = DrawPurposeProse(vm, drawList, currentY, contentWidth);

        // === DIVIDER ===
        currentY = DrawDivider(drawList, vm.X, currentY, contentWidth);

        // === SOULS OF THE ORDER (read-only roster) ===
        DrawSoulsSection(vm, drawList, currentY, contentWidth, events);

        return new ReligionDetailRendererResult(events, vm.Height);
    }

    private static float DrawNavRow(
        ReligionDetailViewModel vm,
        ImDrawListPtr drawList,
        List<DetailEvent> events,
        float y,
        float contentWidth)
    {
        // Empty-label button + left-chevron primitive painted over it. The
        // bundled font has no Geometric-Shapes glyph coverage so `◂` would
        // render as `?`; the chevron is a primitive in the same chrome kit
        // as the other ledger ornaments (palette §5: cream ink on the dark
        // button surface).
        if (ButtonRenderer.DrawButton(drawList, string.Empty,
                vm.X, y, NavBackWidth, NavRowHeight,
                isPrimary: false, enabled: true))
        {
            events.Add(new DetailEvent.BackToBrowseClicked());
        }
        ChromeRenderer.DrawChevron(drawList,
            vm.X + NavBackWidth / 2f,
            y + NavRowHeight / 2f,
            BackGlyphSize,
            ChromeRenderer.ChevronDirection.Left,
            ColorPalette.LightText);

        if (vm.CanJoin && !vm.IsLoading)
        {
            var joinX = vm.X + contentWidth - NavJoinWidth;
            if (ButtonRenderer.DrawButton(
                    drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_ACTION_JOIN),
                    joinX, y, NavJoinWidth, NavRowHeight + 4f,
                    isPrimary: true))
            {
                events.Add(new DetailEvent.JoinClicked(vm.ReligionUID));
            }
        }

        return y + NavRowHeight + NavRowBottomPadding;
    }

    private static (float bodyY, float contentWidth) DrawChapterHeader(
        ReligionDetailViewModel vm,
        ImDrawListPtr drawList,
        float y)
    {
        var deityDomain = DomainHelper.ParseDeityType(vm.Deity);
        var strip = ChapterStripRenderer.Draw(drawList, vm.X, y, vm.Width, 0f,
            vm.ReligionName,
            rightGlyph: deityDomain);
        return (strip.BodyY, strip.ContentWidth);
    }

    private static float DrawProseIntro(
        ReligionDetailViewModel vm,
        ImDrawListPtr drawList,
        float y,
        float width)
    {
        var founder = vm.GetFounderDisplayName();
        var founded = string.IsNullOrWhiteSpace(founder)
            ? string.Empty
            : LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_INTRO_FOUNDED_BY, founder);
        var soulsKey = vm.MemberCount == 1
            ? LocalizationKeys.UI_RELIGION_INFO_INTRO_SOULS_ONE
            : LocalizationKeys.UI_RELIGION_INFO_INTRO_SOULS;
        var souls = vm.MemberCount == 1
            ? LocalizationService.Instance.Get(soulsKey)
            : LocalizationService.Instance.Get(soulsKey, vm.MemberCount);
        var prose = string.IsNullOrEmpty(founded) ? souls : $"{founded} {souls}";

        TextRenderer.DrawInfoText(drawList, prose, vm.X, y, width, Body, ColorPalette.White);
        var height = TextRenderer.MeasureWrappedHeight(prose, width, Body);
        return y + (height > 0 ? height : Body + LinePadding) + ProseBottomSpacing;
    }

    private static float DrawStatBlock(
        ReligionDetailViewModel vm,
        ImDrawListPtr drawList,
        float y,
        float width)
    {
        var currentY = y;

        // Deity row — custom name (Domain) when set, else the bare domain.
        var deityDisplay = !string.IsNullOrWhiteSpace(vm.DeityName)
            ? $"{vm.DeityName} ({vm.Deity})"
            : vm.Deity;
        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_DEITY_LABEL),
            deityDisplay,
            vm.X, currentY, width);
        currentY += StatRowHeight;

        // Founder row — rubric red ink mirrors #309.
        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_FOUNDER_LABEL),
            vm.GetFounderDisplayName(),
            vm.X, currentY, width,
            valueColor: ColorPalette.Vermilion);
        currentY += StatRowHeight;

        // Members row.
        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MEMBERS_COUNT),
            vm.MemberCount.ToString(),
            vm.X, currentY, width);
        currentY += StatRowHeight;

        // Prestige row with progress bar in the dot-leader gap.
        currentY = DrawPrestigeRow(vm, drawList, vm.X, currentY, width);

        return currentY + StatBlockBottomSpacing;
    }

    private static float DrawPrestigeRow(
        ReligionDetailViewModel vm,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        var label = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_PRESTIGE_LABEL);
        var rankName = vm.PrestigeRank;
        var numeral = ToRoman(Math.Max(1, vm.PrestigeRankIndex + 1));

        var labelCol = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        var rankCol = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        var numeralCol = ImGui.ColorConvertFloat4ToU32(ColorPalette.Lapis);

        var labelSize = ImGui.CalcTextSize(label);
        drawList.AddText(new Vector2(x, y), labelCol, label);

        var rankSize = ImGui.CalcTextSize(rankName);
        var rankX = x + labelSize.X + 6f;
        drawList.AddText(new Vector2(rankX, y), rankCol, rankName);

        var numeralSize = ImGui.CalcTextSize(numeral);
        var numeralX = x + width - numeralSize.X;
        drawList.AddText(new Vector2(numeralX, y), numeralCol, numeral);

        const float padding = 6f;
        var barLeft = rankX + rankSize.X + padding;
        var barRight = numeralX - padding;
        var availableBarWidth = barRight - barLeft;
        if (availableBarWidth > 24f)
        {
            var barWidth = MathF.Min(availableBarWidth, PrestigeBarMaxWidth);
            var barX = barRight - barWidth;
            var barY = y + (numeralSize.Y - PrestigeBarHeight) / 2f;
            ProgressBarRenderer.DrawProgressBar(drawList, barX, barY, barWidth, PrestigeBarHeight,
                vm.PrestigeProgressPercentage,
                ColorPalette.Lapis, ColorPalette.TableBackground,
                vm.IsMaxPrestigeRank
                    ? string.Empty
                    : $"{vm.Prestige}/{vm.PrestigeRequired}");
        }

        return y + StatRowHeight;
    }

    private static string ToRoman(int n) => n switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        _ => n.ToString(),
    };

    private static float DrawPurposeProse(
        ReligionDetailViewModel vm,
        ImDrawListPtr drawList,
        float y,
        float width)
    {
        var currentY = y;
        var heading = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DETAIL_DESCRIPTION);
        TextRenderer.DrawLabel(drawList, heading, vm.X, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += StatRowHeight;

        var hasDescription = !string.IsNullOrWhiteSpace(vm.Description);
        var prose = hasDescription
            ? vm.Description
            : LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DETAIL_DESCRIPTION_EMPTY);
        var proseColor = hasDescription ? ColorPalette.White : ColorPalette.Grey;
        TextRenderer.DrawInfoText(drawList, prose, vm.X, currentY, width, Secondary, proseColor);
        var height = TextRenderer.MeasureWrappedHeight(prose, width);
        return currentY + (height > 0 ? height : Secondary + LinePadding) + SectionBottomSpacing;
    }

    private static void DrawSoulsSection(
        ReligionDetailViewModel vm,
        ImDrawListPtr drawList,
        float y,
        float width,
        List<DetailEvent> events)
    {
        var currentY = y;
        var heading = LocalizationService.Instance.Get(
            LocalizationKeys.UI_RELIGION_DETAIL_MEMBERS, vm.MemberCount);
        TextRenderer.DrawLabel(drawList, heading, vm.X, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += StatRowHeight;

        var listHeight = MathF.Max(vm.Height - (currentY - vm.Y) - SectionBottomSpacing, MemberRowHeight);
        var members = vm.Members?.ToList() ?? new List<ReligionDetailResponsePacket.MemberInfo>();

        var newScrollY = ScrollableList.Draw<ReligionDetailResponsePacket.MemberInfo>(
            drawList,
            vm.X,
            currentY,
            width,
            listHeight,
            members,
            MemberRowHeight,
            MemberRowGap,
            vm.MemberScrollY,
            (member, cx, cy, cw, ch) => DrawSoulRow(member, cx, cy, cw, ch, drawList),
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DETAIL_NO_MEMBERS),
            backgroundColor: new Vector4(0f, 0f, 0f, 0f) // transparent — rows sit directly on the vellum page
        );

        if (Math.Abs(newScrollY - vm.MemberScrollY) > 0.001f)
            events.Add(new DetailEvent.MemberScrollChanged(newScrollY));
    }

    private static void DrawSoulRow(
        ReligionDetailResponsePacket.MemberInfo member,
        float x, float y, float width, float height,
        ImDrawListPtr drawList)
    {
        // ✦ Name · · · · · · · · · · · · · · · · · · · Rank
        //
        // Diamond marker is painted as a primitive (Dingbats glyphs don't
        // render in the loaded font). DrawLeader then paints the row with a
        // dot-leader run filling the gap between name and favor rank.
        const float diamondLeftPadding = 4f;
        const float diamondHalfSize = 3.5f;
        const float diamondToLabelGap = 10f;

        var centerY = y + height / 2f;
        ChromeRenderer.DrawDiamond(drawList,
            x + diamondLeftPadding + diamondHalfSize, centerY,
            diamondHalfSize,
            ColorPalette.Gold * 0.6f);

        var leaderX = x + diamondLeftPadding + diamondHalfSize * 2f + diamondToLabelGap;
        var leaderWidth = MathF.Max(width - (leaderX - x) - 8f, 40f);
        var rowY = centerY - Body * 0.5f;
        ChromeRenderer.DrawLeader(drawList,
            member.PlayerName,
            member.FavorRank,
            leaderX, rowY, leaderWidth);
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }
}
