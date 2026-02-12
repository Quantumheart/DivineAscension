using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.Models.Hud;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Utilities;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Components.Overlays;

/// <summary>
///     Renders the persistent rank progress HUD overlay in the bottom-right corner
/// </summary>
[ExcludeFromCodeCoverage]
internal static class RankProgressHudOverlay
{
    private const float PanelWidth = 220f;
    private const float PanelHeight = 85f;
    private const float EdgeMargin = 20f;
    private const float Padding = 10f;
    private const float ProgressBarHeight = 16f;
    private const float RowSpacing = 6f;
    private const float LabelFontSize = 12f;
    private const float FavorFontSize = 14f;

    /// <summary>
    ///     Draw the rank progress HUD overlay
    /// </summary>
    /// <param name="vm">View model containing display data</param>
    internal static void Draw(RankProgressHudViewModel vm)
    {
        if (!vm.IsVisible) return;

        // Use window draw list (requires ImGui window context)
        var drawList = ImGui.GetWindowDrawList();
        var winPos = ImGui.GetWindowPos();

        // Calculate panel position (bottom-right corner, offset by window position)
        var panelX = winPos.X + vm.ScreenWidth - PanelWidth - EdgeMargin;
        var panelY = winPos.Y + vm.ScreenHeight - PanelHeight - EdgeMargin;

        // Draw panel background with rounded corners
        var panelMin = new Vector2(panelX, panelY);
        var panelMax = new Vector2(panelX + PanelWidth, panelY + PanelHeight);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.Background, 0.9f));
        drawList.AddRectFilled(panelMin, panelMax, bgColor, 6f);

        // Draw panel border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddRect(panelMin, panelMax, borderColor, 6f, ImDrawFlags.None, 1.5f);

        // Track current Y position for layout
        var currentY = panelY + Padding;
        var contentX = panelX + Padding;
        var contentWidth = PanelWidth - (Padding * 2);

        // --- Favor Rank Progress ---
        DrawRankProgressRow(
            drawList,
            contentX,
            currentY,
            contentWidth,
            vm.FavorRankName,
            vm.TotalFavorEarned,
            vm.FavorRequiredForNext,
            vm.FavorProgress,
            vm.IsFavorMaxRank,
            ColorPalette.Gold);

        currentY += ProgressBarHeight + RowSpacing;

        // --- Prestige Progress ---
        DrawRankProgressRow(
            drawList,
            contentX,
            currentY,
            contentWidth,
            vm.PrestigeRankName,
            vm.CurrentPrestige,
            vm.PrestigeRequiredForNext,
            vm.PrestigeProgress,
            vm.IsPrestigeMaxRank,
            new Vector4(0.6f, 0.4f, 0.8f, 1.0f)); // Purple for prestige

        currentY += ProgressBarHeight + RowSpacing;

        // --- Spendable Favor Balance ---
        DrawFavorBalance(drawList, contentX, currentY, vm.SpendableFavor);
    }

    /// <summary>
    ///     Draw a single rank progress row with label and progress bar
    /// </summary>
    private static void DrawRankProgressRow(
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        string rankName,
        int current,
        int required,
        float progress,
        bool isMaxRank,
        Vector4 fillColor)
    {
        // Build progress label text
        string labelText;
        if (isMaxRank)
        {
            labelText = $"{rankName} (MAX)";
        }
        else
        {
            labelText = $"{rankName} ({current:N0}/{required:N0})";
        }

        // Draw progress bar with label
        ProgressBarRenderer.DrawProgressBar(
            drawList,
            x, y, width, ProgressBarHeight,
            progress,
            fillColor,
            ColorPalette.DarkBrown,
            labelText,
            showGlow: progress > 0.8f);
    }

    /// <summary>
    ///     Draw the spendable favor balance row
    /// </summary>
    private static void DrawFavorBalance(
        ImDrawListPtr drawList,
        float x,
        float y,
        int spendableFavor)
    {
        // Draw coin icon (simple circle as placeholder)
        var iconRadius = 6f;
        var iconCenterX = x + iconRadius;
        var iconCenterY = y + iconRadius;
        var coinColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddCircleFilled(new Vector2(iconCenterX, iconCenterY), iconRadius, coinColor);

        // Draw favor amount text
        var textX = x + (iconRadius * 2) + 6f;
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        var favorText = $"{spendableFavor:N0}";
        drawList.AddText(ImGui.GetFont(), FavorFontSize, new Vector2(textX, y), textColor, favorText);
    }
}
