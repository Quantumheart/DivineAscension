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
///     Overlay for creating a new religion
///     Displays as modal form on top of browser
/// </summary>
[ExcludeFromCodeCoverage]
[Obsolete("This overlay has been migrated to ReligionCreateRenderer in the BlessingDialog tab system. Use BlessingDialog's Religion tab instead.")]
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
    ///     Draw the create religion overlay
    /// </summary>
    /// <param name="api">Client API</param>
    /// <param name="windowWidth">Parent window width</param>
    /// <param name="windowHeight">Parent window height</param>
    /// <param name="onClose">Callback when close/cancel clicked</param>
    /// <param name="onCreate">Callback when create clicked (name, deity, isPublic)</param>
    /// <returns>True if overlay should remain open</returns>
    public static bool Draw(
        ICoreClientAPI api,
        int windowWidth,
        int windowHeight,
        Action onClose,
        Action<string, string, bool> onCreate)
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
        var headerText = "Create New Religion";
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

        // Religion Name
        TextRenderer.DrawLabel(drawList, "Religion Name:", overlayX + padding, currentY);
        currentY += 25f;

        _state.ReligionName = TextInput.Draw(drawList, "##religionname", _state.ReligionName, overlayX + padding, currentY, fieldWidth, 32f, "Enter religion name...", 32);
        currentY += 40f;

        // Deity Selection
        TextRenderer.DrawLabel(drawList, "Deity:", overlayX + padding, currentY);
        currentY += 25f;

        var dropdownY = currentY;
        DrawDeityDropdown(drawList, api, overlayX + padding, currentY, fieldWidth, 36f);
        currentY += 45f;

        // Public/Private Toggle
        _state.IsPublic = Checkbox.Draw(drawList, api, "Public (anyone can join)", overlayX + padding, currentY, _state.IsPublic);
        currentY += 35f;

        // Info text
        var infoText = _state.IsPublic
            ? "Public religions appear in the browser and anyone can join."
            : "Private religions require an invitation from the founder.";
        TextRenderer.DrawInfoText(drawList, infoText, overlayX + padding, currentY, fieldWidth);
        currentY += 50f;

        // Error message
        if (!string.IsNullOrEmpty(_state.ErrorMessage))
        {
            TextRenderer.DrawErrorText(drawList, _state.ErrorMessage, overlayX + padding, currentY);
            currentY += 30f;
        }

        // Process dropdown menu FIRST (before buttons) to consume clicks
        bool dropdownConsumedClick = false;
        if (_state.DropdownOpen)
        {
            dropdownConsumedClick = DrawDeityDropdownMenu(drawList, api, overlayX + padding, dropdownY, fieldWidth, 36f);
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

                var deityName = DeityHelper.DeityNames[_state.SelectedDeityIndex];
                onCreate.Invoke(_state.ReligionName, deityName, _state.IsPublic);
                return false; // Close overlay after create
            }
            else
            {
                _state.ErrorMessage = "Religion name must be at least 3 characters";
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/error"),
                    api.World.Player.Entity, null, false, 8f, 0.3f);
            }
        }

        // Redraw dropdown menu AFTER buttons for proper z-order (visual only, interaction already handled)
        if (_state.DropdownOpen)
        {
            DrawDeityDropdownMenuVisual(drawList, overlayX + padding, dropdownY, fieldWidth, 36f);
        }

        return true; // Keep overlay open
    }

    /// <summary>
    ///     Draw deity dropdown button (without menu)
    /// </summary>
    private static void DrawDeityDropdown(ImDrawListPtr drawList, ICoreClientAPI api, float x, float y, float width, float height)
    {
        // Create display text for selected deity
        var selectedDeity = DeityHelper.DeityNames[_state.SelectedDeityIndex];
        var deityText = DeityHelper.GetDeityDisplayText(selectedDeity);

        // Draw dropdown button
        if (Dropdown.DrawButton(drawList, x, y, width, height, deityText, _state.DropdownOpen))
        {
            _state.DropdownOpen = !_state.DropdownOpen;
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.3f);
        }
    }

    /// <summary>
    ///     Draw deity dropdown menu (handles interaction and returns if click was consumed)
    /// </summary>
    private static bool DrawDeityDropdownMenu(ImDrawListPtr drawList, ICoreClientAPI api, float x, float y, float width, float height)
    {
        // Create display texts for all deities
        var deityDisplayTexts = new string[DeityHelper.DeityNames.Length];
        for (int i = 0; i < DeityHelper.DeityNames.Length; i++)
        {
            deityDisplayTexts[i] = DeityHelper.GetDeityDisplayText(DeityHelper.DeityNames[i]);
        }

        // Handle interaction
        var (newIndex, shouldClose, clickConsumed) = Dropdown.DrawMenuAndHandleInteraction(
            drawList, api, x, y, width, height, deityDisplayTexts, _state.SelectedDeityIndex);

        _state.SelectedDeityIndex = newIndex;
        if (shouldClose)
        {
            _state.DropdownOpen = false;
        }

        return clickConsumed;
    }

    /// <summary>
    ///     Draw deity dropdown menu visual only (no interaction)
    /// </summary>
    private static void DrawDeityDropdownMenuVisual(ImDrawListPtr drawList, float x, float y, float width, float height)
    {
        // Create display texts for all deities
        var deityDisplayTexts = new string[DeityHelper.DeityNames.Length];
        for (int i = 0; i < DeityHelper.DeityNames.Length; i++)
        {
            deityDisplayTexts[i] = DeityHelper.GetDeityDisplayText(DeityHelper.DeityNames[i]);
        }

        // Draw menu visual
        Dropdown.DrawMenuVisual(drawList, x, y, width, height, deityDisplayTexts, _state.SelectedDeityIndex);
    }
}
