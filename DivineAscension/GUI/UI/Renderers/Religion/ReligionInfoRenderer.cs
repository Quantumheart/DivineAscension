using System;
using System.Collections.Generic;
using System.Numerics;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Info;
using DivineAscension.GUI.Models.Religion.Member;
using DivineAscension.GUI.UI.Components.Lists;
using DivineAscension.GUI.UI.Components.Overlays;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Religion.Info;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
/// Pure renderer for managing player's own religion
/// Takes an immutable view model, returns events representing user interactions
/// Migrates functionality from ReligionManagementOverlay
/// </summary>
internal static class ReligionInfoRenderer
{
    /// <summary>
    /// Renders the religion info/management tab
    /// Pure function: ViewModel + DrawList → RenderResult
    /// </summary>
    public static ReligionInfoRenderResult Draw(
        ReligionInfoViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<InfoEvent>();
        var x = viewModel.X;
        var y = viewModel.Y;
        var width = viewModel.Width;
        var height = viewModel.Height;
        var currentY = y;

        // Loading state
        if (viewModel.IsLoading)
        {
            TextRenderer.DrawInfoText(drawList, "Loading religion data...", x, currentY + 8f, width);
            return new ReligionInfoRenderResult(events, height);
        }

        if (!viewModel.HasReligion)
        {
            TextRenderer.DrawInfoText(drawList, "You are not in a religion. Browse or create one!", x, currentY + 8f,
                width);
            return new ReligionInfoRenderResult(events, height);
        }

        // Prepare top-level scroll for long content in My Religion tab
        const float scrollbarWidth = 16f;
        var contentHeightEstimate = ComputeContentHeight(viewModel.IsFounder);
        var maxScroll = MathF.Max(0f, contentHeightEstimate - height);

        // Mouse wheel scroll when hovering the tab content
        var mousePos = ImGui.GetMousePos();
        var isHover = mousePos.X >= x && mousePos.X <= x + width && mousePos.Y >= y && mousePos.Y <= y + height;
        var scrollY = viewModel.ScrollY;
        if (isHover)
        {
            var wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                var newScrollY = Math.Clamp(scrollY - wheel * 30f, 0f, maxScroll);
                if (Math.Abs(newScrollY - scrollY) > 0.001f)
                {
                    scrollY = newScrollY;
                    events.Add(new InfoEvent.ScrollChanged(newScrollY));
                }
            }
        }

        // Clip to visible area and offset drawing by scroll
        drawList.PushClipRect(new Vector2(x, y), new Vector2(x + width, y + height), true);
        currentY = y - scrollY;

        // === HEADER AND INFO GRID ===
        currentY = ReligionInfoHeaderRenderer.Draw(viewModel, drawList, x, currentY, width);

        // === DESCRIPTION SECTION ===
        currentY = ReligionInfoDescriptionRenderer.Draw(viewModel, drawList, x, currentY, width, events);

        // === MEMBER LIST SECTION ===
        TextRenderer.DrawLabel(drawList, "Members:", x, currentY, 15f, ColorPalette.Gold);
        currentY += 25f;

        const float memberListHeight = 180f;
        var memberScrollY = DrawMemberList(
            drawList, viewModel, x, currentY, width, memberListHeight, events);
        if (Math.Abs(memberScrollY - viewModel.MemberScrollY) > 0.001f)
        {
            events.Add(new InfoEvent.MemberScrollChanged(memberScrollY));
        }

        currentY += memberListHeight + 15f;

        // === BANNED PLAYERS SECTION (founder only) ===
        if (viewModel.IsFounder)
        {
            TextRenderer.DrawLabel(drawList, "Banned Players:", x, currentY, 15f, ColorPalette.Gold);
            currentY += 25f;

            const float banListHeight = 120f;
            var banListScrollY = DrawBanList(
                drawList, viewModel, x, currentY, width, banListHeight, events);
            if (Math.Abs(banListScrollY - viewModel.BanListScrollY) > 0.001f)
            {
                events.Add(new InfoEvent.BanListScrollChanged(banListScrollY));
            }

            currentY += banListHeight + 15f;
        }

        // === INVITE SECTION (founder only) ===
        currentY = ReligionInfoInviteRenderer.Draw(viewModel, drawList, x, currentY, width, events);

        // === ACTION BUTTONS ===
        currentY = ReligionInfoActionsRenderer.Draw(viewModel, drawList, x, currentY, events);

        // End of scrollable content
        drawList.PopClipRect();

