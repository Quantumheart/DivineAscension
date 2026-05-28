using System.Collections.Generic;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Chronicle;
using DivineAscension.GUI.UI.Renderers.Civilization;
using DivineAscension.Network.Civilization;
using ImGuiNET;

namespace DivineAscension.GUI.Managers.Civilization;

/// <summary>
///     Draws the Chronicle chapter (#369). Read-only ledger of significant events;
///     reuses the same CivilizationInfoResponsePacket data as the Info pane. Owned
///     by <see cref="CivilizationStateManager" />.
/// </summary>
internal sealed class CivilizationChroniclePresenter(CivilizationStateManager owner)
{
    public void Draw(float x, float y, float width, float height)
    {
        var civ = owner.State.InfoState.Info;

        var vm = new CivilizationChronicleViewModel(
            owner.State.InfoState.IsLoading,
            civ != null,
            civ?.Name ?? string.Empty,
            civ?.Chronicle ?? new List<CivilizationInfoResponsePacket.ChronicleEntryDto>(),
            x, y, width, height,
            owner.State.InfoState.ChronicleScrollY);

        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationChronicleRenderer.Draw(vm, drawList);

        foreach (var ev in result.Events)
            if (ev is CivilizationChronicleEvent.ScrollChanged sc)
                owner.State.InfoState.ChronicleScrollY = sc.NewScrollY;
    }
}
