using System.Collections.Generic;
using PantheonWars.Models;

namespace PantheonWars.GUI.Models.Blessing.Info;

internal readonly struct BlessingInfoViewModel(
    BlessingNodeState? selectedBlessingState,
    Dictionary<string, BlessingNodeState> blessingStates,
    float x,
    float y,
    float width,
    float height)
{
    public BlessingNodeState? SelectedBlessingState { get; } = selectedBlessingState;
    public Dictionary<string, BlessingNodeState> BlessingStates { get; } = blessingStates;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
}