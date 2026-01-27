using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Request for milestone progress information
/// </summary>
[ProtoContract]
public class MilestoneProgressRequestPacket
{
    /// <summary>
    ///     Civilization ID to get progress for
    /// </summary>
    [ProtoMember(1)]
    public string CivId { get; set; } = string.Empty;
}

/// <summary>
///     Response containing milestone progress information
/// </summary>
[ProtoContract]
public class MilestoneProgressResponsePacket
{
    /// <summary>
    ///     Civilization ID
    /// </summary>
    [ProtoMember(1)]
    public string CivId { get; set; } = string.Empty;

    /// <summary>
    ///     Current civilization rank
    /// </summary>
    [ProtoMember(2)]
    public int Rank { get; set; }

    /// <summary>
    ///     List of completed milestone IDs
    /// </summary>
    [ProtoMember(3)]
    public List<string> CompletedMilestones { get; set; } = new();

    /// <summary>
    ///     Progress information for each milestone
    /// </summary>
    [ProtoMember(4)]
    public List<MilestoneProgressDto> Progress { get; set; } = new();

    /// <summary>
    ///     Active bonuses from completed milestones
    /// </summary>
    [ProtoMember(5)]
    public CivilizationBonusesDto Bonuses { get; set; } = new();
}

/// <summary>
///     DTO for milestone progress in network packets
/// </summary>
[ProtoContract]
public class MilestoneProgressDto
{
    /// <summary>
    ///     Milestone ID
    /// </summary>
    [ProtoMember(1)]
    public string MilestoneId { get; set; } = string.Empty;

    /// <summary>
    ///     Display name of the milestone
    /// </summary>
    [ProtoMember(2)]
    public string MilestoneName { get; set; } = string.Empty;

    /// <summary>
    ///     Current progress value
    /// </summary>
    [ProtoMember(3)]
    public int CurrentValue { get; set; }

    /// <summary>
    ///     Target value to complete
    /// </summary>
    [ProtoMember(4)]
    public int TargetValue { get; set; }

    /// <summary>
    ///     Whether the milestone is completed
    /// </summary>
    [ProtoMember(5)]
    public bool IsCompleted { get; set; }
}

/// <summary>
///     DTO for civilization bonuses in network packets
/// </summary>
[ProtoContract]
public class CivilizationBonusesDto
{
    /// <summary>
    ///     Prestige multiplier (1.0 = no bonus)
    /// </summary>
    [ProtoMember(1)]
    public float PrestigeMultiplier { get; set; } = 1.0f;

    /// <summary>
    ///     Favor multiplier (1.0 = no bonus)
    /// </summary>
    [ProtoMember(2)]
    public float FavorMultiplier { get; set; } = 1.0f;

    /// <summary>
    ///     Conquest multiplier (1.0 = no bonus)
    /// </summary>
    [ProtoMember(3)]
    public float ConquestMultiplier { get; set; } = 1.0f;

    /// <summary>
    ///     Bonus holy site slots
    /// </summary>
    [ProtoMember(4)]
    public int BonusHolySiteSlots { get; set; }
}

/// <summary>
///     Notification sent when a milestone is unlocked
/// </summary>
[ProtoContract]
public class MilestoneUnlockedPacket
{
    /// <summary>
    ///     Civilization ID
    /// </summary>
    [ProtoMember(1)]
    public string CivId { get; set; } = string.Empty;

    /// <summary>
    ///     Milestone ID that was unlocked
    /// </summary>
    [ProtoMember(2)]
    public string MilestoneId { get; set; } = string.Empty;

    /// <summary>
    ///     Display name of the milestone
    /// </summary>
    [ProtoMember(3)]
    public string MilestoneName { get; set; } = string.Empty;

    /// <summary>
    ///     New civilization rank (after milestone)
    /// </summary>
    [ProtoMember(4)]
    public int NewRank { get; set; }

    /// <summary>
    ///     Prestige payout amount
    /// </summary>
    [ProtoMember(5)]
    public int PrestigePayout { get; set; }

    /// <summary>
    ///     Description of the benefit granted
    /// </summary>
    [ProtoMember(6)]
    public string BenefitDescription { get; set; } = string.Empty;
}
