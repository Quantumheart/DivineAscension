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
/// <param name="RefundedFavor">Spendable favor credited on success (0 otherwise).</param>
public readonly record struct UnlearnResult(UnlearnOutcome Outcome, int RefundedFavor)
{
    public bool Success => Outcome == UnlearnOutcome.Success;
}

/// <summary>
///     Unlearns a single owned personal blessing: strips it from the unlocked set, refunds a
///     portion of its favor cost to spendable favor (no lifetime change), and recomputes blessing
///     effects. The unrefunded remainder is the only cost — no cooldown. Server-authoritative.
/// </summary>
public interface IBlessingUnlearnService
{
    /// <summary>
    ///     Unlearns <paramref name="blessingId"/> for the given player. Rejects blessings the
    ///     player does not own and non-personal (religion) blessings.
    /// </summary>
    UnlearnResult UnlearnBlessing(string playerUID, string blessingId);
}
