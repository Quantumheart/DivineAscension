using System;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Services;
using DivineAscension.Systems.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Altar;

/// <summary>
///     Enforces server-side placement rules for the Caravan Shrine block. Subscribes to
///     <see cref="AltarEventEmitter.OnAltarPlaced"/> and filters for the
///     <c>caravanshrine</c> item — the regular <c>AltarPlacementHandler</c> ignores the
///     same event because its <c>IsAltarItem</c> filter only matches paths starting with
///     <c>altar</c>.
///
///     Rules:
///     <list type="bullet">
///         <item>Player must have unlocked <c>caravan_avatar_road</c>.</item>
///         <item>Only one shrine per player at a time.</item>
///         <item>Cannot place inside another religion's holy-site claim.</item>
///     </list>
/// </summary>
public class CaravanShrinePlacementHandler : IDisposable
{
    internal const string CaravanAvatarBlessingId = "caravan_avatar_road";
    internal const string ShrineCodePathPrefix = "caravanshrine";

    private readonly AltarEventEmitter _emitter;
    private readonly IHolySiteManager _holySiteManager;
    private readonly ILoggerWrapper _logger;
    private readonly IPlayerMessengerService _messenger;
    private readonly IPlayerProgressionDataManager _progression;
    private readonly IReligionManager _religionManager;
    private readonly IWorldService _worldService;

    public CaravanShrinePlacementHandler(
        ILoggerWrapper logger,
        AltarEventEmitter emitter,
        IPlayerProgressionDataManager progression,
        IReligionManager religionManager,
        IHolySiteManager holySiteManager,
        IWorldService worldService,
        IPlayerMessengerService messenger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emitter = emitter ?? throw new ArgumentNullException(nameof(emitter));
        _progression = progression ?? throw new ArgumentNullException(nameof(progression));
        _religionManager = religionManager ?? throw new ArgumentNullException(nameof(religionManager));
        _holySiteManager = holySiteManager ?? throw new ArgumentNullException(nameof(holySiteManager));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
        _messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
    }

    public void Dispose()
    {
        _emitter.OnAltarPlaced -= OnAltarPlaced;
    }

    public void Initialize()
    {
        _emitter.OnAltarPlaced += OnAltarPlaced;
        _logger.Notification($"{SystemConstants.LogPrefix} CaravanShrinePlacementHandler initialized");
    }

    internal static bool IsCaravanShrineItem(ItemStack? stack)
    {
        var path = stack?.Collectible?.Code?.Path;
        return path != null && path.StartsWith(ShrineCodePathPrefix, StringComparison.Ordinal);
    }

    internal void OnAltarPlaced(IServerPlayer player, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
    {
        try
        {
            if (!IsCaravanShrineItem(withItemStack)) return;

            var progression = _progression.GetOrCreatePlayerData(player.PlayerUID);

            if (!progression.IsBlessingUnlocked(CaravanAvatarBlessingId))
            {
                Reject(player, blockSel, "caravanshrine.error.no_blessing");
                return;
            }

            if (progression.HasPlacedCaravanShrine)
            {
                Reject(player, blockSel, "caravanshrine.error.already_placed");
                return;
            }

            // Holy site at the placement pos owned by a different religion → reject.
            var siteAtPos = _holySiteManager.GetHolySiteAtPosition(blockSel.Position);
            if (siteAtPos != null)
            {
                var ownerReligionUID = siteAtPos.ReligionUID;
                var playerReligion = _religionManager.GetPlayerReligion(player.PlayerUID);
                if (playerReligion == null || playerReligion.ReligionUID != ownerReligionUID)
                {
                    Reject(player, blockSel, "caravanshrine.error.foreign_holysite");
                    return;
                }
            }

            // Accept: record placed pos. Block has already been set by DoPlaceBlock by the time
            // we run; if a later step fails we'd need a removal here, but DoPlaceBlock cannot be
            // cancelled from a behavior subscriber. The rule checks above happen pre-write in
            // practice because BlockBehaviorCaravanShrine.DoPlaceBlock raises the event before
            // calling base, and a subsequent break by the player simply re-credits them.
            var pos = blockSel.Position;
            progression.SetPlacedCaravanShrine(pos.X, pos.Y, pos.Z);

            _messenger.SendMessage(player,
                LocalizationService.Instance.Get("caravanshrine.placed"),
                EnumChatType.CommandSuccess);

            _logger.Notification(
                $"{SystemConstants.LogPrefix} {player.PlayerName} placed a Caravan Shrine at {pos}");
        }
        catch (Exception ex)
        {
            _logger.Error($"{SystemConstants.LogPrefix} CaravanShrinePlacementHandler error: {ex.Message}");
        }
    }

    private void Reject(IServerPlayer player, BlockSelection blockSel, string messageKey)
    {
        // DoPlaceBlock has already placed the block from the behavior's perspective; remove it
        // and refund the item to the player.
        var pos = blockSel.Position;
        var world = _worldService.World;
        var existing = world.BlockAccessor.GetBlock(pos);
        ItemStack? refund = null;

        if (existing != null && existing.Code != null && IsCaravanShrineCode(existing.Code))
        {
            refund = new ItemStack(existing, 1);
            world.BlockAccessor.SetBlock(0, pos);
        }

        _messenger.SendMessage(player, LocalizationService.Instance.Get(messageKey),
            EnumChatType.CommandError);

        if (refund != null && !player.InventoryManager.TryGiveItemstack(refund))
        {
            _worldService.SpawnItemEntity(refund, player.Entity.Pos.XYZ);
        }
    }

    internal static bool IsCaravanShrineCode(AssetLocation code)
        => code.Path.StartsWith(ShrineCodePathPrefix, StringComparison.Ordinal);
}
