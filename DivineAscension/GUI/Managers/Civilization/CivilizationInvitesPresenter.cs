using System.Collections.Generic;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Invites;
using DivineAscension.GUI.UI.Renderers.Civilization;
using ImGuiNET;

namespace DivineAscension.GUI.Managers.Civilization;

/// <summary>
///     Draws the civilization invites list and reduces its events.
///     Owned by <see cref="CivilizationStateManager" />.
/// </summary>
internal sealed class CivilizationInvitesPresenter(CivilizationStateManager owner)
{
    public void Draw(float x, float y, float width, float height)
    {
        var vm = new CivilizationInvitesViewModel(
            owner.State.InviteState.MyInvites,
            owner.State.InviteState.IsLoading,
            owner.State.InviteState.InvitesScrollY,
            x,
            y,
            width,
            height);

        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationInvitesRenderer.Draw(vm, drawList);
        ProcessEvents(result.Events);
    }

    public void ProcessEvents(IReadOnlyList<InvitesEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case InvitesEvent.ScrollChanged sc:
                    owner.State.InviteState.InvitesScrollY = sc.y;
                    break;

                case InvitesEvent.AcceptInviteClicked aic:
                    owner.RequestCivilizationAction("accept", "", aic.inviteId);
                    break;

                case InvitesEvent.DeclineInviteClicked dic:
                    owner.RequestCivilizationAction("decline", "", dic.inviteId);
                    break;
            }
    }
}
