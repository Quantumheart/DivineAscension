using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Milestones;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Renderer for the Milestones sub-tab in the Civilization tab.
///     Displays milestone progress and active bonuses.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationMilestoneRenderer
{
    // Layout constants
    private const float TopPadding = 10f;
    private const float HeaderHeight = 40f;
    private const float RefreshButtonWidth = 100f;
    private const float RefreshButtonHeight = 30f;
    private const float SectionSpacing = 15f;
    private const float RankSectionHeight = 60f;
    private const float BonusSectionHeight = 120f;
    private const float MilestoneItemHeight = 70f;
    private const float ProgressBarHeight = 16f;

    /// <summary>
    ///     Pure renderer: builds visuals from view model and emits UI events.
    ///     No state or side effects.
    /// </summary>
    public static CivilizationMilestoneRenderResult Draw(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<MilestoneEvent>();
        var currentY = viewModel.Y + TopPadding;

        // === HEADER WITH REFRESH BUTTON ===
        currentY += DrawHeader(viewModel, drawList, currentY, events);

        // === LOADING STATE ===
        if (viewModel.IsLoading)
        {
            DrawLoadingState(viewModel, drawList, currentY);
            return new CivilizationMilestoneRenderResult(events, viewModel.Height);
        }

        // === ERROR STATE ===
        if (!string.IsNullOrEmpty(viewModel.ErrorMsg))
        {
            DrawErrorState(viewModel, drawList, currentY);
            return new CivilizationMilestoneRenderResult(events, viewModel.Height);
        }

        // === EMPTY STATE ===
        if (viewModel.Milestones.Count == 0)
        {
            DrawEmptyState(viewModel, drawList, currentY);
            return new CivilizationMilestoneRenderResult(events, viewModel.Height);
        }

        // === RANK DISPLAY ===
        currentY += DrawRankSection(viewModel, drawList, currentY);
        currentY += SectionSpacing;

        // === BONUSES SECTION ===
        currentY += DrawBonusesSection(viewModel, drawList, currentY);
        currentY += SectionSpacing;

        // === MILESTONES LIST ===
        DrawMilestonesList(viewModel, drawList, currentY);

        return new CivilizationMilestoneRenderResult(events, viewModel.Height);
    }

    private static float DrawHeader(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList,
        float y,
        List<MilestoneEvent> events)
    {
        // Title
        var titleText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_TITLE);
        drawList.AddText(ImGui.GetFont(), 18f,
            new Vector2(viewModel.X + 20f, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            titleText);

        // Refresh button (right-aligned)
        var buttonX = viewModel.X + viewModel.Width - RefreshButtonWidth - 20f;
        var buttonY = y - 5f;

        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_REFRESH),
                buttonX, buttonY, RefreshButtonWidth, RefreshButtonHeight,
                false, !viewModel.IsLoading))
        {
            events.Add(new MilestoneEvent.RefreshClicked());
        }

        return HeaderHeight;
    }

    private static void DrawLoadingState(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList,
        float contentStartY)
    {
        var text = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_LOADING);
        var textSize = ImGui.CalcTextSize(text);
        var contentHeight = viewModel.Height - (contentStartY - viewModel.Y);
        var textPos = new Vector2(
            viewModel.X + viewModel.Width / 2f - textSize.X / 2f,
            contentStartY + contentHeight / 2f - textSize.Y / 2f
        );

        drawList.AddText(ImGui.GetFont(), 14f, textPos,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), text);
    }

    private static void DrawEmptyState(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList,
        float contentStartY)
    {
        var text = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_EMPTY);
        var textSize = ImGui.CalcTextSize(text);
        var contentHeight = viewModel.Height - (contentStartY - viewModel.Y);
        var textPos = new Vector2(
            viewModel.X + viewModel.Width / 2f - textSize.X / 2f,
            contentStartY + contentHeight / 2f - textSize.Y / 2f
        );

        drawList.AddText(ImGui.GetFont(), 14f, textPos,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), text);
    }

    private static void DrawErrorState(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList,
        float contentStartY)
    {
        var text = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_ERROR)
            .Replace("{0}", viewModel.ErrorMsg ?? "Unknown error");
        var textSize = ImGui.CalcTextSize(text);
        var contentHeight = viewModel.Height - (contentStartY - viewModel.Y);
        var textPos = new Vector2(
            viewModel.X + viewModel.Width / 2f - textSize.X / 2f,
            contentStartY + contentHeight / 2f - textSize.Y / 2f
        );

        drawList.AddText(ImGui.GetFont(), 14f, textPos,
            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.3f, 0.3f, 1f)), text);
    }

    private static float DrawRankSection(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList,
        float startY)
    {
        var x = viewModel.X + 20f;
        var width = viewModel.Width - 40f;

        // Background
        var bgRect = new Vector2(x, startY);
        var bgRectEnd = new Vector2(x + width, startY + RankSectionHeight);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.DarkBrown, 0.9f));
        drawList.AddRectFilled(bgRect, bgRectEnd, bgColor, 4f);

        // Border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor);
        drawList.AddRect(bgRect, bgRectEnd, borderColor, 4f, ImDrawFlags.None, 1f);

        // Rank text
        var rankText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_RANK)
            .Replace("{0}", viewModel.Rank.ToString());
        var rankY = startY + (RankSectionHeight - ImGui.CalcTextSize(rankText).Y) / 2f;
        drawList.AddText(ImGui.GetFont(), 24f,
            new Vector2(x + 20f, rankY),
            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.84f, 0f, 1f)), // Gold color
            rankText);

        // Completed milestone count
        var completedCount = viewModel.Milestones.Count(m => m.IsCompleted);
        var totalCount = viewModel.Milestones.Count;
        var progressText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_PROGRESS)
            .Replace("{0}", completedCount.ToString())
            .Replace("{1}", totalCount.ToString());
        var progressSize = ImGui.CalcTextSize(progressText);
        var progressX = x + width - progressSize.X - 20f;
        var progressY = startY + (RankSectionHeight - progressSize.Y) / 2f;
        drawList.AddText(ImGui.GetFont(), 14f,
            new Vector2(progressX, progressY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            progressText);

        return RankSectionHeight;
    }

    private static float DrawBonusesSection(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList,
        float startY)
    {
        var x = viewModel.X + 20f;
        var width = viewModel.Width - 40f;

        // Background
        var bgRect = new Vector2(x, startY);
        var bgRectEnd = new Vector2(x + width, startY + BonusSectionHeight);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.TableBackground, 0.95f));
        drawList.AddRectFilled(bgRect, bgRectEnd, bgColor, 4f);

        // Border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.BorderColor, 0.5f));
        drawList.AddRect(bgRect, bgRectEnd, borderColor, 4f, ImDrawFlags.None, 1f);

        // Title
        var titleText = LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_BONUSES_TITLE);
        drawList.AddText(ImGui.GetFont(), 16f,
            new Vector2(x + 15f, startY + 10f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            titleText);

        var bonuses = viewModel.Bonuses;
        var bonusY = startY + 35f;
        var columnWidth = (width - 30f) / 2f;

        // Prestige multiplier
        DrawBonusLine(drawList, x + 15f, bonusY,
            LocalizationKeys.UI_CIVILIZATION_MILESTONES_BONUS_PRESTIGE,
            $"+{(bonuses.PrestigeMultiplier - 1f) * 100:F0}%",
            bonuses.PrestigeMultiplier > 1f);

        // Favor multiplier
        DrawBonusLine(drawList, x + 15f + columnWidth, bonusY,
            LocalizationKeys.UI_CIVILIZATION_MILESTONES_BONUS_FAVOR,
            $"+{(bonuses.FavorMultiplier - 1f) * 100:F0}%",
            bonuses.FavorMultiplier > 1f);

        bonusY += 25f;

        // Conquest multiplier
        DrawBonusLine(drawList, x + 15f, bonusY,
            LocalizationKeys.UI_CIVILIZATION_MILESTONES_BONUS_CONQUEST,
            $"+{(bonuses.ConquestMultiplier - 1f) * 100:F0}%",
            bonuses.ConquestMultiplier > 1f);

        // Holy site slots
        DrawBonusLine(drawList, x + 15f + columnWidth, bonusY,
            LocalizationKeys.UI_CIVILIZATION_MILESTONES_BONUS_HOLYSITES,
            $"+{bonuses.BonusHolySiteSlots}",
            bonuses.BonusHolySiteSlots > 0);

        return BonusSectionHeight;
    }

    private static void DrawBonusLine(
        ImDrawListPtr drawList,
        float x,
        float y,
        string labelKey,
        string value,
        bool isActive)
    {
        var label = LocalizationService.Instance.Get(labelKey);
        var color = isActive
            ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.9f, 0.3f, 1f)) // Green when active
            : ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);

        drawList.AddText(ImGui.GetFont(), 13f,
            new Vector2(x, y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            $"{label}: ");

        var labelWidth = ImGui.CalcTextSize($"{label}: ").X;
        drawList.AddText(ImGui.GetFont(), 13f,
            new Vector2(x + labelWidth, y),
            color,
            value);
    }

    private static void DrawMilestonesList(
        CivilizationMilestoneViewModel viewModel,
        ImDrawListPtr drawList,
        float startY)
    {
        var currentY = startY;
        var x = viewModel.X + 20f;
        var width = viewModel.Width - 40f;

        // Sort milestones: completed last, then by progress percentage
        var sortedMilestones = viewModel.Milestones
            .OrderBy(m => m.IsCompleted)
            .ThenByDescending(m => m.TargetValue > 0 ? (float)m.CurrentValue / m.TargetValue : 0)
            .ToList();

        foreach (var milestone in sortedMilestones)
        {
            DrawMilestoneItem(drawList, x, currentY, width, milestone);
            currentY += MilestoneItemHeight + 5f;

            // Check if we've exceeded available height
            if (currentY - startY > viewModel.Height - (startY - viewModel.Y))
            {
                break;
            }
        }
    }

    private static void DrawMilestoneItem(
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        MilestoneProgressDto milestone)
    {
        // Background
        var itemRect = new Vector2(x, y);
        var itemRectEnd = new Vector2(x + width, y + MilestoneItemHeight);
        var bgColor = milestone.IsCompleted
            ? ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(new Vector4(0.2f, 0.4f, 0.2f, 1f), 0.8f))
            : ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.TableBackground, 0.95f));
        drawList.AddRectFilled(itemRect, itemRectEnd, bgColor, 3f);

        // Border
        var borderColor = milestone.IsCompleted
            ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.7f, 0.3f, 1f))
            : ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.BorderColor, 0.5f));
        drawList.AddRect(itemRect, itemRectEnd, borderColor, 3f, ImDrawFlags.None, 1f);

        var padding = 12f;
        var currentX = x + padding;
        var currentY = y + padding;

        // Milestone name
        var nameColor = milestone.IsCompleted
            ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 1f, 0.5f, 1f))
            : ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(ImGui.GetFont(), 15f,
            new Vector2(currentX, currentY),
            nameColor,
            milestone.MilestoneName);

        // Status badge
        var statusText = milestone.IsCompleted
            ? LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_COMPLETED)
            : LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_MILESTONES_IN_PROGRESS);
        var statusSize = ImGui.CalcTextSize(statusText);
        var statusX = x + width - statusSize.X - padding;
        var statusColor = milestone.IsCompleted
            ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.9f, 0.3f, 1f))
            : ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.84f, 0f, 1f));
        drawList.AddText(ImGui.GetFont(), 12f,
            new Vector2(statusX, currentY),
            statusColor,
            statusText);

        currentY += 25f;

        // Progress bar
        var progressBarWidth = width - 2 * padding - 80f; // Leave room for text
        var progressBarX = currentX;
        var progressBarY = currentY;

        // Progress bar background
        var progressBgRect = new Vector2(progressBarX, progressBarY);
        var progressBgRectEnd = new Vector2(progressBarX + progressBarWidth, progressBarY + ProgressBarHeight);
        drawList.AddRectFilled(progressBgRect, progressBgRectEnd,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.2f, 1f)), 2f);

        // Progress bar fill
        var progress = milestone.TargetValue > 0
            ? Math.Min((float)milestone.CurrentValue / milestone.TargetValue, 1f)
            : 0f;
        if (progress > 0)
        {
            var fillWidth = progressBarWidth * progress;
            var fillRect = new Vector2(progressBarX, progressBarY);
            var fillRectEnd = new Vector2(progressBarX + fillWidth, progressBarY + ProgressBarHeight);
            var fillColor = milestone.IsCompleted
                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.8f, 0.3f, 1f))
                : ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.6f, 0.9f, 1f));
            drawList.AddRectFilled(fillRect, fillRectEnd, fillColor, 2f);
        }

        // Progress text
        var progressText = $"{milestone.CurrentValue}/{milestone.TargetValue}";
        var progressTextX = progressBarX + progressBarWidth + 10f;
        var progressTextY = progressBarY + (ProgressBarHeight - ImGui.CalcTextSize(progressText).Y) / 2f;
        drawList.AddText(ImGui.GetFont(), 12f,
            new Vector2(progressTextX, progressTextY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            progressText);
    }
}
