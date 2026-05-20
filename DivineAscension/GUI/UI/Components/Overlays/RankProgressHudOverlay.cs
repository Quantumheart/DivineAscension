using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.GUI.Models.Hud;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Components.Overlays;

/// <summary>
///     Renders the persistent rank progress HUD overlay in the bottom-right corner.
///     Multi-deity: stacked mini-bars per deity, patron highlighted, plus prestige + balance.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class RankProgressHudOverlay
{
    private const float PanelWidth = 240f;
    private const float EdgeMargin = 20f;
    private const float Padding = 10f;
    private const float DeityBarHeight = 12f;
    private const float PrestigeBarHeight = 16f;
    private const float RowSpacing = 4f;
    private const float SectionSpacing = 6f;
    private const float BalanceRowHeight = 16f;
    private const float FavorFontSize = 14f;
    private const float PatronBorderThickness = 2f;

    internal static void Draw(RankProgressHudViewModel vm)
    {
        if (!vm.IsVisible) return;

        var drawList = ImGui.GetWindowDrawList();
        var winPos = ImGui.GetWindowPos();

        var collapsed = vm.CollapsedToPatron && vm.PatronDomain != DeityDomain.None;
        var deityRowCount = collapsed ? 1 : vm.Deities.Count;

        var panelHeight = (Padding * 2)
                         + (deityRowCount * DeityBarHeight) + ((deityRowCount - 1) * RowSpacing)
                         + (vm.HasReligion ? SectionSpacing + PrestigeBarHeight : 0)
                         + SectionSpacing + BalanceRowHeight;

        var panelX = winPos.X + vm.ScreenWidth - PanelWidth - EdgeMargin;
        var panelY = winPos.Y + vm.ScreenHeight - panelHeight - EdgeMargin;

        var panelMin = new Vector2(panelX, panelY);
        var panelMax = new Vector2(panelX + PanelWidth, panelY + panelHeight);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.WithAlpha(ColorPalette.Background, 0.9f));
        drawList.AddRectFilled(panelMin, panelMax, bgColor, 6f);
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddRect(panelMin, panelMax, borderColor, 6f, ImDrawFlags.None, 1.5f);

        var contentX = panelX + Padding;
        var contentWidth = PanelWidth - (Padding * 2);
        var currentY = panelY + Padding;

        if (collapsed)
        {
            foreach (var slice in vm.Deities)
            {
                if (slice.Domain != vm.PatronDomain) continue;
                DrawDeityRow(drawList, contentX, currentY, contentWidth, slice);
                currentY += DeityBarHeight;
                break;
            }
        }
        else
        {
            for (var i = 0; i < vm.Deities.Count; i++)
            {
                if (i > 0) currentY += RowSpacing;
                DrawDeityRow(drawList, contentX, currentY, contentWidth, vm.Deities[i]);
                currentY += DeityBarHeight;
            }
        }

        if (vm.HasReligion)
        {
            currentY += SectionSpacing;
            DrawPrestigeRow(drawList, contentX, currentY, contentWidth, vm);
            currentY += PrestigeBarHeight;
        }

        currentY += SectionSpacing;
        DrawFavorBalance(drawList, contentX, currentY, vm.SpendableFavor);
    }

    private static void DrawDeityRow(
        ImDrawListPtr drawList, float x, float y, float width, DeityRankSlice slice)
    {
        var prefix = DomainPrefix(slice.Domain);
        var label = slice.IsMaxRank
            ? $"{prefix} {slice.RankName} (MAX)"
            : $"{prefix} {slice.RankName} ({slice.TotalFavorEarned:N0}/{slice.FavorRequiredForNext:N0})";

        ProgressBarRenderer.DrawProgressBar(
            drawList,
            x, y, width, DeityBarHeight,
            slice.Progress,
            DomainColor(slice.Domain),
            ColorPalette.DarkBrown,
            label,
            showGlow: slice.Progress > 0.8f);

        if (slice.IsPatron)
        {
            var min = new Vector2(x - 1, y - 1);
            var max = new Vector2(x + width + 1, y + DeityBarHeight + 1);
            var border = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
            drawList.AddRect(min, max, border, 4f, ImDrawFlags.None, PatronBorderThickness);
        }
    }

    private static void DrawPrestigeRow(
        ImDrawListPtr drawList, float x, float y, float width, RankProgressHudViewModel vm)
    {
        var label = vm.IsPrestigeMaxRank
            ? $"+ {vm.PrestigeRankName} (MAX)"
            : $"+ {vm.PrestigeRankName} ({vm.CurrentPrestige:N0}/{vm.PrestigeRequiredForNext:N0})";

        ProgressBarRenderer.DrawProgressBar(
            drawList,
            x, y, width, PrestigeBarHeight,
            vm.PrestigeProgress,
            new Vector4(0.6f, 0.4f, 0.8f, 1.0f),
            ColorPalette.DarkBrown,
            label,
            showGlow: vm.PrestigeProgress > 0.8f);
    }

    private static void DrawFavorBalance(ImDrawListPtr drawList, float x, float y, int spendableFavor)
    {
        const float iconRadius = 6f;
        var iconCenter = new Vector2(x + iconRadius, y + iconRadius);
        var coinColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddCircleFilled(iconCenter, iconRadius, coinColor);

        var textX = x + (iconRadius * 2) + 6f;
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(ImGui.GetFont(), FavorFontSize, new Vector2(textX, y), textColor, $"{spendableFavor:N0}");
    }

    private static string DomainPrefix(DeityDomain domain) => domain switch
    {
        DeityDomain.Craft => "C",
        DeityDomain.Wild => "W",
        DeityDomain.Conquest => "Q",
        DeityDomain.Harvest => "H",
        DeityDomain.Stone => "S",
        _ => "?"
    };

    private static Vector4 DomainColor(DeityDomain domain) => domain switch
    {
        DeityDomain.Craft => new Vector4(0.85f, 0.55f, 0.20f, 1.0f),
        DeityDomain.Wild => new Vector4(0.30f, 0.70f, 0.35f, 1.0f),
        DeityDomain.Conquest => new Vector4(0.80f, 0.25f, 0.25f, 1.0f),
        DeityDomain.Harvest => new Vector4(0.95f, 0.80f, 0.30f, 1.0f),
        DeityDomain.Stone => new Vector4(0.55f, 0.55f, 0.60f, 1.0f),
        _ => ColorPalette.Grey
    };
}
