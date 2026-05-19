using System.Collections.Generic;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server sends blessing data for the player. Carries all five deities' blessings
///     plus per-deity favor/rank state.
/// </summary>
[ProtoContract]
public class BlessingDataResponsePacket
{
    [ProtoMember(1)] public bool HasReligion { get; set; }

    [ProtoMember(2)] public string ReligionUID { get; set; } = string.Empty;

    [ProtoMember(3)] public string ReligionName { get; set; } = string.Empty;

    /// <summary>
    ///     Patron deity of the player's religion.
    /// </summary>
    [ProtoMember(4)]
    public DeityDomain PatronDomain { get; set; } = DeityDomain.None;

    // PM 5, 7, 9 retired (single-deity FavorRank, CurrentFavor, TotalFavorEarned). Do not reuse.

    [ProtoMember(6)] public int PrestigeRank { get; set; }

    [ProtoMember(8)] public int CurrentPrestige { get; set; }

    [ProtoMember(10)] public List<BlessingInfo> PlayerBlessings { get; set; } = new();

    [ProtoMember(11)] public List<BlessingInfo> ReligionBlessings { get; set; } = new();

    [ProtoMember(12)] public List<string> UnlockedPlayerBlessings { get; set; } = new();

    [ProtoMember(13)] public List<string> UnlockedReligionBlessings { get; set; } = new();

    /// <summary>
    ///     Patron name (custom display name for the patron deity).
    /// </summary>
    [ProtoMember(14)]
    public string PatronName { get; set; } = string.Empty;

    /// <summary>
    ///     Branches the player has committed to, keyed by domain enum as int.
    /// </summary>
    [ProtoMember(15)]
    public Dictionary<int, string> CommittedBranches { get; set; } = new();

    /// <summary>
    ///     Branches that are locked out due to exclusive branch choices.
    ///     Keyed by domain enum as int.
    /// </summary>
    [ProtoMember(16)]
    public Dictionary<int, List<string>> LockedBranches { get; set; } = new();

    /// <summary>
    ///     Per-deity current favor (all five domains).
    /// </summary>
    [ProtoMember(17)]
    public Dictionary<DeityDomain, int> FavorByDeity { get; set; } = new();

    /// <summary>
    ///     Per-deity favor rank as <see cref="FavorRank"/> cast to int.
    /// </summary>
    [ProtoMember(18)]
    public Dictionary<DeityDomain, int> FavorRanksByDeity { get; set; } = new();

    /// <summary>
    ///     Per-deity total favor earned.
    /// </summary>
    [ProtoMember(19)]
    public Dictionary<DeityDomain, int> TotalFavorEarnedByDeity { get; set; } = new();

    /// <summary>
    ///     Basic blessing information needed for UI display
    /// </summary>
    [ProtoContract]
    public class BlessingInfo
    {
        [ProtoMember(1)] public string BlessingId { get; set; } = string.Empty;

        [ProtoMember(2)] public string Name { get; set; } = string.Empty;

        [ProtoMember(3)] public string Description { get; set; } = string.Empty;

        [ProtoMember(4)] public int RequiredFavorRank { get; set; }

        [ProtoMember(5)] public int RequiredPrestigeRank { get; set; }

        [ProtoMember(6)] public List<string> PrerequisiteBlessings { get; set; } = new();

        [ProtoMember(7)] public int Category { get; set; } // BlessingCategory as int

        [ProtoMember(8)] public Dictionary<string, float> StatModifiers { get; set; } = new();

        [ProtoMember(9)] public string IconName { get; set; } = string.Empty;

        /// <summary>
        ///     Base cost. Client computes 1.5x for non-patron blessings using <see cref="Domain"/>.
        /// </summary>
        [ProtoMember(10)]
        public int Cost { get; set; }

        /// <summary>
        ///     The branch this blessing belongs to (null/empty = shared)
        /// </summary>
        [ProtoMember(11)]
        public string? Branch { get; set; }

        /// <summary>
        ///     Branch names that become locked if this blessing is unlocked
        /// </summary>
        [ProtoMember(12)]
        public List<string>? ExclusiveBranches { get; set; }

        /// <summary>
        ///     Deity this blessing belongs to. Required because a single packet now carries blessings
        ///     for all five deities.
        /// </summary>
        [ProtoMember(13)]
        public DeityDomain Domain { get; set; } = DeityDomain.None;

        /// <summary>
        ///     If true, this blessing can only be unlocked when the player's religion has committed
        ///     to <see cref="Domain"/> as its patron deity. Drives the capstone-locked tooltip.
        /// </summary>
        [ProtoMember(14)]
        public bool RequiresPatron { get; set; }
    }
}
