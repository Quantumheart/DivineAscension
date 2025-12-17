using System.Collections.Generic;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Models.Enum;
using DivineAscension.Models.Enum;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace DivineAscension.GUI.Managers;

public class SoundManager(ICoreClientAPI api) : ISoundManager
{
    private const float SoundRange = 8f;

    private static readonly Dictionary<SoundType, string> SoundPaths = new()
    {
        { SoundType.Click, "pantheonwars:sounds/click" },
        { SoundType.Error, "pantheonwars:sounds/error" },
        { SoundType.Unlock, "pantheonwars:sounds/unlock" },
        { SoundType.Tick, "pantheonwars:sounds/tick" },
        { SoundType.UnlockKhoras, "pantheonwars:sounds/deities/Khoras" },
        { SoundType.UnlockLysa, "pantheonwars:sounds/deities/Lysa" },
        { SoundType.UnlockAethra, "pantheonwars:sounds/deities/Aethra" },
        { SoundType.UnlockGaia, "pantheonwars:sounds/deities/Gaia" }
    };

    private static readonly Dictionary<SoundVolume, float> VolumeValues = new()
    {
        { SoundVolume.Quiet, 0.3f },
        { SoundVolume.Normal, 0.5f },
        { SoundVolume.Loud, 0.7f }
    };

    public void Play(SoundType sound, SoundVolume volume = SoundVolume.Normal)
    {
        if (!SoundPaths.TryGetValue(sound, out var path))
        {
            api.Logger.Warning($"Sound {sound} not found in SoundPaths dictionary");
            return;
        }

        api.World.PlaySoundAt(
            new AssetLocation(path),
            api.World.Player.Entity,
            null,
            false,
            SoundRange,
            VolumeValues[volume]
        );
    }

    public void PlayClick()
    {
        Play(SoundType.Click);
    }

    public void PlayError()
    {
        Play(SoundType.Error, SoundVolume.Quiet);
    }

    public void PlaySuccess()
    {
        Play(SoundType.Unlock, SoundVolume.Loud);
    }

    public void PlayDeityUnlock(DeityType deity)
    {
        var sound = deity switch
        {
            DeityType.Khoras => SoundType.UnlockKhoras,
            DeityType.Lysa => SoundType.UnlockLysa,
            DeityType.Aethra => SoundType.UnlockAethra,
            DeityType.Gaia => SoundType.UnlockGaia,
            _ => SoundType.Unlock
        };
        Play(sound, SoundVolume.Loud);
    }
}