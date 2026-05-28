using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DivineAscension.Models.Enum;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
///     Represents a civilization - an alliance of 1-4 religions with different deities
/// </summary>
[ProtoContract]
public class Civilization
{
    /// <summary>
    ///     Lazy-initialized lock object for thread safety using Interlocked.CompareExchange
    /// </summary>
    [ProtoIgnore]
    private object? _lock;

    [ProtoIgnore]
    private object Lock
    {
        get
        {
            if (_lock == null)
            {
                Interlocked.CompareExchange(ref _lock, new object(), null);
            }
            return _lock;
        }
    }

    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public Civilization()
    {
    }

    /// <summary>
    ///     Creates a new civilization with initial parameters
    /// </summary>
    public Civilization(string civId, string name, string founderUID, string founderReligionId)
    {
        CivId = civId;
        Name = name;
        FounderUID = founderUID;
        FounderReligionUID = founderReligionId;
        _memberReligionIds = [founderReligionId];
        CreatedDate = DateTime.UtcNow;
    }

    /// <summary>
    ///     Unique identifier for the civilization
    /// </summary>
    [ProtoMember(1)]
    public string CivId { get; set; } = string.Empty;

    /// <summary>
    ///     Display name of the civilization
    /// </summary>
    [ProtoMember(2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     Player UID of the civilization founder
    /// </summary>
    [ProtoMember(3)]
    public string FounderUID { get; set; } = string.Empty;


    /// <summary>
    ///     Unique identifier of the religion of the civilization's founder
    /// </summary>
    [ProtoMember(4)]
    public string FounderReligionUID { get; set; } = string.Empty;


    /// <summary>
    ///     Backing field for MemberReligionIds
    /// </summary>
    [ProtoMember(5)]
    private List<string> _memberReligionIds = new();

    /// <summary>
    ///     List of religion UIDs that are members (1-4 religions).
    ///     Thread-safe read-only snapshot.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyList<string> MemberReligionIds
    {
        get { lock (Lock) { return _memberReligionIds.ToList(); } }
    }

    /// <summary>
    ///     Gets a snapshot of member religion IDs for safe iteration
    /// </summary>
    public List<string> GetMemberReligionIdsSnapshot()
    {
        lock (Lock) { return _memberReligionIds.ToList(); }
    }

    /// <summary>
    ///     When the civilization was created
    /// </summary>
    [ProtoMember(6)]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     When the civilization was disbanded (null if active)
    /// </summary>
    [ProtoMember(7)]
    public DateTime? DisbandedDate { get; set; }

    /// <summary>
    ///     Cached count of total members across all religions
    /// </summary>
    [ProtoMember(8)]
    public int MemberCount { get; set; }

    /// <summary>
    ///     Icon name for the civilization (e.g., "shield", "crown")
    /// </summary>
    [ProtoMember(9)]
    public string Icon { get; set; } = "default";

    /// <summary>
    ///     Description or manifesto of the civilization set by the founder
    /// </summary>
    [ProtoMember(10)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Civilization rank - based on number of major milestones completed
    /// </summary>
    [ProtoMember(11)]
    public CivilizationRank Rank { get; set; } = CivilizationRank.Nascent;

    /// <summary>
    ///     Set of completed milestone IDs
    /// </summary>
    [ProtoMember(12)]
    public HashSet<string> CompletedMilestones { get; set; } = new();

    /// <summary>
    ///     Cumulative PvP kills during active wars (for war_heroes milestone)
    /// </summary>
    [ProtoMember(13)]
    public int WarKillCount { get; set; } = 0;

    /// <summary>
    ///     Cumulative completed NPC trader transactions across all members
    ///     (for trade_hub milestone — Caravan domain). Defaults to 0 for legacy saves.
    /// </summary>
    [ProtoMember(24)]
    public int NpcTradeCount { get; set; } = 0;

    /// <summary>
    ///     In-game month captured at civ creation; drives the annual
    ///     Founding Day holiday. 0 for pre-feature civilizations (no migration).
    /// </summary>
    [ProtoMember(20)]
    public int FoundingMonth { get; set; }

    /// <summary>In-game day-of-month captured at civ creation.</summary>
    [ProtoMember(21)]
    public int FoundingDay { get; set; }

    /// <summary>
    ///     Last in-game year the Founding Day fired. The ticker only fires
    ///     when <c>calendar.Year &gt; FoundingDayLastFiredYear</c>, which
    ///     makes the day-rollover idempotent across save/reload. Stamped to
    ///     the founding year at creation so the first-year "anniversary on
    ///     the day of founding" doesn't fire.
    /// </summary>
    [ProtoMember(22)]
    public int FoundingDayLastFiredYear { get; set; }

    /// <summary>
    ///     Last in-game year an advance-notice toast fired for the civ's
    ///     Founding Day (one in-game day before). Same idempotency
    ///     guarantee as <see cref="FoundingDayLastFiredYear"/>.
    /// </summary>
    [ProtoMember(23)]
    public int FoundingDayAdvanceFiredYear { get; set; }

    /// <summary>
    ///     Civilization-wide blessings unlocked via milestones
    /// </summary>
    [ProtoMember(14)]
    public HashSet<string> UnlockedBlessings { get; set; } = new();

    /// <summary>
    ///     Narrative identity axis (Mercantile / Martial / Mystic / Ascetic / Sovereign).
    ///     Derived once at founding from the founder religion's patron domain. Defaults
    ///     to <see cref="CivilizationEthos.Sovereign" /> for legacy saves that pre-date
    ///     this field.
    /// </summary>
    [ProtoMember(15)]
    public CivilizationEthos Ethos { get; set; } = CivilizationEthos.Sovereign;

    /// <summary>
    ///     Founder epithet (e.g. "the Forge-Crowned"). Computed once at founding from
    ///     the founder religion's patron domain and persisted; survives the founder
    ///     later changing religion or leaving the civilization. Empty for legacy saves.
    /// </summary>
    [ProtoMember(16)]
    public string FounderEpithet { get; set; } = string.Empty;

    /// <summary>
    ///     Display name of the civilization's seat. Auto-defaults to "{Name} Seat" at
    ///     founding; founder may edit later. Empty for legacy saves — repair on load.
    /// </summary>
    [ProtoMember(17)]
    public string CapitalName { get; set; } = string.Empty;

    /// <summary>
    ///     Optional binding to a holy site owned by one of the civ's member religions.
    ///     Null means unbound. Cleared automatically when the site is destroyed or
    ///     the owning religion leaves the civilization; <see cref="CapitalName" /> is
    ///     preserved in both cascades.
    /// </summary>
    [ProtoMember(18)]
    public string? CapitalHolySiteId { get; set; }

    /// <summary>
    ///     Backing field for the civilization's chronicle of significant events.
    /// </summary>
    [ProtoMember(19)]
    private List<ChronicleEntry> _chronicle = new();

    /// <summary>
    ///     Chronological list of significant civilization events (oldest first).
    ///     Thread-safe read-only snapshot.
    /// </summary>
    [ProtoIgnore]
    public IReadOnlyList<ChronicleEntry> Chronicle
    {
        get { lock (Lock) { return _chronicle.ToList(); } }
    }

    /// <summary>
    ///     Appends an entry to the civilization's chronicle.
    /// </summary>
    public void AddChronicleEntry(ChronicleEntry entry)
    {
        if (entry == null) return;
        lock (Lock) { _chronicle.Add(entry); }
    }

    /// <summary>
    ///     Checks if the civilization has a valid number of member religions (1-4)
    /// </summary>
    public bool IsValid
    {
        get { lock (Lock) { return _memberReligionIds.Count >= 1 && _memberReligionIds.Count <= 4; } }
    }

    /// <summary>
    ///     Checks if a religion is a member of this civilization
    /// </summary>
    public bool HasReligion(string religionId)
    {
        lock (Lock) { return _memberReligionIds.Contains(religionId); }
    }

    /// <summary>
    ///     Adds a religion to the civilization
    /// </summary>
    public bool AddReligion(string religionId)
    {
        lock (Lock)
        {
            if (_memberReligionIds.Count >= 4 || _memberReligionIds.Contains(religionId))
                return false;

            _memberReligionIds.Add(religionId);
            return true;
        }
    }

    /// <summary>
    ///     Removes a religion from the civilization
    /// </summary>
    public bool RemoveReligion(string religionId)
    {
        lock (Lock) { return _memberReligionIds.Remove(religionId); }
    }

    /// <summary>
    ///     Updates the civilization's icon
    /// </summary>
    public void UpdateIcon(string icon)
    {
        Icon = icon;
    }

    /// <summary>
    ///     Updates the civilization's description
    /// </summary>
    public void UpdateDescription(string description)
    {
        Description = description;
    }

    /// <summary>
    ///     Checks if the specified player is the civilization founder
    /// </summary>
    public bool IsFounder(string playerUID)
    {
        return !string.IsNullOrEmpty(FounderUID) && FounderUID == playerUID;
    }
}

/// <summary>
///     Represents an invitation for a religion to join a civilization
/// </summary>
[ProtoContract]
public class CivilizationInvite
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public CivilizationInvite()
    {
    }

    /// <summary>
    ///     Creates a new civilization invite
    /// </summary>
    public CivilizationInvite(string inviteId, string civId, string religionId, DateTime sentDate)
    {
        InviteId = inviteId;
        CivId = civId;
        ReligionId = religionId;
        SentDate = sentDate;
        ExpiresDate = sentDate.AddDays(7);
    }

    /// <summary>
    ///     Unique identifier for the invite
    /// </summary>
    [ProtoMember(1)]
    public string InviteId { get; set; } = string.Empty;

    /// <summary>
    ///     Civilization ID this invite is for
    /// </summary>
    [ProtoMember(2)]
    public string CivId { get; set; } = string.Empty;

    /// <summary>
    ///     Religion ID being invited
    /// </summary>
    [ProtoMember(3)]
    public string ReligionId { get; set; } = string.Empty;

    /// <summary>
    ///     When the invite was sent
    /// </summary>
    [ProtoMember(4)]
    public DateTime SentDate { get; set; }

    /// <summary>
    ///     When the invite expires (7 days from sent date)
    /// </summary>
    [ProtoMember(5)]
    public DateTime ExpiresDate { get; set; }

    /// <summary>
    ///     Checks if the invite is still valid (not expired)
    /// </summary>
    public bool IsValid => DateTime.UtcNow < ExpiresDate;
}