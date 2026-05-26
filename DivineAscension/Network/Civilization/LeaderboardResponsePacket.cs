using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network.Civilization;

/// <summary>
///     Server response for the Standing of Realms leaderboard: every
///     civilization ranked by Standing (civilization rank), highest first.
/// </summary>
[ProtoContract]
public class LeaderboardResponsePacket
{
    public LeaderboardResponsePacket() { }

    public LeaderboardResponsePacket(List<LeaderboardEntry> entries)
    {
        Entries = entries;
    }

    [ProtoMember(1)] public List<LeaderboardEntry> Entries { get; set; } = new();

    /// <summary>One ranked row: position, civilization, tier label, and score.</summary>
    [ProtoContract]
    public class LeaderboardEntry
    {
        [ProtoMember(1)] public int Position { get; set; }
        [ProtoMember(2)] public string CivId { get; set; } = string.Empty;
        [ProtoMember(3)] public string Name { get; set; } = string.Empty;
        [ProtoMember(4)] public string TierLabel { get; set; } = string.Empty;
        [ProtoMember(5)] public int Score { get; set; }
    }
}
