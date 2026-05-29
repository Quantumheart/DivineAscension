# Divine Ascension - Player Guide

A deity-driven religion and civilization system for Vintage Story.

## Quick Reference

**Opening the menu**: Right-click a **Lectern** block. There is no global hotkey in release builds — every player needs lectern access. (Debug builds additionally bind `Shift+G`.)

**Domains**: Craft, Wild, Conquest, Harvest, Stone — you earn favor with **all five at once**; your patron deity gets a 1.5× bonus.

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

1. **Craft a Lectern** (parchment + book + plank, vertical middle column). Three styles available: Lectern, Reading Stand, Weathered Lectern.
2. **Right-click the Lectern** to open the codex menu. Walking more than ~4 blocks away auto-closes it.
3. **Create or join a religion**: `/religion create "My Religion" craft "Khoras" public` — or browse public religions in the codex.
4. **Create a holy site**: Place an altar within your land claims.
5. **Earn favor**: Play naturally — every domain-aligned action fills its domain's pool. Your patron earns a 1.5× bonus on top.
6. **Unlock blessings**: Spend favor on permanent bonuses (double-click in the blessing tree to inscribe).

### Core Concepts

- **Favor**: Personal progression currency, tracked **per deity** — all five simultaneously
- **Prestige**: Religion-wide reputation (shared)
- **Blessings**: Permanent stat bonuses unlocked with favor — can be **struck** (unlearned) for a refund
- **Holy Sites**: Consecrated areas that multiply prayer rewards
- **Codex Chapters**: Menu is organized into three sections — **I. Of Self · II. Of Orders · III. Of Realms**

---

## Domains

Pick a **patron** domain when founding your religion — it earns a 1.5× favor multiplier and unlocks domain-locked capstone blessings. You still earn favor with the **other four** domains from their respective activities, just at base rate.

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
**Religion Blessings**: Unlocked by founders (or roles with the `UnlockBlessings` permission) using prestige, benefit all members

### Unlocking

1. Open the codex at a Lectern → **I. Of Self → Blessings** (or **II. Of Orders → Religion Blessings** for religion-wide)
2. Browse the branching tree (parchment + wax-seal layout)
3. Hover for blessing details and requirements (rank, prerequisites, cost)
4. **Double-click** the blessing to inscribe (a confirmation modal opens for destructive choices)

Spending favor on blessings does NOT reduce your rank (rank is based on lifetime earned).

### Active Slot Cap

Each religion can carry a limited number of **active** religion blessings at once, tied to the patron deity's favor rank. Rank up to grow your slot count. The slot cap is enforced server-side; trying to unlock past it is blocked. You can free a slot by **striking** an existing blessing (see below).

Personal blessings have an analogous active slot cap that scales with your own favor rank.

### Strike (Unlearn)

Both personal and religion blessings can be **struck** (unlearned) for a favor refund:

1. Find the blessing in the tree
2. Open its detail panel and choose **Strike**
3. A confirmation modal lists every blessing about to fall — **prerequisites cascade**, so dependent blessings are struck too
4. Confirm to commit

Refunds default to a percentage of the favor spent. If a server admin has opened a **free-respec window**, refunds are 100% for the duration of the window.

### Apostasy Penalty

Leaving a religion strips every **domain-locked** blessing you held (capstones, religion-specific gates). Cross-domain blessings stay. Plan religion changes carefully — there is no refund.

### Branches

Some blessings live on **mutually exclusive branches**. Picking one branch locks the others. Choose carefully.

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

### Milestones & Ranks

Civilizations progress through ranks by completing milestones. Each major milestone advances your civilization's rank and provides permanent bonuses that benefit all member religions.

#### Civilization Ranks

| Rank | Major Milestones Required |
|------|---------------------------|
| Nascent | 0 |
| Rising | 1 |
| Dominant | 2 |
| Hegemonic | 3 |
| Eternal | 4+ |

#### Major Milestones

Major milestones advance your civilization's rank and provide permanent bonuses.

