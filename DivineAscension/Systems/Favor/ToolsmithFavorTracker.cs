using System;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Toolsmith;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

/// <summary>
///     Awards Craft-domain favor for Toolsmith tool-related activities:
///     assembly, sharpening, disassembly, and reforging.
///     Subscribes to events raised by BlockBehaviorDivineGrindstone,
///     BlockBehaviorDivineWorkbench, and CollectibleBehaviorDivineWhetstone.
/// </summary>
public class ToolsmithFavorTracker(
    ILoggerWrapper logger,
    IWorldService worldService,
    IPlayerProgressionDataManager playerProgressionDataManager,
    IFavorSystem favorSystem,
    ToolsmithEventEmitter toolsmithEventEmitter)
    : IFavorTracker, IDisposable
{
    // Favor values
    private const int FavorToolAssembled = 3;
    private const int FavorToolSharpened = 1;
    private const int FavorToolDisassembled = 1;
    private const int FavorToolReforged = 2;

    private readonly IFavorSystem _favorSystem =
        favorSystem ?? throw new ArgumentNullException(nameof(favorSystem));

    private readonly ILoggerWrapper _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPlayerProgressionDataManager _playerProgressionDataManager =
        playerProgressionDataManager ?? throw new ArgumentNullException(nameof(playerProgressionDataManager));

    private readonly ToolsmithEventEmitter _emitter =
        toolsmithEventEmitter ?? throw new ArgumentNullException(nameof(toolsmithEventEmitter));

    private readonly IWorldService
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));

    public DeityDomain DeityDomain { get; } = DeityDomain.Craft;

    public void Dispose()
    {
        _emitter.OnToolAssembled -= HandleToolAssembled;
        _emitter.OnToolSharpened -= HandleToolSharpened;
        _emitter.OnToolDisassembled -= HandleToolDisassembled;
        _emitter.OnToolReforged -= HandleToolReforged;
    }

    public void Initialize()
    {
        _emitter.OnToolAssembled += HandleToolAssembled;
        _emitter.OnToolSharpened += HandleToolSharpened;
        _emitter.OnToolDisassembled += HandleToolDisassembled;
        _emitter.OnToolReforged += HandleToolReforged;

        _logger.Notification("[DivineAscension] ToolsmithFavorTracker initialized");
    }

    private void HandleToolAssembled(IServerPlayer? player, BlockPos pos, ItemStack? toolStack)
    {
        if (player == null || toolStack == null) return;

        _favorSystem.AwardFavorForAction(player, "assembling a tinkered tool",
            FavorToolAssembled, DeityDomain.Craft);

        _logger.Debug(
            $"[ToolsmithFavorTracker] Awarded {FavorToolAssembled} favor to {player.PlayerName} " +
            $"for assembling a tool at {pos}");
    }

    private void HandleToolSharpened(IServerPlayer? player, BlockPos pos, ItemStack? toolStack)
    {
        if (player == null || toolStack == null) return;

        _favorSystem.AwardFavorForAction(player, "sharpening a tool",
            FavorToolSharpened, DeityDomain.Craft);

        _logger.Debug(
            $"[ToolsmithFavorTracker] Awarded {FavorToolSharpened} favor to {player.PlayerName} " +
            $"for sharpening {toolStack.Collectible?.Code?.Path} at {pos}");
    }

    private void HandleToolDisassembled(IServerPlayer? player, BlockPos pos, ItemStack? toolStack)
    {
        if (player == null || toolStack == null) return;

        _favorSystem.AwardFavorForAction(player, "disassembling a tinkered tool",
            FavorToolDisassembled, DeityDomain.Craft);

        _logger.Debug(
            $"[ToolsmithFavorTracker] Awarded {FavorToolDisassembled} favor to {player.PlayerName} " +
            $"for disassembling a tool at {pos}");
    }

    private void HandleToolReforged(IServerPlayer? player, BlockPos pos, ItemStack? toolHeadStack)
    {
        if (player == null || toolHeadStack == null) return;

        _favorSystem.AwardFavorForAction(player, "reforging a tool head",
            FavorToolReforged, DeityDomain.Craft);

        _logger.Debug(
            $"[ToolsmithFavorTracker] Awarded {FavorToolReforged} favor to {player.PlayerName} " +
            $"for reforging a tool head at {pos}");
    }
}