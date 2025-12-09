using PantheonWars.Models;

namespace PantheonWars.GUI.Models.Blessing.Actions;

public readonly struct BlessingActionsViewModel(BlessingNodeState? blessingNodeState, float x, float y)
{
    public BlessingNodeState? BlessingNodeState { get; } = blessingNodeState;
    public float X { get; } = x;
    public float Y { get; } = y;
}