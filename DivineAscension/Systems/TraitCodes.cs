namespace DivineAscension.Systems;

/// <summary>
///     Canonical Vintage Story character-trait codes granted by Divine Ascension.
///     Mirrors entries declared in <c>assets/divineascension/config/traits.json</c>.
/// </summary>
public static class TraitCodes
{
    /// <summary>Granted to every player while they are a member of a religion (#560).</summary>
    public const string Member = "da_member";

    /// <summary>Reserved for blessing-tier integration (#561). Not yet wired.</summary>
    public const string Blessed = "da_blessed";

    /// <summary>Reserved for favor-tier integration (#562). Not yet wired.</summary>
    public const string Favored = "da_favored";
}
