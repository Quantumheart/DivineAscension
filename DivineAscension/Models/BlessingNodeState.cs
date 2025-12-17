namespace PantheonWars.Models;

/// <summary>
///     Represents the visual/UI state of a blessing node in the blessing tree
/// </summary>
public class BlessingNodeState
{
    public BlessingNodeState(Blessing blessing)
    {
        Blessing = blessing;
        VisualState = BlessingNodeVisualState.Locked;
        IsUnlocked = false;
        CanUnlock = false;
    }

    /// <summary>
    ///     Reference to the blessing this state represents
    /// </summary>
    public Blessing Blessing { get; set; }

    /// <summary>
    ///     Current visual state of the node
    /// </summary>
    public BlessingNodeVisualState VisualState { get; set; }

    /// <summary>
    ///     Whether the blessing is unlocked
    /// </summary>
    public bool IsUnlocked { get; set; }

    /// <summary>
    ///     Whether the blessing can be unlocked (prerequisites met, rank sufficient)
    /// </summary>
    public bool CanUnlock { get; set; }

    /// <summary>
    ///     X position in the tree layout (calculated by BlessingTreeLayout)
    /// </summary>
    public float PositionX { get; set; }

    /// <summary>
    ///     Y position in the tree layout (calculated by BlessingTreeLayout)
    /// </summary>
    public float PositionY { get; set; }

    /// <summary>
    ///     Width of the node (for hit detection)
    /// </summary>
    public float Width { get; set; } = 64f;

    /// <summary>
    ///     Height of the node (for hit detection)
    /// </summary>
    public float Height { get; set; } = 64f;

    /// <summary>
    ///     Current glow animation alpha (0-1) for unlockable blessings
    ///     Animated via lerp in Phase 5
    /// </summary>
    public float GlowAlpha { get; set; } = 0f;

    /// <summary>
    ///     Whether the node is currently being hovered
    /// </summary>
    public bool IsHovered { get; set; }

    /// <summary>
    ///     Whether the node is currently selected
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    ///     Tier/level of this blessing in the tree (for vertical layout)
    ///     Tier 1 = top, Tier 4 = bottom
    /// </summary>
    public int Tier { get; set; }

    /// <summary>
    ///     Update visual state based on unlock status and unlock eligibility
    /// </summary>
    public void UpdateVisualState()
    {
        if (IsUnlocked)
            VisualState = BlessingNodeVisualState.Unlocked;
        else if (CanUnlock)
            VisualState = BlessingNodeVisualState.Unlockable;
        else
            VisualState = BlessingNodeVisualState.Locked;
    }
}

/// <summary>
///     Visual states for blessing nodes
/// </summary>
public enum BlessingNodeVisualState
{
    /// <summary>
    ///     Blessing is locked (greyed out, prerequisites not met)
    /// </summary>
    Locked,

    /// <summary>
    ///     Blessing can be unlocked (green glow, player meets requirements)
    /// </summary>
    Unlockable,

    /// <summary>
    ///     Blessing is already unlocked (gold/active color)
    /// </summary>
    Unlocked
}