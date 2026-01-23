# Divine Ascension - Configuration Guide

Server administrators can customize Divine Ascension through in-game commands and data files.

## Quick Reference

| Command | Description |
|---------|-------------|
| `/da config profanityfilter [on/off/status]` | Toggle profanity filter |
| `/da config cooldown status` | Show all cooldown settings |
| `/da config cooldown set <operation> <seconds>` | Set cooldown duration |
| `/da config cooldown enable` | Enable cooldown system |
| `/da config cooldown disable` | Disable cooldowns |

All commands require **root privilege**.

---

## Profanity Filter

Validates names and descriptions to prevent inappropriate content.

### Commands

```
/da config profanityfilter on       # Enable
/da config profanityfilter off      # Disable
/da config profanityfilter status   # Check status
```

### What's Filtered

- Religion names and deity names
- Civilization names
- Religion descriptions

### Features

- Multi-language (English, German, Spanish, French, Russian)
- L33t speak detection (`$h1t`, `4ss`)
- Repetition detection (`shiiiit`)
- Word boundaries (allows "assassin", "peacock")

### Custom Word Lists

Default location:
```
assets/divineascension/config/profanity/
├── en.txt
├── de.txt
├── es.txt
├── fr.txt
└── ru.txt
```

To override all defaults, create a single file:
```
assets/divineascension/config/profanity-filter.txt
```

**Format**: One word per line, UTF-8 encoding, `#` for comments.

---

## Cooldown System

Rate-limits destructive operations to prevent griefing.

### View Settings

```
/da config cooldown status
```

### Set Duration

```
/da config cooldown set <operation> <seconds>
```

| Operation | Alias | Default | Description |
|-----------|-------|---------|-------------|
| `religiondeletion` | `deletion` | 60s | Disbanding a religion |
| `religioncreation` | `creation` | 300s | Creating a religion |
| `memberkick` | `kick` | 5s | Kicking a member |
| `memberban` | `ban` | 10s | Banning a member |
| `invite` | - | 2s | Sending invitations |
| `proposal` | `diplomaticproposal` | 30s | Diplomatic proposals |
| `wardeclaration` | `war` | 60s | Declaring war |

**Valid range**: 0-3600 seconds (0 disables specific cooldown)

### Enable/Disable

```
/da config cooldown enable
/da config cooldown disable
```

**Warning**: Disabling removes anti-griefing protection. Only for trusted private servers.

---

## Data Files

Customize blessings, offerings, and rituals via JSON files in `assets/divineascension/config/`.

### Blessings

**Location**: `config/blessings/{domain}.json`

**Files**: `craft.json`, `wild.json`, `conquest.json`, `harvest.json`, `stone.json`

**Example blessing**:
```json
{
  "blessingId": "khoras_craftsmans_touch",
  "name": "Craftsman's Touch",
  "description": "+5% tool durability, +5% ore yield",
  "kind": "Player",
  "category": "Utility",
  "iconName": "hammer-drop",
  "requiredFavorRank": 0,
  "requiredPrestigeRank": 0,
  "prerequisiteBlessings": [],
  "statModifiers": {
    "toolDurability": 0.05,
    "oreDropRate": 0.05
  },
  "specialEffects": []
}
```

**Fields**:
- `blessingId`: Unique identifier (snake_case)
- `kind`: `"Player"` (individual) or `"Religion"` (shared)
- `category`: `"Utility"`, `"Combat"`, or `"Defense"`
- `requiredFavorRank`: 0=Initiate, 1=Disciple, 2=Zealot, 3=Champion, 4=Avatar
- `prerequisiteBlessings`: Array of blessing IDs that must be unlocked first
- `statModifiers`: Key-value pairs (see Stat Modifier Keys below)
- `specialEffects`: Array of special effect handler IDs

### Stat Modifier Keys

**Values**: Decimal percentages (0.05 = 5%, 0.10 = 10%)

Stats are processed by three sources:
- **VS Native**: Vintage Story automatically applies these
- **DA Custom**: Divine Ascension provides Harmony patches or effect handlers
- **CO Required**: Requires Combat Overhaul mod to function

#### Vintage Story Native Stats

These stats work automatically when set on a player - no custom code needed.

**Combat & Defense**:

