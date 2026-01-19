using System;
using System.Collections.Generic;
using ProtoBuf;

namespace DivineAscension.Data;

/// <summary>
/// Serializable chunk position for holy site storage.
/// Uses chunk coordinates (world pos / 256).
/// </summary>
[ProtoContract]
public record SerializableChunkPos
{
    public SerializableChunkPos()
    {
    }

    public SerializableChunkPos(int chunkX, int chunkZ)
    {
        ChunkX = chunkX;
        ChunkZ = chunkZ;
    }

    [ProtoMember(1)] public int ChunkX { get; init; }

    [ProtoMember(2)] public int ChunkZ { get; init; }

    public string ToKey() => $"{ChunkX},{ChunkZ}";
}

/// <summary>
/// Data model for a holy site.
/// Holy sites provide territory and prayer bonuses based on tier (1-3).
/// Tier is determined by number of chunks (1/2-3/4-6).
/// </summary>
[ProtoContract]
public class HolySiteData
{
    [ProtoMember(8)] private List<SerializableChunkPos> _expandedChunks = new();

    // Parameterless constructor for ProtoBuf
    public HolySiteData()
    {
    }

    // Full constructor
    public HolySiteData(string siteUID, string religionUID, string siteName,
        SerializableChunkPos centerChunk, string founderUID, string founderName)
    {
        SiteUID = siteUID;
        ReligionUID = religionUID;
        SiteName = siteName;
        CenterChunk = centerChunk;
        FounderUID = founderUID;
        FounderName = founderName;
        CreationDate = DateTime.UtcNow;
    }

    [ProtoMember(1)] public string SiteUID { get; set; } = string.Empty;

    [ProtoMember(2)] public string ReligionUID { get; set; } = string.Empty;

    [ProtoMember(3)] public string SiteName { get; set; } = string.Empty;

    [ProtoMember(4)] public SerializableChunkPos CenterChunk { get; set; } = new();

    [ProtoMember(5)] public string FounderUID { get; set; } = string.Empty;

    [ProtoMember(6)] public string FounderName { get; set; } = string.Empty;

    [ProtoMember(7)] public DateTime CreationDate { get; set; } = DateTime.UtcNow;

    [ProtoIgnore] public IReadOnlyList<SerializableChunkPos> ExpandedChunks => _expandedChunks.AsReadOnly();

    /// <summary>
    /// Calculates tier based on total chunk count.
    /// Tier 1: 1 chunk
    /// Tier 2: 2-3 chunks
    /// Tier 3: 4-6 chunks
    /// </summary>
    public int GetTier()
    {
        int totalChunks = 1 + _expandedChunks.Count;
        if (totalChunks == 1) return 1;
        if (totalChunks <= 3) return 2;
        return 3;
    }

    /// <summary>
    /// Gets territory favor multiplier based on tier.
    /// Tier 1: 1.5x, Tier 2: 2.0x, Tier 3: 2.5x
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
    /// Gets prayer favor multiplier based on tier.
    /// Tier 1: 2.0x, Tier 2: 2.5x, Tier 3: 3.0x
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
    /// Adds a chunk to the expanded territory.
    /// </summary>
    public void AddChunk(SerializableChunkPos chunk)
    {
        if (!_expandedChunks.Contains(chunk))
            _expandedChunks.Add(chunk);
    }

    /// <summary>
    /// Checks if a chunk is part of this holy site (center or expanded).
    /// </summary>
    public bool ContainsChunk(SerializableChunkPos chunk)
    {
        return CenterChunk.Equals(chunk) || _expandedChunks.Contains(chunk);
    }

    /// <summary>
    /// Gets all chunks (center + expanded).
    /// </summary>
    public List<SerializableChunkPos> GetAllChunks()
    {
        var chunks = new List<SerializableChunkPos> { CenterChunk };
        chunks.AddRange(_expandedChunks);
        return chunks;
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