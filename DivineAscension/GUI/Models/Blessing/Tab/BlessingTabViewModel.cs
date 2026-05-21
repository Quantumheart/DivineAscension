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
    IReadOnlyList<DeityBlessingSummary> deitySummaries,
    int prestigeNextThreshold = 0,
    string? patronDeityName = null,
    bool isReligionFounder = false
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

    /// <summary>Total prestige required for the next prestige rank, for the right-aligned "{N} / {M}" balance on Vows.</summary>
    public int PrestigeNextThreshold { get; } = prestigeNextThreshold;

    /// <summary>Display name of the patron deity (e.g. "Stone"). Drives the "Of {Patron}" sub-heading on Vows.</summary>
    public string? PatronDeityName { get; } = patronDeityName;

    /// <summary>Whether the viewing player founded the religion; gates the [Swear] action on Vows.</summary>
    public bool IsReligionFounder { get; } = isReligionFounder;
}
