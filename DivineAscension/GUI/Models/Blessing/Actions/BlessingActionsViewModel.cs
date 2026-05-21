using DivineAscension.Models;

namespace DivineAscension.GUI.Models.Blessing.Actions;

public readonly struct BlessingActionsViewModel(
    BlessingNodeState? blessingNodeState,
    float x,
    float y,
    int playerFavor,
    int religionPrestige,
    bool isReligionFounder = false)
{
    public BlessingNodeState? BlessingNodeState { get; } = blessingNodeState;
    public float X { get; } = x;
    public float Y { get; } = y;
    public int PlayerFavor { get; } = playerFavor;
    public int ReligionPrestige { get; } = religionPrestige;

    /// <summary>
    ///     Whether the viewing player founded the religion. Religion-kind unlocks
    ///     (the [Swear] action on I.iii — Vows of the Order) are founder-only;
    ///     non-founders see the button rendered as disabled with a tooltip.
    /// </summary>
    public bool IsReligionFounder { get; } = isReligionFounder;
}
