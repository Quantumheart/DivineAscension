using DivineAscension.Models.Enum;
using DivineAscension.Services.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of IPrayerEffectsService for testing.
/// Records all calls for verification without triggering actual visual/audio effects.
/// </summary>
public class FakePrayerEffectsService : IPrayerEffectsService
{
    /// <summary>
    /// List of all calls made to PlayPrayerEffects for verification.
    /// </summary>
    public List<PrayerEffectsCall> Calls { get; } = new();

    /// <inheritdoc />
    public void PlayPrayerEffects(IPlayer player, BlockPos altarPosition, int holySiteTier, DeityDomain domain)
    {
        Calls.Add(new PrayerEffectsCall(player, altarPosition, holySiteTier, domain));
    }

    /// <summary>
    /// Clears all recorded calls.
    /// </summary>
    public void Reset()
    {
        Calls.Clear();
    }

    /// <summary>
    /// Record of a single call to PlayPrayerEffects.
    /// </summary>
    public record PrayerEffectsCall(IPlayer Player, BlockPos AltarPosition, int HolySiteTier, DeityDomain Domain);
}
