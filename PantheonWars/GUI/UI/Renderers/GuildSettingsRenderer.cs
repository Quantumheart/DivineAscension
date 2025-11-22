using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Inputs;
using PantheonWars.GUI.UI.Renderers.Components;
using PantheonWars.GUI.UI.State;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Network;
using Vintagestory.API.Client;

namespace PantheonWars.GUI.UI.Renderers;

/// <summary>
///     Settings/Admin tab renderer for guild management
///     Displays invite, description edit, ban list, and disband (leader only)
/// </summary>
[ExcludeFromCodeCoverage]
internal static class GuildSettingsRenderer
{
    /// <summary>
    ///     Draw the guild settings/admin tab
    /// </summary>
    /// <param name="api">Client API</param>
    /// <param name="drawList">ImGui draw list</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="width">Available width</param>
    /// <param name="height">Available height</param>
    /// <param name="state">Religion management state</param>
    /// <param name="onInvitePlayer">Callback when invite clicked (playerName)</param>
    /// <param name="onEditDescription">Callback when save description clicked (description)</param>
    /// <param name="onUnbanMember">Callback when unban clicked (playerUID)</param>
    /// <param name="onDisband">Callback when disband confirmed</param>
    public static void Draw(
        ICoreClientAPI api,
        ImDrawListPtr drawList,
        float x,
        float y,
        float width,
        float height,
        ReligionManagementState state,
        Action<string> onInvitePlayer,
        Action<string> onEditDescription,
        Action<string> onUnbanMember,
        Action onDisband)
    {
        const float padding = 20f;
        const float sectionSpacing = 20f;

        var currentY = y + padding;

        if (state.ReligionInfo == null)
        {
            // Loading state
            var loadingText = "Loading guild settings...";
            var loadingSize = ImGui.CalcTextSize(loadingText);
            var loadingPos = new Vector2(x + (width - loadingSize.X) / 2, y + height / 2 - loadingSize.Y / 2);
            var loadingColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(ImGui.GetFont(), 16f, loadingPos, loadingColor, loadingText);
            return;
        }

        // Show disband confirmation overlay if active
        if (state.ShowDisbandConfirm)
        {
            DrawDisbandConfirmation(drawList, api, x, y, width, height, state, onDisband);
            return;
        }

        // Only show settings if user is founder
        if (!state.ReligionInfo.IsFounder)
        {
            var notLeaderText = "Only the guild leader can access settings.";
            var notLeaderSize = ImGui.CalcTextSize(notLeaderText);
            var notLeaderPos = new Vector2(x + (width - notLeaderSize.X) / 2, y + height / 2 - notLeaderSize.Y / 2);
            var notLeaderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
            drawList.AddText(ImGui.GetFont(), 16f, notLeaderPos, notLeaderColor, notLeaderText);
            return;
        }

        // === HEADER ===
        var headerText = "Guild Settings";
        var headerSize = ImGui.CalcTextSize(headerText);
        var headerPos = new Vector2(x + padding, currentY);
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 22f, headerPos, headerColor, headerText);

        currentY += headerSize.Y + sectionSpacing;

        // === INVITE PLAYER SECTION ===
        DrawSectionLabel(drawList, "Invite Player", x + padding, currentY);
        currentY += 25f;

        var inviteInputWidth = width - padding * 2 - 130f;
        state.InvitePlayerName = TextInput.Draw(
            drawList,
            "##invite_input",
            state.InvitePlayerName,
            x + padding,
            currentY,
            inviteInputWidth,
            36f,
            "Player name...");

