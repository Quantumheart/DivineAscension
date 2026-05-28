using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Edit;
using DivineAscension.GUI.Models.Civilization.Info;
using DivineAscension.GUI.UI.Renderers.Civilization;
using DivineAscension.GUI.UI.Utilities;
using DivineAscension.Network;
using DivineAscension.Network.Civilization;
using ImGuiNET;

namespace DivineAscension.GUI.Managers.Civilization;

/// <summary>
///     Draws the player's own civilization info chapter (and the icon edit dialog)
///     and reduces their events. Owned by <see cref="CivilizationStateManager" />.
/// </summary>
internal sealed class CivilizationInfoPresenter(CivilizationStateManager owner)
{
    [ExcludeFromCodeCoverage]
    public void Draw(float x, float y, float width, float height)
    {
        var civ = owner.State.InfoState.Info;

        var memberReligions = civ?.MemberReligions
                              ?? new List<CivilizationInfoResponsePacket.MemberReligion>();
        var founderReligionName = string.Empty;
        if (!string.IsNullOrEmpty(owner.CivilizationFounderReligionUID))
        {
            foreach (var m in memberReligions)
            {
                if (m.ReligionId == owner.CivilizationFounderReligionUID)
                {
                    founderReligionName = m.ReligionName;
                    break;
                }
            }
        }
        if (string.IsNullOrEmpty(founderReligionName))
            founderReligionName = civ?.FounderReligionName ?? string.Empty;

        var vm = new CivilizationInfoViewModel(
            owner.State.InfoState.IsLoading,
            civ != null,
            civ?.CivId ?? string.Empty,
            civ?.Name ?? string.Empty,
            civ?.Icon ?? "default",
            civ?.Description ?? string.Empty,
            owner.State.InfoState.DescriptionText,
            owner.State.InfoState.IsEditingDescription,
            civ?.FounderName ?? string.Empty,
            founderReligionName,
            civ?.CreatedDate ?? System.DateTime.MinValue,
            owner.UserIsCivilizationFounder,
            civ?.Rank ?? 0,
            civ?.Ethos ?? 0,
            civ?.FounderEpithet ?? string.Empty,
            civ?.CapitalName ?? string.Empty,
            civ?.CapitalHolySiteId ?? string.Empty,
            owner.State.InfoState.IsEditingCapital,
            owner.State.InfoState.CapitalNameText,
            owner.State.InfoState.CapitalBindingText,
            owner.State.InfoState.IsCapitalSiteDropdownOpen,
            owner.State.HolySitesState.Browse.SitesByReligion,
            memberReligions,
            civ?.PendingInvites ?? new List<CivilizationInfoResponsePacket.PendingInvite>(),
            owner.State.InfoState.InviteReligionName ?? string.Empty,
            owner.State.ShowDisbandConfirm,
            owner.State.KickConfirmReligionId,
            civ?.Bonuses ?? new CivilizationBonusesDto(),
            owner.State.InfoState.ScrollY,
            owner.State.InfoState.MemberScrollY,
            x,
            y,
            width,
            height);

        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationInfoRenderer.Draw(vm, drawList);

        ProcessEvents(result.Events);

        // Draw edit dialog overlay if open
        if (owner.State.EditState.IsOpen) DrawEditDialog(x, y, width, height);
    }

    [ExcludeFromCodeCoverage]
    private void DrawEditDialog(float x, float y, float width, float height)
    {
        var civ = owner.State.InfoState.Info;
        if (civ == null) return;

        var vm = new CivilizationEditViewModel(
            civ.CivId,
            civ.Name,
            civ.Icon,
            owner.State.EditState.EditingIcon,
            x,
            y,
            width,
            height);

        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationEditRenderer.Draw(vm, drawList);

        ProcessEditEvents(result.Events);
    }

