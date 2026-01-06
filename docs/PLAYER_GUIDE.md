# Divine Ascension - Player Guide

**Welcome to Divine Ascension!** This guide will help you navigate the deity-driven religion and civilization systems in Vintage Story.

## Table of Contents

- [Quick Start](#quick-start)
- [The Deities](#the-deities)
- [Understanding Progression](#understanding-progression)
- [Creating & Joining Religions](#creating--joining-religions)
- [Earning Favor](#earning-favor)
- [Unlocking Blessings](#unlocking-blessings)
- [Civilizations](#civilizations)
- [Diplomacy](#diplomacy)
- [PvP & Combat](#pvp--combat)
- [Commands Reference](#commands-reference)
- [GUI Overview](#gui-overview)
- [Advanced Tips](#advanced-tips)

---

## Quick Start

### First Steps

1. **Choose a deity** based on your playstyle (see [The Deities](#the-deities))
2. **Create or join a religion** using `/religion create` or `/religion join`
3. **Open the GUI** with `Shift+G` to view your progression and blessings
4. **Earn Favor** by performing deity-aligned activities
5. **Unlock Blessings** to gain powerful stat bonuses and effects

### Core Concepts

- **Favor**: Your personal progression with your deity (individual)
- **Prestige**: Your religion's collective reputation (shared by all members)
- **Blessings**: Powerful upgrades unlocked using Favor points
- **Civilizations**: Multi-religion alliances for shared goals

---

## The Deities

Choose a deity that matches your playstyle. Each deity rewards different activities and offers unique blessings.

### 🔨 Khoras - God of the Forge & Craft

**Domain**: Forge & Craft, Smithing, Tool-making, Durability
**Alignment**: Lawful Neutral
**Colors**: Forge Orange and Steel Gray
**Playstyle**: Tool durability and crafting focused with ore efficiency bonuses

**Best For**:
- Players who enjoy mining and metalworking
- Crafters who want durable tools
- Self-sufficient smiths and builders
- Players focused on resource efficiency

**Favor Sources**:
- Mining ore: 2 favor per ore
- Smithing at the anvil: 5-15 favor per craft
- Smelting metals: 1-8 favor per smelt
- PvP combat (all deities): 10 favor per kill
- Passive generation: 0.5 favor/hour

---

### 🏹 Lysa - Goddess of the Hunt

**Domain**: Hunt, Wilderness, and Precision
**Alignment**: Neutral
**Colors**: Forest Green and Earth Brown
**Playstyle**: Hunting and foraging in the wilderness

**Best For**:
- Hunters and archers
- Players who enjoy wilderness survival
- Foraging and gathering specialists
- Players who prefer mobile, tactical gameplay

**Favor Sources**:
- Hunting animals: 3-20 favor per kill (varies by animal)
- Foraging plants and resources: 0.5 favor per harvest
- PvP combat (all deities): 10 favor per kill
- Passive generation: 0.5 favor/hour

---

### 🌾 Aethra - Goddess of Light & Agriculture

**Domain**: Light, Agriculture, Growth, Warmth
**Alignment**: Lawful Good
**Colors**: Golden Sun and Wheat Gold
**Playstyle**: Agriculture and cooking focused with crop yield and food bonuses

**Best For**:
- Players who enjoy farming and agriculture
- Cooks who want better food output
- Self-sufficient food producers
- Players focused on sustainable food sources

**Favor Sources**:
- Harvesting crops: 1 favor per harvest
- Cooking meals: 3-8 favor per meal (varies by complexity)
- Planting crops: 0.5 favor per plant
- PvP combat (all deities): 10 favor per kill
- Passive generation: 0.5 favor/hour

---

### 🪨 Gaia - Goddess of Pottery & Clay

**Domain**: Pottery, Clay, Craftsmanship
**Alignment**: Neutral
**Colors**: Earth Brown and Clay Orange
**Playstyle**: Pottery crafting and fortification focused with clay gathering bonuses

**Best For**:
- Players who enjoy pottery crafting
- Builders who work with clay and bricks
- Organizers who need storage solutions
- Players focused on defensive resilience

**Favor Sources**:
- Crafting pottery items: 2-5 favor per craft (varies by item complexity)
- Firing pottery in kilns: 3-8 favor per firing
- Placing clay bricks: 2 favor per brick
- PvP combat (all deities): 10 favor per kill
- Passive generation: 0.5 favor/hour

---

## Understanding Progression

Divine Ascension features **dual progression**: individual Favor and collective Prestige.

### Player Favor (Individual Progression)

Favor represents your personal devotion to your deity. It unlocks player-specific blessings.

**Favor Ranks**:

| Rank | Total Favor Required | Unlocks |
|------|---------------------|---------|
| **Initiate** | 0 - 499 | Starting tier blessings |
| **Disciple** | 500 - 1,999 | Tier 2 blessings |
| **Zealot** | 2,000 - 4,999 | Tier 3 blessings |
| **Champion** | 5,000 - 9,999 | Tier 4 blessings |
| **Avatar** | 10,000+ | Maximum tier blessings |

**Important**:
- Your Favor Rank is based on **lifetime favor earned**, not current favor
- Ranks persist even if you switch religions
- Spending favor on blessings doesn't reduce your rank
- **Rank Up Notifications**: When you reach a new favor rank, you'll receive an on-screen notification celebrating your
  achievement!

### Religion Prestige (Collective Progression)

Prestige is your religion's reputation, earned collectively by all members. It unlocks religion-wide blessings that benefit everyone.

**Prestige Ranks**:

| Rank | Prestige Required | Unlocks |
|------|------------------|---------|
| **Fledgling** | 0 - 499 | Basic religion blessings |
| **Established** | 500 - 1,999 | Improved religion blessings |
| **Renowned** | 2,000 - 4,999 | Advanced religion blessings |
| **Legendary** | 5,000 - 9,999 | Elite religion blessings |
| **Mythic** | 10,000+ | Ultimate religion blessings |

**How Prestige is Earned**:
- Members performing deity-aligned activities
- PvP victories (15 prestige per kill)
- Contributions from all active members

---

## Creating & Joining Religions

### Creating a Religion

```
/religion create <name> <deity> [visibility]
```

**Example**: `/religion create "Knights of Khoras" khoras public`

**Parameters**:
- `name`: Your religion's name (use quotes if it contains spaces)
- `deity`: Choose from `khoras`, `lysa`, `aethra`, or `gaia`
    - **Tip**: In the GUI creation screen, hover over a deity's icon to see detailed information about their domain,
      playstyle, and favor sources!
- `visibility`: `public` (anyone can join) or `private` (invite-only)

**As Founder, You Can**:
- Invite and kick members
- Manage roles and permissions
- Disband the religion
- Form and manage civilization alliances

### Joining a Religion

**Option 1 - Public Religions**:
```
/religion join <name>
```

**Option 2 - Private Religions**:
Wait for an invitation, then accept via the GUI (`Shift+G`)

**Important Notes**:
- You can only be in one religion at a time
- There's a **7-day cooldown** after leaving a religion
- Your Favor Rank persists across religion changes
- You'll lose access to your previous religion's blessings

### Leaving a Religion

```
/religion leave
```

⚠️ **Warning**: This starts a 7-day cooldown before you can join another religion!

**Important**: If you are the **Founder** of a religion, you cannot leave it. You must either:

- Transfer founder status to another member (if available)
- Disband the religion entirely using `/religion disband`

### Managing Members (Founder/Elder)

As a religion Founder or Elder, you have additional management tools:

**Inviting Members**:

```
/religion invite <player_name>
```

Send an invitation to a player to join your religion (for private religions).

**Kicking Members**:

```
/religion kick <player_name>
```

Remove a member from your religion immediately.

**Banning Members**:

You can ban problematic members to prevent them from rejoining:

```
/religion ban <player_name> [reason] [days]
```

**Parameters**:

- `player_name`: The player to ban (required)
- `reason`: Optional reason for the ban (shown in ban list)
- `days`: Optional duration in days (default: permanent)

**Examples**:

- `/religion ban BadPlayer` - Permanent ban
- `/religion ban BadPlayer "Toxic behavior"` - Permanent ban with reason
- `/religion ban BadPlayer "Needs timeout" 7` - 7-day temporary ban

**Viewing Bans**:

```
/religion banlist
```

Shows all active bans with player names, reasons, and expiration dates.

**Unbanning Players**:

```
/religion unban <player_name>
```

Removes a ban, allowing the player to join again (if invited or if religion is public).

**Ban Features**:

- Banned players cannot join the religion (even if public)
- Banned players cannot accept invitations
- Temporary bans automatically expire after the specified duration
- Ban list persists across server restarts

---

## Earning Favor

Favor is earned by performing activities aligned with your deity's domain.

### Khoras (Forge & Craft)

**Primary Activities**:
- ⛏️ **Mining**: Breaking ore blocks (2 favor per ore)
- 🔨 **Smithing**: Crafting items at the anvil (5-15 favor per craft)
- 🔥 **Smelting**: Processing ores in bloomeries and furnaces (1-8 favor per smelt)
- ⚔️ **PvP**: Combat victories (10 favor per kill)

**Tips**:
- Focus on deep mining expeditions for maximum ore collection
- Set up efficient smelting operations
- Craft complex items at the anvil for higher favor rewards

### Lysa (Hunt & Wilderness)

**Primary Activities**:
- 🏹 **Hunting**: Killing animals (3-20 favor per kill, varies by animal)
- 🌿 **Foraging**: Collecting plants, mushrooms, and natural resources (0.5 favor per harvest)
- ⚔️ **PvP**: Combat victories (10 favor per kill)

**Tips**:
- Go on regular hunting trips for high-value animals (wolves, bears)
- Gather from various biomes to find diverse forageable resources
- Maintain sustainable hunting practices

### Aethra (Agriculture & Light)

**Primary Activities**:
- 🌾 **Harvesting Crops**: Breaking harvestable crops (1 favor per harvest)
- 🍞 **Cooking Meals**: Preparing food in firepits and crocks (3-8 favor per meal based on complexity)
- 🌱 **Planting Crops**: Planting seeds (0.5 favor per plant)
- ⚔️ **PvP**: Combat victories (10 favor per kill)

**Tips**:
- Set up large farms for consistent crop harvesting
- Cook complex meals (stews, pies, porridge) for maximum favor
- Automate farming operations with companions

### Gaia (Pottery & Clay)

**Primary Activities**:
- 🏺 **Crafting Pottery**: Forming clay items (2-5 favor per craft, varies by complexity)
- 🔥 **Firing Kilns**: Completing pit kiln firings (3-8 favor per firing)
- 🧱 **Placing Bricks**: Building with clay bricks (2 favor per brick)
- ⚔️ **PvP**: Combat victories (10 favor per kill)

**Tips**:
- Focus on crafting storage vessels (5 favor) and planters (4 favor)
- Batch fire pottery in pit kilns for efficient favor gains
- Build with clay bricks for both favor and base construction

### Passive Favor Generation

All players generate **0.5 favor per hour** automatically, regardless of activity. Stay logged in to benefit!

### Death Penalty

Dying costs you **50 favor** (never reduces below 0). Be careful out there!

---

## Unlocking Blessings

Blessings provide permanent stat bonuses and special abilities. There are two types:

### Player Blessings (Individual)

- Unlocked with your **Favor points**
- Require specific **Favor Ranks**
- Persist even if you change religions (but you lose access until you rejoin that deity)
- Examples: increased health, movement speed, mining speed

### Religion Blessings (Shared)

- Unlocked by religion leaders using **Prestige points**
- Require specific **Prestige Ranks**
- Benefit **all members** of the religion
- Examples: group bonuses, resource multipliers, defensive buffs

### Unlocking Blessings

1. Press `Shift+G` to open the GUI
2. Navigate to the **Blessings** tab
3. View available blessings in the tree layout
4. Click on a blessing to see:
   - Favor/Prestige cost
   - Rank requirements
   - Prerequisites (other blessings needed first)
   - Effects and bonuses
5. Click **Unlock** if you meet all requirements

**Tips**:
- Plan your blessing path carefully - favor is precious!
- Some blessings require prerequisites - check the tree structure
- Coordinate with your religion for shared blessing priorities
- Higher rank unlocks more powerful blessings

---

## Civilizations

Civilizations are alliances of **1-4 religions** with different deities working together.

### Benefits

- Shared identity and coordination
- Multi-deity cooperation
- Potential for larger-scale politics and warfare
- Prestige bonuses for alliance formation

### Creating a Civilization

```
/civilization create <name>
```

**Requirements**:
- You must be the **Founder** of your religion
- Your religion must not already be in a civilization

### Inviting Religions

```
/civilization invite <religion_name>
```

**Notes**:
- Invitations expire after **7 days**
- Target religion must have a different deity
- Maximum of 4 religions per civilization
- Target religion's founder must accept

### Managing Your Civilization

Use `/civilization` commands or the GUI (`Shift+G` → Civilizations tab) to:
- View member religions
- See pending invitations
- Kick religions (founder only)
- Disband the civilization (founder only)

---

## Diplomacy

Civilizations can establish diplomatic relationships with each other, creating strategic alliances, trade agreements, or
declaring war. The diplomacy system adds a layer of politics and strategy to inter-civilization interactions.

### Diplomatic Relationships

There are **four types** of diplomatic relationships between civilizations:

#### 1. Neutral (Default)

- **Description**: No formal relationship exists
- **Requirements**: None
- **Duration**: Permanent until changed
- **Effects**: Standard PvP rules apply (10 favor, 15 prestige per kill)

#### 2. Non-Aggression Pact (NAP)

- **Description**: Formal agreement not to attack each other
- **Requirements**:
    - Both civilizations must be at least **Established** rank (500+ prestige)
    - Must be proposed and accepted by both civilization founders
- **Duration**: **3 days** (real-time), then automatically reverts to Neutral
- **Effects**:
    - Attacking allied civilization members results in **violations** (see Violation System)
    - No favor/prestige rewards for attacking NAP members
    - Can be broken with 24-hour notice (see Breaking Treaties)

#### 3. Alliance

- **Description**: Strong alliance between civilizations
- **Requirements**:
    - Both civilizations must be at least **Renowned** rank (2,000+ prestige)
    - Must be proposed and accepted by both civilization founders
- **Duration**: **Permanent** until broken or war is declared
- **Effects**:
    - **100 prestige** awarded to ALL religions in BOTH civilizations upon formation
    - Attacking allied civilization members results in **violations**
    - No favor/prestige rewards for attacking Alliance members
    - Can be broken with 24-hour notice

#### 4. War

- **Description**: Official state of war between civilizations
- **Requirements**:
    - Can be declared **unilaterally** by any civilization founder
    - No prestige rank requirements
- **Duration**: Until **peace is declared** by one side
- **Effects**:
    - **1.5x multiplier** on favor and prestige for PvP kills (15 favor, 22.5 prestige per kill)
    - Server-wide announcement when war is declared
    - Automatically cancels any pending NAP or Alliance proposals

### Proposing Relationships

**Civilization founders** can propose diplomatic relationships through the GUI or commands:

1. **Via GUI** (`Shift+G` → Civilizations → Diplomacy):
    - Select target civilization from dropdown
    - Choose relationship type (NAP or Alliance)
    - Click "Send Proposal"
    - Proposal expires in **7 days** if not accepted

2. **Via Commands**:

```
/diplomacy propose <civilization_id> <type>
```

Example: `/diplomacy propose "Golden Empire" alliance`

**Important Rules**:

- Only **one pending proposal** allowed per civilization pair
- **Bidirectional proposals** (both sides proposing to each other) are blocked
- Target civilization must have a **different deity** than yours
- Both civilizations must meet the **prestige rank requirements**

### Accepting or Declining Proposals

When you receive a diplomatic proposal:

1. **Via GUI**: Navigate to Civilizations → Diplomacy → Pending Proposals (Incoming)
    - Review the proposal details
    - Check prestige rank requirements
    - Click **Accept** or **Decline**

2. **Via Commands**:

```
/diplomacy accept <proposal_id>
/diplomacy decline <proposal_id>
```

**Acceptance Requirements**:

- Must still meet prestige rank requirements at time of acceptance
- Your civilization must not already be in a conflicting relationship

### Breaking Treaties (NAP/Alliance)

Treaties can be broken with a **24-hour warning period**:

1. **Schedule a Break**:
    - Via GUI: Click "Schedule Break" next to the relationship
    - Via command: `/diplomacy break <civilization_id>`

2. **Warning Period**:
    - Both civilizations are notified
    - **24-hour countdown** begins
    - During this time, the treaty is still active

3. **Cancel Scheduled Break**:
    - Change your mind during the 24-hour period
    - Via GUI: Click "Cancel Break"
    - Via command: `/diplomacy cancelbreak <civilization_id>`

4. **After 24 Hours**:
    - Relationship automatically reverts to **Neutral**
    - No penalties applied

**Important**: There are **no penalties** for breaking treaties, but your reputation may suffer among other players.

### Violation System

Attacking members of civilizations you have a NAP or Alliance with triggers the **violation system**:

**How It Works**:

1. When you attack an allied civilization member:
    - The attack **succeeds** normally (damage is dealt)
    - **No favor or prestige** is awarded
    - A **violation** is recorded (counter increments)
    - Warning message: "Warning: Attacking allied civilization! (Violation X/3)"

2. **3-Strike System**:
    - Each attack = 1 violation
    - After **3 violations**, the treaty **automatically breaks**
    - No 24-hour warning - immediate reversion to Neutral
    - Violation counter resets when new relationship is established

**Strategic Note**: The violation system allows for mistakes or reactive breaking without requiring the 24-hour warning
period.

### Declaring War

War can be declared **unilaterally** (no proposal required):

1. **Via GUI**: Navigate to Diplomacy → "Declare War" button (requires confirmation)
2. **Via Command**: `/war <civilization_id>` or `/diplomacy declarewar <civilization_id>`

**Effects**:

- **Both civilizations** automatically enter War status
- Server-wide announcement: "[Diplomacy] {Your Civ} has declared war on {Target Civ}!"
- Any pending NAP or Alliance proposals between the civilizations are **cancelled**
- PvP kills reward **1.5x favor and prestige** (15 favor, 22.5 prestige)

### Declaring Peace

To end a war, either side can declare peace:

1. **Via GUI**: Click "Declare Peace" next to the War relationship
2. **Via Command**: `/peace <civilization_id>` or `/diplomacy declarepeace <civilization_id>`

**Effects**:

- Both civilizations return to **Neutral** status
- Normal PvP rewards resume (10 favor, 15 prestige)

### Diplomacy Tab (GUI)

Access via `Shift+G` → Civilizations → **Diplomacy**

**Current Relationships Panel**:

- View all active diplomatic relationships
- Color-coded status badges (Green=Alliance, Yellow=NAP, Red=War, Gray=Neutral)
- Shows: Civilization name, status, established date, expiration (for NAP), violations (X/3)
- Actions: Schedule/cancel break, declare peace

**Pending Proposals Panel**:

- **Incoming**: Proposals from other civilizations (Accept/Decline buttons)
- **Outgoing**: Your sent proposals (Cancel option)
- Shows expiration countdown (7 days)

**Propose Relationship Panel**:

- Select target civilization
- Choose relationship type (displays rank requirements)
- Send proposal or declare war

### Diplomacy Commands Reference

| Command                            | Description                                   |
|------------------------------------|-----------------------------------------------|
| `/diplomacy status`                | Show diplomatic status with all civilizations |
| `/diplomacy propose <civ> <type>`  | Propose NAP or Alliance                       |
| `/diplomacy accept <proposal_id>`  | Accept incoming proposal                      |
| `/diplomacy decline <proposal_id>` | Decline incoming proposal                     |
| `/diplomacy break <civ>`           | Schedule treaty break (24-hour warning)       |
| `/diplomacy cancelbreak <civ>`     | Cancel scheduled break                        |
| `/war <civ>`                       | Declare war (shortcut)                        |
| `/peace <civ>`                     | Declare peace (shortcut)                      |

### Strategy Tips

**Forming Alliances**:

- Focus on reaching **Renowned rank** (2,000 prestige) to unlock alliances
- Coordinate with complementary deity civilizations
- Alliance prestige bonus (100 to all religions in both civs) is significant
- Build trust before proposing - broken treaties damage reputation

**Using NAPs**:

- Great for temporary peace during resource gathering
- Remember they expire in **3 days** - coordinate renewal if needed
- Lower prestige requirement (Established/500) makes them accessible early

**Declaring War**:

- Use strategically for **1.5x PvP rewards** (15 favor, 22.5 prestige per kill)
- Server announcement can rally your allies and intimidate opponents
- Can be declared unilaterally - no waiting for acceptance

**Breaking Treaties**:

- **Planned Break**: Use 24-hour warning for diplomatic approach
- **Reactive Break**: Trigger 3 violations for immediate break
- No penalties, but consider long-term reputation

---

## PvP & Combat

### Favor Rewards

Killing another player awards:
- **10 Favor** to the attacker
- **15 Prestige** to the attacker's religion

**Important**: All kills award the same favor regardless of deity matchups (deity relationships were removed in recent updates).

### Death Penalties

- **-50 Favor** when you die (any cause)
- Favor never goes below 0

### PvP Strategy Tips

- Higher Favor Rank unlocks better combat blessings
- Coordinate with your religion for group PvP
- Be strategic - deaths cost you favor!

---

## Commands Reference

### Religion Commands

| Command | Description |
|---------|-------------|
| `/religion create <name> <deity> [visibility]` | Create a new religion |
| `/religion join <name>` | Join a public religion |
| `/religion leave` | Leave your current religion (7-day cooldown) |
| `/religion list [deity]` | List all religions (optionally filter by deity) |
| `/religion info <name>` | Show detailed religion information |
| `/religion members` | View members of your religion |
| `/religion invite <player>` | Invite a player to your religion (founder/elder) |
| `/religion kick <player>` | Remove a player from your religion (founder/elder) |
| `/religion ban <player> [reason] [days]` | Ban a player from your religion |
| `/religion unban <player>` | Unban a player |
| `/religion banlist` | View banned players |
| `/religion disband` | Permanently delete your religion (founder only) |

### Favor Commands

| Command | Description |
|---------|-------------|
| `/favor` | View your current favor and rank |
| `/favor info` | Detailed favor statistics |

### Blessing Commands

| Command | Description |
|---------|-------------|
| `/blessing list` | View available blessings |
| `/blessing info <id>` | Show blessing details |

### Civilization Commands

| Command | Description |
|---------|-------------|
| `/civilization create <name>` | Create a civilization |
| `/civilization invite <religion>` | Invite a religion to your civilization |
| `/civilization kick <religion>` | Remove a religion from your civilization |
| `/civilization leave` | Leave your current civilization |
| `/civilization disband` | Disband your civilization (founder only) |
| `/civilization list` | View all civilizations |
| `/civilization info <name>` | Show civilization details |

### Role Commands

| Command | Description |
|---------|-------------|
| `/role list` | View religion roles |
| `/role assign <player> <role>` | Assign a role to a member |
| `/role remove <player> <role>` | Remove a role from a member |

---

## GUI Overview

Press **`Shift+G`** to open the Divine Ascension interface.

### Tabs

**1. Religion Tab**
- **Browse**: View and join available religions
- **Create**: Start your own religion
- **Info**: View your current religion's details
- **Activity**: Coming soon (placeholder for future activity feed)

**2. Blessings Tab**
- View the blessing tree layout
- See unlocked blessings (highlighted)
- Unlock new blessings
- View blessing effects and tooltips
- Filter by player vs religion blessings

**3. Civilizations Tab**
- **Browse**: Explore existing civilizations
- **Create**: Form a new civilization (founder only)
- **Manage**: Invite religions, view members
- **Info**: Civilization details and member list
- **Diplomacy**: Manage diplomatic relationships between civilizations (founder only)
    - View current relationships (NAP, Alliance, War, Neutral)
    - Send/accept/decline diplomatic proposals
    - Declare war or peace
    - Monitor violation counters
    - Schedule treaty breaks with 24-hour warnings

### Interface Tips

- Hover over blessings to see detailed tooltips
- Icons indicate blessing types and deity associations
- Color coding shows unlock status and requirements
- Use filters to find specific content quickly

---

## Advanced Tips

### Optimal Favor Farming

**Khoras Followers**:
1. Set up automated smelting operations for passive favor
2. Create a deep mine with multiple ore veins
3. Combine mining sessions with smithing batches
4. Focus on complex anvil crafts (10-15 favor each)

**Lysa Followers**:
1. Find areas with high animal spawns (wolves/bears = 12-15 favor)
2. Create hunting routes through multiple biomes
3. Forage consistently while traveling (0.5 favor adds up)
4. Balance hunting with sustainable foraging

**Aethra Followers**:
1. Build large automated crop farms for mass harvesting
2. Cook complex meals (stews, pies, porridge) for 5-8 favor each
3. Plant in bulk during planting season
4. Set up cooking stations near farms for efficiency

**Gaia Followers**:
1. Mass-produce storage vessels (5 favor each) and planters (4 favor)
2. Batch fire pottery in pit kilns for efficient favor
3. Build with clay bricks (2 favor per brick) for dual-purpose construction
4. Keep clay digging and pottery crafting as regular routines

### Religion Management

**As a Founder**:
- Set clear expectations for member activity
- Coordinate blessing unlocks with your members
- Use roles to delegate management tasks
- Build a strong founding team before growing too large

**As a Member**:
- Contribute regularly to religion prestige
- Communicate with leadership about blessing priorities
- Participate in group activities and PvP
- Respect the 7-day cooldown before switching religions

### Blessing Strategy

**Early Game** (Initiate/Disciple):
- Focus on survivability: health, damage resistance
- Quality of life: movement speed, mining speed
- Save favor for high-impact unlocks

**Mid Game** (Zealot/Champion):
- Unlock synergistic blessing combinations
- Invest in your primary activity (combat, crafting, etc.)
- Balance offense and defense

**Late Game** (Avatar):
- Min-max for your specific role
- Coordinate with religion for shared blessings
- Experiment with unique blessing combinations

### Civilization Politics

- Form civilizations with complementary deities
- Coordinate for mutual defense and resource sharing
- Plan long-term strategies with allied religions

### PvP Optimization

- Unlock combat-focused blessings early if PvP-focused
- Travel in groups with religion members
- Use terrain and blessings to your advantage
- Remember: dying costs favor - pick your battles wisely

---

## Troubleshooting

**Q: I can't join a religion after leaving one**
A: There's a 7-day cooldown after leaving. Wait until the cooldown expires.

**Q: My favor isn't increasing**
A: Make sure you're performing activities aligned with your deity:
- **Khoras**: Mining ore, smithing at anvil, smelting metals
- **Lysa**: Hunting animals, foraging plants
- **Aethra**: Harvesting crops, cooking meals, planting
- **Gaia**: Crafting pottery, firing kilns, placing bricks

**Q: I can't unlock a blessing**
A: Check the requirements: Favor Rank, prerequisite blessings, and favor cost. All must be met.

**Q: The GUI won't open**
A: Make sure you're pressing `Shift+G` (not just `G`). Check for keybind conflicts with other mods.

**Q: Can I change deities without leaving my religion?**
A: No, deities are tied to religions. To switch deities, you must leave your religion and join one devoted to a different deity (subject to 7-day cooldown).

---

## FAQ

**Q: What happens to my favor if I switch religions?**
A: Your Favor Rank persists (based on lifetime favor earned), but you lose access to your previous deity's blessings. You'll need to unlock blessings for your new deity.

**Q: Can religions have multiple deities?**
A: No, each religion is devoted to a single deity. However, civilizations can unite religions of different deities.

**Q: How do I become a religion leader?**
A: Create your own religion to become its Founder, or be promoted by the current Founder to Elder rank.

**Q: Is there a maximum religion size?**
A: No hard cap, but coordination becomes harder with very large groups.

**Q: What's the best deity for beginners?**
A: It depends on your playstyle:
- **Khoras**: Easiest for beginners - mining and smithing are core Vintage Story activities
- **Lysa**: Good for players who enjoy hunting and wilderness survival
- **Aethra**: Perfect for players who focus on farming and cooking
- **Gaia**: Great for players who enjoy pottery crafting and building with clay

---

## Community & Support

- Report bugs and issues on the [GitHub repository](https://github.com/Quantumheart/DivineAscension)
- Share strategies and builds with other players
- Coordinate with your religion and civilization for better gameplay
- Experiment with different deity paths and blessing builds

**Good luck on your divine journey, and may your deity watch over you!**

---

*Last Updated: January 5, 2026*
*Divine Ascension v1.25.1*
