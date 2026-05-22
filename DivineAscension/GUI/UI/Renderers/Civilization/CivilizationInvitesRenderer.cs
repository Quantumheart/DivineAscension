using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Events.Letters;
using DivineAscension.GUI.Models.Civilization.Invites;
using DivineAscension.GUI.Models.Letters;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Civilization;

/// <summary>
///     Thin adapter that maps the civilization invite view model into the
///     shared <see cref="LettersRenderer" /> and translates its events back
///     into civilization-scoped <see cref="InvitesEvent" />s. Realms aren't
///     deity-aligned, so each letter's glyph is a heraldic banner rather
///     than a deity-domain mark.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class CivilizationInvitesRenderer
{
    public static CivilizationInvitesRendererResult Draw(
        CivilizationInvitesViewModel vm,
        ImDrawListPtr drawList)
    {
        var letters = new List<LetterEntry>(vm.Invites?.Count ?? 0);
        if (vm.Invites != null)
            foreach (var invite in vm.Invites)
            {
                // Inviting civilization's name is carried in ReligionName for
                // realm-targeting invites (see CivilizationNetworkHandler).
                var sender = LocalizationService.Instance.Get(
                    LocalizationKeys.UI_CIVILIZATION_INVITES_FROM, invite.ReligionName);
                letters.Add(new LetterEntry(
                    Id: invite.InviteId,
                    SenderText: sender,
                    GlyphPainter: DrawBannerGlyph,
                    QuoteLine: LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_QUOTE)));
            }

        var lettersVm = new LettersViewModel(
            letters: letters,
            title: LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_TITLE),
            intro: LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_INTRO),
            closingLine: LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_FOOTER_CLOSING),
            acceptLabel: LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_ACCEPT_BUTTON),
            refuseLabel: LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_DECLINE_BUTTON),
            loadingText: LocalizationService.Instance.Get(LocalizationKeys.UI_CIVILIZATION_INVITES_LOADING),
            isLoading: vm.IsLoading,
            scrollY: vm.ScrollY,
            x: vm.X, y: vm.Y, width: vm.Width, height: vm.Height);

        var lettersResult = LettersRenderer.Draw(drawList, lettersVm);

        var events = new List<InvitesEvent>(lettersResult.Events.Count);
        foreach (var evt in lettersResult.Events)
            events.Add(evt switch
            {
                LettersEvent.AcceptClicked a => new InvitesEvent.AcceptInviteClicked(a.Id),
                LettersEvent.RefuseClicked r => new InvitesEvent.DeclineInviteClicked(r.Id),
                LettersEvent.ScrollChanged s => new InvitesEvent.ScrollChanged(s.NewScrollY),
                _ => (InvitesEvent)null!
            });

        return new CivilizationInvitesRendererResult(events, lettersResult.RenderedHeight);
    }

    private static void DrawBannerGlyph(ImDrawListPtr drawList, Vector2 min, Vector2 max)
    {
        var cx = (min.X + max.X) * 0.5f;
        var cy = (min.Y + max.Y) * 0.5f;
        var size = MathF.Min(max.X - min.X, max.Y - min.Y);
        ChromeRenderer.DrawBanner(drawList, cx, cy, size, ColorPalette.White);
    }
}
