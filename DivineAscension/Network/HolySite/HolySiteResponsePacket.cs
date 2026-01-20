using System;
using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Network.HolySite;

/// <summary>
/// Server sends holy site data to client.
/// Contains list of sites or detailed info for a single site.
/// </summary>
[ProtoContract]
public class HolySiteResponsePacket
{
    public HolySiteResponsePacket()
    {
    }

    public HolySiteResponsePacket(List<HolySiteInfo> sites)
    {
        Sites = sites;
    }

    public HolySiteResponsePacket(HolySiteDetailInfo detailInfo)
    {
        DetailInfo = detailInfo;
    }

    /// <summary>
    /// List of holy sites (for "list" and "religion_sites" actions)
    /// </summary>
    [ProtoMember(1)]
    public List<HolySiteInfo> Sites { get; set; } = new();

    /// <summary>
    /// Detailed info for a single site (for "detail" action)
    /// </summary>
    [ProtoMember(2)]
    public HolySiteDetailInfo? DetailInfo { get; set; }

    /// <summary>
    /// Basic holy site information for list display.
    /// </summary>
    [ProtoContract]
    public class HolySiteInfo
    {
        [ProtoMember(1)] public string SiteUID { get; set; } = string.Empty;

        [ProtoMember(2)] public string SiteName { get; set; } = string.Empty;

        [ProtoMember(3)] public string ReligionUID { get; set; } = string.Empty;

        [ProtoMember(4)] public string ReligionName { get; set; } = string.Empty;

        [ProtoMember(5)] public string Domain { get; set; } = string.Empty;

        [ProtoMember(6)] public int Tier { get; set; }

        [ProtoMember(7)] public int Volume { get; set; }

        [ProtoMember(8)] public int AreaCount { get; set; }

        [ProtoMember(10)] public double PrayerMultiplier { get; set; }

        [ProtoMember(11)] public int CenterX { get; set; }

        [ProtoMember(12)] public int CenterY { get; set; }

        [ProtoMember(13)] public int CenterZ { get; set; }

        [ProtoMember(14)] public string FounderUID { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detailed holy site information including areas and founder info.
    /// </summary>
    [ProtoContract]
    public class HolySiteDetailInfo
    {
        [ProtoMember(1)] public string SiteUID { get; set; } = string.Empty;

        [ProtoMember(2)] public string SiteName { get; set; } = string.Empty;

        [ProtoMember(3)] public string ReligionUID { get; set; } = string.Empty;

        [ProtoMember(4)] public string ReligionName { get; set; } = string.Empty;

        [ProtoMember(5)] public string Domain { get; set; } = string.Empty;

        [ProtoMember(6)] public string FounderUID { get; set; } = string.Empty;

        [ProtoMember(7)] public string FounderName { get; set; } = string.Empty;

        [ProtoMember(8)] public DateTime CreationDate { get; set; }

        [ProtoMember(9)] public int Tier { get; set; }

        [ProtoMember(10)] public int Volume { get; set; }

        [ProtoMember(11)] public int XZArea { get; set; }

        [ProtoMember(13)] public double PrayerMultiplier { get; set; }

        [ProtoMember(14)] public List<ChunkInfo> Areas { get; set; } = new();

        [ProtoMember(15)] public CenterPosition Center { get; set; } = new();

        [ProtoMember(16)] public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 3D area information (serialized from SerializableCuboidi).
    /// </summary>
    [ProtoContract]
    public class ChunkInfo
    {
        [ProtoMember(1)] public int X1 { get; set; }

        [ProtoMember(2)] public int Y1 { get; set; }

        [ProtoMember(3)] public int Z1 { get; set; }

        [ProtoMember(4)] public int X2 { get; set; }

        [ProtoMember(5)] public int Y2 { get; set; }

        [ProtoMember(6)] public int Z2 { get; set; }

        [ProtoMember(7)] public int Volume { get; set; }

        [ProtoMember(8)] public int XZArea { get; set; }
    }

    /// <summary>
    /// Center position of a holy site.
    /// </summary>
    [ProtoContract]
    public class CenterPosition
    {
        [ProtoMember(1)] public int X { get; set; }

        [ProtoMember(2)] public int Y { get; set; }

        [ProtoMember(3)] public int Z { get; set; }
    }
}