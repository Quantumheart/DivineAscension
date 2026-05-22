using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.State;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Components.Overlays;

/// <summary>
/// Rank-up popup styled as a parchment mini-page (vestments-modal language):
/// faded-ink border, gold serif title, ornamental divider, ink body text,
/// primary footer button. Mirrors <c>ReligionRolesBrowseRenderer.DrawRoleEditor</c>.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class RankUpNotificationOverlay
{
    private const float PanelWidth = 500f;
    private const float IconSize = 56f;
    private const float Padding = 18f;
    private const float ButtonWidth = 220f;
    private const float ButtonHeight = 32f;

    internal static void Draw(NotificationState state, out bool dismissed, out bool viewBlessingsClicked,
        float windowWidth, float windowHeight)
    {
        dismissed = false;
        viewBlessingsClicked = false;

        if (!state.IsVisible) return;

        var drawList = ImGui.GetWindowDrawList();
        var winPos = ImGui.GetWindowPos();
        var winSize = ImGui.GetWindowSize();

        // Warm-dark page dim, matches vestments modal §4.
        drawList.AddRectFilled(winPos, new Vector2(winPos.X + winSize.X, winPos.Y + winSize.Y),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BlackOverlay));

        var bodyWidth = PanelWidth - Padding * 2;

        var descriptionHeight = TextRenderer.MeasureWrappedHeight(state.RankDescription, bodyWidth, Body);

        var panelHeight =
            Padding +
            IconSize + 12f +
            PageTitle + 6f +
            14f +
            16f +
            SubsectionLabel + 10f +
            descriptionHeight + 18f +
            ButtonHeight +
            Padding;

        var panelX = winPos.X + (windowWidth - PanelWidth) / 2f;
        var panelY = winPos.Y + 50f;
        var panelMin = new Vector2(panelX, panelY);
        var panelMax = new Vector2(panelX + PanelWidth, panelY + panelHeight);

        // Parchment surface + faded-ink edge (matches vestments modal).
        drawList.AddRectFilled(panelMin, panelMax,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Background), 6f);
        drawList.AddRect(panelMin, panelMax,
            ImGui.ColorConvertFloat4ToU32(ColorPalette.BorderColor), 6f, ImDrawFlags.None, 1.5f);

        var currentY = panelY + Padding;

        var iconX = panelX + (PanelWidth - IconSize) / 2f;
        var textureId = DeityIconLoader.GetDeityTextureId(state.DeityDomain);
        if (textureId != IntPtr.Zero)
        {
            drawList.AddImage(textureId,
                new Vector2(iconX, currentY),
                new Vector2(iconX + IconSize, currentY + IconSize),
                Vector2.Zero, Vector2.One);
        }
        currentY += IconSize + 12f;

        // Title — gold at PageTitle, centered. Scale CalcTextSize from default to PageTitle.
        var titleText = LocalizationService.Instance.Get(LocalizationKeys.UI_RANKUP_TITLE);
        var titleWidth = ImGui.CalcTextSize(titleText).X * (PageTitle / ImGui.GetFontSize());
        TextRenderer.DrawLabel(drawList, titleText,
            panelX + (PanelWidth - titleWidth) / 2f, currentY, PageTitle, ColorPalette.Gold);
        currentY += PageTitle + 6f;

        ChromeRenderer.DrawDivider(drawList, panelX + Padding, currentY, bodyWidth);
        currentY += 16f;

        var rankNameWidth = ImGui.CalcTextSize(state.RankName).X * (SubsectionLabel / ImGui.GetFontSize());
        TextRenderer.DrawLabel(drawList, state.RankName,
            panelX + (PanelWidth - rankNameWidth) / 2f, currentY, SubsectionLabel, ColorPalette.Gold);
        currentY += SubsectionLabel + 10f;

        TextRenderer.DrawInfoText(drawList, state.RankDescription,
            panelX + Padding, currentY, bodyWidth, Body, ColorPalette.White);
        currentY += descriptionHeight + 18f;

        var buttonX = panelX + (PanelWidth - ButtonWidth) / 2f;
        if (ButtonRenderer.DrawButton(drawList,
                LocalizationService.Instance.Get(LocalizationKeys.UI_RANKUP_VIEW_BLESSINGS),
                buttonX, currentY, ButtonWidth, ButtonHeight, isPrimary: true))
        {
            viewBlessingsClicked = true;
            dismissed = true;
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Escape)) dismissed = true;

        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
        {
            var mousePos = ImGui.GetMousePos();
            if (mousePos.X < panelX || mousePos.X > panelX + PanelWidth ||
                mousePos.Y < panelY || mousePos.Y > panelY + panelHeight)
            {
                dismissed = true;
            }
        }
    }
}
