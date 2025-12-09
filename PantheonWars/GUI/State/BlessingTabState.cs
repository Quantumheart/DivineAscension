using System.Collections.Generic;
using PantheonWars.Models;

namespace PantheonWars.GUI.State;

public class BlessingTabState
{
    public BlessingTreeState TreeState { get; } = new();
    public BlessingInfoState InfoState { get; } = new();
    public Dictionary<string, BlessingNodeState> PlayerBlessingStates { get; } = new();
    public Dictionary<string, BlessingNodeState> ReligionBlessingStates { get; } = new();

    public void Reset()
    {
        TreeState.Reset();
        InfoState.Reset();
        PlayerBlessingStates.Clear();
        ReligionBlessingStates.Clear();
    }
}

public class BlessingTreeState
{
    public string? SelectedBlessingId { get; set; }
    public string? HoveringBlessingId { get; set; }
    public ScrollState PlayerScrollState { get; } = new();
    public ScrollState ReligionScrollState { get; } = new();


    public void Reset()
    {
        SelectedBlessingId = null;
        HoveringBlessingId = null;
        PlayerScrollState.Reset();
        ReligionScrollState.Reset();
    }
}

public class BlessingInfoState
{
    public void Reset()
    {
        // No state currently - info panel is display-only
    }
}