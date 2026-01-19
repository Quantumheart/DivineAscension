# Holy Sites Implementation Summary

## Design Changes from Original Plan

### Overview
The holy site system was redesigned from a **chunk-based expansion model** to a **land claim boundary model**. This change simplifies the system and provides a more intuitive user experience.

## Key Changes

### 1. From Chunk-Based to Area-Based Storage

**OLD Design (Chunk-Based)**:
- Holy sites stored center chunk and expanded chunks
- Players could expand sites one chunk at a time using `/holysite expand`
- Maximum size of 6 chunks
- Tier calculated by chunk count (1 chunk = Tier 1, 2-3 = Tier 2, 4-6 = Tier 3)

**NEW Design (Area-Based)**:
- Holy sites match land claim boundaries exactly
- When consecrated, ALL areas of the land claim become holy
- No expansion - to grow a holy site, expand the land claim first
- Tier calculated by 3D volume in blocks³ (see tier thresholds below)

### 2. Tier Calculation Changes

**OLD Tiers (Chunk Count)**:
- Tier 1: 1 chunk (65,536 blocks²)
- Tier 2: 2-3 chunks (131,072 - 196,608 blocks²)
- Tier 3: 4-6 chunks (262,144 - 393,216 blocks²)

**NEW Tiers (3D Volume)**:
- Tier 1: < 50,000 blocks³ (territory 1.5x, prayer 2.0x)
- Tier 2: 50,000 - 200,000 blocks³ (territory 2.0x, prayer 2.5x)
- Tier 3: 200,000+ blocks³ (territory 2.5x, prayer 3.0x)

### 3. Command Changes

**Removed Commands**:
- `/holysite expand <site_name>` - No longer needed

**Updated Commands**:
- `/holysite consecrate <name>` - Now consecrates entire land claim instead of single chunk
- `/holysite info [site_name]` - Shows volume and area count instead of chunk count
- `/holysite list` - Displays volume instead of chunk count
- `/holysite nearby [radius]` - Distance calculated from weighted center of all areas

### 4. Data Model Changes

**OLD Model**:
```csharp
public class HolySiteData
{
    public SerializableChunkPos CenterChunk { get; set; }
    public List<SerializableChunkPos> ExpandedChunks { get; set; }

    public int GetTier() => chunks.Count switch
    {
        1 => 1,
        <= 3 => 2,
        _ => 3
    };
}
```

**NEW Model**:
```csharp
public class HolySiteData
{
    public List<SerializableCuboidi> Areas { get; set; }  // 3D areas from land claim

    public int GetTier()
    {
        int volume = GetTotalVolume();
        if (volume < 50000) return 1;
        if (volume < 200000) return 2;
        return 3;
    }

    public int GetTotalVolume() => Areas.Sum(area => area.GetVolume());
    public bool ContainsPosition(BlockPos pos) => Areas.Any(area => area.Contains(pos));
}
```

### 5. Manager Interface Changes

**Removed Methods**:
- `bool ExpandHolySite(string siteUID, SerializableChunkPos newChunk)`
- `HolySiteData? GetHolySiteAtChunk(SerializableChunkPos chunk)`

**Changed Signatures**:
```csharp
// OLD:
HolySiteData? ConsecrateHolySite(string religionUID, string siteName, SerializableChunkPos centerChunk, string founderUID);

// NEW:
HolySiteData? ConsecrateHolySite(string religionUID, string siteName, List<Cuboidi> claimAreas, string founderUID);
```

**New Methods**:
- `HolySiteData? GetHolySiteAtPosition(BlockPos pos)` - Position-based spatial query

### 6. Localization Changes

**Removed Keys**:
- `CMD_HOLYSITE_EXPAND_DESC`
- `HOLYSITE_EXPANDED`
- `HOLYSITE_CHUNK_OCCUPIED` (replaced with overlap detection)
- `HOLYSITE_MAX_SIZE` (no size limit now)

**Added Keys**:
- `HOLYSITE_NOT_CLAIMED` - "You must be standing in a land claim that you own"

**Updated Messages**:
- Consecration message now shows volume-based tier
- Info displays volume and area count
- List shows volume instead of chunk count

## Implementation Details

### SerializableCuboidi Class

New class to represent 3D rectangular areas (ProtoBuf-compatible):

```csharp
[ProtoContract]
public class SerializableCuboidi
{
    [ProtoMember(1-6)] public int X1, Y1, Z1, X2, Y2, Z2;

    public int GetVolume() => (sizeX * sizeY * sizeZ);
    public bool Contains(BlockPos pos);
    public bool Intersects(SerializableCuboidi other);
}
```

### Land Claim Validation

Consecration now validates:
1. Player is standing in a land claim
2. Player owns the land claim
3. Holy site doesn't overlap existing holy sites
4. Religion hasn't exceeded prestige-based site limit

### Multi-Area Support

When a land claim has multiple areas (e.g., main building + tower + farm), ALL areas become part of the holy site. The center is calculated as a weighted average based on area volumes.

## Migration Notes

### Backward Compatibility

**Breaking Change**: Existing holy sites using the old chunk-based system will not load correctly. A migration would need to:
1. Detect old `SerializableChunkPos` data
2. Convert chunks to area boundaries (each chunk becomes a 256x256 area)
3. Resave as `SerializableCuboidi` list

**However**, since this is a new feature being implemented, no migration is needed.

## Testing Coverage

**37 comprehensive tests** covering:
- SerializableCuboidi operations (volume, area, containment, intersection)
- HolySiteData calculations (tier, multipliers, center, overlap detection)
- Manager CRUD operations
- Spatial queries
- Prestige limits
- Cascading deletion
- Persistence (ProtoBuf roundtrip)

**Coverage**: >85% for HolySiteManager and HolySiteData

## User Experience Benefits

1. **Simpler Mental Model**: "Consecrate your land claim" vs. "Create site then expand chunk by chunk"
2. **Intuitive Boundaries**: Holy site matches exactly what you already claimed
3. **Multi-Area Support**: Complex builds with multiple claim areas work naturally
4. **No Arbitrary Limits**: Size limited by land claim system, not by chunk count
5. **Easier Planning**: Players can see holy site extent by checking land claim boundaries

## Performance Considerations

- Position queries are O(n) where n = number of sites (acceptable for expected <100 sites)
- Overlap detection is O(n*m) where m = areas per site (runs only at consecration)
- Future optimization: Add spatial indexing (quadtree) if performance becomes an issue

## Next Steps

1. ✅ Core implementation complete
2. ✅ Unit tests passing
3. ⏳ Network packets for client-server sync (Phase 3)
4. ⏳ ImGui UI for holy site management (Phase 3)
5. ⏳ Integration with territory bonuses (Phase 4)
6. ⏳ Integration with prayer system (Phase 4)
