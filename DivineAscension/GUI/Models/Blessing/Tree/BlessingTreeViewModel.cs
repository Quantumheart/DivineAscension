using System.Collections.Generic;
using PantheonWars.GUI.State;
using PantheonWars.Models;

namespace PantheonWars.GUI.Models.Blessing.Tree;

/// <summary>
///     View model for the split blessing tree renderer. Immutable snapshot of inputs for rendering.
/// </summary>
public readonly record struct BlessingTreeViewModel(
    ScrollState PlayerTreeScroll,
    ScrollState ReligionTreeScroll,
    IReadOnlyDictionary<string, BlessingNodeState> PlayerBlessingStates,
    IReadOnlyDictionary<string, BlessingNodeState> ReligionBlessingStates,
    float X,
    float Y,
    float Width,
    float Height,
    float DeltaTime,
    string? SelectedBlessingId);