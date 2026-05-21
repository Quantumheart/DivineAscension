using System.Collections.Generic;
using DivineAscension.GUI.State;
using DivineAscension.Models;

namespace DivineAscension.GUI.Models.Blessing.Tree;

/// <summary>
///     View model for the single-tree blessing renderer. Hosting pages (III.ii Blessings
///     for the personal tree, I.iii Vows of the Order for the communal tree) pass in the
///     specific dictionary + scroll state to display; the renderer is otherwise kind-agnostic
///     and emits the generic <see cref="GUI.Events.Blessing.TreeEvent.ScrollChanged"/> event
///     for hosts to translate into page-specific scroll events.
/// </summary>
public readonly record struct BlessingTreeViewModel(
    ScrollState TreeScroll,
    IReadOnlyDictionary<string, BlessingNodeState> BlessingStates,
    float X,
    float Y,
    float Width,
    float Height,
    float DeltaTime,
    string? SelectedBlessingId,
    string PanelId,
    string PanelLabel = "",
    string BalanceText = "",
    bool ShowBalanceHeader = true);
