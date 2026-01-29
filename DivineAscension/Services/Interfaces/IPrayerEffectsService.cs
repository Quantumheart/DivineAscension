using DivineAscension.Models.Enum;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DivineAscension.Services.Interfaces;

/// <summary>
/// Service for handling visual and audio effects during prayer interactions at altars.
/// Responsible for triggering emotes, spawning particles, and playing sounds.
/// </summary>
public interface IPrayerEffectsService
{
    /// <summary>
    /// Plays the full set of prayer effects: player bow emote, divine particles, and bell sound.
    /// </summary>
    /// <param name="player">The player who prayed</param>
    /// <param name="altarPosition">Position of the altar block</param>
    /// <param name="holySiteTier">Tier of the holy site (1=Shrine, 2=Temple, 3=Cathedral)</param>
    /// <param name="domain">The deity domain of the player's religion (determines particle color)</param>
    void PlayPrayerEffects(IPlayer player, BlockPos altarPosition, int holySiteTier, DeityDomain domain);
}
