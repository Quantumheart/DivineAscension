using System;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Altar;

/// <summary>
///     Clears the player's recorded shrine position when their shrine is broken and
///     ensures the shrine item is returned to the placer's inventory (instead of being
///     dropped). Subscribes to <see cref="AltarEventEmitter.OnAltarBroken"/>; the regular
///     <c>AltarDestructionHandler</c> ignores breaks at non-holy-site positions so the
///     events do not collide.
/// </summary>
public class CaravanShrineDestructionHandler : IDisposable
{
    private readonly AltarEventEmitter _emitter;
    private readonly ILoggerWrapper _logger;
    private readonly IPlayerMessengerService _messenger;
    private readonly IPlayerProgressionDataManager _progression;
    private readonly IWorldService _worldService;

    public CaravanShrineDestructionHandler(
        ILoggerWrapper logger,
        AltarEventEmitter emitter,
        IPlayerProgressionDataManager progression,
        IWorldService worldService,
        IPlayerMessengerService messenger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emitter = emitter ?? throw new ArgumentNullException(nameof(emitter));
        _progression = progression ?? throw new ArgumentNullException(nameof(progression));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
    }

    public void Dispose()
    {
        _emitter.OnAltarBroken -= OnAltarBroken;
    }

    public void Initialize()
    {
        _emitter.OnAltarBroken += OnAltarBroken;
        _logger.Notification($"{SystemConstants.LogPrefix} CaravanShrineDestructionHandler initialized");
    }

    internal void OnAltarBroken(IServerPlayer player, BlockPos pos)
    {
        try
        {
            var world = _worldService.World;
            var block = world.BlockAccessor.GetBlock(pos);
            if (block?.Code == null) return;
            if (!CaravanShrinePlacementHandler.IsCaravanShrineCode(block.Code)) return;

            var progression = _progression.GetOrCreatePlayerData(player.PlayerUID);

            if (!progression.IsPlacedCaravanShrineAt(pos.X, pos.Y, pos.Z))
            {
                // Someone else's shrine, or a stale entry. Still clear the world block so we
                // don't leave a duplicate, and don't refund (the breaker isn't the owner).
                return;
            }

            progression.ClearPlacedCaravanShrine();

            // Suppress vanilla drop and hand the shrine back so it can't be duped via lava etc.
            world.BlockAccessor.SetBlock(0, pos);

            var refund = new ItemStack(block, 1);
            if (!player.InventoryManager.TryGiveItemstack(refund))
            {
                _worldService.SpawnItemEntity(refund, player.Entity.Pos.XYZ);
            }

            _messenger.SendMessage(player,
                LocalizationService.Instance.Get("caravanshrine.returned"),
                EnumChatType.CommandSuccess);

            _logger.Notification(
                $"{SystemConstants.LogPrefix} {player.PlayerName} broke their Caravan Shrine at {pos}");
        }
        catch (Exception ex)
        {
            _logger.Error($"{SystemConstants.LogPrefix} CaravanShrineDestructionHandler error: {ex.Message}");
        }
    }
}
