using System.Collections.Generic;
using DivineAscension.GUI.State;
using DivineAscension.Models;

namespace DivineAscension.GUI.Models.Blessing.Tab;

public readonly struct BlessingTabViewModel(
    // Layout
    float x,
    float y,
    float width,
    float height,
    int windowWidth,
    int windowHeight,
    float deltaTime,
    // Data
    string? selectedBlessingId,
    BlessingNodeState? selectedBlessingState,
    Dictionary<string, BlessingNodeState> playerBlessingStates,
    Dictionary<string, BlessingNodeState> religionBlessingStates,
    ScrollState playerTreeScrollState,
    ScrollState religionTreeScrollState
)
{
    // Layout
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public int WindowWidth { get; } = windowWidth;
    public int WindowHeight { get; } = windowHeight;
    public float DeltaTime { get; } = deltaTime;

    // Data
    public string? SelectedBlessingId { get; } = selectedBlessingId;
    public BlessingNodeState? SelectedBlessingState { get; } = selectedBlessingState;
    public Dictionary<string, BlessingNodeState> PlayerBlessingStates { get; } = playerBlessingStates;
    public Dictionary<string, BlessingNodeState> ReligionBlessingStates { get; } = religionBlessingStates;
    public ScrollState PlayerTreeScrollState { get; } = playerTreeScrollState;
    public ScrollState ReligionTreeScrollState { get; } = religionTreeScrollState;
}