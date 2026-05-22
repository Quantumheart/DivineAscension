using System;
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
    private const long WritingSuppressPageTurnMs = 600;
    private long _lastWritingTickMs = long.MinValue;

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
        { SoundType.UnlockConquest, "divineascension:sounds/deities/Conquest" },
        { SoundType.PageTurn, "divineascension:sounds/page-turn" },
        { SoundType.Writing, "divineascension:sounds/writing" }
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

        // Suppress page-turn if a writing sound just played (save/unlock often triggers nav change).
        if (sound == SoundType.PageTurn &&
            Environment.TickCount64 - _lastWritingTickMs < WritingSuppressPageTurnMs)
            return;

        if (sound == SoundType.Writing)
            _lastWritingTickMs = Environment.TickCount64;

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
        // Click sound disconnected from UI; writing sound plays only on save/unlock via PlaySuccess/PlayDeityUnlock.
    }

    public void PlayError()
    {
        Play(SoundType.Error, SoundVolume.Quiet);
    }

    public void PlaySuccess()
    {
        Play(SoundType.Writing, SoundVolume.Normal);
    }

    public void PlayDeityUnlock(DeityDomain deity)
    {
        _ = deity;
        Play(SoundType.Writing, SoundVolume.Normal);
    }
}