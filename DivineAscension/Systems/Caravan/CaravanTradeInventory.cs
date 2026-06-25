using System;
using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using DivineAscension.Constants;
using DivineAscension.Network.Caravan;
using DivineAscension.Services;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Caravan;

/// <summary>
///     Real Vintage Story inventory implementation of <see cref="ICaravanTradeInventory" />.
///     Reads and writes the same hotbar + backpack inventories the client dialog offers from.
/// </summary>
public class CaravanTradeInventory : ICaravanTradeInventory
{
    private static readonly string[] OwnedInventoryClasses =
    {
        GlobalConstants.hotBarInvClassName,
        GlobalConstants.backpackInvClassName
    };

    private readonly ILoggerWrapper _logger;
    private readonly IWorldService _worldService;

    public CaravanTradeInventory(ILoggerWrapper logger, IWorldService worldService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _worldService = worldService ?? throw new ArgumentNullException(nameof(worldService));
    }

    public bool CanProvideOffer(IServerPlayer player, IReadOnlyList<TradeOfferSlot> offer)
    {
        foreach (var (code, needed) in AggregateByCode(offer))
        {
            if (needed <= 0) continue;
            if (CountAvailable(player, code) < needed) return false;
        }

        return true;
    }

    public void SwapOffers(
        IServerPlayer sideA, IReadOnlyList<TradeOfferSlot> offerA,
        IServerPlayer sideB, IReadOnlyList<TradeOfferSlot> offerB,
        BlockPos dropPos)
    {
        // Extract both sides first (callers have already verified provisioning), then deliver
        // crosswise. Extraction and delivery run in one synchronous step — no await, no tick
        // boundary — so no other game logic can observe a half-finished swap.
        var fromA = Extract(sideA, offerA);
        var fromB = Extract(sideB, offerB);

        Deliver(sideB, fromA, dropPos);
        Deliver(sideA, fromB, dropPos);
    }

    private static Dictionary<string, int> AggregateByCode(IReadOnlyList<TradeOfferSlot> offer)
    {
        var totals = new Dictionary<string, int>();
        foreach (var slot in offer)
        {
            if (string.IsNullOrEmpty(slot.ItemCode) || slot.Quantity <= 0) continue;
            totals.TryGetValue(slot.ItemCode, out var running);
            totals[slot.ItemCode] = running + slot.Quantity;
        }

        return totals;
    }

    private int CountAvailable(IServerPlayer player, string itemCode)
    {
        var total = 0;
        foreach (var slot in OwnedSlots(player))
        {
            var stack = slot.Itemstack;
            if (stack?.Collectible?.Code == null) continue;
            if (stack.Collectible.Code.ToString() == itemCode)
                total += stack.StackSize;
        }

        return total;
    }

    /// <summary>
    ///     Pull the offered quantities out of the player's inventory, returning the removed
    ///     stacks. Assumes <see cref="CanProvideOffer" /> already passed, so the requested
    ///     amounts are present; any shortfall is logged and simply yields fewer stacks.
    /// </summary>
    private List<ItemStack> Extract(IServerPlayer player, IReadOnlyList<TradeOfferSlot> offer)
    {
        var removed = new List<ItemStack>();

        foreach (var (code, needed) in AggregateByCode(offer))
        {
            var remaining = needed;
            foreach (var slot in OwnedSlots(player))
            {
                if (remaining <= 0) break;

                var stack = slot.Itemstack;
                if (stack?.Collectible?.Code == null) continue;
                if (stack.Collectible.Code.ToString() != code) continue;

                var take = Math.Min(stack.StackSize, remaining);
                var taken = slot.TakeOut(take);
                slot.MarkDirty();
                if (taken != null)
                {
                    removed.Add(taken);
                    remaining -= taken.StackSize;
                }
            }

            if (remaining > 0)
                _logger.Warning(
                    $"{SystemConstants.LogPrefix} Caravan swap short by {remaining} of '{code}' for " +
                    $"{player.PlayerName}; delivering what was extracted.");
        }

        return removed;
    }

    /// <summary>
    ///     Give the stacks to the receiver, dropping any that will not fit at the shrine so the
    ///     swap never destroys items.
    /// </summary>
    private void Deliver(IServerPlayer receiver, List<ItemStack> stacks, BlockPos dropPos)
    {
        var drop = new Vec3d(dropPos.X + 0.5, dropPos.Y + 0.5, dropPos.Z + 0.5);
        foreach (var stack in stacks)
        {
            if (!receiver.InventoryManager.TryGiveItemstack(stack, true))
                _worldService.SpawnItemEntity(stack, drop);
        }
    }

    private IEnumerable<ItemSlot> OwnedSlots(IServerPlayer player)
    {
        foreach (var className in OwnedInventoryClasses)
        {
            var inventory = player.InventoryManager?.GetOwnInventory(className);
            if (inventory == null) continue;

            foreach (var slot in inventory)
                if (slot != null)
                    yield return slot;
        }
    }
}