| Key | Description |
|-----|-------------|
| `meleeWeaponsDamage` | Melee damage bonus |
| `meleeWeaponsSpeed` | Melee attack speed |
| `rangedWeaponsDamage` | Ranged damage bonus |
| `rangedWeaponsAcc` | Ranged accuracy bonus |
| `maxhealthExtraPoints` | Flat max health bonus |
| `maxhealthExtraMultiplier` | Max health percentage bonus |
| `healingeffectivness` | Healing effectiveness bonus |
| `armorDurabilityLoss` | Armor durability loss reduction |

**Movement & Survival**:

| Key | Description |
|-----|-------------|
| `walkspeed` | Movement speed bonus |
| `miningSpeedMul` | Mining speed multiplier |
| `hungerrate` | Hunger rate modifier (negative = slower) |

**Gathering & Loot**:

| Key | Description |
|-----|-------------|
| `oreDropRate` | Ore drop quantity bonus |
| `animalLootDropRate` | Animal drop quantity bonus |
| `forageDropRate` | Forage drop quantity bonus |
| `wildCropDropRate` | Wild crop drop bonus |
| `animalHarvestingTime` | Skinning speed bonus |

#### Divine Ascension Custom Stats

These stats have custom handlers implemented by Divine Ascension.

**Craft Domain**:

| Key | Description | Implementation |
|-----|-------------|----------------|
| `toolDurability` | Tool damage reduction chance | Harmony patch |
| `coldResistance` | Cold resistance (degrees) | Registered |

**Wild Domain**:

| Key | Description | Implementation |
|-----|-------------|----------------|
| `foodSpoilage` | Food spoilage reduction | FoodSpoilageEffect handler |
| `temperatureResistance` | Temperature resistance | TemperatureResistanceEffect handler |

**Harvest Domain**:

| Key | Description | Implementation |
|-----|-------------|----------------|
| `rareCropChance` | Rare crop variant chance | RareCropDiscoveryEffect handler |
| `cookedFoodSatiety` | Cooked food satiety bonus | BlessedMealsEffect handler |
| `heatResistance` | Heat resistance (degrees) | Shared with temperatureResistance |

**Stone Domain**:

| Key | Description | Implementation |
|-----|-------------|----------------|
| `stoneYield` | Stone mining drop bonus | BlockBehaviorStone |
| `potteryBatchCompletionChance` | Pottery batch completion bonus | GaiaEffectHandlers |

**Combat**:

| Key | Description | Implementation |
|-----|-------------|----------------|
| `criticalHitChance` | Critical hit chance | Registered |
| `criticalHitDamage` | Critical hit damage multiplier | Registered |
| `killHealthRestore` | HP restored on kill | BloodlustEffect handler |
| `damageReduction` | Damage reduction percentage | LastStandEffect handler |

#### Combat Overhaul Stats (Requires CO Mod)

These stats only function when Combat Overhaul is installed.

**Damage Type Bonuses**:

| Key | Description |
|-----|-------------|
| `meleeDamageTierBonusSlashingAttack` | Melee slashing tier bonus |
| `meleeDamageTierBonusPiercingAttack` | Melee piercing tier bonus |
| `meleeDamageTierBonusBluntAttack` | Melee blunt tier bonus |
| `rangedDamageTierBonusSlashingAttack` | Ranged slashing tier bonus |
| `rangedDamageTierBonusPiercingAttack` | Ranged piercing tier bonus |
| `rangedDamageTierBonusBluntAttack` | Ranged blunt tier bonus |

**Armor Penalty Reduction**:

| Key | Description |
|-----|-------------|
| `armorWalkSpeedAffectedness` | Armor walk speed penalty reduction |
| `armorManipulationSpeedAffectedness` | Armor manipulation penalty reduction |
| `armorHungerRateAffectedness` | Armor hunger penalty reduction |

**Weapon Proficiencies** (affect attack/reload/draw speed):

| Key | Description |
|-----|-------------|
| `bowsProficiency` | Bow draw speed |
| `crossbowsProficiency` | Crossbow reload speed |
| `oneHandedSwordsProficiency` | One-handed sword attack speed |
| `twoHandedSwordsProficiency` | Two-handed sword attack speed |
| `spearsProficiency` | Spear attack speed |
| `axesProficiency` | Axe attack speed |

**Body Zone Damage Factors**:

| Key | Default | Description |
|-----|---------|-------------|
| `playerHeadDamageFactor` | 2.0x | Head damage multiplier |
| `playerTorsoDamageFactor` | 1.0x | Torso damage multiplier |
| `playerArmsDamageFactor` | 0.5x | Arms damage multiplier |
| `playerLegsDamageFactor` | 0.5x | Legs damage multiplier |

