# Prayer and Shrine Implementation Options

## The Design Challenge

Prayer bonus needs a trigger mechanism. We have several options, each with different complexity and gameplay implications.

## Option 1: Shrine Blocks (Recommended)

### Concept
Physical altar/shrine blocks that players can interact with. Each deity has unique shrine variants.

### Implementation

#### Create Shrine Blocks
**File:** `DivineAscension/assets/divineascension/blocktypes/shrine/*.json`

```json
{
  "code": "shrine-khoras",
  "class": "BlockShrine",
  "variantgroups": [
    { "code": "side", "states": ["north", "east", "south", "west"] }
  ],
  "behaviors": [
    { "name": "HorizontalOrientable" }
  ],
  "entityClass": "BlockEntityShrine",
  "attributes": {
    "deityType": "Khoras",
    "prayerCooldown": 3600
  },
  "creativeinventory": { "general": ["*"], "decorative": ["*"] },
  "maxStackSize": 64,
  "resistance": 6,
  "sidesolid": { "all": false },
  "sideopaque": { "all": false }
}
```

#### Shrine Block Entity
**File:** `DivineAscension/BlockEntities/BlockEntityShrine.cs`

```csharp
public class BlockEntityShrine : BlockEntity
{
    private DeityType _deityType;
    private Dictionary<string, double> _lastPrayerTimes = new();

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        // Get deity type from block attributes
        var deityName = Block.Attributes?["deityType"].AsString();
        Enum.TryParse(deityName, out _deityType);
    }

    public bool CanPray(IPlayer player, out string reason)
    {
        var playerUID = player.PlayerUID;
        var currentTime = Api.World.Calendar.TotalHours;

        // Check cooldown (1 hour = 3600 seconds)
        if (_lastPrayerTimes.TryGetValue(playerUID, out var lastTime))
        {
            var hoursSinceLastPrayer = currentTime - lastTime;
            if (hoursSinceLastPrayer < 1.0) // 1 hour cooldown
            {
                var minutesRemaining = (int)((1.0 - hoursSinceLastPrayer) * 60);
                reason = $"You must wait {minutesRemaining} more minutes before praying again.";
                return false;
            }
        }

        reason = "";
        return true;
    }

    public void OnPrayer(IServerPlayer player)
    {
        _lastPrayerTimes[player.PlayerUID] = Api.World.Calendar.TotalHours;
        MarkDirty(true);
    }

    public DeityType GetDeityType() => _deityType;
}
```

#### Shrine Block Behavior
**File:** `DivineAscension/Blocks/BlockShrine.cs`

```csharp
public class BlockShrine : Block
{
    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (world.Side == EnumAppSide.Server)
        {
            var be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityShrine;
            if (be != null)
            {
                if (be.CanPray(byPlayer, out string reason))
                {
                    // Trigger prayer through mod system
                    var modSystem = world.Api.ModLoader.GetModSystem<DivineAscensionModSystem>();
                    modSystem.OnPlayerPrayAtShrine(byPlayer as IServerPlayer, be.GetDeityType());

                    be.OnPrayer(byPlayer as IServerPlayer);

                    // Play prayer animation/sound
                    world.PlaySoundAt(new AssetLocation("divineascension:sounds/prayer"),
                        blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z);

                    return true;
                }
                else
                {
                    (byPlayer as IServerPlayer)?.SendMessage(
                        GlobalConstants.GeneralChatGroup,
                        reason,
                        EnumChatType.Notification
                    );
                }
            }
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }
}
```

#### Integration with ActivityBonusSystem
**File:** `DivineAscension/DivineAscensionModSystem.cs`

```csharp
public void OnPlayerPrayAtShrine(IServerPlayer player, DeityType shrineDeity)
{
    var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(player);

    // Check if player has correct deity
    if (!playerData.HasDeity())
    {
        player.SendMessage(
            GlobalConstants.GeneralChatGroup,
            "You must pledge to a deity before praying.",
            EnumChatType.Notification
        );
        return;
    }

    if (playerData.DeityType != shrineDeity)
    {
        player.SendMessage(
            GlobalConstants.GeneralChatGroup,
            $"This shrine is dedicated to {shrineDeity}, but you follow {playerData.DeityType}.",
            EnumChatType.Notification
        );
        return;
    }

    // Apply prayer bonus
    _activityBonusSystem?.ApplyPrayerBonus(player);
}
```

