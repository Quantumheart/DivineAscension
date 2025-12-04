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
///     Overlay for browsing and joining religions
///     Displays as modal panel on top of Blessing Dialog
/// </summary>
[ExcludeFromCodeCoverage]
[Obsolete("This overlay has been migrated to ReligionBrowseRenderer in the BlessingDialog tab system. Use BlessingDialog's Religion tab instead.")]
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
    ///     Draw the religion browser overlay
    /// </summary>
    /// <param name="api">Client API</param>
    /// <param name="windowWidth">Parent window width</param>
    /// <param name="windowHeight">Parent window height</param>
    /// <param name="onClose">Callback when close button clicked</param>
    /// <param name="onJoinReligion">Callback when join button clicked (religionUID)</param>
    /// <param name="onRequestRefresh">Callback when refresh requested (deityFilter)</param>
    /// <param name="onCreateReligion">Callback when create religion clicked</param>
    /// <param name="userHasReligion">Whether the user already has a religion</param>
    /// <returns>True if overlay should remain open</returns>
    public static bool Draw(
        ICoreClientAPI api,
        int windowWidth,
        int windowHeight,
        Action onClose,
        Action<string> onJoinReligion,
        Action<string> onRequestRefresh,
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
        var headerText = "Browse Religions";
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

        // === DEITY FILTER TABS ===
        var deityFilters = new[] { "All", "Khoras", "Lysa", "Aethra", "Gaia" };
        const float tabHeight = 32f;
        const float tabSpacing = 4f;

        // Find current selected index
        var currentSelectedIndex = Array.IndexOf(deityFilters, _state.SelectedDeityFilter);
        if (currentSelectedIndex == -1) currentSelectedIndex = 0; // Default to "All"

        // Draw tabs using TabControl component
        var newSelectedIndex = TabControl.Draw(
            drawList,
            overlayX + padding,
            currentY,
            overlayWidth - padding * 2,
            tabHeight,
            deityFilters,
            currentSelectedIndex,
            tabSpacing);

        // Handle selection change
        if (newSelectedIndex != currentSelectedIndex)
        {
            _state.SelectedDeityFilter = deityFilters[newSelectedIndex];
            _state.SelectedReligionUID = null;
            _state.ScrollY = 0f;
            _state.IsLoading = true;

            // Request refresh with new filter
            var filterString = deityFilters[newSelectedIndex] == "All" ? "" : deityFilters[newSelectedIndex];
            onRequestRefresh.Invoke(filterString);

            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
        }

        currentY += tabHeight + padding;

        // === RELIGION LIST ===
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

        // Only show Create button if user doesn't have a religion
        if (!userHasReligion)
        {
            // Show both Create and Join buttons
            var totalButtonWidth = buttonWidth * 2 + buttonSpacing;
            var buttonsStartX = overlayX + (overlayWidth - totalButtonWidth) / 2;

            // Create Religion button
            var createButtonX = buttonsStartX;
            if (ButtonRenderer.DrawButton(drawList, "Create Religion", createButtonX, buttonY, buttonWidth, buttonHeight, isPrimary: true, enabled: true))
            {
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);
                onCreateReligion.Invoke();
            }

            // Join Religion button
            var joinButtonX = buttonsStartX + buttonWidth + buttonSpacing;
            if (ButtonRenderer.DrawButton(drawList, canJoin ? "Join Religion" : "Select a religion", joinButtonX, buttonY, buttonWidth, buttonHeight, isPrimary: false, enabled: canJoin))
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
            // User has religion - only show centered Join button (for switching religions)
            var joinButtonX = overlayX + (overlayWidth - buttonWidth) / 2;
            if (ButtonRenderer.DrawButton(drawList, canJoin ? "Join Religion" : "Select a religion", joinButtonX, buttonY, buttonWidth, buttonHeight, isPrimary: false, enabled: canJoin))
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
