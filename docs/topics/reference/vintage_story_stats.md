# Vintage Story Player Stats Reference

This document lists the stats that impact the player's experience in Vintage Story. These stats are registered in the `EntityPlayer` class and are used throughout the game to modify various behaviors and mechanics.

## List of Stats

The following stats are available for modification via `EntityStats`:

### Health & Survival
* `healingeffectivness`
* `maxhealthExtraPoints`
* `hungerrate`

### Movement
* `walkspeed`
* `jumpHeightMul`
* `gliderLiftMax`
* `gliderSpeedMax`

### Combat
* `rangedWeaponsAcc`
* `rangedWeaponsSpeed`
* `rangedWeaponsDamage`
* `meleeWeaponsDamage`
* `mechanicalsDamage`
* `bowDrawingStrength`
* `armorDurabilityLoss`
* `armorWalkSpeedAffectedness`

### Gathering & Loot
* `miningSpeedMul`
* `animalLootDropRate`
* `forageDropRate`
* `wildCropDropRate`
* `vesselContentsDropRate`
* `oreDropRate`
* `rustyGearDropRate`
* `wholeVesselLootChance` (FlatSum)
* `temporalGearTLRepairCost` (FlatSum)
* `animalHarvestingTime`

### Other
* `animalSeekingRange`

## Usage

These stats can be accessed and modified using the `Entity.Stats` API.
Example:
```csharp
// Apply a modifier to an existing stat
player.Entity.Stats.Set("walkspeed", "modname", 0.1f, true);
```

## Creating Custom Stats

You can register and use your own custom stats to track arbitrary modifiers for your mod's mechanics.

### Registration

While you can simply call `Set` to implicitly create a stat (defaults to `WeightedSum`), it is best practice to explicitly register your stat, especially if you need a different blending mode or want to document its existence.

Registration is typically done when the entity is initialized (e.g., in an `EntityBehavior` or when the player joins).

```csharp
// Explicit registration
player.Entity.Stats.Register("myCustomStat", EnumStatBlendType.FlatSum);
```

**Blend Types:**
* `WeightedSum` (Default): `(1 + sum(modifiers)) * multipliers` (Note: simplified; check `EntityStats` logic). Generally used for percentage bonuses where 0.1 means +10%.
* `FlatSum`: `base + sum(modifiers)`. Useful for direct value additions (e.g., +5 extra HP).
* `FlatMultiply`: `base * product(modifiers)`.
* `WeightedOverlay`: Used for overriding values.

### Interaction

#### Setting a Value
To apply a modifier/value to a stat:
```csharp
// Set a modifier associated with a specific source key ("myModBuff")
// boolean flag 'true' indicates it is persistent (saved to disk)
player.Entity.Stats.Set("myCustomStat", "myModBuff", 5.0f, true);
```

#### Reading a Value
To get the final calculated value:
```csharp
float currentValue = player.Entity.Stats.GetBlended("myCustomStat");
```

### Example Implementation

```csharp
// Register
entity.Stats.Register("magicPower", EnumStatBlendType.WeightedSum);

// Buff
entity.Stats.Set("magicPower", "potion", 0.5f); // +50% magic power

// Check
float power = entity.Stats.GetBlended("magicPower"); // Returns 1.5 (if base is 1)
```
