using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.GUI.Events.Civilization;
using DivineAscension.GUI.Models.Civilization.Leaderboard;
using DivineAscension.GUI.UI.Renderers.Civilization;
using DivineAscension.Models.Enum;
using ImGuiNET;

namespace DivineAscension.GUI.Managers.Civilization;

/// <summary>
///     Draws the Standing of Realms leaderboard chapter and reduces its events.
///     Owned by <see cref="CivilizationStateManager" />; shared civ context, requests
///     and network handlers stay on the manager.
/// </summary>
internal sealed class CivilizationLeaderboardPresenter(CivilizationStateManager owner)
{
    /// <summary>Selectable leaderboard boards, in display order (#499).</summary>
    private static readonly LeaderboardMetric[] LeaderboardBoards =
    {
        LeaderboardMetric.Standing,
        LeaderboardMetric.Conquest,
        LeaderboardMetric.Endurance,
        LeaderboardMetric.Deeds
    };

    [ExcludeFromCodeCoverage]
    public void Draw(float x, float y, float width, float height)
    {
        var vm = new CivilizationLeaderboardViewModel(
            LeaderboardBoards,
            owner.State.LeaderboardState.SelectedBoard,
            owner.State.LeaderboardState.SelectedEntries,
            owner.State.LeaderboardState.SelectedViewerPosition,
            owner.State.LeaderboardState.TotalRealms,
            owner.State.LeaderboardState.IsLoading,
            owner.State.LeaderboardState.ErrorMsg,
            owner.State.LeaderboardState.ScrollY,
            x, y, width, height);

        var drawList = ImGui.GetWindowDrawList();
        var result = CivilizationLeaderboardRenderer.Draw(vm, drawList);
        ProcessEvents(result.Events);
    }

    public void ProcessEvents(IReadOnlyList<LeaderboardEvent> events)
    {
        foreach (var evt in events)
            switch (evt)
            {
                case LeaderboardEvent.RefreshClicked:
                    owner.RequestLeaderboard();
                    break;
                case LeaderboardEvent.ScrollChanged sc:
                    owner.State.LeaderboardState.ScrollY = sc.NewScrollY;
                    break;
                case LeaderboardEvent.BoardSelected bs:
                    if (owner.State.LeaderboardState.SelectedBoard != bs.Board)
                    {
                        // All boards arrive in one response, so switching is purely
                        // client-side — no re-request, just reset scroll and re-render.
                        owner.State.LeaderboardState.SelectedBoard = bs.Board;
                        owner.State.LeaderboardState.ScrollY = 0f;
                        owner.SoundManager.PlayClick();
                    }

                    break;
            }
    }
}
