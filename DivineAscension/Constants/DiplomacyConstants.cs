using System.Diagnostics.CodeAnalysis;

namespace DivineAscension.Constants;

/// <summary>
/// Constants for the diplomacy system including durations, multipliers, and rank requirements
/// </summary>
[ExcludeFromCodeCoverage]
public static class DiplomacyConstants
{
    #region Log Prefix

    /// <summary>
    /// Log prefix for diplomacy system messages
    /// </summary>
    public const string LogPrefix = "[DivineAscension:Diplomacy]";

    #endregion

    #region Violation Limits

    /// <summary>
    /// Maximum number of PvP violations before treaty auto-breaks (3 strikes)
    /// </summary>
    public const int MaxViolations = 3;

    #endregion

    #region Prestige Bonuses

    /// <summary>
    /// Prestige bonus awarded to all religions in both civilizations when Alliance is formed (100)
    /// </summary>
    public const int AlliancePrestigeBonus = 100;

    #endregion

    #region Data Key

    /// <summary>
    /// World data storage key for diplomacy data
    /// </summary>
    public const string DataKey = "divineascension_diplomacy";

    #endregion

    #region Durations

    /// <summary>
    /// Number of days a diplomatic proposal is valid (7 days)
    /// </summary>
    public const int ProposalExpirationDays = 7;

    /// <summary>
    /// Number of days a Non-Aggression Pact lasts (3 days)
    /// </summary>
    public const int NonAggressionPactDurationDays = 3;

    /// <summary>
    /// Number of hours warning before a treaty break (24 hours)
    /// </summary>
    public const int TreatyBreakWarningHours = 24;

    #endregion

    #region Multipliers

    /// <summary>
    /// Favor multiplier for PvP kills during War (1.5x)
    /// </summary>
    public const double WarFavorMultiplier = 1.5;

    /// <summary>
    /// Prestige multiplier for PvP kills during War (1.5x)
    /// </summary>
    public const double WarPrestigeMultiplier = 1.5;

    #endregion

    #region Rank Requirements

    /// <summary>
    /// Minimum prestige rank required for Non-Aggression Pact (1 = Established)
    /// </summary>
    public const int NonAggressionPactRequiredRank = 1;

    /// <summary>
    /// Minimum prestige rank required for Alliance (2 = Renowned)
    /// </summary>
    public const int AllianceRequiredRank = 2;

    /// <summary>
    /// Name of the rank required for Non-Aggression Pact
    /// </summary>
    public const string NonAggressionPactRankName = "Established";

    /// <summary>
    /// Name of the rank required for Alliance
    /// </summary>
    public const string AllianceRankName = "Renowned";

    #endregion
}