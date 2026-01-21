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
    /// Progress for each requirement in the ritual.
    /// Key: RequirementId (e.g., "copper_ingots")
    /// Value: ItemProgress tracking quantity and contributors
    /// </summary>
    [ProtoMember(3)]
    public Dictionary<string, ItemProgress> Progress { get; set; } = new();
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