### Pros:
- ✅ Tangible, physical interaction
- ✅ Players can build shrines in bases
- ✅ Per-player cooldowns work naturally
- ✅ Can add visual effects/sounds
- ✅ Shrine placement becomes meaningful

### Cons:
- ⚠️ Requires asset creation (models, textures)
- ⚠️ More complex implementation
- ⚠️ Need shrine blocks for each deity

---

## Option 2: Land Claim Holy Sites (Your Idea)

### Concept
Players designate a land claim as a "holy site" for their religion. Praying within the claim grants bonuses.

### Implementation

#### Religion Land Claim Integration
**File:** `DivineAscension/Systems/HolySiteManager.cs`

> **Note:** This is a simplified example. See `land_claim_holy_site_integration.md` for the complete implementation with proper serialization, profanity filtering, localization, and religion deletion cascade.

```csharp
using DivineAscension.Data;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

public class HolySiteManager
{
    private readonly ICoreServerAPI _sapi;
    private readonly ReligionManager _religionManager;
    private readonly Dictionary<SerializableChunkPos, HolySiteData> _holySites = new();

    // See HolySiteData in land_claim_holy_site_integration.md for full data model
    // with SerializableChunkPos and proper ProtoBuf serialization

    public bool DesignateHolySite(IServerPlayer player, BlockPos pos)
    {
        var religion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (religion == null)
        {
            player.SendMessage(0, "You must be in a religion to designate holy sites.",
                EnumChatType.Notification);
            return false;
        }

        // Check if player is founder or has permission
        if (!religion.IsFounder(player.PlayerUID))
        {
            player.SendMessage(0, "Only the religion founder can designate holy sites.",
                EnumChatType.Notification);
            return false;
        }

        // Check land claim
        var landClaim = _sapi.World.Claims.Get(pos);
        if (landClaim == null || !landClaim.OwnedByPlayerUid.Contains(player.PlayerUID))
        {
            player.SendMessage(0, "You must own this land claim to designate it as holy.",
                EnumChatType.Notification);
            return false;
        }

        var chunkX = pos.X / GlobalConstants.ChunkSize;
        var chunkZ = pos.Z / GlobalConstants.ChunkSize;
        var chunkCoord = new SerializableChunkPos(chunkX, chunkZ);

        // Create holy site (simplified - see full implementation for complete code)
        _holySites[chunkCoord] = new HolySiteData(
            Guid.NewGuid().ToString(),
            chunkCoord,
            religion.ReligionUID,
            "Holy Site",  // Name would come from command argument
            player.PlayerUID
        );

        player.SendMessage(0, $"This land is now consecrated to {religion.ReligionName}!",
            EnumChatType.Notification);
        return true;
    }

    public bool IsPlayerInHolySite(IServerPlayer player, out ReligionData? religion)
    {
        var pos = player.Entity.ServerPos.AsBlockPos;
        var chunkX = pos.X / GlobalConstants.ChunkSize;
        var chunkZ = pos.Z / GlobalConstants.ChunkSize;
        var chunkCoord = new SerializableChunkPos(chunkX, chunkZ);

        if (_holySites.TryGetValue(chunkCoord, out var holySite))
        {
            religion = _religionManager.GetReligion(holySite.ReligionUID);
            return religion != null;
        }

        religion = null;
        return false;
    }

    public bool CanPrayAtHolySite(IServerPlayer player, out string reason)
    {
        if (!IsPlayerInHolySite(player, out var siteReligion))
        {
            reason = "You must be in a holy site to pray.";
            return false;
        }

        var playerReligion = _religionManager.GetPlayerReligion(player.PlayerUID);
        if (playerReligion == null || playerReligion.ReligionUID != siteReligion?.ReligionUID)
        {
            reason = $"This holy site belongs to {siteReligion?.ReligionName ?? "another religion"}.";
            return false;
        }

        reason = "";
        return true;
    }
}
```

