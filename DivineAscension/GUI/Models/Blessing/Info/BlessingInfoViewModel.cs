using System.Collections.Generic;
using DivineAscension.Models;

namespace DivineAscension.GUI.Models.Blessing.Info;

internal readonly struct BlessingInfoViewModel(
    BlessingNodeState? selectedBlessingState,
    Dictionary<string, BlessingNodeState> blessingStates,
    float x,
    float y,
    float width,
    float height,
    int playerFavor,
    int religionPrestige,
    bool isDescriptionExpanded = false)
{
    public BlessingNodeState? SelectedBlessingState { get; } = selectedBlessingState;
    public Dictionary<string, BlessingNodeState> BlessingStates { get; } = blessingStates;
    public float X { get; } = x;
    public float Y { get; } = y;
    public float Width { get; } = width;
    public float Height { get; } = height;
    public int PlayerFavor { get; } = playerFavor;
    public int ReligionPrestige { get; } = religionPrestige;

    /// <summary>
    ///     Whether the description block is currently expanded. When false the description is
    ///     truncated past <see cref="BlessingInfoSectionDescription"/>'s preview threshold and
    ///     followed by a "Read more ▾" affordance.
    /// </summary>
    public bool IsDescriptionExpanded { get; } = isDescriptionExpanded;
}
