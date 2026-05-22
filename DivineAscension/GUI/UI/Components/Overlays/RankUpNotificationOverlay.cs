using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Components.Overlays;

/// <summary>
/// Rank-up toast anchored to the bottom-right of the viewport. Non-modal:
/// auto-dismisses on timer, no backdrop dim, no input capture. Click the
/// toast body to jump to the Blessings page.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class RankUpNotificationOverlay
{
    private const float PanelWidth = 320f;
    private const float IconSize = 36f;
    private const float Padding = 12f;
    private const float EdgeMargin = 24f;

    internal static void Draw(NotificationState state, out bool dismissed, out bool viewBlessingsClicked,
        float windowWidth, float windowHeight)
    {
        dismissed = false;
        viewBlessingsClicked = false;

        if (!state.IsVisible) return;

        var drawList = ImGui.GetWindowDrawList();
        var winPos = ImGui.GetWindowPos();

        var bodyWidth = PanelWidth - Padding * 2 - IconSize - Padding;
        var descriptionHeight = TextRenderer.MeasureWrappedHeight(state.RankDescription, bodyWidth, Body);

        var contentHeight =
            SubsectionLabel + 4f +
            SubsectionLabel + 6f +
            descriptionHeight;
        var iconBlockHeight = IconSize;
        var panelHeight = Padding + MathF.Max(contentHeight, iconBlockHeight) + Padding;

        var panelX = winPos.X + windowWidth - PanelWidth - EdgeMargin;
        var panelY = winPos.Y + windowHeight - panelHeight - EdgeMargin;
        var panelMin = new Vector2(panelX, panelY);
        var panelMax = new Vector2(panelX + PanelWidth, panelY + panelHeight);

        // Parchment surface + faded-ink edge (matches vestments modal).
        drawList.AddRectFilled(panelMin, panelMax,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Background), 6f);
        drawList.AddRect(panelMin, panelMax,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor), 6f, ImDrawFlags.None, 1.5f);

        var iconX = panelX + Padding;
        var iconY = panelY + (panelHeight - IconSize) / 2f;
        var textureId = DeityIconLoader.GetDeityTextureId(state.DeityDomain);
        if (textureId != IntPtr.Zero)
        {
            drawList.AddImage(textureId,
                new Vector2(iconX, iconY),
                new Vector2(iconX + IconSize, iconY + IconSize),
                Vector2.Zero, Vector2.One);
        }

        var textX = iconX + IconSize + Padding;
        var textY = panelY + Padding;

        var titleText = LocalizationService.Instance.Get(LocalizationKeys.UI_RANKUP_TITLE);
        TextRenderer.DrawLabel(drawList, titleText, textX, textY, SubsectionLabel, ColorPalette.Gold);
        textY += SubsectionLabel + 4f;

        TextRenderer.DrawLabel(drawList, state.RankName, textX, textY, SubsectionLabel, ColorPalette.White);
        textY += SubsectionLabel + 6f;

        TextRenderer.DrawInfoText(drawList, state.RankDescription,
            textX, textY, bodyWidth, Body, ColorPalette.White);

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            var mousePos = ImGui.GetMousePos();
            if (mousePos.X >= panelX && mousePos.X <= panelX + PanelWidth &&
                mousePos.Y >= panelY && mousePos.Y <= panelY + panelHeight)
            {
                viewBlessingsClicked = true;
                dismissed = true;
            }
        }
    }
}