#### Prayer Command for Holy Sites
```csharp
[Command("pray", "Pray at a holy site for divine favor")]
private void OnPrayCommand(IServerPlayer player, int groupId, CmdArgs args)
{
    if (_holySiteManager.CanPrayAtHolySite(player, out string reason))
    {
        // Check cooldown (stored per player)
        if (_prayerCooldowns.TryGetValue(player.PlayerUID, out var lastPrayer))
        {
            var hoursSince = _sapi.World.Calendar.TotalHours - lastPrayer;
            if (hoursSince < 1.0)
            {
                var minsRemaining = (int)((1.0 - hoursSince) * 60);
                player.SendMessage(groupId,
                    $"You must wait {minsRemaining} more minutes.",
                    EnumChatType.CommandError);
                return;
            }
        }

        _activityBonusSystem.ApplyPrayerBonus(player);
        _prayerCooldowns[player.PlayerUID] = _sapi.World.Calendar.TotalHours;

        player.SendMessage(groupId,
            "You kneel and pray, feeling your deity's favor strengthen.",
            EnumChatType.Notification);
    }
    else
    {
        player.SendMessage(groupId, reason, EnumChatType.CommandError);
    }
}
```

### Pros:
- ✅ Leverages existing land claim system
- ✅ No new assets needed
- ✅ Creates "temple" areas naturally
- ✅ Religion-wide holy sites
- ✅ Encourages base building

### Cons:
- ⚠️ Requires land claim ownership
- ⚠️ Less visual feedback
- ⚠️ Command-based (less immersive)
- ⚠️ Holy site management complexity

---

## Option 3: Hybrid Approach (Best of Both)

### Concept
Combine shrine blocks with land claim bonuses. Shrines are portable prayer points, while holy sites provide additional benefits.

### Implementation

1. **Shrine Blocks** - Can be placed anywhere for basic prayer
2. **Holy Site Designation** - Land claims can be designated as holy
3. **Stacking Bonuses**:
   - Prayer at shrine: 2x favor for 10 minutes
   - Prayer at shrine IN holy site: 2.5x favor for 15 minutes
   - Sacred territory bonus: 1.5x while in holy site (passive, always active)

```csharp
public void ApplyPrayerBonus(IServerPlayer player, bool inHolySite = false)
{
    var multiplier = inHolySite ? 2.5f : 2.0f;
    var durationMinutes = inHolySite ? 15.0 : 10.0;

    double expiryTime = _sapi.World.Calendar.TotalHours + (durationMinutes / 60.0);

    player.Entity.Stats.Set(
        "passiveFavorMultiplier",
        "prayer_bonus",
        multiplier,
        expiryTime
    );

    string message = inHolySite
        ? "The sacred ground amplifies your prayers! 2.5x favor for 15 minutes!"
        : "Your devotion is rewarded! 2x favor for 10 minutes.";

    player.SendMessage(
        GlobalConstants.GeneralChatGroup,
        message,
        EnumChatType.Notification
    );
}
```

### Pros:
- ✅ Flexible for different playstyles
- ✅ Visual interaction (shrines) + strategic placement (holy sites)
- ✅ Rewards base building without requiring it
- ✅ Scales with progression

### Cons:
- ⚠️ Most complex to implement
- ⚠️ Two systems to maintain

---

## Option 4: Simple Command (MVP)

### Concept
Temporary `/deity pray` command for initial testing, replace later with proper system.

