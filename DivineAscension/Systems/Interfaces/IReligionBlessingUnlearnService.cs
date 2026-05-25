namespace DivineAscension.Systems.Interfaces;

/// <summary>
///     Outcome of a religion-blessing strike attempt. Server-authoritative; the network handler
///     maps each value to a localized response message (epic #479, slice 5 — #484).
/// </summary>
public enum ReligionUnlearnOutcome
{
    Success,
    BlessingNotFound,
    NotReligionBlessing,
    NotOwned,
    ReligionNotFound
}

/// <summary>
///     Result of <see cref="IReligionBlessingUnlearnService.UnlearnReligionBlessing"/>.
/// </summary>
/// <param name="Outcome">Why the operation succeeded or failed.</param>
/// <param name="RefundedPrestige">Total spendable prestige credited on success across the cascade (0 otherwise).</param>
/// <param name="StruckBlessingIds">Ids removed (target first, then cascaded dependents); null/empty on failure.</param>
public readonly record struct ReligionUnlearnResult(
    ReligionUnlearnOutcome Outcome,
    int RefundedPrestige,
    System.Collections.Generic.IReadOnlyList<string>? StruckBlessingIds = null)
{
    public bool Success => Outcome == ReligionUnlearnOutcome.Success;

    /// <summary>Number of blessings removed (target + cascaded dependents); 0 on failure.</summary>
    public int StruckCount => StruckBlessingIds?.Count ?? 0;
}

/// <summary>
///     Strikes (unlearns) an inscribed religion blessing and the prerequisite cascade beneath it:
///     strips the target plus every dependent unlocked child from the religion's unlocked set,
///     refunds a configured fraction of each blessing's prestige cost to the religion's spendable
///     prestige (no lifetime/rank change), and recomputes religion blessing effects. The
///     unrefunded remainder is the only cost — no cooldown. Server-authoritative. The founder-only
///     check lives in the network handler, not here.
/// </summary>
public interface IReligionBlessingUnlearnService
{
    /// <summary>
    ///     Strikes <paramref name="blessingId"/> and its cascade for the given religion. Rejects
    ///     blessings the religion does not own and non-religion (personal) blessings.
    /// </summary>
    ReligionUnlearnResult UnlearnReligionBlessing(string religionUID, string blessingId);

    /// <summary>
    ///     Resolves the ordered strike kill list for <paramref name="blessingId"/> — the target
    ///     plus every transitively-orphaned unlocked child — without mutating any state. Returns
    ///     an empty list when the religion does not own the target. Target is first.
    /// </summary>
    System.Collections.Generic.IReadOnlyList<string> ResolveUnlearnCascade(string religionUID, string blessingId);
}
