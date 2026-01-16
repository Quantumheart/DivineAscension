using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network;

/// <summary>
///     Server sends list of available deity domains to client
/// </summary>
[ProtoContract]
public class AvailableDomainsResponsePacket
{
    public AvailableDomainsResponsePacket()
    {
    }

    public AvailableDomainsResponsePacket(List<string> domains)
    {
        Domains = domains;
    }

    [ProtoMember(1)] public List<string> Domains { get; set; } = new();
}
