using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using PantheonWars.GUI.Events.Religion;
using PantheonWars.GUI.Models.Religion.Invites;
using PantheonWars.GUI.UI.Components.Buttons;
using PantheonWars.GUI.UI.Components.Lists;
using PantheonWars.GUI.UI.Utilities;

namespace PantheonWars.GUI.UI.Renderers.Religion;

/// <summary>
/// Pure renderer for religion invitations list
/// Takes immutable view model, returns events representing user interactions
/// </summary>
internal static class ReligionInvitesRenderer
{
    /// <summary>
    /// Renders the invites list
    /// Pure function: ViewModel + DrawList → RenderResult
    /// </summary>
    public static ReligionInvitesRenderResult Draw(
        ReligionInvitesViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var events = new List<InvitesEvent>();
        var currentY = viewModel.Y;

        // === HEADER ===
        TextRenderer.DrawLabel(
            drawList,
            "Your Religion Invitations",
            viewModel.X,
            currentY,
            18f,
            ColorPalette.White);
        currentY += 26f;

        // === HELP TEXT ===
        TextRenderer.DrawInfoText(
            drawList,
            "These are invitations you've received from religions.",
            viewModel.X,
            currentY,
            viewModel.Width);
        currentY += 32f;

        // === EMPTY STATE ===
        if (!viewModel.HasInvites)
        {
            TextRenderer.DrawInfoText(
                drawList,
                viewModel.EmptyStateMessage,
                viewModel.X,
                currentY + 8f,
                viewModel.Width);

            return new ReligionInvitesRenderResult(events, viewModel.Height);
        }

        // === SCROLLABLE LIST ===
        var listHeight = viewModel.Height - (currentY - viewModel.Y);

        // Convert IReadOnlyList to List for ScrollableList.Draw
        var invitesList = viewModel.Invites.ToList();

        var newScrollY = ScrollableList.Draw(
            drawList,
            viewModel.X,
            currentY,
            viewModel.Width,
            listHeight,
            invitesList,
            80f, // itemHeight
            10f, // itemSpacing
            viewModel.ScrollY,
            (invite, cx, cy, cw, ch) =>
                DrawInviteCard(invite, cx, cy, cw, ch, drawList, viewModel.IsLoading, events),
            loadingText: viewModel.IsLoading ? "Loading invitations..." : null
        );

        // Emit scroll event if changed
        if (newScrollY != viewModel.ScrollY)
        {
            events.Add(new InvitesEvent.ScrollChanged(newScrollY));
        }

        return new ReligionInvitesRenderResult(events, viewModel.Height);
    }

    /// <summary>
    /// Draws a single invite card
    /// </summary>
    private static void DrawInviteCard(
        InviteData invite,
        float x, float y, float width, float height,
        ImDrawListPtr drawList,
        bool isLoading,
        List<InvitesEvent> events)
    {
        // === CARD BACKGROUND ===
        drawList.AddRectFilled(
            new Vector2(x, y),
            new Vector2(x + width, y + height),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.LightBrown),
            4f);

        // === CARD CONTENT ===
        TextRenderer.DrawLabel(
            drawList,
            "Invitation to Religion",
            x + 12f,
            y + 8f,
            16f);

        drawList.AddText(
            ImGui.GetFont(),
            14f,
            new Vector2(x + 14f, y + 30f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            $"Religion: {invite.ReligionName}");

        drawList.AddText(
            ImGui.GetFont(),
            14f,
            new Vector2(x + 14f, y + 48f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey),
            invite.FormattedExpiration);

        // === ACTION BUTTONS ===
        var buttonsEnabled = !isLoading;
        var buttonY = y + height - 32f;

        // Accept button
        if (ButtonRenderer.DrawButton(
                drawList,
                "Accept",
                x + width - 180f,
                buttonY,
                80f,
                28f,
                true,
                buttonsEnabled))
        {
            events.Add(new InvitesEvent.AcceptInviteClicked(invite.InviteId));
        }

        // Decline button
        if (ButtonRenderer.DrawButton(
                drawList,
                "Decline",
                x + width - 90f,
                buttonY,
                80f,
                28f,
                false,
                buttonsEnabled))
        {
            events.Add(new InvitesEvent.DeclineInviteClicked(invite.InviteId));
        }
    }
}