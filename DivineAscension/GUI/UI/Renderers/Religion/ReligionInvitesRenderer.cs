using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Constants;
using DivineAscension.GUI.Events.Letters;
using DivineAscension.GUI.Events.Religion;
using DivineAscension.GUI.Models.Letters;
using DivineAscension.GUI.Models.Religion.Invites;
using DivineAscension.GUI.UI.Renderers.Components;
using DivineAscension.GUI.UI.Renderers.Utilities;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Services;
using ImGuiNET;

namespace DivineAscension.GUI.UI.Renderers.Religion;

/// <summary>
///     Thin adapter that maps the religion invite view model into the shared
///     <see cref="LettersRenderer" /> and translates its events back into
///     religion-scoped <see cref="InvitesEvent" />s. Each letter's glyph is
///     the inviting religion's deity-domain mark.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class ReligionInvitesRenderer
{
    public static ReligionInvitesRenderResult Draw(
        ReligionInvitesViewModel viewModel,
        ImDrawListPtr drawList)
    {
        var letters = new List<LetterEntry>(viewModel.Invites.Count);
        foreach (var invite in viewModel.Invites)
        {
            var sender = LocalizationService.Instance.Get(
                LocalizationKeys.UI_RELIGION_INVITES_FROM, invite.ReligionName);
            var domain = invite.Domain;
            letters.Add(new LetterEntry(
                Id: invite.InviteId,
                SenderText: sender,
                GlyphPainter: (dl, min, max) => DomainGlyphRenderer.Draw(dl, domain, min, max, ColorPalette.White),
                QuoteLine: LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_QUOTE)));
        }

        var lettersVm = new LettersViewModel(
            letters: letters,
            title: LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_TITLE),
            intro: LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_INTRO),
            closingLine: LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_FOOTER_CLOSING),
            acceptLabel: LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_ACCEPT),
            refuseLabel: LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_DECLINE),
            loadingText: LocalizationService.Instance.Get(LocalizationKeys.UI_RELIGION_INVITES_LOADING),
            isLoading: viewModel.IsLoading,
            scrollY: viewModel.ScrollY,
            x: viewModel.X, y: viewModel.Y, width: viewModel.Width, height: viewModel.Height);

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

        return new ReligionInvitesRenderResult(events, lettersResult.RenderedHeight);
    }
}
