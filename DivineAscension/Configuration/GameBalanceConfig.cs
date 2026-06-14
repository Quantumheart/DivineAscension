using System;
using System.Collections.Generic;
using System.Reflection;

namespace DivineAscension.Configuration;

/// <summary>
/// Tier 1 server-tunable balance configuration for Divine Ascension.
/// Managed by ConfigLib for YAML serialization and in-game GUI.
/// </summary>
public class GameBalanceConfig
{
    // === FAVOR SYSTEM ===

    /// <summary>Passive favor generated per in-game hour (default: 0.5)</summary>
    public float PassiveFavorRate { get; set; } = 0.5f;

    /// <summary>Favor lost on player death (default: 50)</summary>
    public int DeathPenalty { get; set; } = 50;

    // Favor Rank Multipliers (1.0 to 1.5)

    /// <summary>Favor multiplier for Initiate rank (default: 1.0)</summary>
    public float InitiateMultiplier { get; set; } = 1.0f;

    /// <summary>Favor multiplier for Disciple rank (default: 1.1)</summary>
    public float DiscipleMultiplier { get; set; } = 1.1f;

    /// <summary>Favor multiplier for Zealot rank (default: 1.2)</summary>
    public float ZealotMultiplier { get; set; } = 1.2f;

    /// <summary>Favor multiplier for Champion rank (default: 1.3)</summary>
    public float ChampionMultiplier { get; set; } = 1.3f;

    /// <summary>Favor multiplier for Avatar rank (default: 1.5)</summary>
    public float AvatarMultiplier { get; set; } = 1.5f;

    // Religion Prestige Rank Multipliers (1.0 to 1.5)

    /// <summary>Favor multiplier for Fledgling religion rank (default: 1.0)</summary>
    public float FledglingMultiplier { get; set; } = 1.0f;

    /// <summary>Favor multiplier for Established religion rank (default: 1.1)</summary>
    public float EstablishedMultiplier { get; set; } = 1.1f;

    /// <summary>Favor multiplier for Renowned religion rank (default: 1.2)</summary>
    public float RenownedMultiplier { get; set; } = 1.2f;

    /// <summary>Favor multiplier for Legendary religion rank (default: 1.3)</summary>
    public float LegendaryMultiplier { get; set; } = 1.3f;

    /// <summary>Favor multiplier for Mythic religion rank (default: 1.5)</summary>
    public float MythicMultiplier { get; set; } = 1.5f;

    // === PROGRESSION THRESHOLDS ===

    // Favor Rank Thresholds (lifetime favor earned)

    /// <summary>Lifetime favor required for Disciple rank (default: 500)</summary>
    public int DiscipleThreshold { get; set; } = 500;

    /// <summary>Lifetime favor required for Zealot rank (default: 2000)</summary>
    public int ZealotThreshold { get; set; } = 2000;

    /// <summary>Lifetime favor required for Champion rank (default: 5000)</summary>
    public int ChampionThreshold { get; set; } = 5000;

    /// <summary>Lifetime favor required for Avatar rank (default: 10000)</summary>
    public int AvatarThreshold { get; set; } = 10000;

    // Religion Prestige Rank Thresholds (religion prestige)

    /// <summary>Religion prestige required for Established rank (default: 2500)</summary>
    public int EstablishedThreshold { get; set; } = 2500;

    /// <summary>Religion prestige required for Renowned rank (default: 10000)</summary>
    public int RenownedThreshold { get; set; } = 10000;

    /// <summary>Religion prestige required for Legendary rank (default: 25000)</summary>
    public int LegendaryThreshold { get; set; } = 25000;

    /// <summary>Religion prestige required for Mythic rank (default: 50000)</summary>
    public int MythicThreshold { get; set; } = 50000;

    // === BLESSING SLOTS ===

    // Active blessing slots granted by favor rank.

    /// <summary>Active blessing slots granted at Initiate favor rank (default: 1)</summary>
    public int InitiateActiveBlessingSlots { get; set; } = 1;

    /// <summary>Active blessing slots granted at Disciple favor rank (default: 2)</summary>
    public int DiscipleActiveBlessingSlots { get; set; } = 2;

    /// <summary>Active blessing slots granted at Zealot favor rank (default: 3)</summary>
    public int ZealotActiveBlessingSlots { get; set; } = 3;

