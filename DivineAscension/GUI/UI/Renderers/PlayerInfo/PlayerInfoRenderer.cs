using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.Extensions;
using DivineAscension.GUI.Events.PlayerInfo;
using DivineAscension.GUI.Models.PlayerInfo;
using DivineAscension.GUI.Models.Religion.Header;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.PlayerInfo;

/// <summary>
///     Ledger-chapter page for the III.i "You" destination (#333). Reads
///     top-to-bottom as a chapter in an illuminated codex: title strip
///     personalised by the player's deity + favor rank, prose intro that
///     adapts to religion/civilization presence, three dotted-leader
///     sections (Standing, Favor, Order's Standing) separated by ornamental
///     dividers. Notification feed moved to a planned "Recent Tidings"
///     follow-up; civilization rank lives on the Chronicles chapter (#332).
/// </summary>
[ExcludeFromCodeCoverage]
internal static class PlayerInfoRenderer
{
    private const float DividerHeight = 18f;
    private const float DividerYPadding = 6f;
    private const float SectionLabelHeight = 22f;
    private const float ProseLineHeight = 18f;
    private const float ProseBottomSpacing = 12f;
    private const float StatRowHeight = 22f;
    private const float StatBlockBottomSpacing = 6f;
    private const float ProgressRowHeight = 22f;
    private const float ProgressBarHeight = 12f;
    private const float ScrollbarWidth = 16f;

