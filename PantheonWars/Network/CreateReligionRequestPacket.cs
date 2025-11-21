using System.Collections.Generic;
using ProtoBuf;

namespace PantheonWars.Network;

/// <summary>
///     Client requests to create a new religion
/// </summary>
[ProtoContract]
public class CreateReligionRequestPacket
{
    public CreateReligionRequestPacket()
    {
    }

    public CreateReligionRequestPacket(string religionName, List<string> selectedBlessings, bool isPublic)
    {
        ReligionName = religionName;
        SelectedBlessings = selectedBlessings;
        IsPublic = isPublic;
    }

    [ProtoMember(1)] public string ReligionName { get; set; } = string.Empty;

    [ProtoMember(2)] public List<string> SelectedBlessings { get; set; } = new();

    [ProtoMember(3)] public bool IsPublic { get; set; }
}