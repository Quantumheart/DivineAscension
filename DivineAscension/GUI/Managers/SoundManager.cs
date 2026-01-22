using System.Collections.Generic;
using DivineAscension.GUI.Interfaces;
using DivineAscension.GUI.Models.Enum;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace DivineAscension.GUI.Managers;

public class SoundManager : ISoundManager
{
    private readonly ICoreClientAPI _api;
    private readonly ILoggerWrapper? _logger;
    private const float SoundRange = 8f;

    private static readonly Dictionary<SoundType, string> SoundPaths = new()
    {
        { SoundType.Click, "divineascension:sounds/click" },
        { SoundType.Error, "divineascension:sounds/error" },
        { SoundType.Unlock, "divineascension:sounds/unlock" },
        { SoundType.Tick, "divineascension:sounds/tick" },
        { SoundType.UnlockCraft, "divineascension:sounds/deities/Craft" },
        { SoundType.UnlockWild, "divineascension:sounds/deities/Wild" },
        { SoundType.UnlockHarvest, "divineascension:sounds/deities/Harvest" },
        { SoundType.UnlockStone, "divineascension:sounds/deities/Stone" },
        { SoundType.UnlockConquest, "divineascension:sounds/deities/Conquest" }
    };

    private static readonly Dictionary<SoundVolume, float> VolumeValues = new()
    {
        { SoundVolume.Quiet, 0.3f },
        { SoundVolume.Normal, 0.5f },
        { SoundVolume.Loud, 0.7f }
    };

    /// <summary>
    ///     Creates a new SoundManager instance.
    /// </summary>
    /// <param name="api">Client API for playing sounds</param>
    /// <param name="logger">Optional logger for warnings (defaults to GuiDialog.Logger)</param>
    public SoundManager(ICoreClientAPI api, ILoggerWrapper? logger = null)
    {
        _api = api;
        _logger = logger ?? GuiDialog.Logger;
    }

    public void Play(SoundType sound, SoundVolume volume = SoundVolume.Normal)
    {
        if (!SoundPaths.TryGetValue(sound, out var path))
        {
            _logger?.Warning($"Sound {sound} not found in SoundPaths dictionary");
            return;
        }

        _api.World.PlaySoundAt(
            new AssetLocation(path),
            _api.World.Player.Entity,
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
            DeityDomain.Craft => SoundType.UnlockCraft,
            DeityDomain.Wild => SoundType.UnlockWild,
            DeityDomain.Harvest => SoundType.UnlockHarvest,
            DeityDomain.Stone => SoundType.UnlockStone,
            DeityDomain.Conquest => SoundType.UnlockConquest,
            _ => SoundType.Unlock
        };
        Play(sound, SoundVolume.Loud);
    }
}