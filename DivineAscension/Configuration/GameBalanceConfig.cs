using System;

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

    // === PVP SYSTEM ===

    /// <summary>Base favor awarded for PvP kill (default: 10)</summary>
    public int KillFavorReward { get; set; } = 10;

    /// <summary>Base prestige awarded for PvP kill (default: 75)</summary>
    public int KillPrestigeReward { get; set; } = 75;

    /// <summary>Favor multiplier during war (default: 1.5)</summary>
    public float WarFavorMultiplier { get; set; } = 1.5f;

    /// <summary>Prestige multiplier during war (default: 1.5)</summary>
    public float WarPrestigeMultiplier { get; set; } = 1.5f;

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
    }
}