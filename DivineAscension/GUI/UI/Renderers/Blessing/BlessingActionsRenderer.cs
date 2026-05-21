using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Blessing;
using DivineAscension.GUI.Models.Blessing.Actions;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Models.Enum;
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

    // Color constants — button-specific colors only.
    // Shared semantic colors come from ColorPalette (Gold, White, DisabledGray, DarkBrown, LightBrown).
    private static readonly Vector4 ColorButtonActive = new(0.478f, 0.776f, 0.184f, 1.0f); // #7ac62f lime
    private static readonly Vector4 ColorButtonDisabled = new(0.2f, 0.15f, 0.11f, 0.6f); // Dark, semi-transparent

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
            var isReligionKind = selectedState.Blessing.Kind == BlessingKind.Religion;

            // Religion-kind unlocks are bound vows on behalf of the whole order;
            // non-founders see the button disabled with a tooltip (server enforces
            // the actual permission — this is the UI mirror).
            var founderGateBlocks = isReligionKind && !viewModel.IsReligionFounder;

            // Manuscript voice: communal vows are "Swear"-n; personal blessings
            // keep the existing "Unlock" verb (will be renamed in #335).
            var baseTextKey = isReligionKind
                ? LocalizationKeys.UI_BLESSING_SWEAR_BUTTON
                : LocalizationKeys.UI_BLESSING_UNLOCK_BUTTON;
            var baseText = LocalizationService.Instance.Get(baseTextKey);
            string buttonText;
            if (selectedState.Blessing.Cost > 0)
            {
                // Show cost on button (e.g., "Unlock (400)" / "Swear (400)")
                buttonText = $"{baseText} ({selectedState.Blessing.Cost})";
            }
            else
            {
                buttonText = baseText;
            }

            // Check if player can afford the cost
            var canAfford = true;
            if (selectedState.Blessing.Cost > 0)
            {
                canAfford = isReligionKind
                    ? viewModel.ReligionPrestige >= selectedState.Blessing.Cost
                    : viewModel.PlayerFavor >= selectedState.Blessing.Cost;
            }

            var isEnabled = canUnlock && canAfford && !founderGateBlocks;
            var buttonColor = isEnabled ? ColorButtonActive : ColorButtonDisabled;
            var textColor = isEnabled ? ColorPalette.White : ColorPalette.DisabledGray;

            var clicked = DrawButton(buttonText, unlockButtonX, viewModel.Y, ButtonWidth, ButtonHeight,
                buttonColor, textColor, isEnabled);

            if (clicked)
            {
                if (isEnabled)
                    emitted.Add(new ActionsEvent.UnlockClicked());
                else
                    emitted.Add(new ActionsEvent.UnlockBlockedClicked());
            }

            // Show tooltip on hover if disabled
            if (!isEnabled && IsMouseInRect(unlockButtonX, viewModel.Y, ButtonWidth, ButtonHeight))
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
            currentColor = ColorPalette.LightBrown;
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
        var borderColor = enabled ? ColorPalette.Gold : new Vector4(0.4f, 0.3f, 0.2f, 1.0f);
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