using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Milestones;
using DivineAscension.GUI.UI.Renderers.Civilization;
using ImGuiNET;

namespace DivineAscension.GUI.Managers.Civilization;

/// <summary>
///     Draws the civilization milestone chapter and reduces its events.
///     Owned by <see cref="CivilizationStateManager" />.
/// </summary>
internal sealed class CivilizationMilestonesPresenter(CivilizationStateManager owner)
{
    [ExcludeFromCodeCoverage]
    public void Draw(float x, float y, float width, float height)
    {
        var vm = new CivilizationMilestoneViewModel(
            owner.CurrentCivilizationName,
            owner.State.MilestoneState.Rank,
            owner.State.MilestoneState.Progress,
            owner.State.MilestoneState.Bonuses,
            owner.State.MilestoneState.IsLoading,
            owner.State.MilestoneState.ErrorMsg,
            owner.State.MilestoneState.ScrollY,
            x, y, width, height);

        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationMilestoneRenderer.Draw(vm, drawList);
        ProcessEvents(result.Events);
    }

    public void ProcessEvents(IReadOnlyList<MilestoneEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case MilestoneEvent.RefreshClicked:
                    owner.RequestMilestoneProgress();
                    break;
                case MilestoneEvent.ScrollChanged sc:
                    owner.State.MilestoneState.ScrollY = sc.NewScrollY;
                    break;
            }
    }
}
