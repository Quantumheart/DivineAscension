using System;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.UI.Components;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Inputs;
using PantheonWars.GUI.UI.Components.Overlays;
using PantheonWars.GUI.UI.Renderers.Components;
using PantheonWars.GUI.UI.Utilities;
using PantheonWars.Models.Enum;
using Vintagestory.API.Client;
using PantheonWars.GUI.UI.Components.Lists;

namespace PantheonWars.GUI.UI.Renderers.Religion;

/// <summary>
///     Renderer for managing player's own religion
///     Migrates functionality from ReligionManagementOverlay
/// </summary>
internal static class ReligionMyReligionRenderer
{
    public static float Draw(
        BlessingDialogManager manager,
        ICoreClientAPI api,
        float x, float y, float width, float height)
    {
        var state = manager.ReligionState;
        var drawList = ImGui.GetWindowDrawList();
        var currentY = y;

        // Loading state
        if (state.IsMyReligionLoading)
        {
            TextRenderer.DrawInfoText(drawList, "Loading religion data...", x, currentY + 8f, width);
            return height;
        }

        var religion = state.MyReligionInfo;
        if (religion == null || !religion.HasReligion)
        {
            TextRenderer.DrawInfoText(drawList, "You are not in a religion. Browse or create one!", x, currentY + 8f, width);
            return height;
        }

        // Prepare top-level scroll for long content in My Religion tab
        const float scrollbarWidth = 16f;
        var contentHeightEstimate = ComputeContentHeight(religion.IsFounder);
        var maxScroll = MathF.Max(0f, contentHeightEstimate - height);

        // Mouse wheel scroll when hovering the tab content
        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width && mousePos.Y >= y && mousePos.Y <= y + height;
        var scrollY = state.MyReligionScrollY;
        if (isHover)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                scrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
            }
        }

        // Clip to visible area and offset drawing by scroll
        drawList.PushClipRect(new System.Numerics.Vector2(x, y), new System.Numerics.Vector2(x + width, y + height), true);
        currentY = y - scrollY;

        // === RELIGION HEADER ===
        TextRenderer.DrawLabel(drawList, religion.ReligionName, x, currentY, 20f, ColorPalette.Gold);
        currentY += 32f;

        // Info grid
        var leftCol = x;
        var rightCol = x + width / 2f;

        // Deity
        TextRenderer.DrawLabel(drawList, "Deity:", leftCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), religion.Deity);

        // Member count
        TextRenderer.DrawLabel(drawList, "Members:", rightCol, currentY, 13f, ColorPalette.Grey);
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(rightCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), religion.Members.Count.ToString());

        currentY += 22f;

        // Founder
        TextRenderer.DrawLabel(drawList, "Founder:", leftCol, currentY, 13f, ColorPalette.Grey);
        // Resolve founder player name from members list, fall back to UID if unknown
        var founderName = religion.Members.Find(m => m.PlayerUID == religion.FounderUID)?.PlayerName
                           ?? religion.FounderUID;
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(leftCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Gold), founderName);

        // Prestige
        TextRenderer.DrawLabel(drawList, "Prestige:", rightCol, currentY, 13f, ColorPalette.Grey);
        var prestigeProgress = manager.GetReligionPrestigeProgress();
        drawList.AddText(ImGui.GetFont(), 13f, new Vector2(rightCol + 80f, currentY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White),
            $"{prestigeProgress.CurrentPrestige} (Rank {prestigeProgress.CurrentRank})");

        currentY += 28f;

        // === DESCRIPTION SECTION ===
        if (religion.IsFounder)
        {
            TextRenderer.DrawLabel(drawList, "Description (editable):", x, currentY, 14f, ColorPalette.White);
            currentY += 22f;

            const float descHeight = 80f;
            state.Description = TextInput.DrawMultiline(drawList, "##religionDescription", state.Description,
                x, currentY, width, descHeight, 500);
            currentY += descHeight + 5f;

            // Save Description button
            var saveButtonWidth = 150f;
            var saveButtonX = x + width - saveButtonWidth;
            var trimmedDesc = (state.Description ?? string.Empty).Trim();
            var originalDesc = religion.Description ?? string.Empty;
            var hasChanges = !string.Equals(trimmedDesc, originalDesc, StringComparison.Ordinal);
            if (ButtonRenderer.DrawButton(drawList, "Save Description", saveButtonX, currentY, saveButtonWidth, 32f, false, hasChanges))
            {
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);
                // Send description update to server
                manager.RequestEditReligionDescription(religion.ReligionUID, trimmedDesc);
                // Optimistically update local model to reflect change immediately; server will refresh as well
                religion.Description = trimmedDesc;
                state.Description = trimmedDesc;
                api.ShowChatMessage("Religion description saved.");
            }

            currentY += 40f;
        }
        else
        {
            // Read-only description
            TextRenderer.DrawLabel(drawList, "Description:", x, currentY, 14f, ColorPalette.White);
            currentY += 22f;

            var desc = string.IsNullOrEmpty(religion.Description) ? "[No description set]" : religion.Description;
            TextRenderer.DrawInfoText(drawList, desc, x, currentY, width);
            currentY += 40f;
        }

        // === MEMBER LIST SECTION ===
        TextRenderer.DrawLabel(drawList, "Members:", x, currentY, 15f, ColorPalette.Gold);
        currentY += 25f;

        const float memberListHeight = 180f;
        state.MemberScrollY = MemberListRenderer.Draw(
            drawList, api, x, currentY, width, memberListHeight,
            religion.Members, state.MemberScrollY,
            // Kick callback
            religion.IsFounder ? (memberUID) =>
            {
                // Store for confirmation
                state.KickConfirmPlayerUID = memberUID;
                var member = religion.Members.Find(m => m.PlayerUID == memberUID);
                state.KickConfirmPlayerName = member?.PlayerName ?? memberUID;
            } : null,
            // Ban callback
            religion.IsFounder ? (memberUID) =>
            {
                // Store for confirmation
                state.BanConfirmPlayerUID = memberUID;
                var member = religion.Members.Find(m => m.PlayerUID == memberUID);
                state.BanConfirmPlayerName = member?.PlayerName ?? memberUID;
            } : null
        );
        currentY += memberListHeight + 15f;

        // === BANNED PLAYERS SECTION (founder only) ===
        if (religion.IsFounder)
        {
            TextRenderer.DrawLabel(drawList, "Banned Players:", x, currentY, 15f, ColorPalette.Gold);
            currentY += 25f;

            const float banListHeight = 120f;
            var bannedPlayers = religion.BannedPlayers ?? new System.Collections.Generic.List<Network.PlayerReligionInfoResponsePacket.BanInfo>();
            state.BanListScrollY = BanListRenderer.Draw(
                drawList, api, x, currentY, width, banListHeight,
                bannedPlayers, state.BanListScrollY,
                // Unban callback
                (playerUID) =>
                {
                    api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                        api.World.Player.Entity, null, false, 8f, 0.5f);
                    manager.RequestReligionAction("unban", religion.ReligionUID, playerUID);
                }
            );
            currentY += banListHeight + 15f;
        }

        // === INVITE SECTION (founder only) ===
        if (religion.IsFounder)
        {
            TextRenderer.DrawLabel(drawList, "Invite Player:", x, currentY, 15f, ColorPalette.Gold);
            currentY += 22f;

            var inviteInputWidth = width - 120f;
            state.InvitePlayerName = TextInput.Draw(drawList, "##invitePlayer", state.InvitePlayerName,
                x, currentY, inviteInputWidth, 32f, "Player name...");

            // Invite button
            var inviteButtonX = x + inviteInputWidth + 10f;
            if (ButtonRenderer.DrawButton(drawList, "Invite", inviteButtonX, currentY, 100f, 32f, false,
                    !string.IsNullOrWhiteSpace(state.InvitePlayerName)))
            {
                if (!string.IsNullOrWhiteSpace(state.InvitePlayerName))
                {
                    api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                        api.World.Player.Entity, null, false, 8f, 0.5f);
                    manager.RequestReligionAction("invite", religion.ReligionUID, state.InvitePlayerName.Trim());
                    state.InvitePlayerName = "";
                }
            }

            currentY += 40f;
        }

        // === ACTION BUTTONS ===
        var buttonY = currentY;
        var buttonX = x;

        // Leave Religion button (always available)
        if (ButtonRenderer.DrawButton(drawList, "Leave Religion", buttonX, buttonY, 160f, 34f, false, true))
        {
            api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                api.World.Player.Entity, null, false, 8f, 0.5f);
            manager.RequestReligionAction("leave", religion.ReligionUID);
        }

        // Disband Religion button (founder only)
        if (religion.IsFounder)
        {
            if (ButtonRenderer.DrawButton(drawList, "Disband Religion", x + 170f, buttonY, 180f, 34f, false, true, ColorPalette.Red * 0.7f))
            {
                api.World.PlaySoundAt(new Vintagestory.API.Common.AssetLocation("pantheonwars:sounds/click"),
                    api.World.Player.Entity, null, false, 8f, 0.5f);
                state.ShowDisbandConfirm = true;
            }
        }

        currentY += 40f;

        // End of scrollable content
        drawList.PopClipRect();

        // Draw scrollbar if needed
        if (contentHeightEstimate > height)
        {
            Scrollbar.Draw(drawList, x + width - scrollbarWidth, y, scrollbarWidth, height, scrollY, maxScroll);
        }

        // Persist updated scroll position
        state.MyReligionScrollY = scrollY;

        // === CONFIRMATION OVERLAYS ===
        // Disband confirmation
        if (state.ShowDisbandConfirm)
        {
            DrawDisbandConfirmation(drawList, api, x, y, width, height, () =>
            {
                manager.RequestReligionAction("disband", religion.ReligionUID);
                state.ShowDisbandConfirm = false;
            }, () => { state.ShowDisbandConfirm = false; });
        }

        // Kick confirmation
        if (state.KickConfirmPlayerUID != null)
        {
            DrawKickConfirmation(drawList, api, x, y, width, height,
                state.KickConfirmPlayerName ?? state.KickConfirmPlayerUID,
                () =>
                {
                    manager.RequestReligionAction("kick", religion.ReligionUID, state.KickConfirmPlayerUID!);
                    state.KickConfirmPlayerUID = null;
                    state.KickConfirmPlayerName = null;
                }, () =>
                {
                    state.KickConfirmPlayerUID = null;
                    state.KickConfirmPlayerName = null;
                });
        }

        // Ban confirmation
        if (state.BanConfirmPlayerUID != null)
        {
            DrawBanConfirmation(drawList, api, x, y, width, height,
                state.BanConfirmPlayerName ?? state.BanConfirmPlayerUID,
                () =>
                {
                    manager.RequestReligionAction("ban", religion.ReligionUID, state.BanConfirmPlayerUID!);
                    state.BanConfirmPlayerUID = null;
                    state.BanConfirmPlayerName = null;
                }, () =>
                {
                    state.BanConfirmPlayerUID = null;
                    state.BanConfirmPlayerName = null;
                });
        }

        return height;
    }

    private static float ComputeContentHeight(bool isFounder)
    {
        float h = 0f;
        // Header
        h += 32f;
        // Info grid rows
        h += 22f;
        h += 28f;
        // Description section
        if (isFounder)
        {
            h += 22f; // label
            h += 80f; // box
            h += 5f;  // spacing
            h += 40f; // save button
        }
        else
        {
            h += 22f; // label
            h += 40f; // text approx
        }
        // Members list
        h += 25f; // label
        h += 180f; // list
        h += 15f; // spacing
        // Banned players
        if (isFounder)
        {
            h += 25f; // label
            h += 120f; // list
            h += 15f; // spacing
        }
        // Invite section
        if (isFounder)
        {
            h += 22f; // label
            h += 40f; // input+button
        }
        // Action buttons row
        h += 40f;

        return h;
    }

    private static void DrawDisbandConfirmation(ImDrawListPtr drawList, ICoreClientAPI api, float x, float y, float width, float height,
        Action onConfirm, Action onCancel)
    {
        ConfirmOverlay.Draw(
            "Disband Religion?",
            "This will permanently delete the religion and remove all members. This cannot be undone.",
            out var confirmed, out var cancelled,
            "Disband", "Cancel");

        if (confirmed) onConfirm();
        if (cancelled) onCancel();
    }

    private static void DrawKickConfirmation(ImDrawListPtr drawList, ICoreClientAPI api, float x, float y, float width, float height,
        string playerName, Action onConfirm, Action onCancel)
    {
        ConfirmOverlay.Draw(
            "Kick Player?",
            $"Remove {playerName} from the religion? They can rejoin if invited again.",
            out var confirmed, out var cancelled,
            "Kick", "Cancel");

        if (confirmed) onConfirm();
        if (cancelled) onCancel();
    }

    private static void DrawBanConfirmation(ImDrawListPtr drawList, ICoreClientAPI api, float x, float y, float width, float height,
        string playerName, Action onConfirm, Action onCancel)
    {
        ConfirmOverlay.Draw(
            "Ban Player?",
            $"Permanently ban {playerName} from the religion? They cannot rejoin unless unbanned.",
            out var confirmed, out var cancelled,
            "Ban", "Cancel");

        if (confirmed) onConfirm();
        if (cancelled) onCancel();
    }
}
