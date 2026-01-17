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
}