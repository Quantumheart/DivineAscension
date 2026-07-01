using System.Collections.Generic;
using DivineAscension.Network.Caravan;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Caravan;

/// <summary>
///     Inventory seam for the caravan atomic swap (#434).
///
///     Slice "A": no escrow. Offered items stay in each trader's own inventory until both
///     parties seal, then the swap moves them in a single synchronous step. Because there is
///     never a server-held custody window, a disconnect / shrine break / crash mid-trade
///     simply leaves every item where it already was — nothing can be lost.
///
///     This interface isolates the raw Vintage Story inventory calls so the session manager's
///     commit/abort orchestration stays unit-testable. The real implementation
///     (<see cref="CaravanTradeInventory" />) is exercised by play-testing.
/// </summary>
public interface ICaravanTradeInventory
{
    /// <summary>
    ///     True if <paramref name="player" /> currently holds the full offer (every item code at
    ///     the offered quantity) across their hotbar and backpack. Mutates nothing.
    /// </summary>
    bool CanProvideOffer(IServerPlayer player, IReadOnlyList<TradeOfferSlot> offer);

    /// <summary>
    ///     Atomically move <paramref name="offerA" /> from <paramref name="sideA" /> to
    ///     <paramref name="sideB" /> and <paramref name="offerB" /> from <paramref name="sideB" />
    ///     to <paramref name="sideA" />. Callers must have verified both sides via
    ///     <see cref="CanProvideOffer" /> first. Any item that will not fit the receiver's
    ///     inventory is dropped at <paramref name="dropPos" /> so the swap can never lose items.
    /// </summary>
    void SwapOffers(
        IServerPlayer sideA, IReadOnlyList<TradeOfferSlot> offerA,
        IServerPlayer sideB, IReadOnlyList<TradeOfferSlot> offerB,
        BlockPos dropPos);
}