    /// <summary>Active blessing slots granted at Champion favor rank (default: 4)</summary>
    public int ChampionActiveBlessingSlots { get; set; } = 4;

    /// <summary>Active blessing slots granted at Avatar favor rank (default: 5)</summary>
    public int AvatarActiveBlessingSlots { get; set; } = 5;

    // Bonus active blessing slots granted by religion prestige rank.

    /// <summary>Bonus active blessing slots from Fledgling religion prestige (default: 0)</summary>
    public int FledglingBonusSlots { get; set; } = 0;

    /// <summary>Bonus active blessing slots from Established religion prestige (default: 0)</summary>
    public int EstablishedBonusSlots { get; set; } = 0;

    /// <summary>Bonus active blessing slots from Renowned religion prestige (default: 1)</summary>
    public int RenownedBonusSlots { get; set; } = 1;

    /// <summary>Bonus active blessing slots from Legendary religion prestige (default: 1)</summary>
    public int LegendaryBonusSlots { get; set; } = 1;

    /// <summary>Bonus active blessing slots from Mythic religion prestige (default: 2)</summary>
    public int MythicBonusSlots { get; set; } = 2;

    /// <summary>
    /// Balance ceiling on total active blessing slots (favor + prestige bonus). Default 8.
    /// This is a soft cap enforced by clamping at runtime (see <see cref="BlessingSlotCalculator"/>),
    /// not a structural constraint — raising it lets a player unlock more slots, lowering it caps them.
    /// Out-of-range slot fields are clamped (not rejected) so one bad value never discards the rest
    /// of the config. Range: 1..64.
    /// </summary>
    public int MaxTotalActiveBlessingSlots { get; set; } = 8;

    /// <summary>Lower bound enforced on <see cref="MaxTotalActiveBlessingSlots"/>.</summary>
    public const int MinAllowedMaxTotalActiveBlessingSlots = 1;

    /// <summary>Upper bound enforced on <see cref="MaxTotalActiveBlessingSlots"/>.</summary>
    public const int MaxAllowedMaxTotalActiveBlessingSlots = 64;

    // === RELIGION BLESSING SLOTS ===

    // Inscribe-slot cap on religion blessings, scaling with the religion's prestige rank (#479).
    // Unlike personal slots these are not additive — the religion gets exactly this many slots at
    // each rank. Mythic max stays below the reachable blessing count so the cap binds at every tier.

    /// <summary>Religion blessing inscribe slots at Fledgling prestige rank (default: 2)</summary>
    public int FledglingReligionBlessingSlots { get; set; } = 2;

    /// <summary>Religion blessing inscribe slots at Established prestige rank (default: 3)</summary>
    public int EstablishedReligionBlessingSlots { get; set; } = 3;

    /// <summary>Religion blessing inscribe slots at Renowned prestige rank (default: 4)</summary>
    public int RenownedReligionBlessingSlots { get; set; } = 4;

    /// <summary>Religion blessing inscribe slots at Legendary prestige rank (default: 5)</summary>
    public int LegendaryReligionBlessingSlots { get; set; } = 5;

    /// <summary>Religion blessing inscribe slots at Mythic prestige rank (default: 6)</summary>
    public int MythicReligionBlessingSlots { get; set; } = 6;

    // === BLESSING UNLEARN ===

    /// <summary>
    /// Fraction of a personal blessing's favor cost refunded to spendable favor when unlearned (default: 0.5).
    /// Refund credits spendable favor only — lifetime favor is unchanged, so favor rank cannot flicker.
    /// The unrefunded remainder is the sole cost of unlearning; there is no cooldown or other penalty.
    /// </summary>
    public float UnlearnRefundPercent { get; set; } = 0.5f;

    // === PVP SYSTEM ===

    /// <summary>Base favor awarded for PvP kill (default: 10)</summary>
    public int KillFavorReward { get; set; } = 10;

    /// <summary>Base prestige awarded for PvP kill (default: 75)</summary>
    public int KillPrestigeReward { get; set; } = 75;

    /// <summary>Favor multiplier during war (default: 1.5)</summary>
    public float WarFavorMultiplier { get; set; } = 1.5f;

    /// <summary>Prestige multiplier during war (default: 1.5)</summary>
    public float WarPrestigeMultiplier { get; set; } = 1.5f;