### Adding a New Blessing

1. Open the appropriate domain file (e.g., `craft.json`)
2. Add a new blessing object to the `"blessings"` array:
```json
{
  "blessings": [
    {
      "blessingId": "existing_blessing"
    },
    {
      "blessingId": "khoras_new_blessing",
      "name": "New Blessing",
      "description": "Description here",
      "kind": "Player",
      "category": "Utility",
      "iconName": "icon-name",
      "requiredFavorRank": 1,
      "requiredPrestigeRank": 0,
      "prerequisiteBlessings": ["khoras_craftsmans_touch"],
      "statModifiers": {
        "miningSpeedMul": 0.15
      },
      "specialEffects": []
    }
  ]
}
```
3. Restart the server to load changes

### Offerings

**Location**: `config/offerings/{domain}.json`

**Example offering**:
```json
{
  "name": "Iron Ingot",
  "itemCodes": ["game:ingot-iron"],
  "tier": 2,
  "value": 5,
  "minHolySiteTier": 1,
  "description": "Quality metal offering"
}
```

**Fields**:
- `itemCodes`: Vintage Story item codes (supports wildcards like `game:ingot-*`)
- `tier`: Offering tier (1-3)
- `value`: Base favor value
- `minHolySiteTier`: Minimum holy site tier required to accept

### Rituals

**Location**: `config/rituals/{domain}.json`

**Example ritual**:
```json
{
  "ritualId": "craft_shrine_to_temple",
  "name": "Forge Consecration",
  "description": "Upgrade Shrine to Temple",
  "sourceTier": 1,
  "targetTier": 2,
  "steps": [
    {
      "stepId": "gather_metals",
      "name": "Gather Sacred Metals",
      "requirements": [
        {
          "type": "Category",
          "pattern": "game:ingot-*",
          "quantity": 50,
          "description": "Any metal ingots"
        }
      ]
    }
  ]
}
```

**Fields**:
- `sourceTier` / `targetTier`: Holy site upgrade path
- `steps`: 3-5 steps required (validation enforced)
- `requirements.type`: `"Exact"` (specific item) or `"Category"` (glob pattern)

---

## File Locations

| Type | Path |
|------|------|
| Blessings | `assets/divineascension/config/blessings/*.json` |
| Offerings | `assets/divineascension/config/offerings/*.json` |
| Rituals | `assets/divineascension/config/rituals/*.json` |
| Profanity Lists | `assets/divineascension/config/profanity/*.txt` |
| Language Files | `assets/divineascension/lang/*.json` |

---

## Storage & Persistence

| Setting | Storage | Scope |
|---------|---------|-------|
| Profanity filter | World save | Per-world |
| Cooldown settings | World save | Per-world |
| Data files (JSON) | Mod assets | Global |

World settings persist across server restarts and are specific to each world.

---

## Troubleshooting

**JSON changes not loading?**
- Restart the server (JSON files are loaded at startup)
- Check server logs for JSON parsing errors
- Validate JSON syntax (missing commas, brackets)

**Cooldown commands not working?**
- Verify you have root privilege
- Check command syntax: `/da config cooldown set kick 10`

**Profanity filter blocking legitimate words?**
- Add to custom word list with `#` prefix (comment) to document
- Or disable filter: `/da config profanityfilter off`

**Blessing not appearing in-game?**
- Verify `blessingId` is unique
- Check `domain` matches the file name
- Ensure `requiredFavorRank` is valid (0-4)

**Offering not accepted at altar?**
- Verify `itemCodes` match Vintage Story item codes exactly
- Check `minHolySiteTier` isn't higher than altar tier

---

## Reference Values

### Holy Site Prayer Multipliers

| Tier | Name | Prayer Multiplier |
|------|------|-------------------|
| 1 | Shrine | 2.0x |
| 2 | Temple | 2.5x |
| 3 | Cathedral | 3.0x |

### Favor Rank Thresholds

| Rank | Index | Lifetime Favor |
|------|-------|----------------|
| Initiate | 0 | 0 |
| Disciple | 1 | 500 |
| Zealot | 2 | 2,000 |
| Champion | 3 | 5,000 |
| Avatar | 4 | 10,000 |

### Prestige Rank Thresholds

| Rank | Prestige |
|------|----------|
| Fledgling | 0 |
| Established | 2,500 |
| Renowned | 10,000 |
| Legendary | 25,000 |
| Mythic | 50,000 |

---

*Last Updated: January 23, 2026*
