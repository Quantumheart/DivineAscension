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
    float vowsPageScrollY = 0f,
    float blessingsPageScrollY = 0f,
    int unlockedPlayerCount = 0,
    int maxBlessingSlots = 0,
    BlessingNodeState? pendingUnlockState = null,
    BlessingNodeState? pendingUnlearnState = null,
    IReadOnlyList<string>? pendingUnlearnCascadeNames = null,
    int pendingUnlearnRefundTotal = 0
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

    /// <summary>Vertical scroll position for the Vows page chapter (I.iii).</summary>
    public float VowsPageScrollY { get; } = vowsPageScrollY;

    /// <summary>Vertical scroll position for the III.ii Blessings page chapter.</summary>
    public float BlessingsPageScrollY { get; } = blessingsPageScrollY;

    /// <summary>Count of unlocked personal blessings across all deities, for the "Unlocked: X / max" slot header (#446).</summary>
    public int UnlockedPlayerCount { get; } = unlockedPlayerCount;

    /// <summary>Maximum personal blessing unlock slots (favor rank + prestige bonus). 0 when not yet synced.</summary>
    public int MaxBlessingSlots { get; } = maxBlessingSlots;

    /// <summary>
    ///     The blessing awaiting unlock confirmation (#453), or null when no dialog is open.
    ///     When set, the page renders a modal confirmation summarizing the favor/prestige spend.
    /// </summary>
    public BlessingNodeState? PendingUnlockState { get; } = pendingUnlockState;

    /// <summary>
    ///     The blessing awaiting unlearn confirmation (#459), or null when no dialog is open.
    ///     When set, the page renders a modal confirmation summarizing the favor reclaimed.
    /// </summary>
    public BlessingNodeState? PendingUnlearnState { get; } = pendingUnlearnState;

    /// <summary>
    ///     Names of every blessing in the pending unlearn cascade (#460), target first, or null
    ///     when no unlearn dialog is open. Count > 1 means dependent children will also be struck.
    /// </summary>
    public IReadOnlyList<string>? PendingUnlearnCascadeNames { get; } = pendingUnlearnCascadeNames;

    /// <summary>Total spendable favor the player will reclaim across the pending unlearn cascade (#460).</summary>
    public int PendingUnlearnRefundTotal { get; } = pendingUnlearnRefundTotal;
}