    public static IReadOnlyList<PlayerInfoEvent> Draw(PlayerInfoViewModel vm)
    {
        var events = new List<PlayerInfoEvent>();
        if (vm.Width <= 0f || vm.Height <= 0f) return events;

        var drawList = ImGui.GetWindowDrawList();
        var x = vm.X;
        var y = vm.Y;
        var width = vm.Width;
        var height = vm.Height;
        var header = vm.Header;

        var contentHeightEstimate = ComputeContentHeight(header);
        var maxScroll = MathF.Max(0f, contentHeightEstimate - height);

        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width
                                      && mousePos.Y >= y && mousePos.Y <= y + height;
        var scrollY = vm.ScrollY;
        if (isHover && maxScroll > 0f)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                var newScrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
                if (Math.Abs(newScrollY - scrollY) > 0.001f)
                {
                    scrollY = newScrollY;
                    events.Add(new PlayerInfoEvent.ScrollChanged(newScrollY));
                }
            }
        }

        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);

        // === CHAPTER STRIP ===
        // Title is personalised: "Of you, Hroth's Disciple". The deity-domain
        // glyph appears on the right; no pencil (page has no editable fields).
        var title = BuildChapterTitle(header);
        var rightGlyph = header.HasReligion ? header.CurrentDeity : (DeityDomain?)null;
        var strip = ChapterStripRenderer.Draw(drawList, x, y, width, scrollY,
            title,
            rightGlyph: rightGlyph);
        var contentWidth = strip.ContentWidth;
        var currentY = strip.BodyY;

        // === PROSE INTRO ===
        currentY = DrawProseIntro(header, drawList, x, currentY, contentWidth);

        currentY = DrawDivider(drawList, x, currentY, contentWidth);

        // === OF YOUR STANDING ===
        currentY = DrawStandingSection(header, drawList, x, currentY, contentWidth);

        if (header.HasReligion)
        {
            currentY = DrawDivider(drawList, x, currentY, contentWidth);
            currentY = DrawFavorSection(header, drawList, x, currentY, contentWidth);

            currentY = DrawDivider(drawList, x, currentY, contentWidth);
            currentY = DrawOrderStandingSection(header, drawList, x, currentY, contentWidth);
        }

        drawList.PopClipRect();

        if (contentHeightEstimate > height)
            Scrollbar.Draw(drawList, x + width - ScrollbarWidth, y, ScrollbarWidth, height,
                scrollY, maxScroll);

        return events;
    }

    private static string BuildChapterTitle(ReligionHeaderViewModel header)
    {
        if (!header.HasReligion)
        {
            return LocalizationService.Instance.Get(LocalizationKeys.UI_PLAYER_INFO_TITLE_UNSWORN);
        }

        var deityName = !string.IsNullOrWhiteSpace(header.CurrentDeityName)
            ? header.CurrentDeityName!
            : header.CurrentDeity.ToLocalizedString();
        var rankName = RankRequirements.GetFavorRankName(header.PlayerFavorProgress.CurrentRank);
        return LocalizationService.Instance.Get(LocalizationKeys.UI_PLAYER_INFO_TITLE_SWORN,
            deityName, rankName);
    }

    private static float DrawProseIntro(ReligionHeaderViewModel header,
        ImDrawListPtr drawList, float x, float y, float width)
    {
        var prose = BuildIntroProse(header);
        TextRenderer.DrawInfoText(drawList, prose, x, y, width, Body, ColorPalette.White);
        var lines = TextRenderer.MeasureWrappedHeight(prose, width, Body);
        return y + (lines > 0 ? lines : ProseLineHeight) + ProseBottomSpacing;
    }

    private static string BuildIntroProse(ReligionHeaderViewModel header)
    {
        if (header.HasReligion && header.HasCivilization)
        {
            return LocalizationService.Instance.Get(LocalizationKeys.UI_PLAYER_INFO_INTRO_BOTH,
                FavorRankWord(header),
                header.CurrentReligionName ?? string.Empty,
                DeityWord(header),
                header.CurrentDeity.ToLocalizedString(),
                header.CurrentCivilizationName ?? string.Empty);
        }
        if (header.HasReligion)
        {
            return LocalizationService.Instance.Get(LocalizationKeys.UI_PLAYER_INFO_INTRO_RELIGION,
                FavorRankWord(header),
                header.CurrentReligionName ?? string.Empty,
                DeityWord(header),
                header.CurrentDeity.ToLocalizedString());
        }
        if (header.HasCivilization)
        {
            return LocalizationService.Instance.Get(LocalizationKeys.UI_PLAYER_INFO_INTRO_CIVILIZATION,
                header.CurrentCivilizationName ?? string.Empty);
        }
        return LocalizationService.Instance.Get(LocalizationKeys.UI_PLAYER_INFO_INTRO_NEITHER);
    }

    private static string FavorRankWord(ReligionHeaderViewModel header)
        => RankRequirements.GetFavorRankName(header.PlayerFavorProgress.CurrentRank);

    private static string DeityWord(ReligionHeaderViewModel header)
        => !string.IsNullOrWhiteSpace(header.CurrentDeityName)
            ? header.CurrentDeityName!
            : header.CurrentDeity.ToLocalizedString();

    private static float DrawStandingSection(ReligionHeaderViewModel header,
        ImDrawListPtr drawList, float x, float y, float width)
    {
        var currentY = y;
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_PLAYER_INFO_SECTION_STANDING),
            x, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionLabelHeight;

        if (header.HasReligion)
        {
            var deityDisplay = $"{DeityWord(header)} ({header.CurrentDeity.ToLocalizedString()})";
            ChromeRenderer.DrawLeader(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_PLAYER_INFO_ROW_DEITY),
                deityDisplay, x, currentY, width);
            currentY += StatRowHeight;

            ChromeRenderer.DrawLeader(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_PLAYER_INFO_ROW_ORDER),
                header.CurrentReligionName ?? string.Empty,
                x, currentY, width);
            currentY += StatRowHeight;

            if (!string.IsNullOrWhiteSpace(header.PlayerRoleInReligion))
            {
                ChromeRenderer.DrawLeader(drawList,
                    LocalizationService.Instance.Get(LocalizationKeys.UI_PLAYER_INFO_ROW_VESTMENT),
                    header.PlayerRoleInReligion!,
                    x, currentY, width);
                currentY += StatRowHeight;
            }
        }

        if (header.HasCivilization && !string.IsNullOrWhiteSpace(header.CurrentCivilizationName))
        {
            ChromeRenderer.DrawLeader(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_PLAYER_INFO_ROW_REALM),
                header.CurrentCivilizationName!,
                x, currentY, width);
            currentY += StatRowHeight;
        }

        return currentY + StatBlockBottomSpacing;
    }

    private static float DrawFavorSection(ReligionHeaderViewModel header,
        ImDrawListPtr drawList, float x, float y, float width)
    {
        var currentY = y;
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_PLAYER_INFO_SECTION_FAVOR),
            x, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionLabelHeight;

        var favor = header.PlayerFavorProgress;
        var rankName = RankRequirements.GetFavorRankName(favor.CurrentRank);
        DrawProgressLine(drawList, x, currentY, width,
            rankName,
            favor.IsMaxRank ? 1f : favor.ProgressPercentage,
            favor.IsMaxRank ? string.Empty : $"{favor.CurrentFavor} / {favor.RequiredFavor}",
            favor.CurrentRank,
            ColorPalette.Gold);
        currentY += ProgressRowHeight;
        return currentY + StatBlockBottomSpacing;
    }

    private static float DrawOrderStandingSection(ReligionHeaderViewModel header,
        ImDrawListPtr drawList, float x, float y, float width)
    {
        var currentY = y;
        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_PLAYER_INFO_SECTION_ORDER_STANDING),
            x, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SectionLabelHeight;

        var prestige = header.ReligionPrestigeProgress;
        var rankName = RankRequirements.GetPrestigeRankName(prestige.CurrentRank);
        DrawProgressLine(drawList, x, currentY, width,
            rankName,
            prestige.IsMaxRank ? 1f : prestige.ProgressPercentage,
            prestige.IsMaxRank ? string.Empty : $"{prestige.CurrentPrestige} / {prestige.RequiredPrestige}",
            prestige.CurrentRank,
            ColorPalette.Lapis);
        currentY += ProgressRowHeight;
        return currentY + StatBlockBottomSpacing;
    }

    /// <summary>
    ///     Draw a single dotted progress line: rank-name on the left, "Rank
    ///     N" numeral on the right (in <paramref name="rankInkColor" />), and
    ///     a progress bar with <c>N / M</c> label filling the gap between
    ///     them. Mirrors the prestige-row pattern in
    ///     <c>ReligionInfoHeaderRenderer</c>.
    /// </summary>
    private static void DrawProgressLine(ImDrawListPtr drawList,
        float x, float y, float width,
        string rankName, float percentage, string barLabel, int rankIndex,
        Vector4 rankInkColor)
    {
        var rightText = "Rank " + ToRoman(Math.Max(1, rankIndex + 1));
        var rightSize = ImGui.CalcTextSize(rightText);
        var rightX = x + width - rightSize.X;
        drawList.AddText(new Vector2(rightX, y),
            ImGui.ColorConvertFloat4ToU32(rankInkColor), rightText);

        var rankSize = ImGui.CalcTextSize(rankName);
        drawList.AddText(new Vector2(x, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), rankName);

        const float padding = 8f;
        var barLeft = x + rankSize.X + padding;
        var barRight = rightX - padding;
        var availableBarWidth = barRight - barLeft;
        if (availableBarWidth > 24f)
        {
            var barY = y + (rightSize.Y - ProgressBarHeight) / 2f;
            ProgressBarRenderer.DrawProgressBar(drawList, barLeft, barY,
                availableBarWidth, ProgressBarHeight,
                percentage, rankInkColor, ColorPalette.TableBackground, barLabel);
        }
    }

    private static string ToRoman(int n)
    {
        return n switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => n.ToString(),
        };
    }

    private static float DrawDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList, x, dividerY, width);
        return y + DividerHeight;
    }

    private static float ComputeContentHeight(ReligionHeaderViewModel header)
    {
        var h = 0f;
        // Chapter strip (drop-cap row + divider below)
        h += PaneHeaderRenderer.DropCapRowHeight + PaneHeaderRenderer.DividerBelowSpacing;

        // Prose intro (~2 lines).
        h += ProseLineHeight * 2f + ProseBottomSpacing;

        h += DividerHeight;

        // Standing section: heading + up to four leader rows.
        h += SectionLabelHeight;
        var standingRows = 0;
        if (header.HasReligion)
        {
            standingRows += 2; // Deity + Order
            if (!string.IsNullOrWhiteSpace(header.PlayerRoleInReligion)) standingRows++;
        }
        if (header.HasCivilization && !string.IsNullOrWhiteSpace(header.CurrentCivilizationName))
            standingRows++;
        h += StatRowHeight * standingRows + StatBlockBottomSpacing;

        if (header.HasReligion)
        {
            h += DividerHeight + SectionLabelHeight + ProgressRowHeight + StatBlockBottomSpacing;
            h += DividerHeight + SectionLabelHeight + ProgressRowHeight + StatBlockBottomSpacing;
        }

        return h;
    }
}