```csharp
[Command("deity pray", "Pray to your deity for favor")]
private void OnPrayCommand(IServerPlayer player, int groupId, CmdArgs args)
{
    var playerData = _playerProgressionDataManager.GetOrCreatePlayerData(player);

    if (!playerData.HasDeity())
    {
        player.SendMessage(groupId, "You have no deity to pray to.", EnumChatType.CommandError);
        return;
    }

    // Check cooldown
    if (_prayerCooldowns.TryGetValue(player.PlayerUID, out var lastPrayer))
    {
        var hoursSince = _sapi.World.Calendar.TotalHours - lastPrayer;
        if (hoursSince < 1.0)
        {
            var minsRemaining = (int)((1.0 - hoursSince) * 60);
            player.SendMessage(groupId, $"You must wait {minsRemaining} more minutes.",
                EnumChatType.CommandError);
            return;
        }
    }

    _activityBonusSystem.ApplyPrayerBonus(player);
    _prayerCooldowns[player.PlayerUID] = _sapi.World.Calendar.TotalHours;
}
```

### Pros:
- ✅ Very quick to implement
- ✅ Good for testing Phase 3
- ✅ No assets needed

### Cons:
- ⚠️ Not immersive
- ⚠️ No visual feedback
- ⚠️ Temporary solution

---

## Sacred Territory Integration

All options can integrate with sacred territory detection:

### With Shrines (Option 1)
```csharp
private bool CheckIfInSacredTerritory(IServerPlayer player)
{
    var pos = player.Entity.ServerPos.AsBlockPos;

    // Search for nearby shrines (10 block radius)
    var nearbyBlocks = _sapi.World.BlockAccessor.SearchBlocks(
        pos.AddCopy(-10, -5, -10),
        pos.AddCopy(10, 5, 10),
        (block, blockPos) => block is BlockShrine
    );

    return nearbyBlocks.Length > 0;
}
```

### With Holy Sites (Option 2)
```csharp
private bool CheckIfInSacredTerritory(IServerPlayer player)
{
    return _holySiteManager.IsPlayerInHolySite(player, out _);
}
```

### With Hybrid (Option 3)
```csharp
private bool CheckIfInSacredTerritory(IServerPlayer player)
{
    // Check both shrines AND holy sites
    bool nearShrine = CheckNearbyShrine(player);
    bool inHolySite = _holySiteManager.IsPlayerInHolySite(player, out _);

    return nearShrine || inHolySite;
}
```

---

## Recommendation

**For MVP / Phase 3 Initial Implementation:**
Start with **Option 4 (Simple Command)** to test Phase 3 mechanics quickly.

**For Full Release:**
Implement **Option 3 (Hybrid Approach)** for best gameplay:
- Shrine blocks provide immediate interaction
- Holy sites create meaningful religion territories
- Stacking bonuses reward strategic planning
- Sacred territory bonus works naturally with both

**Implementation Order:**
1. **Phase 3.1**: Simple `/deity pray` command (test mechanics)
2. **Phase 3.2**: Add shrine blocks (visual interaction)
3. **Phase 3.3**: Add holy site system (strategic depth)
4. **Phase 3.4**: Integrate with land claims and sacred territory

---

## Your Land Claim Idea - Deep Dive

I really like your land claim idea! Here's how we could make it work:

### Religion Temple System

```csharp
// Religion founder can designate a land claim as temple
/religion temple create <name>

// Creates a "temple" that provides:
// 1. Sacred territory bonus (1.5x passive favor)
// 2. Prayer location (2.5x for 15 min when praying)
// 3. Religion spawn point (optional)
// 4. Prestige generation bonus (Phase 4)

// Temples could have tiers:
// - Tier 1: Basic temple (1 land claim)
// - Tier 2: Large temple (3x3 land claims)
// - Tier 3: Grand cathedral (5x5 land claims)

// Higher tiers = better bonuses
```

### Implementation Benefits:
- Encourages collaborative building
- Creates server landmarks
- Religion PvP conflict zones
- Prestige cost to create (prestige sink)
- Can be upgraded over time

This could be a whole system on its own!

---

## Next Steps - Your Choice

Which approach would you like me to implement?

1. **Start simple** - Command-based prayer for Phase 3 testing
2. **Go full shrine blocks** - Physical interaction system
3. **Implement your land claim idea** - Holy site/temple system
4. **Hybrid approach** - Plan for all three

What sounds most interesting to you?
