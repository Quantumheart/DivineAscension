using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.Models.Blessing.Actions;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Blessing;

/// <summary>
///     Renders action buttons (Unlock, Close) at the bottom-right of the dialog
///     Handles button states and click events
/// </summary>
[ExcludeFromCodeCoverage]
internal static class BlessingActionsRenderer
{
    private const float ButtonWidth = 120f;
    private const float ButtonHeight = 36f;
    private const float ButtonSpacing = 12f;

    private const float CornerRadius = 4f;

    // Color constants
    private static readonly Vector4 ColorButtonNormal = new(0.24f, 0.18f, 0.13f, 1.0f); // #3d2e20
    private static readonly Vector4 ColorButtonHover = new(0.35f, 0.26f, 0.19f, 1.0f); // Lighter brown
    private static readonly Vector4 ColorButtonActive = new(0.478f, 0.776f, 0.184f, 1.0f); // #7ac62f lime
    private static readonly Vector4 ColorButtonDisabled = new(0.2f, 0.15f, 0.11f, 0.6f); // Dark, semi-transparent
    private static readonly Vector4 ColorTextNormal = new(0.9f, 0.9f, 0.9f, 1.0f);
    private static readonly Vector4 ColorTextDisabled = new(0.5f, 0.5f, 0.5f, 1.0f);
    private static readonly Vector4 ColorGold = new(0.996f, 0.682f, 0.204f, 1.0f); // #feae34

    /// <summary>
    ///     Draw action buttons using EDA style. Emits events based on user interaction.
    /// </summary>
    public static BlessingActionsRendererResult Draw(BlessingActionsViewModel viewModel)
    {
        var emitted = new List<ActionsEvent>(2);

        // Unlock button - only show if blessing is selected and not already unlocked
        var selectedState = viewModel.BlessingNodeState;
        if (selectedState is { IsUnlocked: false })
        {
            var unlockButtonX = viewModel.X - ButtonWidth - ButtonSpacing;
            var canUnlock = selectedState.CanUnlock;

            var buttonText = LocalizationService.Instance.Get(LocalizationKeys.UI_BLESSING_UNLOCK_BUTTON);
            var buttonColor = canUnlock ? ColorButtonActive : ColorButtonDisabled;
            var textColor = canUnlock ? ColorTextNormal : ColorTextDisabled;

            var clicked = DrawButton(buttonText, unlockButtonX, viewModel.Y, ButtonWidth, ButtonHeight,
                buttonColor, textColor, canUnlock);

            if (clicked)
            {
                if (canUnlock)
                    emitted.Add(new ActionsEvent.UnlockClicked());
                else
                    emitted.Add(new ActionsEvent.UnlockBlockedClicked());
            }

            // Show tooltip on hover if disabled
            if (!canUnlock && IsMouseInRect(unlockButtonX, viewModel.Y, ButtonWidth, ButtonHeight))
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.NotAllowed);

                if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                {
                    emitted.Add(new ActionsEvent.UnlockBlockedClicked());
                }
            }
        }

        return new BlessingActionsRendererResult(emitted, ButtonHeight);
    }

    /// <summary>
    ///     Draw a button with hover and click handling
    /// </summary>
    /// <returns>True if button was clicked</returns>
    private static bool DrawButton(
        string text,
        float x, float y, float width, float height,
        Vector4 baseColor,
        Vector4 textColor,
        bool enabled)
    {
        var drawList = ImGui.GetWindowDrawList();
        var buttonStart = new Vector2(x, y);
        var buttonEnd = new Vector2(x + width, y + height);

        var isHovering = enabled && IsMouseInRect(x, y, width, height);
        var isClicked = false;

        // Determine button color based on state
        Vector4 currentColor;
        if (!enabled)
        {
            currentColor = baseColor;
        }
        else if (isHovering && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            // Pressed state - slightly darker
            currentColor = baseColor * 0.8f;
        }
        else if (isHovering)
        {
            // Hover state
            currentColor = ColorButtonHover;
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
        }
        else
        {
            // Normal state
            currentColor = baseColor;
        }

        // Draw button background
        var bgColor = ImGui.ColorConvertFloat4ToU32(currentColor);
        drawList.AddRectFilled(buttonStart, buttonEnd, bgColor, CornerRadius);

        // Draw border (gold for active buttons)
        var borderColor = enabled ? ColorGold : new Vector4(0.4f, 0.3f, 0.2f, 1.0f);
        var borderColorU32 = ImGui.ColorConvertFloat4ToU32(borderColor);
        drawList.AddRect(buttonStart, buttonEnd, borderColorU32, CornerRadius, ImDrawFlags.None, 2f);

        // Draw button text (centered)
        var textSize = ImGui.CalcTextSize(text);
        var textPos = new Vector2(
            x + (width - textSize.X) / 2,
            y + (height - textSize.Y) / 2
        );
        var textColorU32 = ImGui.ColorConvertFloat4ToU32(textColor);
        drawList.AddText(textPos, textColorU32, text);

        // Handle click
        if (isHovering && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            isClicked = true;
        }

        return isClicked;
    }

    /// <summary>
    ///     Check if mouse is within a rectangle
    /// </summary>
    private static bool IsMouseInRect(float x, float y, float width, float height)
    {
        var mousePos = ImGui.GetMousePos();
        return mousePos.X >= x && mousePos.X <= x + width &&
               mousePos.Y >= y && mousePos.Y <= y + height;
    }
}