        // Invite button
        var inviteButtonX = x + padding + inviteInputWidth + 10f;
        if (ButtonRenderer.DrawButton(drawList, "Invite", inviteButtonX, currentY, 110f, 36f, false, !string.IsNullOrWhiteSpace(state.InvitePlayerName)))
        {
            if (!string.IsNullOrWhiteSpace(state.InvitePlayerName))
            {
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);
                onInvitePlayer.Invoke(state.InvitePlayerName.Trim());
                state.InvitePlayerName = "";
            }
        }

        currentY += 40f + sectionSpacing;

        // === EDIT DESCRIPTION SECTION ===
        DrawSectionLabel(drawList, "Guild Description", x + padding, currentY);
        currentY += 25f;

        const float descHeight = 100f;
        state.Description = TextInput.DrawMultiline(
            drawList,
            "##description_input",
            state.Description,
            x + padding,
            currentY,
            width - padding * 2,
            descHeight,
            500);

        currentY += descHeight + 8f;

        // Save Description button
        var saveButtonWidth = 160f;
        var saveButtonX = x + width - padding - saveButtonWidth;
        var descChanged = state.Description != (state.ReligionInfo.Description ?? "");
        if (ButtonRenderer.DrawButton(drawList, "Save Description", saveButtonX, currentY, saveButtonWidth, 36f, false, descChanged))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            onEditDescription.Invoke(state.Description);
        }

        currentY += 40f + sectionSpacing;

        // === BANNED PLAYERS SECTION ===
        DrawSectionLabel(drawList, "Banned Players", x + padding, currentY);
        currentY += 25f;

        var banListHeight = Math.Min(120f, height - (currentY - y) - padding - 60f); // Leave room for disband button
        var bannedPlayers = state.ReligionInfo?.BannedPlayers ?? new List<PlayerReligionInfoResponsePacket.BanInfo>();
        state.BanListScrollY = BanListRenderer.Draw(
            drawList,
            api,
            x + padding,
            currentY,
            width - padding * 2,
            banListHeight,
            bannedPlayers,
            state.BanListScrollY,
            onUnbanMember);

        currentY += banListHeight + sectionSpacing * 2;

        // === DISBAND GUILD BUTTON ===
        const float disbandButtonWidth = 200f;
        const float disbandButtonHeight = 40f;
        var disbandButtonX = x + (width - disbandButtonWidth) / 2;
        var disbandButtonY = y + height - padding - disbandButtonHeight;

        if (ButtonRenderer.DrawButton(drawList, "Disband Guild", disbandButtonX, disbandButtonY, disbandButtonWidth, disbandButtonHeight, isPrimary: false, enabled: true, customColor: ColorPalette.Red))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            state.ShowDisbandConfirm = true;
        }

        // Warning text
        var warningText = "Warning: Disbanding the guild is permanent and cannot be undone.";
        var warningSize = ImGui.CalcTextSize(warningText);
        var warningPos = new Vector2(x + (width - warningSize.X) / 2, disbandButtonY - 28f);
        var warningColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red * 0.8f);
        drawList.AddText(ImGui.GetFont(), 13f, warningPos, warningColor, warningText);

        // Display error message if any
        if (!string.IsNullOrEmpty(state.ErrorMessage))
        {
            var errorPos = new Vector2(x + padding, disbandButtonY - 55f);
            TextRenderer.DrawErrorText(drawList, state.ErrorMessage, errorPos.X, errorPos.Y);
        }
    }

    /// <summary>
    ///     Draw section label helper
    /// </summary>
    private static void DrawSectionLabel(ImDrawListPtr drawList, string text, float x, float y)
    {
        var color = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
        drawList.AddText(ImGui.GetFont(), 17f, new Vector2(x, y), color, text);
    }

    /// <summary>
    ///     Draw disband confirmation dialog
    /// </summary>
    private static void DrawDisbandConfirmation(
        ImDrawListPtr drawList,
        ICoreClientAPI api,
        float x,
        float y,
        float width,
        float height,
        ReligionManagementState state,
        Action onDisband)
    {
        const float dialogWidth = 500f;
        const float dialogHeight = 250f;
        const float padding = 20f;

        // Semi-transparent background
        var bgStart = new Vector2(x, y);
        var bgEnd = new Vector2(x + width, y + height);
        var bgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.8f));
        drawList.AddRectFilled(bgStart, bgEnd, bgColor);

        // Dialog panel
        var dialogX = x + (width - dialogWidth) / 2;
        var dialogY = y + (height - dialogHeight) / 2;
        var dialogStart = new Vector2(dialogX, dialogY);
        var dialogEnd = new Vector2(dialogX + dialogWidth, dialogY + dialogHeight);
        var panelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Background);
        drawList.AddRectFilled(dialogStart, dialogEnd, panelColor, 8f);

        // Border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red);
        drawList.AddRect(dialogStart, dialogEnd, borderColor, 8f, ImDrawFlags.None, 2f);

        var currentY = dialogY + padding;

        // Header
        var headerText = "Confirm Guild Disbandment";
        var headerSize = ImGui.CalcTextSize(headerText);
        var headerPos = new Vector2(dialogX + (dialogWidth - headerSize.X) / 2, currentY);
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red);
        drawList.AddText(ImGui.GetFont(), 20f, headerPos, headerColor, headerText);

        currentY += headerSize.Y + padding * 1.5f;

        // Message
        var message = $"Are you sure you want to disband {state.ReligionInfo?.ReligionName ?? "this guild"}?\n\nThis action is permanent and cannot be undone.\nAll members will be removed and the guild will be deleted.";
        var messageLines = message.Split('\n');
        foreach (var line in messageLines)
        {
            // Skip null or empty lines, or use a space for ImGui to handle properly
            var displayLine = string.IsNullOrEmpty(line) ? " " : line;
            var lineSize = ImGui.CalcTextSize(displayLine);
            var linePos = new Vector2(dialogX + (dialogWidth - lineSize.X) / 2, currentY);
            var lineColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
            drawList.AddText(ImGui.GetFont(), 15f, linePos, lineColor, displayLine);
            currentY += lineSize.Y + 6f;
        }

        currentY = dialogY + dialogHeight - padding - 40f;

        // Buttons
        const float buttonWidth = 150f;
        const float buttonHeight = 40f;
        const float buttonSpacing = 20f;
        var totalButtonWidth = buttonWidth * 2 + buttonSpacing;
        var buttonsStartX = dialogX + (dialogWidth - totalButtonWidth) / 2;

        // Cancel button
        if (ButtonRenderer.DrawButton(drawList, "Cancel", buttonsStartX, currentY, buttonWidth, buttonHeight, isPrimary: true, enabled: true))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            state.ShowDisbandConfirm = false;
        }

        // Disband button
        var disbandButtonX = buttonsStartX + buttonWidth + buttonSpacing;
        if (ButtonRenderer.DrawButton(drawList, "Disband", disbandButtonX, currentY, buttonWidth, buttonHeight, isPrimary: false, enabled: true, customColor: ColorPalette.Red))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            state.ShowDisbandConfirm = false;
            onDisband.Invoke();
        }
    }
}