    // === CARAVAN DOMAIN ===

    /// <summary>
    ///     Multiplier applied to favor awarded by completed NPC trader transactions
    ///     (consumed by <c>TraderTransactionFavorTracker</c>). Default 1.0.
    /// </summary>
    public float CaravanTradeFavorMultiplier { get; set; } = 1.0f;

    /// <summary>
    ///     Multiplier applied to favor awarded on first-time chunk discovery
    ///     (consumed by <c>ExplorationFavorTracker</c>). Default 1.0.
    /// </summary>
    public float CaravanExplorationFavorMultiplier { get; set; } = 1.0f;

    /// <summary>
    ///     Multiplier applied to favor awarded by the player-to-player trade table
    ///     (consumed once #435 lands). Default 1.0.
    /// </summary>
    public float CaravanTradeTableFavorMultiplier { get; set; } = 1.0f;

    /// <summary>
    ///     Hard cap on favor awarded per single NPC trade. Overrides the tracker's
    ///     compiled <c>MaxFavorPerTrade</c> when set, so admins can rebalance whale-trade
    ///     anti-farming without a code release. Default 20.
    /// </summary>
    public int CaravanPerTradeFavorCap { get; set; } = 20;

    // === HOLY SITE BUFFS ===

    /// <summary>Favor/Prestige multiplier for Tier 1 holy sites (default: 1.25)</summary>
    public float HolySiteTier1Multiplier { get; set; } = 1.25f;

    /// <summary>Favor/Prestige multiplier for Tier 2 holy sites (default: 1.5)</summary>
    public float HolySiteTier2Multiplier { get; set; } = 1.5f;

    /// <summary>Favor/Prestige multiplier for Tier 3 holy sites (default: 1.75)</summary>
    public float HolySiteTier3Multiplier { get; set; } = 1.75f;

    // === LOGGING ===
    //
    // Per-level toggles for the mod's category loggers. Booleans render as checkboxes in the
    // ConfigLib GUI and round-trip reliably through YAML. Defaults are all-on, matching prior
    // behaviour. Uncheck all four for complete silence. Applied at startup and re-applied live
    // when changed via the ConfigLib GUI. Event/Build/Chat levels follow the notification toggle.

    /// <summary>Enable Debug-level logs — verbose, highest volume (default: true).</summary>
    public bool EnableDebugLogs { get; set; } = true;

    /// <summary>Enable Notification-level logs — startup and major events (default: true).</summary>
    public bool EnableNotificationLogs { get; set; } = true;

    /// <summary>Enable Warning-level logs (default: true).</summary>
    public bool EnableWarningLogs { get; set; } = true;

    /// <summary>Enable Error-level logs — recommended to keep on (default: true).</summary>
    public bool EnableErrorLogs { get; set; } = true;

    /// <summary>
    /// Builds the <see cref="LoggingConfig" /> described by the per-level toggles above.
    /// Event/Build/Chat (rarely used) follow the notification toggle.
    /// </summary>
    public LoggingConfig BuildLoggingConfig()
    {
        return new LoggingConfig
        {
            EnableDebug = EnableDebugLogs,
            EnableNotification = EnableNotificationLogs,
            EnableWarning = EnableWarningLogs,
            EnableError = EnableErrorLogs,
            EnableEvent = EnableNotificationLogs,
            EnableBuild = EnableNotificationLogs,
            EnableChat = EnableNotificationLogs
        };
    }

    // === RESET ===

    /// <summary>
    /// Copies every settable property from <paramref name="other" /> into this instance.
    /// Used to reset to defaults in place after a failed validation WITHOUT replacing the object
    /// reference — ConfigLib holds a reference to the registered instance and writes GUI changes
    /// into it, so swapping the reference would strand ConfigLib's updates on an orphaned object.
    /// </summary>
    public void CopyFrom(GameBalanceConfig other)
    {
        foreach (var property in typeof(GameBalanceConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.CanRead && property.CanWrite)
            {
                property.SetValue(this, property.GetValue(other));
            }
        }
    }

    // === VALIDATION ===

