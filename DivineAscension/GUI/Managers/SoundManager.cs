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
        { SoundType.Click, "divineascension:sounds/click" },
        { SoundType.Error, "divineascension:sounds/error" },
        { SoundType.Unlock, "divineascension:sounds/unlock" },
        { SoundType.Tick, "divineascension:sounds/tick" },
        { SoundType.UnlockKhoras, "divineascension:sounds/deities/Khoras" },
        { SoundType.UnlockLysa, "divineascension:sounds/deities/Lysa" },
        { SoundType.UnlockAethra, "divineascension:sounds/deities/Aethra" },
        { SoundType.UnlockGaia, "divineascension:sounds/deities/Gaia" }
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

    public void PlayDeityUnlock(DeityDomain deity)
    {
        var sound = deity switch
        {
            DeityDomain.Craft => SoundType.UnlockKhoras,
            DeityDomain.Wild => SoundType.UnlockLysa,
            DeityDomain.Harvest => SoundType.UnlockAethra,
            DeityDomain.Stone => SoundType.UnlockGaia,
            _ => SoundType.Unlock
        };
        Play(sound, SoundVolume.Loud);
    }
}