        // Draw scrollbar if needed
        if (contentHeightEstimate > height)
            Scrollbar.Draw(drawList, x + width - scrollbarWidth, y, scrollbarWidth, height, scrollY, maxScroll);

        // === CONFIRMATION OVERLAYS ===
        // Disband confirmation
        if (viewModel.ShowDisbandConfirm)
            DrawDisbandConfirmation(drawList, events);

        // Ban confirmation
        if (viewModel.BanConfirmPlayerUID != null)
            DrawBanConfirmation(drawList, viewModel.BanConfirmPlayerName ?? viewModel.BanConfirmPlayerUID,
                viewModel.BanConfirmPlayerUID, events);

        return new ReligionInfoRenderResult(events, height);
    }

    private static float DrawMemberList(
        ImDrawListPtr drawList,
        ReligionInfoViewModel viewModel,
        float x, float y, float width, float height,
        List<InfoEvent> events)
    {
        // Build VM for member list component
        var mlVm = new MemberListViewModel(
            x: x,
            y: y,
            width: width,
            height: height,
            scrollY: viewModel.MemberScrollY,
            members: viewModel.Members,
            viewModel.CurrentPlayerUID);

        var result = MemberListRenderer.Draw(mlVm, drawList);

        // Translate events
        var newScrollY = viewModel.MemberScrollY;
        foreach (var ev in result.Events)
        {
            switch (ev)
            {
                case MemberListEvent.ScrollChanged(var s):
                    newScrollY = s;
                    break;
                case MemberListEvent.KickClicked(var uid):
                {
                    var member = FindMemberByUid(viewModel.Members, uid);
                    var memberName = member?.PlayerName ?? uid;
                    events.Add(new InfoEvent.KickOpen(uid, memberName));
                    break;
                }
                case MemberListEvent.BanClicked(var uid):
                {
                    var member = FindMemberByUid(viewModel.Members, uid);
                    var memberName = member?.PlayerName ?? uid;
                    events.Add(new InfoEvent.BanOpen(uid, memberName));
                    break;
                }
            }
        }

        return newScrollY;
    }

    private static float DrawBanList(
        ImDrawListPtr drawList,
        ReligionInfoViewModel viewModel,
        float x, float y, float width, float height,
        List<InfoEvent> events)
    {
        return BanListRenderer.Draw(
            drawList,
            null!, // API not needed for pure rendering - will be refactored later
            x, y, width, height,
            new List<PlayerReligionInfoResponsePacket.BanInfo>(viewModel.BannedPlayers),
            viewModel.BanListScrollY,
            // Unban callback
            playerUid => { events.Add(new InfoEvent.UnbanClicked(playerUid)); }
        );
    }

    private static PlayerReligionInfoResponsePacket.MemberInfo? FindMemberByUid(
        IReadOnlyList<PlayerReligionInfoResponsePacket.MemberInfo> members,
        string memberUid)
    {
        foreach (var member in members)
        {
            if (member.PlayerUID == memberUid)
                return member;
        }

        return null;
    }

    private static float ComputeContentHeight(bool isFounder)
    {
        var h = 0f;
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
            h += 5f; // spacing
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

    private static void DrawDisbandConfirmation(
        ImDrawListPtr drawList,
        List<InfoEvent> events)
    {
        ConfirmOverlay.Draw(
            "Disband Religion?",
            "This will permanently delete the religion and remove all members. This cannot be undone.",
            out var confirmed, out var cancelled,
            "Disband");

        if (confirmed) events.Add(new InfoEvent.DisbandConfirm());
        if (cancelled) events.Add(new InfoEvent.DisbandCancel());
    }

    private static void DrawKickConfirmation(
        ImDrawListPtr drawList,
        string playerName,
        string playerUid,
        List<InfoEvent> events)
    {
        ConfirmOverlay.Draw(
            "Kick Player?",
            $"Remove {playerName} from the religion? They can rejoin if invited again.",
            out var confirmed, out var cancelled,
            "Kick");

        if (confirmed) events.Add(new InfoEvent.KickConfirm(playerUid));
        if (cancelled) events.Add(new InfoEvent.KickCancel());
    }

    private static void DrawBanConfirmation(
        ImDrawListPtr drawList,
        string playerName,
        string playerUid,
        List<InfoEvent> events)
    {
        ConfirmOverlay.Draw(
            "Ban Player?",
            $"Permanently ban {playerName} from the religion? They cannot rejoin unless unbanned.",
            out var confirmed, out var cancelled,
            "Ban");

        if (confirmed) events.Add(new InfoEvent.BanConfirm(playerUid));
        if (cancelled) events.Add(new InfoEvent.BanCancel());
    }
}