    /// <summary>
    /// Validates that all thresholds are in ascending order and values are within safe ranges.
    /// Called by ConfigLib after deserialization and can be called manually after changes.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when validation fails</exception>
    public void Validate()
    {
        // Validate favor rank thresholds are ascending
        if (!(DiscipleThreshold < ZealotThreshold &&
              ZealotThreshold < ChampionThreshold &&
              ChampionThreshold < AvatarThreshold))
        {
            throw new InvalidOperationException(
                "Favor rank thresholds must be ascending: Disciple < Zealot < Champion < Avatar");
        }

        // Validate prestige rank thresholds are ascending
        if (!(EstablishedThreshold < RenownedThreshold &&
              RenownedThreshold < LegendaryThreshold &&
              LegendaryThreshold < MythicThreshold))
        {
            throw new InvalidOperationException(
                "Prestige rank thresholds must be ascending: Established < Renowned < Legendary < Mythic");
        }

        // Validate ranges for safety
        if (PassiveFavorRate < 0 || PassiveFavorRate > 10)
        {
            throw new InvalidOperationException("PassiveFavorRate must be between 0 and 10");
        }

        if (DeathPenalty < 0 || DeathPenalty > 1000)
        {
            throw new InvalidOperationException("DeathPenalty must be between 0 and 1000");
        }

        if (KillFavorReward < 0 || KillFavorReward > 1000)
        {
            throw new InvalidOperationException("KillFavorReward must be between 0 and 1000");
        }

        if (KillPrestigeReward < 0 || KillPrestigeReward > 10000)
        {
            throw new InvalidOperationException("KillPrestigeReward must be between 0 and 10000");
        }

        // Validate multipliers are positive
        if (InitiateMultiplier <= 0 || DiscipleMultiplier <= 0 || ZealotMultiplier <= 0 ||
            ChampionMultiplier <= 0 || AvatarMultiplier <= 0)
        {
            throw new InvalidOperationException("Favor rank multipliers must be positive");
        }

        if (FledglingMultiplier <= 0 || EstablishedMultiplier <= 0 || RenownedMultiplier <= 0 ||
            LegendaryMultiplier <= 0 || MythicMultiplier <= 0)
        {
            throw new InvalidOperationException("Religion prestige rank multipliers must be positive");
        }

        if (WarFavorMultiplier <= 0 || WarPrestigeMultiplier <= 0)
        {
            throw new InvalidOperationException("War multipliers must be positive");
        }

        // Validate holy site tier multipliers
        if (HolySiteTier1Multiplier < 1.0f || HolySiteTier1Multiplier > 5.0f ||
            HolySiteTier2Multiplier < 1.0f || HolySiteTier2Multiplier > 5.0f ||
            HolySiteTier3Multiplier < 1.0f || HolySiteTier3Multiplier > 5.0f)
        {
            throw new InvalidOperationException("Holy site tier multipliers must be between 1.0 and 5.0");
        }

        if (!(HolySiteTier1Multiplier <= HolySiteTier2Multiplier &&
              HolySiteTier2Multiplier <= HolySiteTier3Multiplier))
        {
            throw new InvalidOperationException("Holy site tier multipliers must be ascending");
        }

        // Blessing-slot bounds are NOT validated here: out-of-range slot values are clamped
        // (see ClampBlessingSlots) rather than thrown, so one bad slot field never causes the
        // entire config to be discarded and reset to defaults. The balance ceiling is enforced
        // at runtime by BlessingSlotCalculator.

        if (UnlearnRefundPercent < 0f || UnlearnRefundPercent > 1f)
        {
            throw new InvalidOperationException("UnlearnRefundPercent must be between 0 and 1");
        }

        if (CaravanTradeFavorMultiplier <= 0 || CaravanExplorationFavorMultiplier <= 0 ||
            CaravanTradeTableFavorMultiplier <= 0)
        {
            throw new InvalidOperationException("Caravan favor multipliers must be positive");
        }

        if (CaravanPerTradeFavorCap <= 0 || CaravanPerTradeFavorCap > 1000)
        {
            throw new InvalidOperationException("CaravanPerTradeFavorCap must be between 1 and 1000");
        }
    }

