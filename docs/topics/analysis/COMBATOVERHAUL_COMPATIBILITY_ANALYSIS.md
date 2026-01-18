# Combat Overhaul & Overhaullib Systems Analysis

**Date:** 2026-01-17
**Purpose:** Comprehensive documentation of CombatOverhaul and Overhaullib systems for Divine Ascension integration
**Related Mods:** CombatOverhaul, Overhaullib

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Stats System](#1-stats-system)
3. [Armor System](#2-armor-system)
4. [Weapon Types](#3-weapon-types)
5. [Damage Calculation](#4-damage-calculation)
6. [Integration Guide](#5-integration-guide)
7. [File References](#6-file-references)

---

## Executive Summary

CombatOverhaul completely replaces Vintage Story's damage calculation system. While **melee damage modifiers are compatible** (CombatOverhaul reads `meleeWeaponsDamage` stat), **ranged damage modifiers are NOT compatible** because CombatOverhaul's projectile system does not read the `rangedWeaponsDamage` stat.

**Key Findings:**
- Melee damage uses `meleeWeaponsDamage` stat multiplier (compatible)
- Ranged damage uses tier bonus system only, not `rangedWeaponsDamage`
- Armor uses a tier-based lookup table (24×9 matrix)
- Body zone damage multipliers range from 0.5x (limbs) to 2.0x (head/neck)
- 13 weapon proficiencies affect attack/reload speeds

---

## 1. Stats System

### 1.1 Core Stats Framework (vsmod_Overhaullib)

**Location:** `/home/quantumheart/RiderProjects/vsmod_Overhaullib/source/Framework/StatsSystem.cs`

The framework uses Vintage Story's built-in `Entity.Stats` system with network synchronization:

```csharp
public sealed class StatsPacket
{
    public string Stat { get; set; } = "";
    public string Category { get; set; } = "";
    public float Value { get; set; } = 0;
}
```

- **Channel ID:** `"CombatOverhaul:stats"`
- **Category Used:** `"CombatOverhaul:Armor"` for armor stat modifiers

### 1.2 Player Stats

#### Movement & Action Stats

| Stat Name | Range | Description |
|-----------|-------|-------------|
| `walkspeed` | -0.29 to 0 | Movement speed multiplier (negative = slower) |
| `manipulationSpeed` | -0.4 to 0 | Item interaction/crafting speed |
| `steadyAim` | -0.4 to 0 | Ranged weapon aiming stability |
| `healingeffectivness` | -0.18 to 0 | Healing item effectiveness |
| `hungerrate` | 0.02 to 0.21 | Food consumption rate (positive = faster hunger) |

#### Combat Damage Stats

| Stat Name | Description |
|-----------|-------------|
| `meleeWeaponsDamage` | Multiplier for all melee weapon damage |
| `mechanicalsDamage` | Additional multiplier vs mechanical entities |
| `meleeDamageTierBonusSlashingAttack` | Armor piercing tier bonus for slashing |
| `meleeDamageTierBonusPiercingAttack` | Armor piercing tier bonus for piercing |
| `meleeDamageTierBonusBluntAttack` | Armor piercing tier bonus for blunt |
| `rangedDamageTierBonusSlashingAttack` | Ranged armor piercing for slashing |
| `rangedDamageTierBonusPiercingAttack` | Ranged armor piercing for piercing |
| `rangedDamageTierBonusBluntAttack` | Ranged armor piercing for blunt |

#### Body Zone Damage Factors

| Stat Name | Default Value | Description |
|-----------|---------------|-------------|
| `playerHeadDamageFactor` | 2.0x | Damage multiplier to head |
| `playerFaceDamageFactor` | 1.5x | Damage multiplier to face |
| `playerNeckDamageFactor` | 2.0x | Damage multiplier to neck |
| `playerTorsoDamageFactor` | 1.0x | Damage multiplier to torso (baseline) |
| `playerArmsDamageFactor` | 0.5x | Damage multiplier to arms |
| `playerLegsDamageFactor` | 0.5x | Damage multiplier to legs |
| `playerHandsDamageFactor` | 0.5x | Damage multiplier to hands |
| `playerFeetDamageFactor` | 0.5x | Damage multiplier to feet |

#### Armor Penalty Reduction Stats

| Stat Name | Description |
|-----------|-------------|
| `armorWalkSpeedAffectedness` | Reduces armor walk speed penalty (e.g., -0.5 = 50% reduction) |
| `armorManipulationSpeedAffectedness` | Reduces armor manipulation speed penalty |
| `armorHungerRateAffectedness` | Reduces armor hunger rate penalty |

#### Second Chance Mechanic Stats

| Stat Name | Default | Description |
|-----------|---------|-------------|
| `secondChanceCooldown` | 300s | Cooldown between second chance triggers |
| `secondChanceGracePeriod` | 8s | No-damage period after second chance |

### 1.3 Weapon Proficiencies

**Location:** CombatOverhaul traits system (`configlib-patches.json`)

| Proficiency | Effect | Value |
|-------------|--------|-------|
| `bowsProficiency` | Draw speed | +50% at 0.5 |
| `crossbowsProficiency` | Reload speed | +50% at 0.5 |
| `firearmsProficiency` | Reload speed | +50% at 0.5 |
| `oneHandedSwordsProficiency` | Attack speed | +30% at 0.3 |
| `twoHandedSwordsProficiency` | Attack speed | +30% at 0.3 |
| `spearsProficiency` | Attack speed | +30% at 0.3 |
| `javelinsProficiency` | Attack speed | +30% at 0.3 |
| `macesProficiency` | Attack speed | +30% at 0.3 |
| `clubsProficiency` | Attack speed | +30% at 0.3 |
| `halberdsProficiency` | Attack speed | +30% at 0.3 |
| `axesProficiency` | Attack speed | +30% at 0.3 |
| `quarterstaffProficiency` | Attack speed | +30% at 0.3 |
| `slingsProficiency` | Reload speed + damage | +30% at 0.3 |

---

## 2. Armor System

### 2.1 Armor Layer System

**Location:** `/home/quantumheart/RiderProjects/vsmod_Overhaullib/source/Framework/ArmorSystems/ArmorTypes.cs`

```csharp
[Flags]
public enum ArmorLayers
{
    None = 0,
    Skin = 1,      // Underwear/Light layer (jerkins, linen)
    Middle = 2,    // Mid-layer (chain, lamellar)
    Outer = 4      // Full plate/heavy armor (plate, brigandine)
}
```

Armor pieces can occupy one or more layers. Stacking is allowed when layers don't conflict.

### 2.2 Damage Zones

```csharp
[Flags]
public enum DamageZone
{
    None = 0,
    Head = 1,
    Face = 2,
    Neck = 4,
    Torso = 8,
    Arms = 16,
    Hands = 32,
    Legs = 64,
    Feet = 128
}
```

### 2.3 Player Body Parts (More Detailed)

```csharp
[Flags]
public enum PlayerBodyPart
{
    None = 0,
    Head = 1,
    Face = 2,
    Neck = 4,
    Torso = 8,
    LeftArm = 16,
    RightArm = 32,
    LeftHand = 64,
    RightHand = 128,
    LeftLeg = 256,
    RightLeg = 512,
    LeftFoot = 1024,
    RightFoot = 2048
}
```

### 2.4 Armor Stats Structure

```csharp
public sealed class ArmorStatsJson
{
    public string[] Layers { get; set; }        // Which layers this armor occupies
    public string[] Zones { get; set; }         // Which body zones it protects
    public Dictionary<string, float> Resists { get; set; }           // % damage reduction
    public Dictionary<string, float> FlatReduction { get; set; }     // Absolute reduction
    public Dictionary<string, float> PlayerStats { get; set; }       // Stat modifiers
    public Dictionary<string, Dictionary<string, float>> ResistsByZone { get; set; }  // Zone-specific
}
```

### 2.5 Damage Types

| Damage Type | Description | Effective Against |
|-------------|-------------|-------------------|
| `PiercingAttack` | Arrows, thrusts, stabs | Chain mail (weak to piercing) |
| `SlashingAttack` | Sword cuts, axes | Brigandine (weak to slashing) |
| `BluntAttack` | Hammers, clubs, impacts | Jerkins, linen (weak to blunt) |

### 2.6 Damage Reduction Calculation

**Location:** `/home/quantumheart/RiderProjects/vsmod_Overhaullib/source/Framework/DamageSystems/DamageTypes.cs`

The system uses a tier-based lookup table:

- **Max Attack Tier:** 9
- **Max Armor Tier:** 24
- **Lookup Table:** 24×9 matrix of damage multipliers

```
Attack Tier 1 vs Armor Tier 1 = 0.75x damage
Attack Tier 5 vs Armor Tier 10 = 0.35x damage
Minimum: 0.01x (overwhelming armor)
Maximum: 1.0x (no protection)
```

**Armor Piercing:** Added to attack tier before lookup, bypassing protection tier-for-tier.

**Non-Player Flat Multiplier Formula:**
```csharp
if (attackTier >= protection) return 1;
else if (attackTier < protection - 1) return 0.5f;
else return 0.75f;
```

### 2.7 Armor Types by Construction

| Type | Layers | Materials | Piercing | Slashing | Blunt | Notes |
|------|--------|-----------|----------|----------|-------|-------|
| Improvised | All | Wood | 0-1 | 1 | 1 | Weakest |
| Jerkin | Skin | Leather/Hide | 0 | 0 | 4 | Light, minimal penalties |
| Sewn | Outer+Middle or Skin | Leather/Linen | 1-2 | 1-3 | 1-5 | Light-medium |
| Tailored | Skin | Linen | 2 | 2 | 6 | No walkspeed penalty |
| Chain | Middle | Metals | 0-3 | 2-5 | 0 | Weak to piercing/blunt |
| Lamellar | All | Metals | 3-6 | 3-6 | 3-6 | Balanced protection |
| Scale | Outer+Middle | Metals | 3-6 | 3-6 | 0-2 | Weak to blunt |
| Brigandine | Outer | Metals | 4-7 | 4-7 | 1-7 | Heavy penalties |
| Plate | Outer | Metals | 6-9 | 4-7 | 2-8 | Highest protection |
| Antique | Outer | Legendary | 6-10 | 6-11 | 3-7 | Best stats, severe penalties |

### 2.8 Metal Tier Progression

| Material | Piercing | Slashing | Blunt | Durability Multiplier |
|----------|----------|----------|-------|----------------------|
| Copper | 3-4 | 3-4 | 0-2 | 1x |
| Bronze | 4-5 | 4-5 | 0-4 | 2x |
| Iron | 5-6 | 5-6 | 0-6 | 4x |
| Steel | 6-9 | 5-7 | 2-8 | 8x |
| Antique | 6-10 | 6-11 | 3-7 | Variable |

### 2.9 Armor Stat Penalties (Full Steel Plate Example)

| Stat | Penalty |
|------|---------|
| Walking | -0.20 (-20%) |
| Manipulation | -0.20 (-20%) |
| Steady Aim | -0.30 (-30%) |
| Healing | -0.09 (-9%) |
| Hunger Rate | +0.12 (+12%) |

---

## 3. Weapon Types

### 3.1 Melee Weapon Framework

**Location:** `/home/quantumheart/RiderProjects/vsmod_Overhaullib/source/Implementations/MeleeWeapon/Stats.cs`

#### Stance System

```csharp
public class StanceStats
{
    public bool CanAttack { get; set; }
    public bool CanParry { get; set; }
    public bool CanBlock { get; set; }
    public bool CanThrow { get; set; }
    public bool CanBash { get; set; }
    public bool CanRiposte { get; set; }

    public float SpeedPenalty { get; set; }
    public float BlockSpeedPenalty { get; set; }

    public float GripLengthFactor { get; set; }  // For adjustable-length weapons
    public float GripMinLength { get; set; }
    public float GripMaxLength { get; set; }

    public MeleeAttackStats? Attack { get; set; }
    public MeleeAttackStats? Riposte { get; set; }
    public MeleeAttackStats? BlockBash { get; set; }
    public Dictionary<string, MeleeAttackStats>? DirectionalAttacks { get; set; }
    public DamageBlockJson? Block { get; set; }
    public DamageBlockJson? Parry { get; set; }
}
```

#### Weapon Configuration

```csharp
public class MeleeWeaponStats : WeaponStats
{
    public StanceStats? OneHandedStance { get; set; }
    public StanceStats? TwoHandedStance { get; set; }
    public StanceStats? OffHandStance { get; set; }
    public Dictionary<string, StanceStats> MainHandDualWieldStances { get; set; }
    public Dictionary<string, StanceStats> OffHandDualWieldStances { get; set; }
    public ThrowWeaponStats? ThrowAttack { get; set; }
}
```

#### Per-Itemstack Modifiers

```csharp
public readonly struct ItemStackMeleeWeaponStats
{
    public readonly float DamageMultiplier;       // Quality/enchantment bonus
    public readonly float DamageBonus;            // Flat damage addition
    public readonly int DamageTierBonus;          // Armor piercing bonus
    public readonly float AttackSpeed;            // Speed modifier
    public readonly int BlockTierBonus;           // Block defense bonus
    public readonly int ParryTierBonus;           // Parry defense bonus
    public readonly float ThrownDamageMultiplier;
    public readonly int ThrownDamageTierBonus;
    public readonly float KnockbackMultiplier;
    public readonly int ArmorPiercingBonus;
}
```

### 3.2 Weapon Categories

#### Swords (Blades)

**File:** `CombatOverhaul/resources/assets/combatoverhaul/patches/weapons/blade.json`

| Variant | Damage Type | Special |
|---------|-------------|---------|
| Regular | Slashing + Piercing thrust | One/two-handed stances |
| Falx | Slashing | Unique attack patterns |
| Blackguard | Slashing + Piercing | Antique, high tier |
| Forlorn | Slashing + Piercing | Antique variant |

**Proficiency:** `oneHandedSwordsProficiency` / `twoHandedSwordsProficiency` → +30% attack speed

#### Clubs

**File:** `CombatOverhaul/resources/assets/combatoverhaul/patches/weapons/club.json`

| Characteristic | Value |
|----------------|-------|
| Damage Type | 100% Blunt |
| Range | Short |
| Special | High stagger potential |

**Proficiency:** `clubsProficiency` → +30% attack speed

#### Spears

**File:** `CombatOverhaul/resources/assets/combatoverhaul/patches/weapons/spear.json`

| Characteristic | Value |
|----------------|-------|
| Melee Damage | Piercing |
| Thrown Damage | Piercing |
| Range | Long (polearm) |
| Materials | Iron, Steel, Meteoric Iron |

**Settings:**
- `spears_aiming_difficulty` - Thrown aiming sensitivity
- `spears_thrown_distance` - Air drag/gravity multiplier
- `spears_range_damage_multiplier` - Thrown damage multiplier
- `spears_directional_attacks` - TopBottom attack toggle

**Proficiency:** `spearsProficiency` → +30% attack speed

#### Shields

**File:** `CombatOverhaul/resources/assets/combatoverhaul/patches/weapons/shield.json`

| Type | Parry Tier | Block Tier | Block Speed | Special |
|------|------------|------------|-------------|---------|
| Crude (Wood) | 4 | 2 | 0 | Basic parry/block |
| WoodMetal | 5 | 4 | -0.05 | Shield bash |
| Metal | 7 | 6 | -0.10 | Strong defense |
| Blackguard | 7 | 5 | 0 | No penalty, high stagger |

**Block Damage Formula:**
```csharp
damage *= 1 - MathF.Exp((blockTier - damageTier) / 2f);
```

#### Bows

**File:** `CombatOverhaul/resources/assets/combatoverhaul/patches/weapons/bow.json`

```csharp
public sealed class BowStats : WeaponStats
{
    public AimingStatsJson Aiming { get; set; }
    public float ArrowDamageMultiplier { get; set; } = 1;
    public int ArrowDamageTier { get; set; } = 1;
    public float ArrowVelocity { get; set; } = 1;
    public float Zeroing { get; set; } = 1.5f;
    public float[] DispersionMOA { get; set; } = [0, 0];  // Accuracy
    public bool TwoHanded { get; set; } = true;
}
```

**Bow States:** Unloaded → Load → PreLoaded → Loaded → Draw → Drawn

**Settings:**
- `bow_aiming_difficulty` - Aiming sensitivity
- `bow_fov_multiplier` / `bow_fov_effect` - Zoom on draw
- `bow_screenshake` - Screen shake on release

**Proficiency:** `bowsProficiency` → +50% draw speed

#### Slings

**File:** `CombatOverhaul/resources/assets/combatoverhaul/patches/weapons/sling.json`

| Characteristic | Value |
|----------------|-------|
| Ammunition | Sling bullets (crafted) |
| Damage Type | Blunt (projectile) |

**Proficiency:** `slingsProficiency` → +30% reload speed + 30% damage

### 3.3 Attack Properties

```csharp
public class MeleeAttackStats
{
    public bool StopOnTerrainHit { get; set; } = false;
    public bool StopOnEntityHit { get; set; } = false;
    public bool CollideWithTerrain { get; set; } = true;
    public bool HitOnlyOneEntity { get; set; } = false;
    public float MaxReach { get; set; } = 6;
    public MeleeDamageTypeJson[] DamageTypes { get; set; }
}

public class MeleeDamageTypeJson
{
    public DamageDataJson Damage { get; set; };     // Type + amount
    public float Knockback { get; set; } = 0;       // Push force
    public int DurabilityDamage { get; set; } = 1;  // Weapon wear
    public float[] Collider { get; set; };          // Hit detection (6 floats)
    public float Radius { get; set; } = 0.1f;
    public int StaggerTimeMs { get; set; } = 0;     // Stun duration
    public int StaggerTier { get; set; } = 1;       // Stun resistance check
    public int PushTier { get; set; } = 0;
}
```

### 3.4 Entity Collider Types

**Location:** `/home/quantumheart/RiderProjects/vsmod_Overhaullib/source/Colliders/CollidersEntityBehavior.cs`

```csharp
public enum ColliderTypes
{
    Torso,        // Normal damage (1.0x)
    Head,         // High damage (1.25x typical)
    Arm,          // Low damage (0.5x typical)
    Leg,          // Low damage (0.5x typical)
    Critical,     // Very high damage (2.0x typical)
    Resistant     // No damage (0.0x, immune)
}
```

---

## 4. Damage Calculation

### 4.1 Melee Damage (COMPATIBLE)

**Source:** `MeleeAttackDamageType.cs:189`

```csharp
float damage = Damage * attacker.Stats.GetBlended("meleeWeaponsDamage");
if (target.Properties.Attributes?["isMechanical"].AsBool() == true)
{
    damage *= attacker.Stats.GetBlended("mechanicalsDamage");
}
damage += stats.DamageBonus;
```

**Formula:**
```
FinalDamage = (BaseDamage × meleeWeaponsDamage × [mechanicalsDamage if applicable]) + DamageBonus
```

**Divine Ascension Compatibility:** ✅ **COMPATIBLE**

### 4.2 Ranged/Projectile Damage (NOT COMPATIBLE)

**Source:** `Projectile.cs:81, 99-108`

```csharp
public const string DamageTierPlayerStatPrefix = "rangedDamageTierBonus";

// In Attack() method:
string damageTierStat = DamageTierPlayerStatPrefix + _stats.DamageStats.DamageType.ToString();
float statValue = attacker.Stats.GetBlended(damageTierStat) - 1;

float damage = _stats.DamageStats.Damage * _spawnStats.DamageMultiplier;
int damageTierBonus = _stats.DamageTierBonus + (int)statValue;
```

**Formula:**
```
FinalDamage = BaseDamage × SpawnDamageMultiplier
EffectiveTier = BaseTier + DamageTierBonus + rangedDamageTierBonus{DamageType}
```

**Divine Ascension Compatibility:** ❌ **NOT COMPATIBLE**
- `rangedWeaponsDamage` stat is **never read**
- Only `rangedDamageTierBonus{DamageType}` affects damage (via tier, not multiplier)

### 4.3 Block/Parry System

**Location:** `/home/quantumheart/RiderProjects/vsmod_Overhaullib/source/Framework/DamageSystems/PlayerDamageModel.cs`

```csharp
public sealed class DamageBlockStats
{
    public readonly PlayerBodyPart ZoneType;           // Zones that can block
    public readonly DirectionConstrain Directions;    // Block cone angle
    public readonly Dictionary<EnumDamageType, float>? BlockTier;
    public readonly bool CanBlockProjectiles;
    public readonly TimeSpan StaggerTime;              // Counter-attack window
    public readonly int StaggerTier;
}
```

**Block Resolution:**
1. Zone Check - Blocked zone must match attack zone
2. Direction Check - Attack within block cone
3. Projectile Check - If applicable
4. Tier Comparison - Block tier vs attack tier
5. Damage Reduction - Exponential formula based on tier difference

### 4.4 Second Chance Mechanic

**Location:** `PlayerDamageModel.cs:465-493`

- Triggers when fatal damage received
- Cooldown: 300 seconds default (configurable)
- Grace period: 8 seconds default (no damage during this time)
- Reduces player health to 1 on activation

---

## 5. Integration Guide

### 5.1 Stat Compatibility Matrix

#### Stats Read by CombatOverhaul

| Stat Name | Used For | Read Location | Divine Ascension Uses |
|-----------|----------|---------------|----------------------|
| `meleeWeaponsDamage` | Melee damage multiplier | `MeleeAttackDamageType.cs:189` | ✅ Yes |
| `mechanicalsDamage` | Damage vs mechanical entities | `MeleeAttackDamageType.cs:192` | ❌ No |
| `meleeDamageTierBonusSlashingAttack` | Melee tier bonus | Traits system | ❌ No |
| `meleeDamageTierBonusPiercingAttack` | Melee tier bonus | Traits system | ❌ No |
| `meleeDamageTierBonusBluntAttack` | Melee tier bonus | Traits system | ❌ No |
| `rangedDamageTierBonusPiercingAttack` | Ranged tier bonus | `Projectile.cs:99` | ✅ Yes |
| `rangedDamageTierBonusSlashingAttack` | Ranged tier bonus | `Projectile.cs:99` | ❌ No |
| `rangedDamageTierBonusBluntAttack` | Ranged tier bonus | `Projectile.cs:99` | ❌ No |

#### Stats NOT Read by CombatOverhaul

| Stat Name | Vanilla VS Purpose | Divine Ascension Uses |
|-----------|-------------------|----------------------|
| `rangedWeaponsDamage` | Ranged damage multiplier | ✅ Wild: Silent Death |
| `rangedWeaponsAcc` | Ranged accuracy | ✅ Wild: Silent Death |
| `rangedWeaponsRange` | Ranged distance | ✅ Wild: Avatar of the Wild |

### 5.2 Recommended Divine Ascension Stats

Based on CombatOverhaul's stat system, these stats are available for blessing effects:

```csharp
// Combat Stats (Melee)
"meleeWeaponsDamage"                    // % melee damage bonus
"mechanicalsDamage"                     // % bonus vs mechanical
"meleeDamageTierBonusSlashingAttack"   // +tier for slashing
"meleeDamageTierBonusPiercingAttack"   // +tier for piercing
"meleeDamageTierBonusBluntAttack"      // +tier for blunt

// Combat Stats (Ranged - tier only)
"rangedDamageTierBonusSlashingAttack"  // +tier for slashing
"rangedDamageTierBonusPiercingAttack"  // +tier for piercing
"rangedDamageTierBonusBluntAttack"     // +tier for blunt

// Defense Stats
"armorWalkSpeedAffectedness"           // Reduce armor penalties
"armorManipulationSpeedAffectedness"
"armorHungerRateAffectedness"

// Body Zone Damage Factors
"playerHeadDamageFactor"
"playerTorsoDamageFactor"
"playerArmsDamageFactor"
"playerLegsDamageFactor"

// Proficiencies
"bowsProficiency"
"crossbowsProficiency"
"oneHandedSwordsProficiency"
"twoHandedSwordsProficiency"
"spearsProficiency"
"axesProficiency"
"macesProficiency"
"clubsProficiency"
"halberdsProficiency"
"quarterstaffProficiency"
"javelinsProficiency"
"slingsProficiency"

// Movement/Utility
"walkspeed"
"manipulationSpeed"
"steadyAim"
"healingeffectivness"
```

### 5.3 Domain-Appropriate Stat Bonuses

| Domain | Thematic Stats |
|--------|----------------|
| **Conquest** | `meleeWeaponsDamage`, `meleeDamageTierBonus*`, proficiencies (swords, axes, maces) |
| **Wild** | `bowsProficiency`, `spearsProficiency`, `steadyAim`, body zone damage reduction |
| **Craft** | `manipulationSpeed`, `armorWalkSpeedAffectedness`, crossbow/firearm proficiency |
| **Harvest** | `hungerrate` (reduction), `healingeffectivness`, `walkspeed` |
| **Stone** | Body damage factors (reduction), blunt resistance, armor penalty reduction |

### 5.4 Blessing Effect Examples

```csharp
// Conquest Domain - Warrior's Might
new BlessingStatModifier("meleeWeaponsDamage", 0.15f),  // +15% melee damage
new BlessingStatModifier("meleeDamageTierBonusSlashingAttack", 1),  // +1 armor piercing

// Wild Domain - Hunter's Focus
new BlessingStatModifier("bowsProficiency", 0.25f),  // +25% draw speed
new BlessingStatModifier("steadyAim", 0.20f),  // +20% aim stability

// Craft Domain - Artificer's Precision
new BlessingStatModifier("manipulationSpeed", 0.20f),  // +20% crafting speed
new BlessingStatModifier("crossbowsProficiency", 0.30f),  // +30% reload speed

// Stone Domain - Earthen Resilience
new BlessingStatModifier("playerTorsoDamageFactor", -0.15f),  // -15% torso damage
new BlessingStatModifier("armorWalkSpeedAffectedness", -0.25f),  // 25% less armor penalty
```

### 5.5 Stat Application Method

To apply stats compatible with CombatOverhaul, use:

```csharp
entity.Stats.Set(statName, "DivineAscension:Blessing", value, false);
```

The category `"DivineAscension:Blessing"` keeps our modifiers separate from CombatOverhaul's `"CombatOverhaul:Armor"` category.

### 5.6 Implemented Solution for Silent Death

**Date Implemented:** 2026-01-17
**Approach:** Dual Stat System

The "Silent Death" blessing now includes both vanilla and CombatOverhaul stats:

```csharp
StatModifiers = new Dictionary<string, float>
{
    { VintageStoryStats.RangedWeaponsAccuracy, 0.15f },
    { VintageStoryStats.RangedWeaponsDamage, 0.15f },
    // CombatOverhaul compatibility: +1 tier bonus for piercing attacks (arrows)
    { VintageStoryStats.RangedDamageTierBonusPiercing, 1.0f }
}
```

| User Configuration | Stats Applied | Effect |
|-------------------|---------------|--------|
| Vanilla VS only | `rangedWeaponsDamage` | +15% ranged damage multiplier |
| CombatOverhaul installed | `rangedDamageTierBonusPiercingAttack` | +1 effective attack tier for arrows |
| Both stats visible | Both stats set | Each system reads its own stat |

---

## 6. File References

### vsmod_Overhaullib Key Files

| Purpose | Path |
|---------|------|
| Stats System | `/source/Framework/StatsSystem.cs` |
| Armor Types | `/source/Framework/ArmorSystems/ArmorTypes.cs` |
| Armor Slots | `/source/Framework/Inventory/ArmorInventory/ArmorSlots.cs` |
| Armor Behavior | `/source/Framework/ArmorSystems/ArmorStatsBehavior.cs` |
| Damage Types | `/source/Framework/DamageSystems/DamageTypes.cs` |
| Player Damage Model | `/source/Framework/DamageSystems/PlayerDamageModel.cs` |
| Melee Stats | `/source/Implementations/MeleeWeapon/Stats.cs` |
| Melee Attack | `/source/Implementations/MeleeWeapon/MeleeAttack.cs` |
| Melee Attack Damage | `/source/Implementations/MeleeWeapon/MeleeAttackDamageType.cs` |
| Bow | `/source/Implementations/Bow.cs` |
| Projectile | `/source/Framework/RangedSystems/Projectile.cs` |
| Colliders | `/source/Colliders/CollidersEntityBehavior.cs` |
| Entity Damage Model | `/source/Framework/DamageSystems/EntityDamageModel.cs` |
| Stagger | `/source/Framework/StaggerBehavior.cs` |

### CombatOverhaul Key Files

| Purpose | Path |
|---------|------|
| Config Patches | `/resources/assets/combatoverhaul/config/configlib-patches.json` |
| Traits | `/resources/assets/combatoverhaul/config/traits.json` |
| Armor | `/resources/assets/combatoverhaul/patches/armor.json` |
| Hide Armor | `/resources/assets/combatoverhaul/patches/armor-hide.json` |
| Armor Models | `/resources/assets/combatoverhaul/patches/armor-models.json` |
| Blades | `/resources/assets/combatoverhaul/patches/weapons/blade.json` |
| Clubs | `/resources/assets/combatoverhaul/patches/weapons/club.json` |
| Spears | `/resources/assets/combatoverhaul/patches/weapons/spear.json` |
| Shields | `/resources/assets/combatoverhaul/patches/weapons/shield.json` |
| Bows | `/resources/assets/combatoverhaul/patches/weapons/bow.json` |
| Slings | `/resources/assets/combatoverhaul/patches/weapons/sling.json` |
| Animations | `/resources/assets/combatoverhaul/config/animations/` |

### Divine Ascension

| Purpose | Path |
|---------|------|
| Stat Constants | `/Constants/VintageStoryStats.cs` |
| Blessing Definitions | `/Systems/BlessingDefinitions.cs` |
| Blessing Effect System | `/Systems/BlessingEffectSystem.cs` |

---

## Appendix: CombatOverhaul Trait Examples

From `traits.json`:
```json
{
  "code": "meleeExpert",
  "attributes": {"meleeDamageTierBonusSlashingAttack": 1}
},
{
  "code": "frightenedOfMelee",
  "attributes": {"meleeDamageTierBonusSlashingAttack": -1}
},
{
  "code": "selfDefence",
  "attributes": {"rangedDamageTierBonusPiercingAttack": 1}
}
```

This shows CombatOverhaul's intended stat naming convention for damage bonuses.

---

*Document generated: 2026-01-17*
*Based on analysis of vsmod_Overhaullib and CombatOverhaul source code*
