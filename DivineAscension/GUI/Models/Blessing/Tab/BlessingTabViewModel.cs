using System.Collections.Generic;
using DivineAscension.GUI.State;
using DivineAscension.Models;
using DivineAscension.Models.Enum;

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
    IReadOnlyDictionary<string, BlessingNodeState> playerBlessingStates,
    IReadOnlyDictionary<string, BlessingNodeState> religionBlessingStates,
    ScrollState playerTreeScrollState,
    ScrollState religionTreeScrollState,
    int playerFavor,
    int religionPrestige,
    DeityDomain activeDeity,
    DeityDomain patronDomain,
    IReadOnlyList<DeityBlessingSummary> deitySummaries
)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public int WindowWidth { get; } = windowWidth;
    public int WindowHeight { get; } = windowHeight;
    public float DeltaTime { get; } = deltaTime;

    public string? SelectedBlessingId { get; } = selectedBlessingId;
    public BlessingNodeState? SelectedBlessingState { get; } = selectedBlessingState;
    public IReadOnlyDictionary<string, BlessingNodeState> PlayerBlessingStates { get; } = playerBlessingStates;
    public IReadOnlyDictionary<string, BlessingNodeState> ReligionBlessingStates { get; } = religionBlessingStates;
    public ScrollState PlayerTreeScrollState { get; } = playerTreeScrollState;
    public ScrollState ReligionTreeScrollState { get; } = religionTreeScrollState;
    public int PlayerFavor { get; } = playerFavor;
    public int ReligionPrestige { get; } = religionPrestige;
    public DeityDomain ActiveDeity { get; } = activeDeity;
    public DeityDomain PatronDomain { get; } = patronDomain;
    public IReadOnlyList<DeityBlessingSummary> DeitySummaries { get; } = deitySummaries;
}
