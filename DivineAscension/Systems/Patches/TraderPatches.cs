using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace DivineAscension.Systems.Patches;

/// <summary>
///     Harmony patch on <c>InventoryTrader.TryBuySell</c> (1.22) raising an event on every
///     successful NPC trade. The method is internal but Harmony patches it by name. Prefix
///     snapshots the cart values in gears; postfix emits only on
///     <see cref="EnumTransactionResult.Success"/> so cancelled/insufficient-funds attempts
///     don't grant favor.
/// </summary>
[HarmonyPatch]
public static class TraderPatches
{
    /// <summary>
    ///     Fires once per completed NPC transaction with (player, total value in rusty gears).
    ///     <para>
    ///         Total value sums the price columns of every non-empty cart slot — both items the
    ///         player buys from the trader and items the player sells to the trader. The value
    ///         is the trader's gear quote, not the underlying item's rarity.
    ///     </para>
    /// </summary>
    public static event Action<IPlayer, int>? OnTraderTransaction;

    /// <summary>
    ///     Drops all subscribers; called from <c>DivineAscensionSystemInitializer</c> on
    ///     server start/reload so stale handlers from a previous load don't accumulate.
    /// </summary>
    public static void ClearSubscribers()
    {
        OnTraderTransaction = null;
    }

    [HarmonyPatch(typeof(InventoryTrader), "TryBuySell")]
    [HarmonyPrefix]
    public static void Prefix_TryBuySell(InventoryTrader __instance, IPlayer buyingPlayer, out int __state)
    {
        __state = 0;
        try
        {
            __state = SnapshotCartValue(__instance);
        }
        catch
        {
            // Never throw from a Harmony prefix — a failed snapshot just means no favor.
            __state = 0;
        }
    }

    [HarmonyPatch(typeof(InventoryTrader), "TryBuySell")]
    [HarmonyPostfix]
    public static void Postfix_TryBuySell(IPlayer buyingPlayer, EnumTransactionResult __result, int __state)
    {
        if (__result != EnumTransactionResult.Success) return;
        if (__state <= 0) return;
        if (buyingPlayer is not { } player) return;

        OnTraderTransaction?.Invoke(player, __state);
    }

    /// <summary>
    ///     Walks both cart sides and returns the trader-priced gear value of the trade.
    ///     Mirrors the math in <c>InventoryTrader.GetTraderAssets</c> from 1.22.
    /// </summary>
    private static int SnapshotCartValue(InventoryTrader inv)
    {
        var total = 0;

        for (var i = 0; i < 4; i++)
        {
            var buyingCartSlot = inv.GetBuyingCartSlot(i);
            var stack = buyingCartSlot?.Itemstack;
            var tradeItem = buyingCartSlot?.TradeItem;
            if (stack == null || tradeItem?.Stack == null || tradeItem.Stack.StackSize <= 0) continue;
            total += tradeItem.Price * (stack.StackSize / tradeItem.Stack.StackSize);
        }

        for (var i = 0; i < 4; i++)
        {
            var sellingCartSlot = inv.GetSellingCartSlot(i);
            var stack = sellingCartSlot?.Itemstack;
            if (stack == null) continue;
            var conditions = inv.GetBuyingConditionsSlot(stack);
            var tradeItem = conditions?.TradeItem;
            if (tradeItem?.Stack == null || tradeItem.Stack.StackSize <= 0) continue;
            total += tradeItem.Price * (stack.StackSize / tradeItem.Stack.StackSize);
        }

        return total;
    }
}
