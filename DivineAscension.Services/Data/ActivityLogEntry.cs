using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
///     Represents a single activity entry in a religion's activity log.
///     Tracks favor/prestige awards from member actions.
/// </summary>
[ProtoContract]
public class ActivityLogEntry
{
    /// <summary>
    ///     Parameterless constructor for serialization
    /// </summary>
    public ActivityLogEntry()
    {
    }

    /// <summary>
    ///     Creates a new activity log entry
    /// </summary>
    public ActivityLogEntry(string playerUID, string playerName, string actionType,
        int favorAmount, int prestigeAmount, string deityDomain)
    {
        EntryId = Guid.NewGuid().ToString();
        PlayerUID = playerUID;
        PlayerName = playerName;
        ActionType = actionType;
        FavorAmount = favorAmount;
        PrestigeAmount = prestigeAmount;
        Timestamp = DateTime.UtcNow;
        DeityDomain = deityDomain;
    }

    /// <summary>
    ///     Unique identifier for this entry (for deduplication)
    /// </summary>
    [ProtoMember(1)]
    public string EntryId { get; set; } = string.Empty;

    /// <summary>
    ///     Player UID who earned the reward
    /// </summary>
    [ProtoMember(2)]
    public string PlayerUID { get; set; } = string.Empty;

    /// <summary>
    ///     Cached player name for display (name at time of action)
    /// </summary>
    [ProtoMember(3)]
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    ///     Action description (e.g., "hunting deer", "mining copper ore")
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
    ///     When the action occurred (UTC)
    /// </summary>
    [ProtoMember(7)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Deity domain this activity was aligned with (e.g., "Wild", "Craft")
    /// </summary>
    [ProtoMember(8)]
    public string DeityDomain { get; set; } = string.Empty;
}