    public void ProcessEvents(IReadOnlyList<InfoEvent> events)
    {
        // Block civ info background interaction behind an open disband/kick confirm (#455).
        events = ModalInputGuard.FilterBackground(events);

        var civ = owner.State.InfoState.Info;
        var civId = civ?.CivId ?? string.Empty;

        foreach (var evt in events)
            switch (evt)
            {
                case InfoEvent.ScrollChanged sc:
                    owner.State.InfoState.ScrollY = sc.y;
                    break;

                case InfoEvent.MemberScrollChanged msc:
                    owner.State.InfoState.MemberScrollY = msc.y;
                    break;

                case InfoEvent.InviteReligionNameChanged irnc:
                    owner.State.InfoState.InviteReligionName = irnc.text;
                    break;

                case InfoEvent.InviteReligionClicked irc:
                    if (!string.IsNullOrWhiteSpace(irc.religionName) && !string.IsNullOrWhiteSpace(civId))
                    {
                        owner.RequestCivilizationAction("invite", civId, irc.religionName);
                        owner.State.InfoState.InviteReligionName = string.Empty;
                    }

                    break;

                case InfoEvent.DescriptionChanged dc:
                    owner.State.InfoState.DescriptionText = dc.newDescription;
                    break;

                case InfoEvent.EditDescriptionOpen:
                    owner.State.InfoState.IsEditingDescription = true;
                    owner.State.InfoState.DescriptionText = civ?.Description ?? string.Empty;
                    break;

                case InfoEvent.EditDescriptionCancel:
                    owner.State.InfoState.IsEditingDescription = false;
                    owner.State.InfoState.DescriptionText = civ?.Description ?? string.Empty;
                    break;

                case InfoEvent.SaveDescriptionClicked:
                    if (!string.IsNullOrWhiteSpace(civId))
                    {
                        owner.RequestCivilizationAction("setdescription", civId, "", "", "",
                            owner.State.InfoState.DescriptionText);
                    }

                    owner.State.InfoState.IsEditingDescription = false;
                    break;

                case InfoEvent.EditCapitalOpen:
                    owner.State.InfoState.IsEditingCapital = true;
                    owner.State.InfoState.CapitalNameText = civ?.CapitalName ?? string.Empty;
                    owner.State.InfoState.CapitalBindingText = civ?.CapitalHolySiteId ?? string.Empty;
                    // Lazy-fetch eligible holy sites if not already loaded
                    if (owner.State.HolySitesState.Browse.SitesByReligion.Count == 0)
                        owner.RequestCivilizationHolySites();
                    break;

                case InfoEvent.EditCapitalCancel:
                    owner.State.InfoState.IsEditingCapital = false;
                    owner.State.InfoState.IsCapitalSiteDropdownOpen = false;
                    owner.State.InfoState.CapitalNameText = civ?.CapitalName ?? string.Empty;
                    owner.State.InfoState.CapitalBindingText = civ?.CapitalHolySiteId ?? string.Empty;
                    break;

                case InfoEvent.CapitalNameChanged cnc:
                    owner.State.InfoState.CapitalNameText = cnc.text;
                    break;

                case InfoEvent.CapitalBindingChanged cbc:
                    owner.State.InfoState.CapitalBindingText = cbc.siteId ?? string.Empty;
                    break;

                case InfoEvent.ToggleCapitalSiteDropdown tcd:
                    owner.State.InfoState.IsCapitalSiteDropdownOpen = tcd.isOpen;
                    break;

                case InfoEvent.SaveCapitalClicked:
                    if (!string.IsNullOrWhiteSpace(civId))
                    {
                        owner.RequestCivilizationAction("setcapital", civId, "", "", "", "", -1,
                            owner.State.InfoState.CapitalNameText, owner.State.InfoState.CapitalBindingText);
                    }

                    owner.State.InfoState.IsEditingCapital = false;
                    owner.State.InfoState.IsCapitalSiteDropdownOpen = false;
                    break;

                case InfoEvent.LeaveClicked:
                    if (!string.IsNullOrWhiteSpace(civId))
                        owner.RequestCivilizationAction("leave");
                    break;

                case InfoEvent.EditIconClicked:
                    // Sigil edit dialog hidden until the ledger redesign — see #385.
                    // Keep the event handler so any lingering button click is a no-op
                    // instead of opening the legacy PNG-grid dialog.
                    break;

                case InfoEvent.DisbandOpened:
                    owner.State.ShowDisbandConfirm = true;
                    break;

                case InfoEvent.DisbandCancel:
                    owner.State.ShowDisbandConfirm = false;
                    break;

                case InfoEvent.DisbandConfirmed:
                    if (!string.IsNullOrWhiteSpace(civId))
                    {
                        owner.RequestCivilizationAction("disband", civId);
                        owner.State.ShowDisbandConfirm = false;
                    }

                    break;

                case InfoEvent.KickOpen ko:
                    owner.State.KickConfirmReligionId = ko.religionId;
                    break;

                case InfoEvent.KickCancel:
                    owner.State.KickConfirmReligionId = null;
                    break;

                case InfoEvent.KickConfirm kc:
                    if (!string.IsNullOrWhiteSpace(civId) &&
                        !string.IsNullOrWhiteSpace(owner.State.KickConfirmReligionId))
                    {
                        owner.RequestCivilizationAction("kick", civId, owner.State.KickConfirmReligionId);
                        owner.State.KickConfirmReligionId = null;
                    }

                    break;
            }
    }

    public void ProcessEditEvents(IReadOnlyList<EditEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case EditEvent.IconSelected iconSelected:
                    owner.State.EditState.EditingIcon = iconSelected.icon;
                    owner.SoundManager.PlayClick();
                    break;

                case EditEvent.SubmitClicked:
                    if (!string.IsNullOrWhiteSpace(owner.State.EditState.CivId) &&
                        !string.IsNullOrWhiteSpace(owner.State.EditState.EditingIcon))
                    {
                        owner.RequestCivilizationAction("updateicon", owner.State.EditState.CivId, "", "",
                            owner.State.EditState.EditingIcon);
                        owner.State.EditState.IsOpen = false;
                        owner.State.EditState.Reset();
                    }

                    break;

                case EditEvent.CancelClicked:
                    owner.State.EditState.IsOpen = false;
                    owner.State.EditState.Reset();
                    owner.SoundManager.PlayClick();
                    break;
            }
    }
}
