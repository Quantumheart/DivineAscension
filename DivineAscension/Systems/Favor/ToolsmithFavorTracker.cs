using System;
using DivineAscension.API.Interfaces;
using DivineAscension.Models.Enum;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using DivineAscension.Systems.Toolsmith;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Favor;

/// <summary>
///     Awards Craft-domain favor for Toolsmith tool-related activities:
///     assembly, sharpening, disassembly, and reforging.
///     Favor scales with tool quality — read from Toolsmith's ItemStack attributes
///     (head/handle/binding max durability + max sharpness).
/// </summary>
public class ToolsmithFavorTracker(
    ILoggerWrapper logger,
    IWorldService worldService,
    IPlayerProgressionDataManager playerProgressionDataManager,
    IFavorSystem favorSystem,
    ToolsmithEventEmitter toolsmithEventEmitter)
    : IFavorTracker, IDisposable
{
    // Toolsmith attribute names (from Toolsmith.Utils.ToolsmithAttributes)
    private const string AttrToolSharpnessMax = "toolSharpnessMax";
    private const string AttrToolheadMaxDurability = "tinkeredToolHeadMaxDurability";
    private const string AttrToolhandleMaxDurability = "tinkeredToolHandleMaxDurability";
    private const string AttrToolbindingMaxDurability = "tinkeredToolBindingMaxDurability";

    // Quality divisor — higher means more quality needed per favor point.
    // Total max durability of a basic copper tool is ~2000, steel with good parts ~8000+.
    private const int AssemblyQualityDivisor = 2000;
    private const int SharpeningQualityDivisor = 4000;
    private const int DisassemblyQualityDivisor = 6000;
    private const int ReforgingQualityDivisor = 3000;

    // Minimum favor for any activity (even a basic tool is worth something)
    private const int MinFavor = 1;

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

    private void HandleToolAssembled(IServerPlayer player, BlockPos pos, ItemStack toolStack)
    {
        if (player == null || toolStack == null) return;

        var favor = CalculateQualityFavor(toolStack, AssemblyQualityDivisor);
        _favorSystem.AwardFavorForAction(player, "assembling a tinkered tool", favor, DeityDomain.Craft);

        _logger.Debug(
            $"[ToolsmithFavorTracker] Awarded {favor} favor to {player.PlayerName} " +
            $"for assembling {toolStack.Collectible?.Code?.Path} at {pos}");
    }

    private void HandleToolSharpened(IServerPlayer player, BlockPos pos, ItemStack toolStack)
    {
        if (player == null || toolStack == null) return;

        var favor = CalculateQualityFavor(toolStack, SharpeningQualityDivisor);
        _favorSystem.AwardFavorForAction(player, "sharpening a tool", favor, DeityDomain.Craft);

        _logger.Debug(
            $"[ToolsmithFavorTracker] Awarded {favor} favor to {player.PlayerName} " +
            $"for sharpening {toolStack.Collectible?.Code?.Path} at {pos}");
    }

    private void HandleToolDisassembled(IServerPlayer player, BlockPos pos, ItemStack toolStack)
    {
        if (player == null || toolStack == null) return;

        var favor = CalculateQualityFavor(toolStack, DisassemblyQualityDivisor);
        _favorSystem.AwardFavorForAction(player, "disassembling a tinkered tool", favor, DeityDomain.Craft);

        _logger.Debug(
            $"[ToolsmithFavorTracker] Awarded {favor} favor to {player.PlayerName} " +
            $"for disassembling a tool at {pos}");
    }

    private void HandleToolReforged(IServerPlayer player, BlockPos pos, ItemStack toolHeadStack)
    {
        if (player == null || toolHeadStack == null) return;

        var favor = CalculateQualityFavor(toolHeadStack, ReforgingQualityDivisor);
        _favorSystem.AwardFavorForAction(player, "reforging a tool head", favor, DeityDomain.Craft);

        _logger.Debug(
            $"[ToolsmithFavorTracker] Awarded {favor} favor to {player.PlayerName} " +
            $"for reforging a tool head at {pos}");
    }

    /// <summary>
    ///     Computes quality-based favor from the tool's Toolsmith attributes.
    ///     Reads max sharpness and per-part max durabilities from the ItemStack,
    ///     sums them as a quality score, and divides by the activity-specific divisor.
    ///     Falls back to MinFavor if no Toolsmith attributes are present.
    /// </summary>
    internal static int CalculateQualityFavor(ItemStack stack, int divisor)
    {
        var attrs = stack.Attributes;
        if (attrs == null) return MinFavor;

        var headMax = attrs.GetInt(AttrToolheadMaxDurability);
        var handleMax = attrs.GetInt(AttrToolhandleMaxDurability);
        var bindingMax = attrs.GetInt(AttrToolbindingMaxDurability);
        var maxSharpness = attrs.GetInt(AttrToolSharpnessMax);

        var totalQuality = headMax + handleMax + bindingMax + maxSharpness;
        if (totalQuality <= 0) return MinFavor;

        return Math.Max(MinFavor, totalQuality / divisor);
    }
}