# Divine Ascension - Player Guide

A deity-driven religion and civilization system for Vintage Story.

## Quick Reference

**Hotkey**: `Shift+G` opens the Divine Ascension GUI

**Domains**: Craft, Wild, Conquest, Harvest, Stone

| Command | Description |
|---------|-------------|
| `/religion create <name> <domain> <deityname> [public/private]` | Create religion |
| `/religion join <name>` | Join public religion |
| `/religion leave` | Leave religion |
| `/favor` | Check your favor and rank |
| `/holysite list` | List your holy sites |

**Favor Ranks** (based on lifetime favor earned):

| Rank | Favor Required |
|------|----------------|
| Initiate | 0 - 499 |
| Disciple | 500 - 1,999 |
| Zealot | 2,000 - 4,999 |
| Champion | 5,000 - 9,999 |
| Avatar | 10,000+ |

---

## Getting Started

1. **Create or join a religion**: `/religion create "My Religion" craft "Khoras" public`
2. **Open the GUI**: Press `Shift+G`
3. **Create a holy site**: Place an altar within your land claims
4. **Earn favor**: Perform domain-aligned activities
5. **Unlock blessings**: Spend favor on permanent bonuses

### Core Concepts

- **Favor**: Personal progression currency (individual)
- **Prestige**: Religion-wide reputation (shared)
- **Blessings**: Permanent stat bonuses unlocked with favor
- **Holy Sites**: Consecrated areas that multiply prayer rewards

---

## Domains

Choose a domain when creating your religion. Each rewards different activities.

### Craft Domain

**Focus**: Smithing, mining, metalworking

**Favor Sources**:
- Mining ore: 2 favor
- Smithing at anvil: 5-15 favor
- Smelting metals: 1-8 favor

**Example Blessings**:
- *Craftsman's Touch*: +5% tool durability, +5% ore yield
- *Masterwork Tools*: +10% mining speed
- *Legendary Smith*: +22.5% total tool durability

---

### Wild Domain

**Focus**: Hunting, foraging, wilderness survival

**Favor Sources**:
- Hunting animals: 3-15 favor (by weight: bears 15, wolves 12, deer 8, rabbits 3)
- Skinning animals: 2-8 favor (50% of hunting value)
- Foraging plants: 0.5 favor

**Example Blessings**:
- *Hunter's Instinct*: +15% animal drops, +5% movement speed
- *Apex Predator*: +35% total animal drops
- *Silent Death*: +15% ranged accuracy and damage

---

### Conquest Domain

**Focus**: Combat, exploration, domination

**Favor Sources**:
- Killing hostile creatures (by health):
  - 100+ HP (bosses): 15 favor
  - 50+ HP: 10 favor
  - 25+ HP: 7 favor
  - 10+ HP: 5 favor
  - Under 10 HP: 3 favor
- Discovering ruins:
  - Devastation structures: 100 favor
  - Temporal machinery: 75 favor
  - Locust nests: 25 favor
  - Brick ruins: 20 favor
- Patrols: 15-50+ favor (see Patrol System below)

**Patrol System** (Conquest only):
Visit 2+ holy sites in your civilization within 30 minutes to complete a patrol.
- Base: 15 favor for 2 sites, +10 per extra site
- Full circuit (all sites): +25 bonus
- Speed bonus: +10 favor under 10 min, +5 under 20 min
- Cooldown: 60 minutes between patrols

**Example Blessings**:
- *Bloodthirst*: +8% melee damage, +10% attack speed
- *Iron Will*: +10% max health, +10% damage reduction
- *Avatar of Conquest*: Heal 5% HP on kills

---

### Harvest Domain

**Focus**: Farming, cooking, agriculture

**Favor Sources**:
- Harvesting crops: 1 favor
- Cooking meals: 3-8 favor (by complexity)
- Planting crops: 0.5 favor

**Example Blessings**:
- *Sun's Blessing*: +12% crop yield, +10% satiety
- *Baker's Touch*: +25% cooking yield, food spoils 20% slower
- *Master Farmer*: +47% total crop yield

---

### Stone Domain

**Focus**: Pottery, construction, stonework

**Favor Sources**:
- Clay forming: clay consumed × 2 favor (storage vessel = 70 favor)
- Pit kiln firing: clay × 2 favor per item
- Placing bricks: 5 favor per brick
- Mining stone: 1 favor per block
- Chiseling: 0.02 favor per voxel (combo multipliers up to 2.5x)

**Chiseling Combo Tiers**:
| Actions | Multiplier |
|---------|------------|
| 1-2 | 1.0x |
| 3-5 | 1.25x |
| 6-10 | 1.5x |
| 11-20 | 2.0x |
| 21+ | 2.5x |

**Example Blessings**:
- *Master Builder*: +20% stone yield, +15% digging speed
- *Artisan Potter*: +25% clay yield
- *Fortress Architect*: +75% total stone yield, +35% armor effectiveness

---

## Holy Sites & Prayer

### Creating a Holy Site

1. Join a religion
2. Have active land claims
3. Place an altar within your claims
4. Holy site is created automatically at Tier 1 (Shrine)

### Tiers

| Tier | Name | Prayer Multiplier | How to Upgrade |
|------|------|-------------------|----------------|
| 1 | Shrine | 2.0x | Created automatically |
| 2 | Temple | 2.5x | Complete tier upgrade ritual |
| 3 | Cathedral | 3.0x | Complete tier upgrade ritual |

### Prayer

Right-click a consecrated altar to pray.

