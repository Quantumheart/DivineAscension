using DivineAscension.Data;
using DivineAscension.Models.Enum;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DivineAscension.Systems.Altar.Pipeline;

/// <summary>
/// Context object that flows through the prayer pipeline, accumulating state.
/// Input properties are immutable after creation; discovered/calculated properties are set by steps.
/// </summary>
public class PrayerContext
{
    // Input (immutable after creation)
    public required string PlayerUID { get; init; }
    public required string PlayerName { get; init; }
    public required BlockPos AltarPosition { get; init; }
    public required ItemStack? Offering { get; init; }
    public required long CurrentTime { get; init; }
    public required IPlayer Player { get; init; }

    // Discovered during pipeline (set by validation steps)
    public HolySiteData? HolySite { get; set; }
    public ReligionData? Religion { get; set; }
    public int HolySiteTier { get; set; }
    public double PrayerMultiplier { get; set; }
    public DeityDomain Domain { get; set; } = DeityDomain.None;

    // Calculated rewards (set by processing steps)
    public int OfferingBonus { get; set; }
    public int FavorAwarded { get; set; }
    public int PrestigeAwarded { get; set; }
    public float BuffMultiplier { get; set; }
    public bool OfferingRejectedDomain { get; set; }

    // Flags
    public bool ShouldConsumeOffering { get; set; }
    public bool ShouldUpdateCooldown { get; set; } = true;
    public bool IsRitualContribution { get; set; }

    // Result
    public bool IsComplete { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
}