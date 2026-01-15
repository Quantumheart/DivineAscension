using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server sends activity log entries to client
/// </summary>
[ProtoContract]
public class ActivityLogResponsePacket
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public ActivityLogResponsePacket()
    {
    }

    /// <summary>
    ///     Creates a new activity log response packet
    /// </summary>
    public ActivityLogResponsePacket(List<ActivityEntry> entries)
    {
        Entries = entries;
    }

    /// <summary>
    ///     List of activity log entries
    /// </summary>
    [ProtoMember(1)]
    public List<ActivityEntry> Entries { get; set; } = new();

    /// <summary>
    ///     Represents a single activity entry for network transmission
    /// </summary>
    [ProtoContract]
    public class ActivityEntry
    {
        /// <summary>
        ///     Unique identifier for this entry
        /// </summary>
        [ProtoMember(1)]
        public string EntryId { get; set; } = string.Empty;

        /// <summary>
        ///     Player UID who earned the reward
        /// </summary>
        [ProtoMember(2)]
        public string PlayerUID { get; set; } = string.Empty;

        /// <summary>
        ///     Cached player name for display
        /// </summary>
        [ProtoMember(3)]
        public string PlayerName { get; set; } = string.Empty;

        /// <summary>
        ///     Action description (e.g., "hunting deer")
        /// </summary>
        [ProtoMember(4)]
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        ///     Favor amount awarded to the player
        /// </summary>
        [ProtoMember(5)]
        public int FavorAmount { get; set; }

        /// <summary>
        ///     Prestige amount awarded to the religion
        /// </summary>
        [ProtoMember(6)]
        public int PrestigeAmount { get; set; }

        /// <summary>
        ///     Timestamp as ticks (DateTime serialization for ProtoBuf)
        /// </summary>
        [ProtoMember(7)]
        public long TimestampTicks { get; set; }

        /// <summary>
        ///     Deity domain this activity was aligned with
        /// </summary>
        [ProtoMember(8)]
        public string DeityDomain { get; set; } = string.Empty;
    }
}