using System.Collections.Generic;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Religion.Invites;
using DivineAscension.GUI.UI.Components.Buttons;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;
using static DivineAscension.GUI.UI.Utilities.FontSizes;

namespace DivineAscension.GUI.UI.Renderers.Religion.Letters;

/// <summary>
/// Paints each invite as an illuminated letter row: envelope primitive +
/// domain glyph, "From {ReligionName}", a default quoted line, and the
/// Accept / Refuse buttons. Adjacent letters separated by a single-diamond
/// ornamental divider.
/// </summary>
internal static class ReligionLettersListRenderer
{
    public const float EnvelopeSize = 18f;
    public const float GlyphSize = 16f;
    public const float GlyphGap = 6f;
    public const float MarkColumnWidth = EnvelopeSize + GlyphGap + GlyphSize + 8f;
    public const float HeaderLineHeight = 22f;
    public const float QuoteLineHeight = 20f;
    public const float ButtonHeight = 26f;
    public const float ButtonWidth = 88f;
    public const float ButtonGap = 10f;
    public const float ButtonTopSpacing = 6f;
    public const float ButtonBottomSpacing = 8f;
    public const float DividerHeight = 18f;
    public const float DividerYPadding = 4f;
    public const float RowLeftPadding = 16f;

    public const float RowHeight =
        HeaderLineHeight + QuoteLineHeight + ButtonTopSpacing + ButtonHeight + ButtonBottomSpacing;

    public static float Draw(
        ImDrawListPtr drawList,
        ReligionInvitesViewModel viewModel,
        float x,
        float y,
        float width,
        List<InvitesEvent> events)
    {
        var enabled = !viewModel.IsLoading;
        var currentY = y;

        for (var i = 0; i < viewModel.Invites.Count; i++)
        {
            var invite = viewModel.Invites[i];
            DrawLetter(drawList, invite,
                x + RowLeftPadding, currentY, width - RowLeftPadding,
                enabled, events);
            currentY += RowHeight;

            if (i < viewModel.Invites.Count - 1)
            {
                currentY = DrawSlimDivider(drawList, x, currentY, width);
            }
        }

        return currentY;
    }

    public static float MeasureHeight(ReligionInvitesViewModel viewModel)
    {
        if (viewModel.Invites.Count == 0) return 0f;
        var rows = viewModel.Invites.Count;
        return rows * RowHeight + (rows - 1) * DividerHeight;
    }

    private static void DrawLetter(
        ImDrawListPtr drawList,
        InviteData invite,
        float x, float y, float width,
        bool enabled,
        List<InvitesEvent> events)
    {
        // Envelope + domain glyph share a centred baseline at the header line.
        var markCy = y + HeaderLineHeight / 2f;
        ChromeRenderer.DrawEnvelope(drawList,
            x + EnvelopeSize / 2f, markCy, EnvelopeSize, ColorPalette.White);

        var glyphX = x + EnvelopeSize + GlyphGap;
        var glyphMin = new Vector2(glyphX, markCy - GlyphSize / 2f);
        var glyphMax = new Vector2(glyphX + GlyphSize, markCy + GlyphSize / 2f);
        DomainGlyphRenderer.Draw(drawList, invite.Domain, glyphMin, glyphMax, ColorPalette.White);

        var textX = x + MarkColumnWidth;
        var sender = LocalizationService.Instance.Get(
            LocalizationKeys.UI_RELIGION_INVITES_FROM, invite.ReligionName);
        drawList.AddText(ImGui.GetFont(), Body,
            new Vector2(textX, y + 2f),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.White), sender);

        var quoteY = y + HeaderLineHeight;
        var quote = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_QUOTE);
        drawList.AddText(ImGui.GetFont(), Body,
            new Vector2(textX, quoteY),
            ImGui.ColorConvertFloat4ToU32(ColorPalette.Grey), quote);

        var buttonY = quoteY + QuoteLineHeight + ButtonTopSpacing;
        var acceptLabel = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_ACCEPT);
        var refuseLabel = LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_DECLINE);

        if (ButtonRenderer.DrawButton(drawList, acceptLabel,
                textX, buttonY, ButtonWidth, ButtonHeight, true, enabled))
        {
            events.Add(new InvitesEvent.AcceptInviteClicked(invite.InviteId));
        }

        if (ButtonRenderer.DrawButton(drawList, refuseLabel,
                textX + ButtonWidth + ButtonGap, buttonY, ButtonWidth, ButtonHeight, false, enabled))
        {
            events.Add(new InvitesEvent.DeclineInviteClicked(invite.InviteId));
        }
    }

    private static float DrawSlimDivider(ImDrawListPtr drawList, float x, float y, float width)
    {
        // Tighter inset divider between adjacent letters so the section-level
        // divider above/below the list reads as the louder break.
        var inset = width * 0.20f;
        var dividerY = y + DividerYPadding;
        ChromeRenderer.DrawDivider(drawList,
            x + inset, dividerY, width - inset * 2f,
            ColorPalette.Gold * 0.35f);
        return y + DividerHeight;
    }
}
