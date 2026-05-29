namespace DivineAscension.Configuration;

/// <summary>
///     Compile-time feature flags for domains/features still in development.
///     Flip to <c>true</c> and rebuild to enable. Kept off so unfinished work can
///     ship in mainline (no branch divergence) while it bakes.
/// </summary>
public static class FeatureFlags
{
    /// <summary>
    ///     Caravan domain (#433): the Caravan patron deity, its favor sources
    ///     (exploration + NPC-trade), the portable trade-table shrine block, and the
    ///     <c>trade_hub</c> civilization milestone. Off until the domain is debugged
    ///     and balance-tested. When false:
    ///     <list type="bullet">
    ///         <item>Caravan is hidden from the deity selector.</item>
    ///         <item>The shrine placement/destruction + trade-session servers are not constructed.</item>
    ///         <item>The Civilization milestone manager ignores NPC-trade events.</item>
    ///         <item><c>trade_hub</c> is dropped from the loaded milestone set.</item>
    ///     </list>
    ///     The caravan favor trackers still construct (harmless: no player can worship
    ///     Caravan, so their awards no-op), and the shrine block stays registered but
    ///     creative-inventory-only.
    /// </summary>
    public const bool CaravanDomainEnabled = false;
}
