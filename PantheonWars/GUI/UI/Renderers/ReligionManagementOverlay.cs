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
///     Overlay for managing religion (leader only)
///     Displays member list, kick, invite, edit description, disband
/// </summary>
[ExcludeFromCodeCoverage]
[Obsolete("This overlay has been migrated to ReligionMyReligionRenderer in the BlessingDialog tab system. Use BlessingDialog's Religion tab instead.")]
internal static class ReligionManagementOverlay
{
    // State
    private static readonly ReligionManagementState _state = new();

    /// <summary>
    ///     Initialize/reset overlay state
    /// </summary>
    public static void Initialize()
    {
        _state.Reset();
    }

    /// <summary>
    ///     Update religion info from server
    /// </summary>
    public static void UpdateReligionInfo(PlayerReligionInfoResponsePacket info)
    {
        _state.UpdateReligionInfo(info);
    }

    /// <summary>
    ///     Draw the religion management overlay
    /// </summary>
    /// <param name="api">Client API</param>
    /// <param name="windowWidth">Parent window width</param>
    /// <param name="windowHeight">Parent window height</param>
    /// <param name="onClose">Callback when close clicked</param>
    /// <param name="onKickMember">Callback when kick clicked (memberUID)</param>
    /// <param name="onBanMember">Callback when ban clicked (memberUID)</param>
    /// <param name="onUnbanMember">Callback when unban clicked (playerUID)</param>
    /// <param name="onInvitePlayer">Callback when invite clicked (playerName)</param>
    /// <param name="onEditDescription">Callback when save description clicked (description)</param>
    /// <param name="onDisband">Callback when disband confirmed</param>
    /// <param name="onRequestRefresh">Callback to request fresh data</param>
    /// <returns>True if overlay should remain open</returns>
    public static bool Draw(
        ICoreClientAPI api,
        int windowWidth,
        int windowHeight,
        Action onClose,
        Action<string> onKickMember,
        Action<string> onBanMember,
        Action<string> onUnbanMember,
        Action<string> onInvitePlayer,
        Action<string> onEditDescription,
        Action onDisband,
        Action onRequestRefresh)
    {
        const float overlayWidth = 700f;
        const float overlayHeight = 750f;
        const float padding = 20f;

        var windowPos = ImGui.GetWindowPos();
        var overlayX = windowPos.X + (windowWidth - overlayWidth) / 2;
        var overlayY = windowPos.Y + (windowHeight - overlayHeight) / 2;

        var drawList = ImGui.GetForegroundDrawList();

        // Draw semi-transparent background
        var bgStart = windowPos;
        var bgEnd = new Vector2(windowPos.X + windowWidth, windowPos.Y + windowHeight);
        var bgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0f, 0f, 0f, 0.8f));
        drawList.AddRectFilled(bgStart, bgEnd, bgColor);

        // Draw main panel
        var panelStart = new Vector2(overlayX, overlayY);
        var panelEnd = new Vector2(overlayX + overlayWidth, overlayY + overlayHeight);
        var panelColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Background);
        drawList.AddRectFilled(panelStart, panelEnd, panelColor, 8f);

        // Draw border
        var borderColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.7f);
        drawList.AddRect(panelStart, panelEnd, borderColor, 8f, ImDrawFlags.None, 2f);

        // Create invisible child window for input handling
        ImGui.SetNextWindowPos(panelStart);
        ImGui.SetNextWindowSize(new Vector2(overlayWidth, overlayHeight));
        ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0, 0, 0, 0)); // Transparent background
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.BeginChild("##religion_mgmt_overlay", new Vector2(overlayWidth, overlayHeight), false, ImGuiWindowFlags.NoScrollbar);

        // Check if data loaded
        if (_state.ReligionInfo == null || !_state.ReligionInfo.HasReligion)
        {
            DrawLoadingOrError(drawList, overlayX, overlayY, overlayWidth, overlayHeight, onClose, onRequestRefresh, api);
            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
            return true;
        }

        // Disband confirmation dialog
        if (_state.ShowDisbandConfirm)
        {
            var result = DrawDisbandConfirmation(drawList, api, overlayX, overlayY, overlayWidth, overlayHeight, onDisband);
            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.PopStyleColor();
            return result;
        }

        var currentY = overlayY + padding;

        // === HEADER ===
        var headerText = $"Manage {_state.ReligionInfo.ReligionName}";
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

        currentY += headerSize.Y + padding;

        // === MEMBER LIST ===
        DrawSectionLabel(drawList, "Members", overlayX + padding, currentY);
        currentY += 25f;

        const float memberListHeight = 200f;
        var members = _state.ReligionInfo?.Members ?? new List<PlayerReligionInfoResponsePacket.MemberInfo>();
        _state.MemberScrollY = MemberListRenderer.Draw(drawList, api, overlayX + padding, currentY,
            overlayWidth - padding * 2, memberListHeight, members, _state.MemberScrollY, onKickMember, onBanMember);
        currentY += memberListHeight + padding;

        // === BANNED PLAYERS LIST ===
        DrawSectionLabel(drawList, "Banned Players", overlayX + padding, currentY);
        currentY += 25f;

        const float banListHeight = 150f;
        var bannedPlayers = _state.ReligionInfo?.BannedPlayers ?? new List<PlayerReligionInfoResponsePacket.BanInfo>();
        _state.BanListScrollY = BanListRenderer.Draw(drawList, api, overlayX + padding, currentY,
            overlayWidth - padding * 2, banListHeight, bannedPlayers, _state.BanListScrollY, onUnbanMember);
        currentY += banListHeight + padding;

        // === INVITE PLAYER ===
        DrawSectionLabel(drawList, "Invite Player", overlayX + padding, currentY);
        currentY += 25f;

        var inviteInputWidth = overlayWidth - padding * 2 - 120f;
        _state.InvitePlayerName = TextInput.Draw(drawList, "##invite_input", _state.InvitePlayerName, overlayX + padding, currentY, inviteInputWidth, 32f, "Player name...");

        // Invite button
        var inviteButtonX = overlayX + padding + inviteInputWidth + 10f;
        if (ButtonRenderer.DrawButton(drawList, "Invite", inviteButtonX, currentY, 100f, 32f, false, !string.IsNullOrWhiteSpace(_state.InvitePlayerName)))
        {
            if (!string.IsNullOrWhiteSpace(_state.InvitePlayerName))
            {
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);
                onInvitePlayer.Invoke(_state.InvitePlayerName.Trim());
                _state.InvitePlayerName = "";
            }
        }

        currentY += 40f;

        // === DESCRIPTION ===
        DrawSectionLabel(drawList, "Description", overlayX + padding, currentY);
        currentY += 25f;

        const float descHeight = 80f;
        _state.Description = TextInput.DrawMultiline(drawList, "##description_input", _state.Description, overlayX + padding, currentY, overlayWidth - padding * 2, descHeight, 500);
        currentY += descHeight + 5f;

        // Save Description button
        var saveButtonWidth = 150f;
        var saveButtonX = overlayX + overlayWidth - padding - saveButtonWidth;
        if (ButtonRenderer.DrawButton(drawList, "Save Description", saveButtonX, currentY, saveButtonWidth, 32f, false, true))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            onEditDescription.Invoke(_state.Description);
        }

        currentY += 40f;

        // Error message
        if (!string.IsNullOrEmpty(_state.ErrorMessage))
        {
            DrawErrorText(drawList, _state.ErrorMessage, overlayX + padding, currentY, overlayWidth - padding * 2);
            currentY += 30f;
        }

        // === DISBAND BUTTON ===
        const float disbandButtonWidth = 150f;
        const float disbandButtonHeight = 36f;
        var disbandButtonX = overlayX + padding;
        var disbandButtonY = overlayY + overlayHeight - padding - disbandButtonHeight;

        if (ButtonRenderer.DrawButton(drawList, "Disband Religion", disbandButtonX, disbandButtonY, disbandButtonWidth, disbandButtonHeight, false, true, ColorPalette.Red))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            _state.ShowDisbandConfirm = true;
        }

        ImGui.EndChild();
        ImGui.PopStyleVar();
        ImGui.PopStyleColor();
        return true;
    }

    /// <summary>
    ///     Draw loading or error state
    /// </summary>
    private static void DrawLoadingOrError(ImDrawListPtr drawList, float x, float y, float width, float height, Action onClose, Action onRequestRefresh, ICoreClientAPI api)
    {
        var currentY = y + 20f;

        // Header with close button
        var headerText = "Manage Religion";
        var headerPos = new Vector2(x + 20f, currentY);
        var headerColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold);
        drawList.AddText(ImGui.GetFont(), 20f, headerPos, headerColor, headerText);

        const float closeButtonSize = 24f;
        var closeButtonX = x + width - 20f - closeButtonSize;
        if (ButtonRenderer.DrawCloseButton(drawList, closeButtonX, currentY, closeButtonSize))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            onClose.Invoke();
        }

        currentY += 60f;

        // Loading/Error message
        var message = _state.ReligionInfo == null ? "Loading religion data..." : "You are not in a religion.";
        var messageSize = ImGui.CalcTextSize(message);
        var messagePos = new Vector2(x + (width - messageSize.X) / 2, y + (height - messageSize.Y) / 2);
        var messageColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey);
        drawList.AddText(messagePos, messageColor, message);

        // Refresh button if not loading
        if (_state.ReligionInfo != null)
        {
            const float buttonWidth = 120f;
            const float buttonHeight = 36f;
            var buttonX = x + (width - buttonWidth) / 2;
            var buttonY = messagePos.Y + 40f;

            if (ButtonRenderer.DrawButton(drawList, "Refresh", buttonX, buttonY, buttonWidth, buttonHeight, false, true))
            {
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);
                onRequestRefresh.Invoke();
            }
        }
    }

    /// <summary>
    ///     Draw disband confirmation dialog
    /// </summary>
    private static bool DrawDisbandConfirmation(ImDrawListPtr drawList, ICoreClientAPI api, float x, float y, float width, float height, Action onDisband)
    {
        const float confirmWidth = 400f;
        const float confirmHeight = 200f;
        var confirmX = x + (width - confirmWidth) / 2;
        var confirmY = y + (height - confirmHeight) / 2;

        // Draw confirm background
        var confirmStart = new Vector2(confirmX, confirmY);
        var confirmEnd = new Vector2(confirmX + confirmWidth, confirmY + confirmHeight);
        var confirmBg = ImGui.ColorConvertFloat4ToU32(ColorPalette.DarkBrown);
        drawList.AddRectFilled(confirmStart, confirmEnd, confirmBg, 8f);

        // Draw border
        var confirmBorder = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red);
        drawList.AddRect(confirmStart, confirmEnd, confirmBorder, 8f, ImDrawFlags.None, 3f);

        var currentY = confirmY + 20f;

        // Warning text
        var warningText = "Disband Religion?";
        var warningSize = ImGui.CalcTextSize(warningText);
        var warningPos = new Vector2(confirmX + (confirmWidth - warningSize.X) / 2, currentY);
        var warningColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Red);
        drawList.AddText(ImGui.GetFont(), 18f, warningPos, warningColor, warningText);
        currentY += warningSize.Y + 20f;

        // Description
        var descText = "This will permanently delete the religion\nand remove all members. This cannot be undone.";
        var lines = descText.Split('\n');
        foreach (var line in lines)
        {
            var lineSize = ImGui.CalcTextSize(line);
            var linePos = new Vector2(confirmX + (confirmWidth - lineSize.X) / 2, currentY);
            var lineColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.White);
            drawList.AddText(linePos, lineColor, line);
            currentY += lineSize.Y + 5f;
        }

        currentY += 20f;

        // Buttons
        const float buttonWidth = 120f;
        const float buttonHeight = 36f;
        const float buttonSpacing = 20f;
        var buttonsStartX = confirmX + (confirmWidth - buttonWidth * 2 - buttonSpacing) / 2;
        var buttonY = confirmY + confirmHeight - 20f - buttonHeight;

        // Cancel button
        if (ButtonRenderer.DrawButton(drawList, "Cancel", buttonsStartX, buttonY, buttonWidth, buttonHeight, false, true))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            _state.ShowDisbandConfirm = false;
        }

        // Confirm button
        var confirmButtonX = buttonsStartX + buttonWidth + buttonSpacing;
        if (ButtonRenderer.DrawButton(drawList, "Disband", confirmButtonX, buttonY, buttonWidth, buttonHeight, false, true, ColorPalette.Red))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            onDisband.Invoke();
            _state.ShowDisbandConfirm = false;
            return false; // Close overlay
        }

        return true;
    }

    /// <summary>
    ///     Draw section label
    /// </summary>
    private static void DrawSectionLabel(ImDrawListPtr drawList, string text, float x, float y)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold * 0.9f);
        drawList.AddText(ImGui.GetFont(), 15f, new Vector2(x, y), textColor, text);
    }

    /// <summary>
    ///     Draw error text
    /// </summary>
    private static void DrawErrorText(ImDrawListPtr drawList, string text, float x, float y, float width)
    {
        var textColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.3f, 0.3f, 1f));
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(x, y), textColor, text);
    }
}
