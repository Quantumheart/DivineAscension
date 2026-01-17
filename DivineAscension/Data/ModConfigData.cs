using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
///     World-level configuration data for the Divine Ascension mod.
///     Persisted per-world via world save data.
/// </summary>
[ProtoContract]
public class ModConfigData
{
    /// <summary>
    ///     Data version for migration support
    /// </summary>
    [ProtoMember(1)]
    public int DataVersion { get; set; } = 1;

    /// <summary>
    ///     Whether the profanity filter is enabled for this world.
    ///     Default is true.
    /// </summary>
    [ProtoMember(2)]
    public bool ProfanityFilterEnabled { get; set; } = true;

    /// <summary>
    ///     Whether the cooldown system is enabled globally for this world.
    ///     Default is true (CRITICAL: disabling removes anti-griefing protection).
    /// </summary>
    [ProtoMember(10)]
    public bool CooldownsEnabled { get; set; } = true;

    /// <summary>
    ///     Cooldown duration in seconds for religion deletion operations.
    ///     Default: 60 seconds (CRITICAL security mitigation)
    /// </summary>
    [ProtoMember(3)]
    public int ReligionDeletionCooldown { get; set; } = 60;

    /// <summary>
    ///     Cooldown duration in seconds for member kick operations.
    ///     Default: 5 seconds (CRITICAL security mitigation)
    /// </summary>
    [ProtoMember(4)]
    public int MemberKickCooldown { get; set; } = 5;

    /// <summary>
    ///     Cooldown duration in seconds for member ban operations.
    ///     Default: 10 seconds (CRITICAL security mitigation)
    /// </summary>
    [ProtoMember(5)]
    public int MemberBanCooldown { get; set; } = 10;

    /// <summary>
    ///     Cooldown duration in seconds for invite operations.
    ///     Default: 2 seconds (HIGH security mitigation)
    /// </summary>
    [ProtoMember(6)]
    public int InviteCooldown { get; set; } = 2;

    /// <summary>
    ///     Cooldown duration in seconds for religion creation operations.
    ///     Default: 300 seconds / 5 minutes (HIGH security mitigation)
    /// </summary>
    [ProtoMember(7)]
    public int ReligionCreationCooldown { get; set; } = 300;

    /// <summary>
    ///     Cooldown duration in seconds for diplomatic proposal operations.
    ///     Default: 30 seconds (MEDIUM security mitigation)
    /// </summary>
    [ProtoMember(8)]
    public int ProposalCooldown { get; set; } = 30;

    /// <summary>
    ///     Cooldown duration in seconds for war declaration operations.
    ///     Default: 60 seconds (MEDIUM security mitigation)
    /// </summary>
    [ProtoMember(9)]
    public int WarDeclarationCooldown { get; set; } = 60;
}