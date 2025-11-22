using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Inputs;
using PantheonWars.GUI.UI.State;
using PantheonWars.GUI.UI.Utilities;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers;

/// <summary>
///     Overlay for creating a new guild
///     Displays as modal form on top of browser
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CreateReligionOverlay
{
    // State
    private static readonly CreateReligionState _state = new();

    /// <summary>
    ///     Initialize/reset overlay state
    /// </summary>
    public static void Initialize()
    {
        _state.Reset();
    }

    /// <summary>
    ///     Draw the create guild overlay
    /// </summary>
    /// <param name="api">Client API</param>
    /// <param name="windowWidth">Parent window width</param>
    /// <param name="windowHeight">Parent window height</param>
    /// <param name="onClose">Callback when close/cancel clicked</param>
    /// <param name="onCreate">Callback when create clicked (name, isPublic)</param>
    /// <returns>True if overlay should remain open</returns>
    public static bool Draw(
        ICoreClientAPI api,
        int windowWidth,
        int windowHeight,
        Action onClose,
        Action<string, bool> onCreate)
    {
        const float overlayWidth = 500f;
        const float overlayHeight = 400f;
        const float padding = 20f;

        var windowPos = ImGui.GetWindowPos();
        var overlayX = windowPos.X + (windowWidth - overlayWidth) / 2;
        var overlayY = windowPos.Y + (windowHeight - overlayHeight) / 2;

        var drawList = ImGui.GetForegroundDrawList();

        // Draw semi-transparent background
        var bgStart = windowPos;
        var bgEnd = new Vector2(windowPos.X + windowWidth, windowPos.Y + windowHeight);
        var bgColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.BlackOverlay);
        drawList.AddRectFilled(bgStart, bgEnd, bgColor);

        // Draw main panel
        var panelStart = new Vector2(overlayX, overlayY);
        var panelEnd = new Vector2(overlayX + overlayWidth, overlayY + overlayHeight);
        var panelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Background);
        drawList.AddRectFilled(panelStart, panelEnd, panelColor, 8f);

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.7f);
        drawList.AddRect(panelStart, panelEnd, borderColor, 8f, ImDrawFlags.None, 2f);

        var currentY = overlayY + padding;

        // === HEADER ===
        var headerText = "Create New Guild";
        var headerSize = ImGui.CalcTextSize(headerText);
        var headerPos = new Vector2(overlayX + padding, currentY);
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 20f, headerPos, headerColor, headerText);

        // Close button (X)
        const float closeButtonSize = 24f;
        var closeButtonX = overlayX + overlayWidth - padding - closeButtonSize;
        var closeButtonY = currentY;
        if (ButtonRenderer.DrawCloseButton(drawList, closeButtonX, closeButtonY, closeButtonSize))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            onClose.Invoke();
            return false;
        }

        currentY += headerSize.Y + padding * 1.5f;

        // === FORM FIELDS ===
        var fieldWidth = overlayWidth - padding * 2;

        // Guild Name
        TextRenderer.DrawLabel(drawList, "Guild Name:", overlayX + padding, currentY);
        currentY += 25f;

        _state.ReligionName = TextInput.Draw(drawList, "##religionname", _state.ReligionName, overlayX + padding, currentY, fieldWidth, 32f, "Enter guild name...", 32);
        currentY += 40f;

        // Public/Private Toggle
        _state.IsPublic = Checkbox.Draw(drawList, api, "Public (anyone can join)", overlayX + padding, currentY, _state.IsPublic);
        currentY += 35f;

        // Info text
        var infoText = _state.IsPublic
            ? "Public guilds appear in the browser and anyone can join."
            : "Private guilds require an invitation from the founder.";
        TextRenderer.DrawInfoText(drawList, infoText, overlayX + padding, currentY, fieldWidth);
        currentY += 50f;

        // Error message
        if (!string.IsNullOrEmpty(_state.ErrorMessage))
        {
            TextRenderer.DrawErrorText(drawList, _state.ErrorMessage, overlayX + padding, currentY);
            currentY += 30f;
        }

        // === CREATE BUTTON (centered) ===
        const float buttonWidth = 120f;
        const float buttonHeight = 36f;
        var buttonY = overlayY + overlayHeight - padding - buttonHeight;
        var createButtonX = overlayX + (overlayWidth - buttonWidth) / 2; // Center the button

        var canCreate = !string.IsNullOrWhiteSpace(_state.ReligionName) && _state.ReligionName.Length >= 3;

        // Draw Create button
        if (ButtonRenderer.DrawButton(drawList, "Create", createButtonX, buttonY, buttonWidth, buttonHeight, isPrimary: true, enabled: canCreate))
        {
            if (canCreate)
            {
                // Validate name length
                if (_state.ReligionName.Length > 32)
                {
                    _state.ErrorMessage = "Name must be 32 characters or less";
                    api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/error"),
                        api.World.Player.Entity, null, false, 8f, 0.3f);
                    return true;
                }

                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);

                onCreate.Invoke(_state.ReligionName, _state.IsPublic);
                return false; // Close overlay after create
            }
            else
            {
                _state.ErrorMessage = "Guild name must be at least 3 characters";
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/error"),
                    api.World.Player.Entity, null, false, 8f, 0.3f);
            }
        }

        return true; // Keep overlay open
    }
}