| Milestone | Trigger | Prestige | Permanent Bonus |
|-----------|---------|----------|-----------------|
| First Alliance | 2 religions | 250 | +5% prestige multiplier |
| Holy Expansion | 2 holy sites | 300 | +1 holy site slot |
| Ritual Mastery | 5 rituals completed | 400 | Unlock civilization blessing |
| United Front | 4 domains represented | 500 | +10% favor multiplier |
| Population Surge | 25 members | 350 | +2% all rewards |

#### Minor Milestones

Minor milestones provide one-time prestige payouts without advancing rank.

| Milestone | Trigger | Prestige | Bonus |
|-----------|---------|----------|-------|
| Tier Triumph | Holy site reaches Tier 2 | 300 | - |
| Diplomatic Victory | NAP/Alliance with another civ | 200 | - |
| War Heroes | 50 PvP kills in wars | 300 | +5% conquest (7 days) |
| Cultural Monument | All 5 major milestones done | 1000 | +2% prestige |

#### Viewing Milestones

Open the codex at a Lectern → **III. Of Realms → This Realm → Laurels** to view your civilization's milestone progress.

### Civic Boons

Earned milestones surface as **civic boons** on the Civilization Detail page with distinct glyphs per boon and ornate dividers. Hover any boon for its value, source milestone, and the rule it modifies.

### Ethos

When founding a civilization, the founder picks an **Ethos** — an epithet (e.g. "Mercantile", "Crusading", "Eclectic") that flavors the civilization's character and shows up in chronicle entries and the create-form preview.

### Capital Binding

A civilization founder can bind one **holy site** within the civilization as the **capital**. The capital's name persists across reloads and shows on the civilization detail page.

### Annual Founding Day

Every civilization (and religion) celebrates its own **Founding Day** holiday on the in-game anniversary of its founding. The day-of fires a "holiday kept" toast and a chronicle entry; the prior in-game day fires an advance-notice toast.

---

## Sacred Calendar

Religions get holidays. Two flavors:

**Automatic — Founding Day**: fires every in-game anniversary of the religion's founding. No setup needed.

**Custom — Founder-defined feast days**: religion founders can add or remove custom feast days from the codex calendar. Each feast day fires:
- An **advance-notice toast** one in-game day before
- A **"holiday kept" toast** day-of
- A **chronicle entry** logging the observance

Open the codex → **II. Of Orders → This Order → Sacred Calendar** to manage feast days as founder, or just view them as a member.

## Realm Chronicles

Both religions and civilizations keep an automatic **chronicle of significant events**:

- Founding, member joins, founder transfers
- Holidays kept
- Capital binding (civilizations)
- Wars declared, peace accords signed
- Major milestones

Every entry is stamped with the in-game calendar date. Find them under **II. Of Orders → Chronicles** (religion) or **III. Of Realms → Chronicles** (civilization).

## Standing of Realms

The codex includes a **leaderboard chapter** that ranks public religions across three boards:

| Board | Measures |
|---|---|
| **Conquest** | PvP kills, wars won |
| **Endurance** | Longevity, member retention |
| **Deeds** | Milestones, prestige |

Your own religion's standing is **pinned** at the top even when you're not in the top ranks. Use the board selector at the top of the page to switch between boards.

Find it under **III. Of Realms → Standing of Realms**.

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
| `/religion leave` | Leave religion (strips domain-locked blessings — apostasy penalty) |
| `/religion list [domain]` | List religions |
| `/religion info <name>` | Show religion details |
| `/religion invite <player>` | Invite player |
| `/religion kick <player>` | Remove player |
| `/religion ban <player> [reason] [days]` | Ban player |
| `/religion unban <player>` | Unban player |
| `/religion setdeityname "name"` | Change deity name (founder) |
| `/religion description "<text>"` | Set religion description / motto |
| `/religion disband` | Delete religion (founder) |

Most religion management is also available from the codex menu opened at a **Lectern**.

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
- Check the **active slot cap** — you may need to strike an existing blessing first
- Confirm the blessing isn't on a **locked-out exclusive branch**

**Codex menu won't open?**
- Right-click a **Lectern** block (release builds have no global hotkey)
- Make sure you're within ~4 blocks of the lectern — further than that and the dialog auto-closes
- Debug builds only: try `Shift+G`

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

*Last Updated: 2026-05-29 (v5.0.0)*