- **Base favor**: 5 × tier multiplier (Shrine: 10, Temple: 12.5, Cathedral: 15)
- **Cooldown**: 1 hour between prayers
- **Offerings**: Hold an item when praying for bonus favor

### Rituals

Upgrade your holy site by completing rituals:

1. Offer items at the altar that match ritual requirements
2. Ritual auto-starts when you offer a matching item
3. Complete 3-5 steps (discovered progressively)
4. Steps can be completed in any order
5. Multiple players can contribute

Ritual offerings award 50% normal favor (no cooldown).

---

## Blessings

Blessings provide permanent stat bonuses. Two types:

**Player Blessings**: Unlocked with your favor, benefit only you
**Religion Blessings**: Unlocked by founders with prestige, benefit all members

### Unlocking

1. Open GUI (`Shift+G`) → Blessings tab
2. Select a blessing
3. Check requirements (rank, prerequisites, cost)
4. Click Unlock

Spending favor on blessings does NOT reduce your rank (rank is based on lifetime earned).

---

## Civilizations

Alliances of 1-4 religions with different domains.

### Creating

```
/civilization create <name>
```

Requirements: Must be religion founder, not already in a civilization.

### Inviting Religions

```
/civilization invite <religion_name>
```

- Target must have different domain
- Invitations expire in 7 days
- Maximum 4 religions per civilization

---

## Diplomacy

Civilizations can form diplomatic relationships.

| Type | Requirements | Duration | Effects |
|------|--------------|----------|---------|
| Neutral | None | Permanent | Standard PvP (10 favor, 75 prestige) |
| NAP | Both Established rank | 3 days | Attacks cause violations |
| Alliance | Both Renowned rank | Permanent | +500 prestige to all, attacks cause violations |
| War | None | Until peace | 1.5x PvP rewards (15 favor, 112 prestige) |

**Violations**: Attacking allies increments a counter. After 3 violations, treaty breaks automatically.

**Commands**:
- `/diplomacy propose <civ> <nap/alliance>` - Propose relationship
- `/war <civ>` - Declare war
- `/peace <civ>` - Declare peace

---

## PvP

- **Kill reward**: 10 favor, 75 prestige (112 during war)
- **Death penalty**: -50 favor (never below 0)

---

## Commands

### Religion

| Command | Description |
|---------|-------------|
| `/religion create <name> <domain> <deityname> [visibility]` | Create religion |
| `/religion join <name>` | Join public religion |
| `/religion leave` | Leave religion |
| `/religion list [domain]` | List religions |
| `/religion info <name>` | Show religion details |
| `/religion invite <player>` | Invite player |
| `/religion kick <player>` | Remove player |
| `/religion ban <player> [reason] [days]` | Ban player |
| `/religion unban <player>` | Unban player |
| `/religion setdeityname "name"` | Change deity name (founder) |
| `/religion disband` | Delete religion (founder) |

### Other

| Command | Description |
|---------|-------------|
| `/favor` | View favor and rank |
| `/blessing list` | View blessings |
| `/holysite list` | List holy sites |
| `/holysite info [name]` | Holy site details |
| `/civilization create <name>` | Create civilization |
| `/civilization invite <religion>` | Invite religion |
| `/role assign <player> <role>` | Assign role |

**Tip**: Use quotes for names with spaces: `/religion join "Knights of Khoras"`

---

## Admin Commands

All require server administrator privileges.

| Command | Description |
|---------|-------------|
| `/favor set <amount> [player]` | Set player's favor |
| `/favor add <amount> [player]` | Add favor |
| `/favor reset [player]` | Reset favor to 0 |
| `/blessings admin unlock <id> [player]` | Force unlock blessing |
| `/blessings admin reset [player]` | Clear all blessings |
| `/religion admin repair [player]` | Fix religion data |
| `/religion admin join <religion> [player]` | Force join |
| `/civ admin create <name> <religion1> [religion2...]` | Create civilization |
| `/civ admin dissolve <name>` | Force disband |

---

## Troubleshooting

**Favor not increasing?**
Perform activities matching your domain:
- Craft: Mine ore, smith at anvil, smelt metals
- Wild: Hunt animals, skin corpses, forage plants
- Conquest: Kill hostile mobs, discover ruins, complete patrols
- Harvest: Harvest crops, cook meals, plant seeds
- Stone: Form clay, fire pottery, place bricks, chisel stone

**Altar not creating holy site?**
- Must be in a religion
- Must have land claims in the area
- Altar must be within your claims

**Can't unlock blessing?**
- Check favor rank requirement
- Check prerequisite blessings
- Ensure you have enough favor

**GUI won't open?**
- Press `Shift+G` (not just G)
- Check for keybind conflicts

**Can't leave religion as founder?**
- Transfer founder status first, or
- Disband with `/religion disband`

---

## Tips

### Favor Farming

**Craft**: Deep mine for ore, batch smithing sessions, complex anvil crafts (10-15 favor each)

**Wild**: Hunt large animals (bears 15, wolves 12), skin everything, forage while traveling

**Conquest**: Farm drifters in temporal storms, explore for devastation sites (100 favor), patrol between holy sites

**Harvest**: Large crop farms, cook complex meals (stews, pies), plant in bulk

**Stone**: Mass-produce storage vessels (70 favor), fire full pit kilns, chisel continuously for combo multipliers

### General

- Prayer at Cathedral = 15 base favor per hour
- Save rare offerings for Tier 3 holy sites (3.0x multiplier)
- Coordinate ritual contributions with religion members
- Prestige ranks unlock diplomacy options (NAP at Established, Alliance at Renowned)

---

*Last Updated: January 23, 2026*
