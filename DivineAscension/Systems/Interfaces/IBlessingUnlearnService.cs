namespace DivineAscension.Systems.Interfaces;

/// <summary>
///     Outcome of an unlearn attempt. Server-authoritative; the network handler maps each
///     value to a localized response message (epic #425, slice 1 — #459).
/// </summary>
public enum UnlearnOutcome
{
    Success,
    BlessingNotFound,
    NotPlayerBlessing,
    NotOwned
}

/// <summary>
///     Result of <see cref="IBlessingUnlearnService.UnlearnBlessing"/>.
/// </summary>
/// <param name="Outcome">Why the operation succeeded or failed.</param>
/// <param name="RefundedFavor">Total spendable favor credited on success across the cascade (0 otherwise).</param>
/// <param name="StruckBlessingIds">Ids removed (target first, then cascaded dependents); null/empty on failure.</param>
public readonly record struct UnlearnResult(
    UnlearnOutcome Outcome,
    int RefundedFavor,
    System.Collections.Generic.IReadOnlyList<string>? StruckBlessingIds = null)
{
    public bool Success => Outcome == UnlearnOutcome.Success;

    /// <summary>Number of blessings removed (target + cascaded dependents); 0 on failure.</summary>
    public int StruckCount => StruckBlessingIds?.Count ?? 0;
}

/// <summary>
///     Unlearns an owned personal blessing and the prerequisite cascade beneath it: strips the
///     target plus every dependent unlocked child from the unlocked set, refunds 50% of each
///     blessing's favor cost to spendable favor (no lifetime change), and recomputes effects.
///     The unrefunded remainder is the only cost — no cooldown. Server-authoritative.
/// </summary>
public interface IBlessingUnlearnService
{
    /// <summary>
    ///     Unlearns <paramref name="blessingId"/> and its cascade for the given player. Rejects
    ///     blessings the player does not own and non-personal (religion) blessings.
    /// </summary>
    UnlearnResult UnlearnBlessing(string playerUID, string blessingId);

    /// <summary>
    ///     Resolves the ordered unlearn kill list for <paramref name="blessingId"/> — the target
    ///     plus every transitively-orphaned unlocked child — without mutating any state. Returns
    ///     an empty list when the player does not own the target. Target is first.
    /// </summary>
    System.Collections.Generic.IReadOnlyList<string> ResolveUnlearnCascade(string playerUID, string blessingId);
}