    /// <summary>
    /// Brings every blessing-slot field into a usable range in place, returning a human-readable
    /// message for each field that had to be adjusted. Negative slot counts are clamped to 0 and
    /// <see cref="MaxTotalActiveBlessingSlots"/> is clamped to [<see cref="MinAllowedMaxTotalActiveBlessingSlots"/>,
    /// <see cref="MaxAllowedMaxTotalActiveBlessingSlots"/>].
    ///
    /// The balance ceiling itself (favor + prestige bonus exceeding the cap) is intentionally NOT
    /// clamped here — it is enforced gracefully at runtime by <see cref="BlessingSlotCalculator"/>,
    /// so an admin can raise the dials and the cap moves with them. This method only repairs values
    /// that are structurally invalid, never rejecting the whole config.
    /// </summary>
    /// <returns>Empty when nothing was changed; otherwise one message per adjusted field.</returns>
    public IReadOnlyList<string> ClampBlessingSlots()
    {
        var adjustments = new List<string>();

        ClampNonNegative(nameof(InitiateActiveBlessingSlots), InitiateActiveBlessingSlots,
            v => InitiateActiveBlessingSlots = v, adjustments);
        ClampNonNegative(nameof(DiscipleActiveBlessingSlots), DiscipleActiveBlessingSlots,
            v => DiscipleActiveBlessingSlots = v, adjustments);
        ClampNonNegative(nameof(ZealotActiveBlessingSlots), ZealotActiveBlessingSlots,
            v => ZealotActiveBlessingSlots = v, adjustments);
        ClampNonNegative(nameof(ChampionActiveBlessingSlots), ChampionActiveBlessingSlots,
            v => ChampionActiveBlessingSlots = v, adjustments);
        ClampNonNegative(nameof(AvatarActiveBlessingSlots), AvatarActiveBlessingSlots,
            v => AvatarActiveBlessingSlots = v, adjustments);

        ClampNonNegative(nameof(FledglingBonusSlots), FledglingBonusSlots,
            v => FledglingBonusSlots = v, adjustments);
        ClampNonNegative(nameof(EstablishedBonusSlots), EstablishedBonusSlots,
            v => EstablishedBonusSlots = v, adjustments);
        ClampNonNegative(nameof(RenownedBonusSlots), RenownedBonusSlots,
            v => RenownedBonusSlots = v, adjustments);
        ClampNonNegative(nameof(LegendaryBonusSlots), LegendaryBonusSlots,
            v => LegendaryBonusSlots = v, adjustments);
        ClampNonNegative(nameof(MythicBonusSlots), MythicBonusSlots,
            v => MythicBonusSlots = v, adjustments);

        ClampNonNegative(nameof(FledglingReligionBlessingSlots), FledglingReligionBlessingSlots,
            v => FledglingReligionBlessingSlots = v, adjustments);
        ClampNonNegative(nameof(EstablishedReligionBlessingSlots), EstablishedReligionBlessingSlots,
            v => EstablishedReligionBlessingSlots = v, adjustments);
        ClampNonNegative(nameof(RenownedReligionBlessingSlots), RenownedReligionBlessingSlots,
            v => RenownedReligionBlessingSlots = v, adjustments);
        ClampNonNegative(nameof(LegendaryReligionBlessingSlots), LegendaryReligionBlessingSlots,
            v => LegendaryReligionBlessingSlots = v, adjustments);
        ClampNonNegative(nameof(MythicReligionBlessingSlots), MythicReligionBlessingSlots,
            v => MythicReligionBlessingSlots = v, adjustments);

        var clampedCap = Math.Clamp(MaxTotalActiveBlessingSlots,
            MinAllowedMaxTotalActiveBlessingSlots, MaxAllowedMaxTotalActiveBlessingSlots);
        if (clampedCap != MaxTotalActiveBlessingSlots)
        {
            adjustments.Add(
                $"{nameof(MaxTotalActiveBlessingSlots)} {MaxTotalActiveBlessingSlots} out of range " +
                $"[{MinAllowedMaxTotalActiveBlessingSlots}, {MaxAllowedMaxTotalActiveBlessingSlots}] — clamped to {clampedCap}");
            MaxTotalActiveBlessingSlots = clampedCap;
        }

        return adjustments;
    }

    private static void ClampNonNegative(string fieldName, int value, Action<int> setter, List<string> adjustments)
    {
        if (value < 0)
        {
            adjustments.Add($"{fieldName} {value} is negative — clamped to 0");
            setter(0);
        }
    }
}