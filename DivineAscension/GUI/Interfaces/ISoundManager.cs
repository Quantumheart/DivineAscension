using PantheonWars.GUI.Models.Enum;
using PantheonWars.Models.Enum;

namespace PantheonWars.GUI.Interfaces;

public interface ISoundManager
{
    /// <summary>
    ///     Plays a sound at the player's position with the specified volume.
    /// </summary>
    /// <param name="sound">The type of sound to play</param>
    /// <param name="volume">The volume level (defaults to Normal)</param>
    void Play(SoundType sound, SoundVolume volume = SoundVolume.Normal);

    /// <summary>
    ///     Plays a click sound at normal volume.
    /// </summary>
    void PlayClick();

    /// <summary>
    ///     Plays an error sound at quiet volume.
    /// </summary>
    void PlayError();

    /// <summary>
    ///     Plays a success/unlock sound at loud volume.
    /// </summary>
    void PlaySuccess();

    /// <summary>
    ///     Plays a deity-specific unlock sound based on the deity type.
    /// </summary>
    /// <param name="deity">The deity type to play the unlock sound for</param>
    void PlayDeityUnlock(DeityType deity);
}