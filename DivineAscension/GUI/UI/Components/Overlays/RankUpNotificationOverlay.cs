using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Components.Overlays;

[ExcludeFromCodeCoverage]
internal static class RankUpNotificationOverlay
{
    private const float PanelWidth = 500f;
    private const float IconSize = 64f;
    private const float Padding = 20f;
    private const float Spacing = 15f;
    private const float ButtonWidth = 200f;
    private const float ButtonHeight = 40f;
    private const float TitleFontSize = 20f;
    private const float RankNameFontSize = 18f;
    private const float DescriptionFontSize = 13f;

    internal static void Draw(NotificationState state, out bool dismissed, out bool viewBlessingsClicked,
        float windowWidth, float windowHeight)
    {
        dismissed = false;
        viewBlessingsClicked = false;

        if (!state.IsVisible) return;

        var drawList = ImGui.GetWindowDrawList();
        var winPos = ImGui.GetWindowPos();

        // Calculate wrapped description height
        var descriptionWidth = PanelWidth - (Padding * 2);
        var descriptionHeight = TextRenderer.MeasureWrappedHeight(
            state.RankDescription,
            descriptionWidth,
            DescriptionFontSize);

        // Calculate panel height based on content
        var panelHeight = Padding + // Top padding
                          IconSize + // Deity icon
                          Spacing + // Space after icon
                          TitleFontSize + 6f + // Title with spacing
                          Spacing + // Space after title
                          RankNameFontSize + 6f + // Rank name with spacing
                          Spacing + // Space after rank name
                          descriptionHeight + // Description (wrapped)
                          Spacing + // Space after description
                          ButtonHeight + // Button
                          Padding; // Bottom padding

        // Position panel at top-center of screen
        var panelX = winPos.X + (windowWidth - PanelWidth) / 2f;
        var panelY = winPos.Y + 50f; // 50px from top

        // Draw panel with rounded corners
        var panelMin = new Vector2(panelX, panelY);
        var panelMax = new Vector2(panelX + PanelWidth, panelY + panelHeight);
        var panelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Background);
        drawList.AddRectFilled(panelMin, panelMax, panelColor, 8f);

        // Draw panel border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddRect(panelMin, panelMax, borderColor, 8f, ImDrawFlags.None, 2f);

        // Track current Y position for layout
        var currentY = panelY + Padding;

        // Draw deity icon (centered horizontally)
        var iconX = panelX + (PanelWidth - IconSize) / 2f;
        var textureId = DeityIconLoader.GetDeityTextureId(state.DeityDomain);
        if (textureId != IntPtr.Zero)
        {
            var iconMin = new Vector2(iconX, currentY);
            var iconMax = new Vector2(iconX + IconSize, currentY + IconSize);
            drawList.AddImage(textureId, iconMin, iconMax, Vector2.Zero, Vector2.One);
        }

        currentY += IconSize + Spacing;

        // Draw "Rank Up!" title (centered, white, 20pt)
        var titleText = LocalizationService.Instance.Get(LocalizationKeys.UI_RANKUP_TITLE);
        var titleSize = ImGui.CalcTextSize(titleText);
        var titleX = panelX + (PanelWidth - titleSize.X) / 2f;
        TextRenderer.DrawLabel(drawList, titleText, titleX, currentY, TitleFontSize, ColorPalette.White);
        currentY += TitleFontSize + 6f + Spacing;

        // Draw rank name (centered, gold, 18pt)
        var rankNameSize = ImGui.CalcTextSize(state.RankName);
        var rankNameX = panelX + (PanelWidth - rankNameSize.X) / 2f;
        TextRenderer.DrawLabel(drawList, state.RankName, rankNameX, currentY, RankNameFontSize, ColorPalette.Gold);
        currentY += RankNameFontSize + 6f + Spacing;

        // Draw description (word-wrapped, grey, 13pt)
        var descriptionX = panelX + Padding;
        TextRenderer.DrawInfoText(
            drawList,
            state.RankDescription,
            descriptionX,
            currentY,
            descriptionWidth,
            DescriptionFontSize);
        currentY += descriptionHeight + Spacing;

        // Draw "View Blessings" button (centered)
        var buttonX = panelX + (PanelWidth - ButtonWidth) / 2f;
        var buttonClicked = ButtonRenderer.DrawButton(
            drawList,
            LocalizationService.Instance.Get(LocalizationKeys.UI_RANKUP_VIEW_BLESSINGS),
            buttonX,
            currentY,
            ButtonWidth,
            ButtonHeight,
            isPrimary: true);

        if (buttonClicked)
        {
            viewBlessingsClicked = true;
            dismissed = true;
        }

        // Check for ESC key press to dismiss
        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            dismissed = true;
        }

        // Handle mouse clicks for dismissal
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            var mousePos = ImGui.GetMousePos();
            // Only dismiss if click is outside the notification panel
            if (mousePos.X < panelX || mousePos.X > panelX + PanelWidth ||
                mousePos.Y < panelY || mousePos.Y > panelY + panelHeight)
            {
                dismissed = true;
            }
        }
    }
}