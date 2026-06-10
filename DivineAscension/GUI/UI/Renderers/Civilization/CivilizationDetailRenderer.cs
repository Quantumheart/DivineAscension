using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Detail;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using DivineAscension.Network;
using DivineAscension.Network.Civilization;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Renders the Realm detail chapter opened from II.i Other Realms (#325).
///     Sibling of <see cref="Religion.ReligionDetailRenderer" /> (#315). Ledger
///     framing: serif title strip, prose intro, dotted-leader stat block,
///     prose history, and a read-only Member Orders roster.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationDetailRenderer
{
    private static float TopPadding => UiScale.Scaled(8f);
    private static float NavRowHeight => UiScale.Scaled(32f);
    private static float NavRowBottomPadding => UiScale.Scaled(12f);
    private static float NavBackWidth => UiScale.Scaled(36f);
    private static float NavJoinWidth => UiScale.Scaled(130f);
    private static float BackGlyphSize => UiScale.Scaled(14f);
    private static float DividerHeight => UiScale.Scaled(18f);
    private static float DividerYPadding => UiScale.Scaled(6f);
    private static float StatRowHeight => UiScale.Scaled(22f);
    private static float StatBlockBottomSpacing => UiScale.Scaled(8f);
    private static float ProseBottomSpacing => UiScale.Scaled(12f);
    private static float MemberRowHeight => UiScale.Scaled(26f);
    private static float MemberRowGap => UiScale.Scaled(2f);
    private static float SectionBottomSpacing => UiScale.Scaled(8f);

    public static CivilizationDetailRendererResult Draw(
        CivilizationDetailViewModel vm,
        ImDrawListPtr drawList)
    {
        var events = new List<DetailEvent>();
        var currentY = vm.Y + TopPadding;
        var contentWidth = vm.Width - ChapterStripRenderer.ScrollbarGutter;

        currentY = DrawNavRow(vm, drawList, events, currentY, contentWidth);

        if (vm.IsLoading)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_LOADING),
                vm.X, currentY + UiScale.Scaled(8f), contentWidth, Body, ColorPalette.Grey);
            return new CivilizationDetailRendererResult(events, vm.Height);
        }

        var strip = ChapterStripRenderer.Draw(drawList, vm.X, currentY, vm.Width, 0f, vm.CivName);
        contentWidth = strip.ContentWidth;
        currentY = strip.BodyY;

        currentY = DrawProseIntro(vm, drawList, currentY, contentWidth);
        currentY = DrawStatBlock(vm, drawList, currentY, contentWidth);
        currentY = DrawDivider(drawList, vm.X, currentY, contentWidth);
        currentY = DrawHistoryProse(vm, drawList, currentY, contentWidth);
        currentY = DrawDivider(drawList, vm.X, currentY, contentWidth, ornate: true);
        currentY = DrawBoonsSection(vm, drawList, currentY, contentWidth);
        currentY = DrawDivider(drawList, vm.X, currentY, contentWidth, ornate: true);
        DrawMembersSection(vm, drawList, currentY, contentWidth, events);

        return new CivilizationDetailRendererResult(events, vm.Height);
    }

    private static float DrawNavRow(
        CivilizationDetailViewModel vm,
        ImDrawListPtr drawList,
        List<DetailEvent> events,
        float y,
        float contentWidth)
    {
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

        if (vm.CanRequestToJoin)
        {
            var joinX = vm.X + contentWidth - NavJoinWidth;
            if (ButtonRenderer.DrawButton(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_ACTION_JOIN),
                    joinX, y, NavJoinWidth, NavRowHeight + UiScale.Scaled(4f),
                    isPrimary: true))
            {
                events.Add(new DetailEvent.RequestToJoinClicked(vm.CivId));
            }
        }

        return y + NavRowHeight + NavRowBottomPadding;
    }

    private static float DrawProseIntro(
        CivilizationDetailViewModel vm,
        ImDrawListPtr drawList,
        float y,
        float width)
    {
        var founder = string.IsNullOrWhiteSpace(vm.FounderName) ? vm.FounderName : vm.FounderName;
        var month = vm.CreatedDate == DateTime.MinValue
            ? string.Empty
            : vm.CreatedDate.ToString("MMMM", CultureInfo.InvariantCulture);

        var founded = string.IsNullOrEmpty(month) || string.IsNullOrWhiteSpace(founder)
            ? string.Empty
            : LocalizationService.Instance.Get(
                LocalizationKeys.UI_CIVILIZATION_DETAIL_INTRO_FOUNDED, month, founder);

        var bannerKey = vm.MemberCount == 1
            ? LocalizationKeys.UI_CIVILIZATION_DETAIL_INTRO_BANNER_ONE
            : LocalizationKeys.UI_CIVILIZATION_DETAIL_INTRO_BANNER;
        var banner = vm.MemberCount == 1
            ? LocalizationService.Instance.Get(bannerKey)
            : LocalizationService.Instance.Get(bannerKey, vm.MemberCount);

        var prose = string.IsNullOrEmpty(founded) ? banner : $"{founded} {banner}";

        TextRenderer.DrawInfoText(drawList, prose, vm.X, y, width, Body, ColorPalette.White);
        var height = TextRenderer.MeasureWrappedHeight(prose, width, Body);
        return y + (height > 0 ? height : Body + LinePadding) + ProseBottomSpacing;
    }

    private static float DrawStatBlock(
        CivilizationDetailViewModel vm,
        ImDrawListPtr drawList,
        float y,
        float width)
    {
        var currentY = y;

        var founderValue = string.IsNullOrWhiteSpace(vm.FounderName) ? "—" : vm.FounderName;
        if (!string.IsNullOrWhiteSpace(vm.FounderEpithet))
            founderValue = $"{founderValue}, {vm.FounderEpithet}";

        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_FOUNDER),
            founderValue,
            vm.X, currentY, width,
            valueColor: ColorPalette.Vermilion);
        currentY += StatRowHeight;

        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_FOUNDING_ORDER),
            string.IsNullOrWhiteSpace(vm.FounderReligionName) ? "—" : vm.FounderReligionName,
            vm.X, currentY, width);
        currentY += StatRowHeight;

        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_ETHOS),
            LocalizationService.Instance.Get(EthosLocKey((CivilizationEthos)vm.Ethos)),
            vm.X, currentY, width);
        currentY += StatRowHeight;

        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_SEAT),
            string.IsNullOrWhiteSpace(vm.CapitalName) ? "—" : vm.CapitalName,
            vm.X, currentY, width);
        currentY += StatRowHeight;

        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_MEMBER_ORDERS,
                vm.MemberCount),
            $"{vm.MemberCount}/4",
            vm.X, currentY, width);
        currentY += StatRowHeight;

        return currentY + StatBlockBottomSpacing;
    }

    private static string EthosLocKey(CivilizationEthos ethos) => ethos switch
    {
        CivilizationEthos.Mercantile => LocalizationKeys.CIVILIZATION_ETHOS_MERCANTILE,
        CivilizationEthos.Martial => LocalizationKeys.CIVILIZATION_ETHOS_MARTIAL,
        CivilizationEthos.Mystic => LocalizationKeys.CIVILIZATION_ETHOS_MYSTIC,
        CivilizationEthos.Ascetic => LocalizationKeys.CIVILIZATION_ETHOS_ASCETIC,
        _ => LocalizationKeys.CIVILIZATION_ETHOS_SOVEREIGN
    };

    private static float DrawHistoryProse(
        CivilizationDetailViewModel vm,
        ImDrawListPtr drawList,
        float y,
        float width)
    {
        var currentY = y;
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_DESCRIPTION),
            vm.X, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += StatRowHeight;

        var hasDescription = !string.IsNullOrWhiteSpace(vm.Description);
        var prose = hasDescription
            ? vm.Description
            : LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_DESCRIPTION_EMPTY);
        var proseColor = hasDescription ? ColorPalette.White : ColorPalette.Grey;
        TextRenderer.DrawInfoText(drawList, prose, vm.X, currentY, width, Secondary, proseColor);
        var height = TextRenderer.MeasureWrappedHeight(prose, width);
        return currentY + (height > 0 ? height : Secondary + LinePadding) + SectionBottomSpacing;
    }

    private static float DrawBoonsSection(
        CivilizationDetailViewModel vm,
        ImDrawListPtr drawList,
        float y,
        float width)
    {
        var currentY = y;
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_BOONS_HEADING),
            vm.X, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += StatRowHeight;

        TextRenderer.DrawInfoText(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_BOONS_INTRO),
            vm.X, currentY, width, Secondary, ColorPalette.Grey);
        currentY += StatRowHeight;

        var leaders = ActiveBoonLeaders(vm.Bonuses).ToList();
        if (leaders.Count == 0)
        {
            TextRenderer.DrawInfoText(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_BOONS_EMPTY),
                vm.X, currentY, width, Secondary, ColorPalette.Grey);
            return currentY + StatRowHeight + SectionBottomSpacing;
        }

        var glyphLeftPadding = UiScale.Scaled(4f);
        var glyphBox = UiScale.Scaled(14f);
        var glyphToLabelGap = UiScale.Scaled(8f);
        var leaderX = vm.X + glyphLeftPadding + glyphBox + glyphToLabelGap;
        var leaderWidth = MathF.Max(width - (leaderX - vm.X) - UiScale.Scaled(8f), UiScale.Scaled(40f));
        var glyphCx = vm.X + glyphLeftPadding + glyphBox / 2f;

        foreach (var boon in leaders)
        {
            var centerY = currentY + StatRowHeight / 2f;
            DrawBoonGlyph(drawList, boon.Glyph, glyphCx, centerY, glyphBox);
            ChromeRenderer.DrawLeader(drawList,
                LocalizationService.Instance.Get(boon.LabelKey),
                boon.Value,
                leaderX, centerY - Body * 0.5f, leaderWidth,
                valueColor: ColorPalette.Gold);
            currentY += StatRowHeight;
        }

        return currentY + SectionBottomSpacing;
    }

    private static void DrawBoonGlyph(ImDrawListPtr drawList, BoonGlyph glyph,
        float cx, float cy, float box)
    {
        switch (glyph)
        {
            case BoonGlyph.Swords:
                DomainGlyphRenderer.Draw(drawList, DeityDomain.Conquest,
                    new Vector2(cx - box / 2f, cy - box / 2f),
                    new Vector2(cx + box / 2f, cy + box / 2f),
                    ColorPalette.Gold);
                break;
            case BoonGlyph.Hallow:
                ChromeRenderer.DrawHallow(drawList, cx, cy, box, ColorPalette.Gold);
                break;
            default:
                // Fleuron — a gold diamond with a small centre spark, matching
                // the Boons of Standing bullet on the Chronicles chapter.
                ChromeRenderer.DrawDiamond(drawList, cx, cy, box * 0.32f, ColorPalette.Gold);
                drawList.AddCircleFilled(new Vector2(cx, cy), UiScale.Scaled(1.2f),
                    ImGui.ColorConvertFloat4ToU32(ColorPalette.LightText));
                break;
        }
    }

    private enum BoonGlyph
    {
        Fleuron,
        Swords,
        Hallow
    }

    /// <summary>
    ///     Active civic boons as (glyph, label key, value) leader rows, in display
    ///     order. Inactive boons (multiplier == 1.0, slot count 0) are omitted.
    ///     Multipliers render as percentages to match the PvP chat convention;
    ///     hallow slots as a flat count.
    /// </summary>
    private static IEnumerable<(BoonGlyph Glyph, string LabelKey, string Value)> ActiveBoonLeaders(
        CivilizationBonusesDto bonuses)
    {
        if (bonuses.FavorMultiplier > 1f)
            yield return (BoonGlyph.Fleuron, LocalizationKeys.UI_CIVILIZATION_DETAIL_BOONS_FAVOR,
                LocalizationService.Instance.Get(
                    LocalizationKeys.UI_CIVILIZATION_DETAIL_BOONS_VALUE_PERCENT,
                    Percent(bonuses.FavorMultiplier)));
        if (bonuses.PrestigeMultiplier > 1f)
            yield return (BoonGlyph.Fleuron, LocalizationKeys.UI_CIVILIZATION_DETAIL_BOONS_PRESTIGE,
                LocalizationService.Instance.Get(
                    LocalizationKeys.UI_CIVILIZATION_DETAIL_BOONS_VALUE_PERCENT,
                    Percent(bonuses.PrestigeMultiplier)));
        if (bonuses.ConquestMultiplier > 1f)
            yield return (BoonGlyph.Swords, LocalizationKeys.UI_CIVILIZATION_DETAIL_BOONS_CONQUEST,
                LocalizationService.Instance.Get(
                    LocalizationKeys.UI_CIVILIZATION_DETAIL_BOONS_VALUE_CONQUEST,
                    Percent(bonuses.ConquestMultiplier)));
        if (bonuses.BonusHolySiteSlots > 0)
            yield return (BoonGlyph.Hallow, LocalizationKeys.UI_CIVILIZATION_DETAIL_BOONS_HALLOWS,
                LocalizationService.Instance.Get(
                    LocalizationKeys.UI_CIVILIZATION_DETAIL_BOONS_VALUE_HALLOWS,
                    bonuses.BonusHolySiteSlots));
    }

    private static string Percent(float multiplier)
    {
        return ((multiplier - 1f) * 100f).ToString("F0", CultureInfo.InvariantCulture);
    }

    private static void DrawMembersSection(
        CivilizationDetailViewModel vm,
        ImDrawListPtr drawList,
        float y,
        float width,
        List<DetailEvent> events)
    {
        var currentY = y;
        var heading = LocalizationService.Instance.Get(
            LocalizationKeys.UI_CIVILIZATION_DETAIL_MEMBER_ORDERS, vm.MemberCount);
        TextRenderer.DrawLabel(drawList, heading, vm.X, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += StatRowHeight;

        var listHeight = MathF.Max(vm.Height - (currentY - vm.Y) - SectionBottomSpacing, MemberRowHeight);
        var members = vm.MemberReligions?.ToList()
                      ?? new List<CivilizationInfoResponsePacket.MemberReligion>();

        var newScrollY = ScrollableList.Draw<CivilizationInfoResponsePacket.MemberReligion>(
            drawList,
            vm.X,
            currentY,
            width,
            listHeight,
            members,
            MemberRowHeight,
            MemberRowGap,
            vm.MemberScrollY,
            (member, cx, cy, cw, ch) => DrawMemberRow(member, cx, cy, cw, ch, drawList),
            LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_DETAIL_NO_MEMBERS),
            backgroundColor: new Vector4(0f, 0f, 0f, 0f)
        );

        if (Math.Abs(newScrollY - vm.MemberScrollY) > 0.001f)
            events.Add(new DetailEvent.MemberScrollChanged(newScrollY));
    }

    private static void DrawMemberRow(
        CivilizationInfoResponsePacket.MemberReligion member,
        float x, float y, float width, float height,
        ImDrawListPtr drawList)
    {
        // ✦ <OrderName> · · · · · <Deity>
        var diamondLeftPadding = UiScale.Scaled(4f);
        var diamondHalfSize = UiScale.Scaled(3.5f);
        var diamondToLabelGap = UiScale.Scaled(10f);

        var centerY = y + height / 2f;
        ChromeRenderer.DrawDiamond(drawList,
            x + diamondLeftPadding + diamondHalfSize, centerY,
            diamondHalfSize,
            ColorPalette.Gold * 0.6f);

        var leaderX = x + diamondLeftPadding + diamondHalfSize * 2f + diamondToLabelGap;
        var leaderWidth = MathF.Max(width - (leaderX - x) - UiScale.Scaled(8f), UiScale.Scaled(40f));
        var rowY = centerY - Body * 0.5f;
        ChromeRenderer.DrawLeader(drawList,
            member.ReligionName,
            member.Domain,
            leaderX, rowY, leaderWidth);
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width,
        bool ornate = false)
    {
        var dividerY = y + DividerYPadding;
        if (ornate)
            ChromeRenderer.DrawDividerOrnate(drawList, x, dividerY, width);
        else
            ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }
}
