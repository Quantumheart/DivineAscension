# Blessing Reference - PantheonWars

**Version:** 1.0.0 (3-Deity System)
**Last Updated:** 2025-11-20
**Total Blessings:** 30 (3 deities × 10 blessings each)

---

## Table of Contents

1. [Overview](#overview)
2. [How Blessings Work](#how-blessings-work)
3. [Blessing Commands](#blessing-commands)
4. [Deity Blessing Trees](#deity-blessing-trees)
   - [Aethra (Light)](#aethra-light)
   - [Gaia (Nature)](#gaia-nature)
   - [Morthen (Shadow & Death)](#morthen-shadow--death)
5. [Progression Guide](#progression-guide)
6. [Build Recommendations](#build-recommendations)

---

## Overview

Blessings are permanent passive bonuses that enhance your character and religion. Each of the 3 deities has a unique blessing tree with 10 blessings:

- **6 Player Blessings** - Personal bonuses that only affect you
- **4 Religion Blessings** - Congregation-wide bonuses that affect all members

### Blessing Structure

**Player Blessings (6 total per deity):**
- **Tier 1 (Initiate):** 1 foundational blessing - Available at 0 favor
- **Tier 2 (Disciple):** 2 path blessings - Choose your playstyle at 500 favor
- **Tier 3 (Zealot):** 2 specialization blessings - Double down at 2,000 favor
- **Tier 4 (Champion):** 1 capstone blessing - Ultimate power at 5,000 favor (requires both Tier 3)

**Religion Blessings (4 total per deity):**
- **Tier 1 (Fledgling):** Basic group buff - Available at 0 prestige
- **Tier 2 (Established):** Improved group buff - Available at 500 prestige
- **Tier 3 (Renowned):** Advanced group buff - Available at 2,000 prestige
- **Tier 4 (Legendary):** Capstone group buff - Available at 5,000 prestige

### Key Features

- **3 Deities:** Aethra (Light/Good), Gaia (Nature/Neutral), Morthen (Shadow & Death/Evil)
- **Clear Faction Identity:** Choose between Good, Neutral, or Evil alignment
- **Meaningful Choices:** Each deity represents 33% of total content
- **Simple Triangle Dynamic:** Light ↔ Shadow (Rivals), Nature (Neutral with both)
## How Blessings Work

### Unlocking Blessings

**Player Blessings:**
1. Join a religion serving your chosen deity
2. Earn favor through PvP combat (10 favor per kill, modified by deity relationships)
3. Reach required favor rank (Initiate, Disciple, Zealot, Champion)
4. Unlock prerequisite blessings first (Tier 2+ have prerequisites)
5. Use `/blessings unlock <blessing_id>` to unlock

**Religion Blessings:**
1. Your religion must reach required prestige rank
2. Only the religion founder can unlock religion blessings
3. Prestige is earned collectively by all members through PvP
4. Use `/blessings unlock <blessing_id>` as founder

### Favor Ranks (Player Progression)

| Rank | Total Favor Required | Blessings Available |
|------|---------------------|-----------------|
| **Initiate** | 0 | Tier 1 |
| **Disciple** | 500 | Tier 2 |
| **Zealot** | 2,000 | Tier 3 |
| **Champion** | 5,000 | Tier 4 |
| **Avatar** | 10,000 | (Future expansion) |

### Prestige Ranks (Religion Progression)

| Rank | Total Prestige Required | Blessings Available |
|------|------------------------|-----------------|
| **Fledgling** | 0 | Tier 1 |
| **Established** | 500 | Tier 2 |
| **Renowned** | 2,000 | Tier 3 |
| **Legendary** | 5,000 | Tier 4 |
| **Mythic** | 10,000 | (Future expansion) |

### Stat Modifiers Explained

**Percentage Bonuses:**
- `+10% melee damage` = 10% more damage with melee weapons
- `+15% max health` = 15% higher health pool
- `+20% movement speed` = 20% faster walking/running

**Multiplicative Stacking:**
- Player blessings stack additively with each other
- Religion blessings stack additively with each other
- Player + religion blessings combine for total bonus
- Example: Player +15% damage + Religion +10% damage = +25% total damage

**Special Effects:**
- Some blessings grant special abilities (lifesteal, critical hits, stealth, etc.)
- Special effects are noted in blessing descriptions
- **Note:** Special effect handlers are planned for future implementation

---


## Blessing Commands

### Viewing Blessings

```bash
# List all available blessings for your deity
/blessings list

# View your unlocked player blessings
/blessings player

# View your religion's unlocked blessings
/blessings religion

# View details about a specific blessing
/blessings info <blessing_id>

# View your deity's blessing tree
/blessings tree player
/blessings tree religion

# View all active blessings affecting you
/blessings active
```

### Unlocking Blessings

```bash
# Unlock a player blessing (if you meet requirements)
/blessings unlock <blessing_id>

# Unlock a religion blessing (founder only, if religion meets requirements)
/blessings unlock <blessing_id>
```

### Requirements Check

Before unlocking, you must:
- ✅ Be in a religion of the correct deity
- ✅ Meet favor/prestige rank requirements
- ✅ Have unlocked all prerequisite blessings
- ✅ Not already have the blessing unlocked

---


## Deity Blessing Trees

## Aethra (Light)

**Theme:** Healing, divine protection, support, balanced holy warrior

**Playstyle:** Paladin/Cleric - Support allies, tank with healing

### Player Blessings (6)

#### Tier 1: Initiate

**✨ Divine Grace**
- **ID:** `aethra_divine_grace`
- **Rank Required:** Initiate (0 favor)
- **Prerequisites:** None
- **Effects:**
  - +10% max health
  - +12% healing effectiveness
- **Description:** The light blesses you with divine vitality.

---

#### Tier 2: Disciple - Choose Your Path

**⚔️ Radiant Strike** (Offense Path)
- **ID:** `aethra_radiant_strike`
- **Rank Required:** Disciple (500 favor)
- **Prerequisites:** Divine Grace
- **Effects:**
  - +12% melee damage
  - +10% ranged damage
  - Lifesteal: Heal 5% of damage dealt
- **Description:** Your attacks radiate holy energy. Hybrid offense with healing.

**🛡️ Blessed Shield** (Defense Path)
- **ID:** `aethra_blessed_shield`
- **Rank Required:** Disciple (500 favor)
- **Prerequisites:** Divine Grace
- **Effects:**
  - +18% armor
  - +15% max health
  - 8% damage reduction
- **Description:** Light shields you from harm. Divine tank.

---

#### Tier 3: Zealot - Specialization

**☀️ Purifying Light** (Offense Specialization)
- **ID:** `aethra_purifying_light`
- **Rank Required:** Zealot (2,000 favor)
- **Prerequisites:** Radiant Strike
- **Effects:**
  - +22% melee damage
  - +18% ranged damage
  - Lifesteal: Heal 12% of damage dealt
  - AoE healing pulse
- **Description:** Unleash devastating holy power. Smite with healing.

**🌟 Aegis of Light** (Defense Specialization)
- **ID:** `aethra_aegis_of_light`
- **Rank Required:** Zealot (2,000 favor)
- **Prerequisites:** Blessed Shield
- **Effects:**
  - +28% armor
  - +25% max health
  - +18% healing effectiveness
  - 15% damage reduction
- **Description:** Become nearly invincible with divine protection.

---

#### Tier 4: Champion - Capstone

**👑 Avatar of Light** (Ultimate Power)
- **ID:** `aethra_avatar_of_light`
- **Rank Required:** Champion (5,000 favor)
- **Prerequisites:** Purifying Light AND Aegis of Light
- **Effects:**
  - +15% melee damage
  - +15% ranged damage
  - +15% armor
  - +15% max health
  - +20% healing effectiveness
  - Lifesteal: Heal 15% of damage dealt
  - Radiant aura heals allies
  - Smite enemies
- **Description:** Embody divine radiance. Beacon of hope and destruction.

---

### Religion Blessings (4)

#### Tier 1: Fledgling

**🕊️ Blessing of Light**
- **ID:** `aethra_blessing_of_light`
- **Rank Required:** Fledgling (0 prestige)
- **Prerequisites:** None
- **Effects (All Members):**
  - +8% max health
  - +10% healing effectiveness
- **Description:** Your congregation is blessed by divine light.

#### Tier 2: Established

**⛪ Divine Sanctuary**
- **ID:** `aethra_divine_sanctuary`
- **Rank Required:** Established (500 prestige)
- **Prerequisites:** Blessing of Light
- **Effects (All Members):**
  - +12% armor
  - +10% max health
  - +12% healing effectiveness
- **Description:** Sacred protection shields all.

#### Tier 3: Renowned

**🤝 Sacred Bond**
- **ID:** `aethra_sacred_bond`
- **Rank Required:** Renowned (2,000 prestige)
- **Prerequisites:** Divine Sanctuary
- **Effects (All Members):**
  - +15% armor
  - +15% max health
  - +15% healing effectiveness
  - +10% melee damage
  - +10% ranged damage
- **Description:** Divine unity empowers the congregation.

#### Tier 4: Legendary

**🏛️ Cathedral of Light**
- **ID:** `aethra_cathedral_of_light`
- **Rank Required:** Legendary (5,000 prestige)
- **Prerequisites:** Sacred Bond
- **Effects (All Members):**
  - +20% armor
  - +20% max health
  - +20% healing effectiveness
  - +15% melee damage
  - +15% ranged damage
  - +8% movement speed
  - Divine sanctuary ability
- **Description:** Your religion becomes a beacon of divine power.

---


## Gaia (Nature)

**Theme:** Maximum durability, regeneration, defense, nature magic

**Playstyle:** Tank - Immovable object, outlast all enemies

### Player Blessings (6)

#### Tier 1: Initiate

**🌍 Earthen Resilience**
- **ID:** `gaia_earthen_resilience`
- **Rank Required:** Initiate (0 favor)
- **Prerequisites:** None
- **Effects:**
  - +15% max health
  - +10% armor
  - +8% healing effectiveness
- **Description:** Earth's strength flows through you.

---

#### Tier 2: Disciple - Choose Your Path

**🗿 Stone Form** (Defense Path)
- **ID:** `gaia_stone_form`
- **Rank Required:** Disciple (500 favor)
- **Prerequisites:** Earthen Resilience
- **Effects:**
  - +22% armor
  - +18% max health
  - 10% damage reduction
- **Description:** Become as unyielding as stone. Ultimate tank.

**🌿 Nature's Blessing** (Regeneration Path)
- **ID:** `gaia_natures_blessing`
- **Rank Required:** Disciple (500 favor)
- **Prerequisites:** Earthen Resilience
- **Effects:**
  - +20% max health
  - +18% healing effectiveness
  - Slow passive regeneration
- **Description:** Nature restores you constantly. Sustain tank.

---

#### Tier 3: Zealot - Specialization

**⛰️ Mountain Guard** (Defense Specialization)
- **ID:** `gaia_mountain_guard`
- **Rank Required:** Zealot (2,000 favor)
- **Prerequisites:** Stone Form
- **Effects:**
  - +32% armor
  - +28% max health
  - +10% melee damage
  - 15% damage reduction
- **Description:** Stand immovable like a mountain. Maximum defense.

**🌸 Lifebloom** (Regeneration Specialization)
- **ID:** `gaia_lifebloom`
- **Rank Required:** Zealot (2,000 favor)
- **Prerequisites:** Nature's Blessing
- **Effects:**
  - +30% max health
  - +28% healing effectiveness
  - Strong passive regeneration
  - Heal nearby allies
- **Description:** Life flourishes around you. Ultimate sustain.

---

#### Tier 4: Champion - Capstone

**👑 Avatar of Earth** (Ultimate Power)
- **ID:** `gaia_avatar_of_earth`
- **Rank Required:** Champion (5,000 favor)
- **Prerequisites:** Mountain Guard AND Lifebloom
- **Effects:**
  - +25% armor
  - +35% max health
  - +30% healing effectiveness
  - +15% melee damage
  - 15% damage reduction
  - Earthen aura protects and heals
- **Description:** Embody the eternal earth. Immortal guardian.

---

### Religion Blessings (4)

#### Tier 1: Fledgling

**🌱 Earthwardens**
- **ID:** `gaia_earthwardens`
- **Rank Required:** Fledgling (0 prestige)
- **Prerequisites:** None
- **Effects (All Members):**
  - +10% max health
  - +8% armor
- **Description:** Your congregation stands as guardians of the earth.

#### Tier 2: Established

**🏰 Living Fortress**
- **ID:** `gaia_living_fortress`
- **Rank Required:** Established (500 prestige)
- **Prerequisites:** Earthwardens
- **Effects (All Members):**
  - +15% max health
  - +12% armor
  - +10% healing effectiveness
- **Description:** United, you become an impenetrable fortress.

#### Tier 3: Renowned

**🌳 Nature's Wrath**
- **ID:** `gaia_natures_wrath`
- **Rank Required:** Renowned (2,000 prestige)
- **Prerequisites:** Living Fortress
- **Effects (All Members):**
  - +20% max health
  - +18% armor
  - +15% healing effectiveness
  - +12% melee damage
- **Description:** Nature defends its own with fury.

#### Tier 4: Legendary

**🌲 World Tree**
- **ID:** `gaia_world_tree`
- **Rank Required:** Legendary (5,000 prestige)
- **Prerequisites:** Nature's Wrath
- **Effects (All Members):**
  - +30% max health
  - +25% armor
  - +22% healing effectiveness
  - +18% melee damage
  - Massive regeneration aura
- **Description:** Your religion becomes the eternal world tree.

---


## Morthen (Shadow & Death)

**Theme:** Lifesteal, poison, durability, sustained damage over time

**Playstyle:** Sustain fighter - Outlast enemies with lifesteal and DoT

### Player Blessings (6)

#### Tier 1: Initiate

**💀 Death's Embrace**
- **ID:** `morthen_deaths_embrace`
- **Rank Required:** Initiate (0 favor)
- **Prerequisites:** None
- **Effects:**
  - +10% melee damage
  - +10% max health
  - Lifesteal: Heal 3% of damage dealt
- **Description:** Death empowers your strikes and body.

---

#### Tier 2: Disciple - Choose Your Path

**⚔️ Soul Reaper** (Offense Path)
- **ID:** `morthen_soul_reaper`
- **Rank Required:** Disciple (500 favor)
- **Prerequisites:** Death's Embrace
- **Effects:**
  - +15% melee damage
  - Lifesteal: Heal 10% of damage dealt
  - Poison: Attacks apply poison DoT
- **Description:** Harvest souls with dark magic. Lifesteal and poison.

**🛡️ Undying** (Defense Path)
- **ID:** `morthen_undying`
- **Rank Required:** Disciple (500 favor)
- **Prerequisites:** Death's Embrace
- **Effects:**
  - +20% max health
  - +15% armor
  - +10% healing effectiveness
- **Description:** Resist death itself. Tank with regeneration.

---

#### Tier 3: Zealot - Specialization

**☠️ Plague Bearer** (Offense Specialization)
- **ID:** `morthen_plague_bearer`
- **Rank Required:** Zealot (2,000 favor)
- **Prerequisites:** Soul Reaper
- **Effects:**
  - +25% melee damage
  - Lifesteal: Heal 15% of damage dealt
  - Poison: Strong DoT
  - Plague aura weakens enemies
- **Description:** Spread pestilence and decay. Death incarnate.

**⚰️ Deathless** (Defense Specialization)
- **ID:** `morthen_deathless`
- **Rank Required:** Zealot (2,000 favor)
- **Prerequisites:** Undying
- **Effects:**
  - +30% max health
  - +25% armor
  - +20% healing effectiveness
  - 10% damage reduction
- **Description:** Transcend mortality. Extreme durability.

---

#### Tier 4: Champion - Capstone

**👑 Lord of Death** (Ultimate Power)
- **ID:** `morthen_lord_of_death`
- **Rank Required:** Champion (5,000 favor)
- **Prerequisites:** Plague Bearer AND Deathless
- **Effects:**
  - +15% melee damage
  - +15% armor
  - +15% max health
  - +10% attack speed
  - +15% healing effectiveness
  - Lifesteal: Heal 20% of damage dealt
  - Death aura
  - Execute low health enemies
- **Description:** Command death itself. Unkillable sustain fighter.

---

### Religion Blessings (4)

#### Tier 1: Fledgling

**🕯️ Death Cult**
- **ID:** `morthen_death_cult`
- **Rank Required:** Fledgling (0 prestige)
- **Prerequisites:** None
- **Effects (All Members):**
  - +8% melee damage
  - +8% max health
- **Description:** Your congregation embraces the darkness.

#### Tier 2: Established

**📜 Necromantic Covenant**
- **ID:** `morthen_necromantic_covenant`
- **Rank Required:** Established (500 prestige)
- **Prerequisites:** Death Cult
- **Effects (All Members):**
  - +12% melee damage
  - +10% armor
  - +8% healing effectiveness
- **Description:** Dark pact strengthens all.

#### Tier 3: Renowned

**⚔️ Deathless Legion**
- **ID:** `morthen_deathless_legion`
- **Rank Required:** Renowned (2,000 prestige)
- **Prerequisites:** Necromantic Covenant
- **Effects (All Members):**
  - +18% melee damage
  - +15% armor
  - +15% max health
  - +12% healing effectiveness
- **Description:** Unkillable army of the dead.

#### Tier 4: Legendary

**👁️ Empire of Death**
- **ID:** `morthen_empire_of_death`
- **Rank Required:** Legendary (5,000 prestige)
- **Prerequisites:** Deathless Legion
- **Effects (All Members):**
  - +25% melee damage
  - +20% armor
  - +20% max health
  - +18% healing effectiveness
  - +10% attack speed
  - Death mark ability
- **Description:** Your religion rules over death itself.

---


## Progression Guide

### Early Game (0-500 Favor)
1. Choose your deity based on playstyle:
   - **Aethra:** Support/healer role, help allies
   - **Gaia:** Tank role, survive everything
   - **Morthen:** DPS role, drain and damage
2. Unlock Tier 1 blessing (free at 0 favor)
3. Earn 500 favor through PvP or deity-specific actions

### Mid Game (500-2,000 Favor)
1. Choose your path at Tier 2 (offense vs defense)
2. Work toward 2,000 favor
3. Consider religion membership for group buffs

### Late Game (2,000-5,000 Favor)
1. Unlock Tier 3 specialization in your chosen path
2. OR unlock the other path to prepare for capstone
3. Grind to 5,000 favor for ultimate power

### End Game (5,000+ Favor)
1. Unlock Tier 4 capstone (requires BOTH Tier 3 blessings)
2. Maximize religion prestige for legendary group buffs
3. Dominate PvP with full blessing tree

## Build Recommendations

### Aethra Builds

**Holy Paladin (Offense)**
- Tier 1: Divine Grace
- Tier 2: Radiant Strike
- Tier 3: Purifying Light
- Tier 4: Avatar of Light (after unlocking Aegis of Light)

**Divine Protector (Defense)**
- Tier 1: Divine Grace  
- Tier 2: Blessed Shield
- Tier 3: Aegis of Light
- Tier 4: Avatar of Light (after unlocking Purifying Light)

### Gaia Builds

**Stone Guardian (Defense)**
- Tier 1: Earthen Resilience
- Tier 2: Stone Form
- Tier 3: Mountain Guard
- Tier 4: Avatar of Earth (after unlocking Lifebloom)

**Nature Priest (Regeneration)**
- Tier 1: Earthen Resilience
- Tier 2: Nature's Blessing
- Tier 3: Lifebloom
- Tier 4: Avatar of Earth (after unlocking Mountain Guard)

### Morthen Builds

**Shadow Reaper (Offense)**
- Tier 1: Death's Embrace
- Tier 2: Soul Reaper
- Tier 3: Plague Bearer
- Tier 4: Lord of Shadow & Death (after unlocking Deathless)

**Death Knight (Defense)**
- Tier 1: Death's Embrace
- Tier 2: Undying
- Tier 3: Deathless
- Tier 4: Lord of Shadow & Death (after unlocking Plague Bearer)

---

## Frequently Asked Questions

**Q: Can I reset my blessing choices?**
A: No, blessing choices are permanent. Choose carefully!

**Q: Can I have blessings from multiple deities?**
A: No, you can only worship one deity at a time. Switching deities resets all progress.

**Q: What happens if I leave my religion?**
A: You keep your player blessings but lose all religion blessing effects.

**Q: How many blessings can I unlock total?**
A: All 10 blessings per deity (6 player + 4 religion), but you must choose paths wisely.

**Q: Which deity is best for PvP?**
A: All deities are viable! Aethra = support, Gaia = tank, Morthen = DPS.

**Q: Do religion blessings stack with player blessings?**
A: Yes! Both types work together for maximum power.

---

**Document Version:** 1.0.0
**Generated:** 2025-11-20
