using System.Collections.Generic;
using DivineAscension.GUI.State;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
///     Persisted UI preferences. Round-tripped via <c>SerializerUtil</c> as part
///     of <see cref="ModConfigData" />.
/// </summary>
[ProtoContract]
public class UiPrefs
{
    [ProtoMember(1)] public int WindowWidth { get; set; } = 1400;

    [ProtoMember(2)] public int WindowHeight { get; set; } = 900;

    [ProtoMember(3)] public bool SidebarCollapsed { get; set; }

    [ProtoMember(4)] public Dictionary<string, bool> CollapsedGroups { get; set; } = new();

    [ProtoMember(5)] public SidebarNavId LastNavId { get; set; } = SidebarNavId.ReligionInfo;
}
