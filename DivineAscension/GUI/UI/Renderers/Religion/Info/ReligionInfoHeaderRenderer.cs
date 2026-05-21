using System.Collections.Generic;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Components.Inputs;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion.Info;

/// <summary>
/// Pure renderer for the "This Order" chapter header: order title with founder
/// edit affordance, a prose intro line, then the dotted-leader stat block
/// (deity / founder / members / prestige). The chapter-level edit pencil
/// opens the existing deity-rename flow.
/// </summary>
internal static class ReligionInfoHeaderRenderer
{
    private const float StatRowHeight = 22f;
    private const float StatBlockBottomSpacing = 6f;
    private const float ProseLineHeight = 18f;
    private const float ProseBottomSpacing = 12f;
    private const float PrestigeBarHeight = 12f;
    private const float PrestigeBarMaxWidth = 180f;

    public static float Draw(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events)
    {
        // Title strip / right-side ornaments / pencil button are owned by the
        // shared ChapterStripRenderer; this method renders only prose intro
        // and the dotted-leader stat block below it.
        var currentY = DrawProseIntro(viewModel, drawList, x, y, width);

        // Stat block
        if (viewModel.IsEditingDeityName)
            currentY = DrawDeityNameEditMode(viewModel, drawList, x, currentY, width, events);
        else
            currentY = DrawDeityRow(viewModel, drawList, x, currentY, width);

        // Founder gets the rubric ink — manuscripts marked author names red.
        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_FOUNDER_LABEL),
            viewModel.GetFounderDisplayName(),
            x, currentY, width,
            valueColor: ColorPalette.Vermilion);
        currentY += StatRowHeight;

        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_MEMBERS_COUNT),
            viewModel.MemberCount.ToString(),
            x, currentY, width);
        currentY += StatRowHeight;

        currentY = DrawPrestigeRow(viewModel, drawList, x, currentY, width);

        return currentY + StatBlockBottomSpacing;
    }

    private static float DrawProseIntro(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        var founded = LocalizationService.Instance.Get(
            LocalizationKeys.UI_RELIGION_INFO_INTRO_FOUNDED_BY,
            viewModel.GetFounderDisplayName());
        var soulsKey = viewModel.MemberCount == 1
            ? LocalizationKeys.UI_RELIGION_INFO_INTRO_SOULS_ONE
            : LocalizationKeys.UI_RELIGION_INFO_INTRO_SOULS;
        var souls = viewModel.MemberCount == 1
            ? LocalizationService.Instance.Get(soulsKey)
            : LocalizationService.Instance.Get(soulsKey, viewModel.MemberCount);
        var prose = $"{founded} {souls}";

        // Body prose on parchment → iron-gall ink (palette §5).
        TextRenderer.DrawInfoText(drawList, prose, x, y, width, Body, ColorPalette.White);
        var lines = TextRenderer.MeasureWrappedHeight(prose, width, Body);
        return y + (lines > 0 ? lines : ProseLineHeight) + ProseBottomSpacing;
    }

    private static float DrawDeityRow(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        var deityDisplay = !string.IsNullOrWhiteSpace(viewModel.DeityName)
            ? $"{viewModel.DeityName} ({viewModel.Deity})"
            : viewModel.Deity;

        ChromeRenderer.DrawLeader(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_DEITY_LABEL),
            deityDisplay,
            x, y, width);
        return y + StatRowHeight;
    }

    private static float DrawPrestigeRow(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x, float y, float width)
    {
        // Prestige · · · · · Established [████░░░] II
        //
        // The leader row paints "Label · · · · · Value" with a right-aligned
        // value; here the "value" is rank-name + bar + numeral, but the bar
        // needs to be drawn as filled rectangles, not text. So: paint the rank
        // text + numeral via the leader, then overlay a progress bar in the
        // gap between them.
        var label = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_PRESTIGE_LABEL);
        var rankName = viewModel.PrestigeRank;
        var numeral = ToRoman(System.Math.Max(1, viewModel.PrestigeRankIndex + 1));
        var rightText = numeral;

        // Right-aligned numeral — Lapis matches the prestige bar ink.
        var numeralSize = ImGui.CalcTextSize(rightText);
        var numeralX = x + width - numeralSize.X;
        var numeralColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Lapis);
        drawList.AddText(new Vector2(numeralX, y), numeralColor, rightText);

        // Rank-name (left, after the label)
        var labelSize = ImGui.CalcTextSize(label);
        var labelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(new Vector2(x, y), labelColor, label);

        var rankSize = ImGui.CalcTextSize(rankName);
        var rankX = x + labelSize.X + 6f;
        var rankColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(new Vector2(rankX, y), rankColor, rankName);

        // Progress bar fills the dot-leader gap between rank-name and numeral.
        const float padding = 6f;
        var barLeft = rankX + rankSize.X + padding;
        var barRight = numeralX - padding;
        var availableBarWidth = barRight - barLeft;
        if (availableBarWidth > 24f)
        {
            var barWidth = System.MathF.Min(availableBarWidth, PrestigeBarMaxWidth);
            var barX = barRight - barWidth;
            var barY = y + (numeralSize.Y - PrestigeBarHeight) / 2f;
            // Prestige inks Lapis per palette.md; folded-edge ground for the
            // empty portion so the bar sits on a recessed surface.
            ProgressBarRenderer.DrawProgressBar(drawList, barX, barY, barWidth, PrestigeBarHeight,
                viewModel.PrestigeProgressPercentage,
                ColorPalette.Lapis, ColorPalette.TableBackground,
                viewModel.IsMaxPrestigeRank
                    ? string.Empty
                    : $"{viewModel.Prestige}/{viewModel.PrestigeRequired}");
        }

        return y + StatRowHeight + 6f;
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

    private static float DrawDeityNameEditMode(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        List<InfoEvent> events)
    {
        var currentY = y;
        const float inputHeight = 28f;
        const float inputWidth = 300f;
        const float buttonWidth = 60f;
        const float buttonHeight = 24f;
        const float buttonGap = 8f;

        TextRenderer.DrawLabel(drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INFO_DEITY_LABEL),
            x, currentY, Body, ColorPalette.Grey);

        var newValue = TextInput.Draw(
            drawList,
            "##editDeityName",
            viewModel.EditDeityNameValue,
            x + 80f,
            currentY,
            inputWidth,
            inputHeight,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_PLACEHOLDER),
            48);

        if (newValue != viewModel.EditDeityNameValue)
            events.Add(new InfoEvent.EditDeityNameChanged(newValue));

        currentY += inputHeight + 4f;

        if (!string.IsNullOrEmpty(viewModel.DeityNameError))
        {
            TextRenderer.DrawErrorText(drawList, viewModel.DeityNameError, x + 80f, currentY);
            currentY += 20f;
        }

        var buttonX = x + 80f;
        var canSave = !viewModel.IsSavingDeityName &&
                      !string.IsNullOrWhiteSpace(viewModel.EditDeityNameValue) &&
                      viewModel.EditDeityNameValue.Length >= 2 &&
                      viewModel.EditDeityNameValue.Length <= 48;

        if (ButtonRenderer.DrawButton(
                drawList,
                viewModel.IsSavingDeityName
                    ? LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_SAVING)
                    : LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_SAVE),
                buttonX, currentY, buttonWidth, buttonHeight,
                isPrimary: true, enabled: canSave))
        {
            events.Add(new InfoEvent.EditDeityNameSave(viewModel.EditDeityNameValue));
        }

        buttonX += buttonWidth + buttonGap;

        if (ButtonRenderer.DrawButton(
                drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_DEITY_NAME_CANCEL),
                buttonX, currentY, buttonWidth, buttonHeight,
                isPrimary: false, enabled: !viewModel.IsSavingDeityName))
        {
            events.Add(new InfoEvent.EditDeityNameCancel());
        }

        currentY += buttonHeight + 8f;
        return currentY;
    }
}
