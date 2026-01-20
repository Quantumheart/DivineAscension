using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.MathTools;

namespace DivineAscension.Data;

/// <summary>
/// Serializable block position for ProtoBuf compatibility.
/// BlockPos from Vintage Story cannot be serialized directly.
/// </summary>
[ProtoContract]
public class SerializableBlockPos
{
    public SerializableBlockPos() { }

    public SerializableBlockPos(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    [ProtoMember(1)] public int X { get; set; }
    [ProtoMember(2)] public int Y { get; set; }
    [ProtoMember(3)] public int Z { get; set; }

    /// <summary>
    /// Convert to Vintage Story's BlockPos.
    /// </summary>
    public BlockPos ToBlockPos() => new BlockPos(X, Y, Z);

    /// <summary>
    /// Create from Vintage Story's BlockPos.
    /// </summary>
    public static SerializableBlockPos FromBlockPos(BlockPos pos)
        => new SerializableBlockPos(pos.X, pos.Y, pos.Z);

    /// <summary>
    /// Check equality with another BlockPos.
    /// </summary>
    public bool Equals(BlockPos pos)
    {
        return X == pos.X && Y == pos.Y && Z == pos.Z;
    }
}

/// <summary>
/// Serializable 3D rectangular area for ProtoBuf compatibility.
/// Cuboidi from Vintage Story cannot be serialized directly.
/// </summary>
[ProtoContract]
public class SerializableCuboidi
{
    public SerializableCuboidi() { }

    public SerializableCuboidi(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        X1 = x1;
        Y1 = y1;
        Z1 = z1;
        X2 = x2;
        Y2 = y2;
        Z2 = z2;
    }

    public SerializableCuboidi(Cuboidi source)
    {
        X1 = source.X1;
        Y1 = source.Y1;
        Z1 = source.Z1;
        X2 = source.X2;
        Y2 = source.Y2;
        Z2 = source.Z2;
    }

    [ProtoMember(1)] public int X1 { get; set; }
    [ProtoMember(2)] public int Y1 { get; set; }
    [ProtoMember(3)] public int Z1 { get; set; }
    [ProtoMember(4)] public int X2 { get; set; }
    [ProtoMember(5)] public int Y2 { get; set; }
    [ProtoMember(6)] public int Z2 { get; set; }

    /// <summary>
    /// Convert back to Vintage Story's Cuboidi.
    /// </summary>
    public Cuboidi ToCuboidi() => new Cuboidi(X1, Y1, Z1, X2, Y2, Z2);

    /// <summary>
    /// Calculate 3D volume of this area.
    /// </summary>
    public int GetVolume()
    {
        int sizeX = Math.Abs(X2 - X1) + 1;
        int sizeY = Math.Abs(Y2 - Y1) + 1;
        int sizeZ = Math.Abs(Z2 - Z1) + 1;
        return sizeX * sizeY * sizeZ;
    }

    /// <summary>
    /// Calculate 2D footprint (X×Z area, ignoring Y).
    /// </summary>
    public int GetXZArea()
    {
        int sizeX = Math.Abs(X2 - X1) + 1;
        int sizeZ = Math.Abs(Z2 - Z1) + 1;
        return sizeX * sizeZ;
    }

    /// <summary>
    /// Check if this area contains a block position.
    /// </summary>
    public bool Contains(BlockPos pos)
    {
        return pos.X >= Math.Min(X1, X2) && pos.X <= Math.Max(X1, X2) &&
               pos.Y >= Math.Min(Y1, Y2) && pos.Y <= Math.Max(Y1, Y2) &&
               pos.Z >= Math.Min(Z1, Z2) && pos.Z <= Math.Max(Z1, Z2);
    }

    /// <summary>
    /// Check if this area intersects another area.
    /// </summary>
    public bool Intersects(SerializableCuboidi other)
    {
        return ToCuboidi().Intersects(other.ToCuboidi());
    }
}

/// <summary>
/// Data model for a holy site.
/// Holy sites cover entire land claim boundaries (all areas) with exact claim boundaries.
/// Tier calculation based on 3D volume (X×Y×Z blocks).
/// </summary>
[ProtoContract]
public class HolySiteData
{
    // Parameterless constructor for ProtoBuf
    public HolySiteData() { }

    // Full constructor
    public HolySiteData(
        string siteUID,
        string religionUID,
        string siteName,
        List<SerializableCuboidi> areas,
        string founderUID,
        string founderName)
    {
        SiteUID = siteUID;
        ReligionUID = religionUID;
        SiteName = siteName;
        Areas = areas ?? new List<SerializableCuboidi>();
        FounderUID = founderUID;
        FounderName = founderName;
        CreationDate = DateTime.UtcNow;
    }

