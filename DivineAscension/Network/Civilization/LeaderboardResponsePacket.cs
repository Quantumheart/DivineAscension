using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network.Civilization;

/// <summary>
///     Server response for the Standing of Realms leaderboard. Carries every
///     board (Standing, Conquest, Endurance, Deeds) in one response so the
///     client can switch boards instantly without a network round-trip. The
///     realm count is shared across boards; each board carries its own ordered
///     entries and the viewer's position within that ordering.
/// </summary>
[ProtoContract]
public class LeaderboardResponsePacket
{
    public LeaderboardResponsePacket() { }

    public LeaderboardResponsePacket(List<Board> boards)
    {
        Boards = boards;
    }

    [ProtoMember(1)] public List<Board> Boards { get; set; } = new();

    /// <summary>Total number of ranked realms (shared across all boards).</summary>
    [ProtoMember(2)] public int TotalRealms { get; set; }

    /// <summary>One board: its metric, ordered entries, and the viewer's position within it.</summary>
    [ProtoContract]
    public class Board
    {
        /// <summary>The <see cref="DivineAscension.Models.Enum.LeaderboardMetric" /> this board ranks by.</summary>
        [ProtoMember(1)] public int Metric { get; set; }

        /// <summary>Realms ranked by this board's metric, highest first.</summary>
        [ProtoMember(2)] public List<LeaderboardEntry> Entries { get; set; } = new();

        /// <summary>
        ///     The viewer's own realm position (1-based) in this board, or 0 when
        ///     the viewer belongs to no civilization.
        /// </summary>
        [ProtoMember(3)] public int ViewerPosition { get; set; }
    }

    /// <summary>One ranked row: position, civilization, tier label, and score.</summary>
    [ProtoContract]
    public class LeaderboardEntry
    {
        [ProtoMember(1)] public int Position { get; set; }
        [ProtoMember(2)] public string CivId { get; set; } = string.Empty;
        [ProtoMember(3)] public string Name { get; set; } = string.Empty;
        [ProtoMember(4)] public string TierLabel { get; set; } = string.Empty;
        [ProtoMember(5)] public int Score { get; set; }

        /// <summary>The realm's <see cref="DivineAscension.Models.Enum.CivilizationEthos" />, drives the per-realm heraldic glyph.</summary>
        [ProtoMember(6)] public int Ethos { get; set; }
    }
}
