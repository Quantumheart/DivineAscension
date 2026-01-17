namespace DivineAscension.Models.Enum;

/// <summary>
///     Enumeration of all cooldown types with their default durations in seconds.
///     Used by CooldownManager to prevent griefing attacks.
/// </summary>
public enum CooldownType
{
    /// <summary>
    ///     Religion deletion cooldown (60 seconds).
    ///     Prevents repeated deletion griefing.
    ///     CRITICAL security mitigation.
    /// </summary>
    ReligionDeletion = 0,

    /// <summary>
    ///     Member kick cooldown (5 seconds).
    ///     Prevents mass kick attacks.
    ///     CRITICAL security mitigation.
    /// </summary>
    MemberKick = 1,

    /// <summary>
    ///     Member ban cooldown (10 seconds).
    ///     Prevents mass ban attacks.
    ///     CRITICAL security mitigation.
    /// </summary>
    MemberBan = 2,

    /// <summary>
    ///     Religion/civilization invite cooldown (2 seconds).
    ///     Prevents invite spam.
    ///     HIGH security mitigation.
    /// </summary>
    Invite = 3,

    /// <summary>
    ///     Religion creation cooldown (300 seconds / 5 minutes).
    ///     Prevents religion spam.
    ///     HIGH security mitigation.
    /// </summary>
    ReligionCreation = 4,

    /// <summary>
    ///     Diplomatic proposal cooldown (30 seconds).
    ///     Prevents proposal spam.
    ///     MEDIUM security mitigation.
    /// </summary>
    Proposal = 5,

    /// <summary>
    ///     War declaration cooldown (60 seconds).
    ///     Prevents war declaration spam.
    ///     MEDIUM security mitigation.
    /// </summary>
    WarDeclaration = 6
}
