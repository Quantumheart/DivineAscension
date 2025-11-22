using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Renderers.Components;
using PantheonWars.GUI.UI.State;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers;

/// <summary>
///     Overlay for browsing and joining guilds
///     Displays as modal panel on top of Guild Management Dialog
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionBrowserOverlay
{
    // State
    private static readonly ReligionBrowserState _state = new();

    /// <summary>
    ///     Initialize/reset overlay state
    /// </summary>
    public static void Initialize()
    {
        _state.Reset();
    }

    /// <summary>
    ///     Update religion list from server response
    /// </summary>
    public static void UpdateReligionList(List<ReligionListResponsePacket.ReligionInfo> religions)
    {
        _state.UpdateReligionList(religions);
    }

    /// <summary>
    ///     Draw the guild browser overlay
    /// </summary>
    /// <param name="api">Client API</param>
    /// <param name="windowWidth">Parent window width</param>
    /// <param name="windowHeight">Parent window height</param>
    /// <param name="onClose">Callback when close button clicked</param>
    /// <param name="onJoinReligion">Callback when join button clicked (religionUID)</param>
    /// <param name="onCreateReligion">Callback when create guild clicked</param>
    /// <param name="userHasReligion">Whether the user already has a guild</param>
    /// <returns>True if overlay should remain open</returns>
    public static bool Draw(
        ICoreClientAPI api,
        int windowWidth,
        int windowHeight,
        Action onClose,
        Action<string> onJoinReligion,
        Action onCreateReligion,
        bool userHasReligion)
    {
        const float overlayWidth = 800f;
        const float overlayHeight = 600f;
        const float padding = 16f;

        var windowPos = ImGui.GetWindowPos();
        var overlayX = windowPos.X + (windowWidth - overlayWidth) / 2;
        var overlayY = windowPos.Y + (windowHeight - overlayHeight) / 2;

        var drawList = ImGui.GetForegroundDrawList();

        // Draw semi-transparent background overlay
        var bgStart = windowPos;
        var bgEnd = new Vector2(windowPos.X + windowWidth, windowPos.Y + windowHeight);
        var bgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.7f));
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
        var headerText = "Browse Guilds";
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

        currentY += headerSize.Y + padding * 2;

        // === GUILD LIST ===
        var listHeight = overlayHeight - (currentY - overlayY) - padding * 2 - 40f; // 40f for join button
        ReligionListResponsePacket.ReligionInfo? hoveredReligion;
        (_state.ScrollY, _state.SelectedReligionUID, hoveredReligion) = ReligionListRenderer.Draw(
            drawList, api, overlayX + padding, currentY, overlayWidth - padding * 2, listHeight,
            _state.Religions, _state.IsLoading, _state.ScrollY, _state.SelectedReligionUID);

        currentY += listHeight + padding;

        // === ACTION BUTTONS ===
        const float buttonWidth = 180f;
        const float buttonHeight = 36f;
        const float buttonSpacing = 12f;
        var buttonY = currentY;
        var canJoin = !string.IsNullOrEmpty(_state.SelectedReligionUID);

        // Only show Create button if user doesn't have a guild
        if (!userHasReligion)
        {
            // Show both Create and Join buttons
            var totalButtonWidth = buttonWidth * 2 + buttonSpacing;
            var buttonsStartX = overlayX + (overlayWidth - totalButtonWidth) / 2;

            // Create Guild button
            var createButtonX = buttonsStartX;
            if (ButtonRenderer.DrawButton(drawList, "Create Guild", createButtonX, buttonY, buttonWidth, buttonHeight, isPrimary: true, enabled: true))
            {
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);
                onCreateReligion.Invoke();
            }

            // Join Guild button
            var joinButtonX = buttonsStartX + buttonWidth + buttonSpacing;
            if (ButtonRenderer.DrawButton(drawList, canJoin ? "Join Guild" : "Select a guild", joinButtonX, buttonY, buttonWidth, buttonHeight, isPrimary: false, enabled: canJoin))
            {
                if (canJoin)
                {
                    api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                        api.World.Player.Entity, null, false, 8f, 0.5f);
                    onJoinReligion.Invoke(_state.SelectedReligionUID!);
                    return false; // Close overlay after join
                }
                else
                {
                    api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/error"),
                        api.World.Player.Entity, null, false, 8f, 0.3f);
                }
            }
        }
        else
        {
            // User has guild - only show centered Join button (for switching guilds)
            var joinButtonX = overlayX + (overlayWidth - buttonWidth) / 2;
            if (ButtonRenderer.DrawButton(drawList, canJoin ? "Join Guild" : "Select a guild", joinButtonX, buttonY, buttonWidth, buttonHeight, isPrimary: false, enabled: canJoin))
            {
                if (canJoin)
                {
                    api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                        api.World.Player.Entity, null, false, 8f, 0.5f);
                    onJoinReligion.Invoke(_state.SelectedReligionUID!);
                    return false; // Close overlay after join
                }
                else
                {
                    api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/error"),
                        api.World.Player.Entity, null, false, 8f, 0.3f);
                }
            }
        }

        // === TOOLTIP ===
        // Draw tooltip last so it appears on top of everything
        if (hoveredReligion != null)
        {
            var mousePos = ImGui.GetMousePos();
            ReligionListRenderer.DrawTooltip(hoveredReligion, mousePos.X, mousePos.Y, overlayWidth, overlayHeight);
        }

        return true; // Keep overlay open
    }

}
