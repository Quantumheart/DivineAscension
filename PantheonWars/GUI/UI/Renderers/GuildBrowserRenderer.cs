using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Renderers.Components;
using PantheonWars.GUI.UI.State;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers;

/// <summary>
///     Guild browser renderer for main window
///     Displays list of guilds with create/join actions
/// </summary>
[ExcludeFromCodeCoverage]
internal static class GuildBrowserRenderer
{
    // State
    private static readonly ReligionBrowserState _state = new();

    /// <summary>
    ///     Initialize/reset browser state
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
    ///     Draw the guild browser in the main window
    /// </summary>
    /// <param name="api">Client API</param>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Available width</param>
    /// <param name="height">Available height</param>
    /// <param name="onJoinReligion">Callback when join button clicked (religionUID)</param>
    /// <param name="onCreateReligion">Callback when create guild clicked</param>
    /// <param name="userHasReligion">Whether the user already has a guild</param>
    public static void Draw(
        ICoreClientAPI api,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        float height,
        Action<string> onJoinReligion,
        Action onCreateReligion,
        bool userHasReligion)
    {
        const float padding = 16f;

        var currentY = y;

        // === HEADER ===
        var headerText = "Browse Guilds";
        var headerSize = ImGui.CalcTextSize(headerText);
        var headerPos = new Vector2(x + padding, currentY);
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 24f, headerPos, headerColor, headerText);

        currentY += headerSize.Y + padding * 2;

        // === GUILD LIST ===
        var listHeight = height - (currentY - y) - padding * 2 - 50f; // 50f for action buttons
        ReligionListResponsePacket.ReligionInfo? hoveredReligion;
        (_state.ScrollY, _state.SelectedReligionUID, hoveredReligion) = ReligionListRenderer.Draw(
            drawList, api, x + padding, currentY, width - padding * 2, listHeight,
            _state.Religions, _state.IsLoading, _state.ScrollY, _state.SelectedReligionUID);

        currentY += listHeight + padding;

        // === ACTION BUTTONS ===
        const float buttonWidth = 180f;
        const float buttonHeight = 40f;
        const float buttonSpacing = 16f;
        var buttonY = currentY;
        var canJoin = !string.IsNullOrEmpty(_state.SelectedReligionUID);

        // Only show Create button if user doesn't have a guild
        if (!userHasReligion)
        {
            // Show both Create and Join buttons
            var totalButtonWidth = buttonWidth * 2 + buttonSpacing;
            var buttonsStartX = x + (width - totalButtonWidth) / 2;

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
            var joinButtonX = x + (width - buttonWidth) / 2;
            if (ButtonRenderer.DrawButton(drawList, canJoin ? "Join Guild" : "Select a guild", joinButtonX, buttonY, buttonWidth, buttonHeight, isPrimary: false, enabled: canJoin))
            {
                if (canJoin)
                {
                    api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                        api.World.Player.Entity, null, false, 8f, 0.5f);
                    onJoinReligion.Invoke(_state.SelectedReligionUID!);
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
            ReligionListRenderer.DrawTooltip(hoveredReligion, mousePos.X, mousePos.Y, width, height);
        }
    }
}