    [ProtoMember(1)]
    public string SiteUID { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string ReligionUID { get; set; } = string.Empty;

    [ProtoMember(3)]
    public string SiteName { get; set; } = string.Empty;

    [ProtoMember(4)]
    public string FounderUID { get; set; } = string.Empty;

    [ProtoMember(5)]
    public string FounderName { get; set; } = string.Empty;

    [ProtoMember(6)]
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;

    [ProtoMember(7)]
    public List<SerializableCuboidi> Areas { get; set; } = new();

    /// <summary>
    /// Position of the altar block that created this holy site (optional).
    /// Null for legacy sites created via command.
    /// </summary>
    [ProtoMember(8)]
    public SerializableBlockPos? AltarPosition { get; set; }

    /// <summary>
    /// Optional description for the holy site (max 200 characters).
    /// Can only be edited by the site consecrator (FounderUID).
    /// </summary>
    [ProtoMember(9)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Check if this is an altar-based holy site.
    /// </summary>
    public bool IsAltarSite() => AltarPosition != null;

    /// <summary>
    /// Check if the given position matches this site's altar position.
    /// </summary>
    public bool IsAtAltarPosition(BlockPos pos)
    {
        return AltarPosition?.Equals(pos) ?? false;
    }

    /// <summary>
    /// Calculate total 3D volume of all areas (used for tier calculation).
    /// </summary>
    public int GetTotalVolume()
    {
        return Areas.Sum(area => area.GetVolume());
    }

    /// <summary>
    /// Calculate total 2D footprint of all areas.
    /// </summary>
    public int GetTotalXZArea()
    {
        return Areas.Sum(area => area.GetXZArea());
    }

    /// <summary>
    /// Calculate weighted center position of all areas.
    /// </summary>
    public BlockPos GetCenter()
    {
        if (Areas.Count == 0)
            return new BlockPos(0, 0, 0);

        long totalX = 0, totalY = 0, totalZ = 0;
        int totalVolume = 0;

        foreach (var area in Areas)
        {
            int volume = area.GetVolume();
            int centerX = (area.X1 + area.X2) / 2;
            int centerY = (area.Y1 + area.Y2) / 2;
            int centerZ = (area.Z1 + area.Z2) / 2;

            // Cast to long before multiplication to prevent integer overflow
            // Block coordinates can be very large (500k+), causing overflow when multiplied by volume
            totalX += (long)centerX * volume;
            totalY += (long)centerY * volume;
            totalZ += (long)centerZ * volume;
            totalVolume += volume;
        }

        return new BlockPos(
            (int)(totalX / totalVolume),
            (int)(totalY / totalVolume),
            (int)(totalZ / totalVolume)
        );
    }

    /// <summary>
    /// Tier calculation based on 3D volume:
    /// Tier 1: &lt;50,000 blocks³ (territory 1.5x, prayer 2.0x)
    /// Tier 2: 50,000-200,000 blocks³ (territory 2.0x, prayer 2.5x)
    /// Tier 3: 200,000+ blocks³ (territory 2.5x, prayer 3.0x)
    /// </summary>
    public int GetTier()
    {
        int volume = GetTotalVolume();
        if (volume < 50000) return 1;
        if (volume < 200000) return 2;
        return 3;
    }

    /// <summary>
    /// Territory multiplier based on tier.
    /// </summary>
    public double GetTerritoryMultiplier()
    {
        return GetTier() switch
        {
            1 => 1.5,
            2 => 2.0,
            3 => 2.5,
            _ => 1.0
        };
    }

    /// <summary>
    /// Prayer multiplier based on tier.
    /// </summary>
    public double GetPrayerMultiplier()
    {
        return GetTier() switch
        {
            1 => 2.0,
            2 => 2.5,
            3 => 3.0,
            _ => 1.0
        };
    }

    /// <summary>
    /// Check if a block position is within any area of this holy site.
    /// </summary>
    public bool ContainsPosition(BlockPos pos)
    {
        return Areas.Any(area => area.Contains(pos));
    }

    /// <summary>
    /// Check if this holy site intersects with another (for overlap detection).
    /// </summary>
    public bool Intersects(HolySiteData other)
    {
        foreach (var myArea in Areas)
        {
            foreach (var otherArea in other.Areas)
            {
                if (myArea.Intersects(otherArea))
                    return true;
            }
        }
        return false;
    }
}

/// <summary>
/// World data container for all holy sites.
/// </summary>
[ProtoContract]
public class HolySiteWorldData
{
    public HolySiteWorldData()
    {
        HolySites = new List<HolySiteData>();
    }

    [ProtoMember(1)] public List<HolySiteData> HolySites { get; set; }
}
