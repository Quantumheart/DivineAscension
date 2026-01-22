using System;
using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
/// Tracks progress of an active ritual at a holy site.
/// Stored in HolySiteData.ActiveRitual and persisted to world save.
/// </summary>
[ProtoContract]
public class RitualProgressData
{
    /// <summary>
    /// The ritual being performed (e.g., "craft_tier2_ritual")
    /// </summary>
    [ProtoMember(1)]
    public string RitualId { get; set; } = string.Empty;

    /// <summary>
    /// When the ritual was started
    /// </summary>
    [ProtoMember(2)]
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Progress for each step in the ritual.
    /// Key: StepId (e.g., "base_metals")
    /// Value: StepProgress tracking completion, discovery, and contributors
    /// </summary>
    [ProtoMember(3)]
    public Dictionary<string, StepProgress> Progress { get; set; } = new();
}

/// <summary>
/// Tracks progress for a single step within a ritual.
/// A step is complete when all its requirements are satisfied.
/// Steps start as undiscovered and are revealed when players offer matching items.
/// </summary>
[ProtoContract]
public class StepProgress
{
    /// <summary>
    /// Whether this step is complete (all requirements satisfied)
    /// </summary>
    [ProtoMember(1)]
    public bool IsComplete { get; set; }

    /// <summary>
    /// Whether this step has been discovered (visible to players)
    /// </summary>
    [ProtoMember(2)]
    public bool IsDiscovered { get; set; }

    /// <summary>
    /// Progress for each requirement within this step.
    /// Key: RequirementId (e.g., "copper_ingots")
    /// Value: ItemProgress tracking quantity and contributors
    /// </summary>
    [ProtoMember(3)]
    public Dictionary<string, ItemProgress> RequirementProgress { get; set; } = new();
}

/// <summary>
/// Tracks progress for a single requirement within a ritual.
/// Includes contributor tracking for UI display.
/// </summary>
[ProtoContract]
public class ItemProgress
{
    /// <summary>
    /// How many items have been contributed so far
    /// </summary>
    [ProtoMember(1)]
    public int QuantityContributed { get; set; }

    /// <summary>
    /// How many items are required to complete this requirement
    /// </summary>
    [ProtoMember(2)]
    public int QuantityRequired { get; set; }

    /// <summary>
    /// Tracks contributions by player.
    /// Key: Player UID
    /// Value: Quantity contributed by that player
    /// </summary>
    [ProtoMember(3)]
    public Dictionary<string, int> Contributors { get; set; } = new();
}
