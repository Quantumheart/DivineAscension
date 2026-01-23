# Divine Ascension - Player Guide

**Welcome to Divine Ascension!** This guide will help you navigate the deity-driven religion and civilization systems in Vintage Story.

## Table of Contents

- [Quick Start](#quick-start)
- [The Deities](#the-deities)
- [Understanding Progression](#understanding-progression)
- [Holy Sites & Prayer](#holy-sites--prayer)
- [Creating & Joining Religions](#creating--joining-religions)
- [Earning Favor](#earning-favor)
- [Unlocking Blessings](#unlocking-blessings)
- [Civilizations](#civilizations)
- [Diplomacy](#diplomacy)
- [PvP & Combat](#pvp--combat)
- [Commands Reference](#commands-reference)
- [Admin Commands](#admin-commands)
- [GUI Overview](#gui-overview)
- [Advanced Tips](#advanced-tips)

---

## Quick Start

### First Steps

1. **Choose a deity** based on your playstyle (see [The Deities](#the-deities))
2. **Create or join a religion** using `/religion create` or `/religion join`
3. **Open the GUI** with `Shift+G` to view your progression and blessings
4. **Create a holy site** by placing an altar within your land claims
5. **Earn Favor** by performing deity-aligned activities and praying at holy sites
6. **Unlock Blessings** to gain powerful stat bonuses and effects

### Core Concepts

- **Favor**: Your personal progression with your deity (individual)
- **Prestige**: Your religion's collective reputation (shared by all members)
- **Holy Sites**: Sacred territories that amplify prayers and provide bonuses (tier-based)
- **Blessings**: Powerful upgrades unlocked using Favor points
- **Civilizations**: Multi-religion alliances for shared goals

---

## The Deities

Choose a deity that matches your playstyle. Each deity rewards different activities and offers unique blessings.

### Domains vs Deity Names

Divine Ascension uses a **domain-based** system:

- **Domain**: The mechanical archetype (Craft, Wild, Harvest, Stone) that determines game mechanics, blessings, and
  favor activities
- **Deity Name**: A customizable name for the deity your religion worships (e.g., "Khoras the Eternal Smith")

When creating a religion, you choose a **domain** and can give your deity any name you like. The default deity names are
Khoras, Lysa, Aethra, and Gaia, but founders can customize these with `/religion setdeityname`.

---

### 🔨 Craft Domain (Default Deity: Khoras)

**Focus**: Forge & Craft, Smithing, Tool-making, Durability
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
- Prayer at holy sites: 5 base favor (multiplied by holy site tier)
- PvP combat (all deities): 10 favor per kill
- Passive generation: 0.5 favor/hour

---

### 🏹 Wild Domain (Default Deity: Lysa)

**Focus**: Hunt, Wilderness, and Precision
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
- Prayer at holy sites: 5 base favor (multiplied by holy site tier)
- PvP combat (all deities): 10 favor per kill
- Passive generation: 0.5 favor/hour

---

### 🌾 Harvest Domain (Default Deity: Aethra)

**Focus**: Light, Agriculture, Growth, Warmth
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
- Prayer at holy sites: 5 base favor (multiplied by holy site tier)
- PvP combat (all deities): 10 favor per kill
- Passive generation: 0.5 favor/hour

---

### 🪨 Stone Domain (Default Deity: Gaia)

**Focus**: Pottery, Clay, Craftsmanship
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
- Prayer at holy sites: 5 base favor (multiplied by holy site tier)
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

| Rank            | Prestige Required | Unlocks                     |
|-----------------|-------------------|-----------------------------|
| **Fledgling**   | 0 - 2,499         | Basic religion blessings    |
| **Established** | 2,500 - 9,999     | Improved religion blessings |
| **Renowned**    | 10,000 - 24,999   | Advanced religion blessings |
| **Legendary**   | 25,000 - 49,999   | Elite religion blessings    |
| **Mythic**      | 50,000+           | Ultimate religion blessings |

**How Prestige is Earned**:

- Members performing deity-aligned activities (1:1 favor-to-prestige conversion)
- PvP victories (75 prestige per kill)
- Diplomatic alliances (500 prestige bonus)
- Contributions from all active members

---

## Holy Sites & Prayer

Holy Sites are sacred territories that amplify your connection to your deity. They provide prayer bonuses and are
automatically created when you place an altar within your land claims.

### What are Holy Sites?

Holy Sites are blessed areas that enhance your religious activities:

- **Automatic Creation**: Placing an altar automatically creates a holy site from your land claims
- **Tiered System**: Upgrade through rituals for greater bonuses (3 tiers)
- **Prayer Bonuses**: Multipliers to favor rewards when praying at consecrated altars
- **Territory-Based**: Based on land claim areas around the altar

### Holy Site Tiers

Holy Sites progress through three tiers by completing sacred rituals:

| Tier       | Type          | How to Achieve                              | Prayer Multiplier |
|------------|---------------|---------------------------------------------|-------------------|
| **Tier 1** | **Shrine**    | Created automatically when placing an altar | 2.0x              |
| **Tier 2** | **Temple**    | Complete the Tier 1→2 ritual                | 2.5x              |
| **Tier 3** | **Cathedral** | Complete the Tier 2→3 ritual                | 3.0x              |

**Ritual-Based Progression**:

- Each tier upgrade requires completing a multi-step ritual specific to your deity's domain
- Rituals consist of 3-5 steps that must be completed by offering sacred items at the altar
- Steps are discovered progressively as you offer matching items (they start hidden as "??? Undiscovered")
- Multiple players can contribute to the same ritual, making cooperation beneficial
- Steps can be completed in any order - the ritual finishes when all steps are complete
- Only the holy site founder (consecrator) can start or cancel rituals
- The prayer multiplier applies to all favor earned from prayers and offerings

### Creating a Holy Site

**Automatic Creation**:

1. Ensure you have active land claims in the area
2. Place a vanilla altar block anywhere within your claims
3. A holy site is automatically created using all your land claim areas
4. The site is named based on the religion's deity and a unique suffix

**Requirements**:

- You must be a member of a religion
- You must have at least one active land claim
- The altar must be placed within your land claims

**What Happens**:

- Holy site is created immediately at Tier 1 (Shrine)
- All your land claim areas within range are included
- Prayer multiplier is set based on tier (2.0x for Shrine)
- You'll receive a confirmation message
- Complete rituals to upgrade to higher tiers

### Removing a Holy Site

Holy sites are automatically removed when:

- The altar is destroyed (broken by any player)
- The land claims expire or are removed
- The religion is disbanded

**Deconsecration**:

- Breaking an altar automatically deconsecrates the holy site
- All associated bonuses are removed
- Players will be notified the site has been deconsecrated

### Rituals & Tier Upgrades

Transform your holy site from a humble shrine into a magnificent cathedral through sacred rituals.

**How Rituals Work**:

1. **Auto-Discovery**: Offer any sacred item at your altar - if it matches a ritual requirement, the ritual automatically starts!
2. **Step Discovery**: Rituals have 3-5 hidden steps. Each step is revealed when you offer a matching item.
3. **Progressive Completion**: Complete steps in any order by offering the required items.
4. **Cooperation**: Multiple players can contribute to the same ritual.
5. **Tier Upgrade**: When all steps are complete, your holy site advances to the next tier.

**Step States**:

- **Undiscovered** (???) - Hidden until you offer a matching item
- **Discovered** (☐) - Visible with checkbox, shows what's needed
- **Complete** (✓) - Step finished with green checkmark

**Ritual Offerings**:

- Award 50% of normal favor/prestige (encourages participation without replacing prayer)
- **No cooldown** - contribute as often as you have materials
- Items are consumed when offered (no refunds if ritual is cancelled)

**Founder Controls**:

- Only the holy site founder (altar placer) can cancel active rituals
- Cancelling a ritual provides no refunds - all contributed items are lost

**Viewing Progress**:

- Open the Divine Ascension UI (Shift+G)
- Navigate to Holy Sites tab
- Select your holy site to view active ritual progress
- Discovered steps show requirements and top contributors

### Prayer Mechanics

Pray at consecrated altars to earn favor from your deity.

**How to Pray**:

1. Find a consecrated altar (one that's part of a holy site)
2. Right-click the altar with an empty hand or while holding an offering
3. Receive favor based on the altar's tier and any offering

**Prayer Rewards**:

- **Base Favor**: 5 favor per prayer
- **Holy Site Multiplier**: 2.0x (Shrine), 2.5x (Temple), or 3.0x (Cathedral)
- **Offering Bonus**: Additional favor for valuable offerings (varies by item)
- **Total Example**: Cathedral (Tier 3, 3.0x) = 15 base favor per prayer

**Prayer Cooldown**:

- **1 hour** cooldown between prayers
- Cooldown is per-player, not per-altar
- You'll receive a message with remaining time if on cooldown

### Offerings

Enhance your prayers with offerings for bonus favor.

**How Offerings Work**:

1. Hold an offering item in your hand
2. Right-click the altar to pray
3. The offering is consumed
4. Receive bonus favor based on the offering's value

**Offering Categories**:

Offerings are categorized by type, with different favor bonuses:

- **Precious Metals**: Gold, silver nuggets and items (high favor)
- **Gemstones**: Diamonds, emeralds, precious gems (very high favor)
- **Food Items**: Cooked meals, bread, prepared foods (moderate favor)
- **Crafted Items**: Tools, weapons, armor (varies by quality)
- **Raw Materials**: Ores, ingots, refined materials (moderate favor)

**Tips**:

- Higher quality offerings provide more favor
- Offerings are multiplied by the holy site tier
- Plan your offerings around the 1-hour cooldown
- Save rare offerings for Cathedrals (Tier 3) for maximum 3.0x benefit

### Holy Site Commands

Manage and view holy sites with these commands:

| Command                      | Description                                                        |
|------------------------------|--------------------------------------------------------------------|
| `/holysite info [site_name]` | View details about a holy site (current location if no name given) |
| `/holysite list`             | List all holy sites belonging to your religion                     |
| `/holysite nearby [radius]`  | Show nearby holy sites within radius (default: 10 chunks)          |

**Examples**:

```
/holysite info                    # Show info for holy site at your current location
/holysite info "Temple of Khoras" # Show info for a specific site
/holysite list                    # List all your religion's holy sites
/holysite nearby 20               # Find holy sites within 20 chunks
```

**Holy Site Information Includes**:

- Site name and tier
- Prayer multiplier bonus
- Founder and creation date
- Center coordinates
- Altar position (for altar-based sites)
- Total volume and number of land claim areas

### Strategy Tips

**Maximizing Holy Site Benefits**:

1. **Complete Rituals**: Upgrade your holy site by completing domain-specific rituals
2. **Gather Ritual Materials**: Each tier upgrade requires specific items - explore and prepare
3. **Coordinate Contributions**: Multiple players can contribute to the same ritual
4. **Discover Steps**: Offer matching items to reveal hidden ritual steps
5. **Plan Upgrades**:
    - **Temple (Tier 2)**: Complete the Shrine → Temple ritual for your domain
    - **Cathedral (Tier 3)**: Complete the Temple → Cathedral ritual for maximum 3.0x multiplier

**Prayer Optimization**:

1. **Set Timers**: Track your 1-hour cooldown to maximize prayers
2. **Save Offerings**: Use best offerings at Cathedrals (Tier 3) for 3.0x multiplier
3. **Group Activities**: Coordinate with religion members for collective prayer sessions
4. **Regular Routine**: Build prayer into your daily gameplay loop

**Protection**:

- Holy sites are tied to land claims - maintain your claims to keep them active
- Altars can be broken by anyone - protect them with building design
- Consider multiple backup holy sites across your territory
- Larger holy sites are harder to fully remove (multiple claim areas)

---

## Creating & Joining Religions

### Creating a Religion

```
/religion create <name> <domain> <deityname> [visibility]
```

**Example**: `/religion create "Knights of the Forge" craft "Khoras the Eternal Smith" public`

**Parameters**:
- `name`: Your religion's name (use quotes if it contains spaces)
- `domain`: Choose from `craft`, `wild`, `harvest`, or `stone`
    - **Tip**: In the GUI creation screen, hover over a domain's icon to see detailed information about its
      playstyle and favor sources!
- `deityname`: The name of your deity (required, 2-48 characters, use quotes)
- `visibility`: `public` (anyone can join) or `private` (invite-only, defaults to public)

You can later change the deity name with `/religion setdeityname "New Name"`.

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

**Example**: `/religion join "Knights of Khoras"` or `/religion join TestReligion`

**Option 2 - Private Religions**:
Wait for an invitation, then accept via the GUI (`Shift+G`)

**Important Notes**:
- You can only be in one religion at a time
- Your Favor Rank persists across religion changes
- You'll lose access to your previous religion's blessings

### Leaving a Religion

```
/religion leave
```

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

### Craft Domain (Forge & Craft)

**Primary Activities**:
- ⛏️ **Mining**: Breaking ore blocks (2 favor per ore)
- 🔨 **Smithing**: Crafting items at the anvil (5-15 favor per craft)
- 🔥 **Smelting**: Processing ores in bloomeries and furnaces (1-8 favor per smelt)
- ⚔️ **PvP**: Combat victories (10 favor per kill)

**Tips**:
- Focus on deep mining expeditions for maximum ore collection
- Set up efficient smelting operations
- Craft complex items at the anvil for higher favor rewards

### Wild Domain (Hunt & Wilderness)

**Primary Activities**:
- 🏹 **Hunting**: Killing animals (3-20 favor per kill, varies by animal)
- 🌿 **Foraging**: Collecting plants, mushrooms, and natural resources (0.5 favor per harvest)
- ⚔️ **PvP**: Combat victories (10 favor per kill)

**Tips**:
- Go on regular hunting trips for high-value animals (wolves, bears)
- Gather from various biomes to find diverse forageable resources
- Maintain sustainable hunting practices

### Harvest Domain (Agriculture & Light)

**Primary Activities**:
- 🌾 **Harvesting Crops**: Breaking harvestable crops (1 favor per harvest)
- 🍞 **Cooking Meals**: Preparing food in firepits and crocks (3-8 favor per meal based on complexity)
- 🌱 **Planting Crops**: Planting seeds (0.5 favor per plant)
- ⚔️ **PvP**: Combat victories (10 favor per kill)

**Tips**:
- Set up large farms for consistent crop harvesting
- Cook complex meals (stews, pies, porridge) for maximum favor
- Automate farming operations with companions

### Stone Domain (Pottery & Clay)

**Primary Activities**:
- 🏺 **Crafting Pottery**: Forming clay items (2-5 favor per craft, varies by complexity)
- 🔥 **Firing Kilns**: Completing pit kiln firings (3-8 favor per firing)
- 🧱 **Placing Bricks**: Building with clay bricks (2 favor per brick)
- ⚔️ **PvP**: Combat victories (10 favor per kill)

**Tips**:
- Focus on crafting storage vessels (5 favor) and planters (4 favor)
- Batch fire pottery in pit kilns for efficient favor gains
- Build with clay bricks for both favor and base construction

### Prayer (All Domains)

**Primary Activities**:

- 🙏 **Prayer at Altars**: Visit consecrated altars within holy sites (5 base favor, multiplied by holy site tier)
- 🎁 **Offerings**: Bring valuable items to earn bonus favor (varies by item quality and tier)

**How Prayer Works**:

1. Place an altar within your land claims to create a holy site
2. Right-click the altar with an empty hand to pray (5 base favor × tier multiplier)
3. Hold an offering item to gain additional bonus favor
4. Wait 1 hour before you can pray again

**Tips**:

- Complete rituals to upgrade your holy site to Cathedral (Tier 3, 3.0x multiplier = 15 base favor)
- Save rare offerings for Cathedrals to maximize the multiplier
- Set a timer to track your 1-hour cooldown
- Coordinate prayer sessions with your religion for group activities
- Contribute to rituals together - multiple players can help complete steps faster
- See the [Holy Sites & Prayer](#holy-sites--prayer) section for complete details

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

Civilizations are alliances of **1-4 religions** with different domains working together.

### Benefits

- Shared identity and coordination
- Multi-domain cooperation
- Potential for larger-scale politics and warfare
- Prestige bonuses for alliance formation

### Creating a Civilization

```
/civilization create <name>
```

**Example**: `/civilization create "Alliance of Light"` or `/civilization create TestCiv`

**Requirements**:
- You must be the **Founder** of your religion
- Your religion must not already be in a civilization

### Inviting Religions

```
/civilization invite <religion_name>
```

**Example**: `/civilization invite "Knights of Khoras"`

**Notes**:
- Invitations expire after **7 days**
- Target religion must have a different domain
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
- **Effects**: Standard PvP rules apply (10 favor, 75 prestige per kill)

#### 2. Non-Aggression Pact (NAP)

- **Description**: Formal agreement not to attack each other
- **Requirements**:
    - Both civilizations must be at least **Established** rank (2,500+ prestige)
    - Must be proposed and accepted by both civilization founders
- **Duration**: **3 days** (real-time), then automatically reverts to Neutral
- **Effects**:
    - Attacking allied civilization members results in **violations** (see Violation System)
    - No favor/prestige rewards for attacking NAP members
    - Can be broken with 24-hour notice (see Breaking Treaties)

#### 3. Alliance

- **Description**: Strong alliance between civilizations
- **Requirements**:
    - Both civilizations must be at least **Renowned** rank (10,000+ prestige)
    - Must be proposed and accepted by both civilization founders
- **Duration**: **Permanent** until broken or war is declared
- **Effects**:
    - **500 prestige** awarded to ALL religions in BOTH civilizations upon formation
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
    - **1.5x multiplier** on favor and prestige for PvP kills (15 favor, 112 prestige per kill)
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
- Target civilization must have a **different domain** than yours
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
- PvP kills reward **1.5x favor and prestige** (15 favor, 112 prestige)

### Declaring Peace

To end a war, either side can declare peace:

1. **Via GUI**: Click "Declare Peace" next to the War relationship
2. **Via Command**: `/peace <civilization_id>` or `/diplomacy declarepeace <civilization_id>`

**Effects**:

- Both civilizations return to **Neutral** status
- Normal PvP rewards resume (10 favor, 75 prestige)

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

- Focus on reaching **Renowned rank** (10,000 prestige) to unlock alliances
- Coordinate with complementary domain civilizations
- Alliance prestige bonus (500 to all religions in both civs) is significant
- Build trust before proposing - broken treaties damage reputation

**Using NAPs**:

- Great for temporary peace during resource gathering
- Remember they expire in **3 days** - coordinate renewal if needed
- Lower prestige requirement (Established/2,500) makes them accessible early

**Declaring War**:

- Use strategically for **1.5x PvP rewards** (15 favor, 112 prestige per kill)
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
- **75 Prestige** to the attacker's religion (112 prestige during war)

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

> **Tip: Names with Spaces**
> For religion or civilization names containing spaces, wrap the name in quotes:
> - `/religion join "Knights of Khoras"`
> - `/civilization create "Empire of the Sun"`
>
> Single-word names work without quotes: `/religion join TestReligion`

### Religion Commands

| Command                                                     | Description                                        |
|-------------------------------------------------------------|----------------------------------------------------|
| `/religion create <name> <domain> <deityname> [visibility]` | Create a new religion                              |
| `/religion join <name>`                                     | Join a public religion                             |
| `/religion leave`                                           | Leave your current religion                        |
| `/religion list [domain]`                                   | List all religions (optionally filter by domain)   |
| `/religion setdeityname "name"`                             | Set your religion's deity name (founder only)      |
| `/religion info <name>`                                     | Show detailed religion information                 |
| `/religion members`                                         | View members of your religion                      |
| `/religion invite <player>`                                 | Invite a player to your religion (founder/elder)   |
| `/religion kick <player>`                                   | Remove a player from your religion (founder/elder) |
| `/religion ban <player> [reason] [days]`                    | Ban a player from your religion                    |
| `/religion unban <player>`                                  | Unban a player                                     |
| `/religion banlist`                                         | View banned players                                |
| `/religion disband`                                         | Permanently delete your religion (founder only)    |

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

### Holy Site Commands

| Command                      | Description                                                        |
|------------------------------|--------------------------------------------------------------------|
| `/holysite info [site_name]` | View details about a holy site (current location if no name given) |
| `/holysite list`             | List all holy sites belonging to your religion                     |
| `/holysite nearby [radius]`  | Show nearby holy sites within radius (default: 10 chunks)          |

---

## Admin Commands

**Note**: All admin commands require **server administrator privileges** (`root` permission). These commands bypass
normal restrictions and are designed for server management and troubleshooting.

### Blessing Admin Commands

Admin commands for managing player blessings, bypassing all validation requirements (rank, deity, prerequisites).

| Command                                             | Description                                                |
|-----------------------------------------------------|------------------------------------------------------------|
| `/blessings admin unlock <blessingid> [playername]` | Unlock a blessing for a player, bypassing all requirements |
| `/blessings admin lock <blessingid> [playername]`   | Remove/lock a blessing from a player                       |
| `/blessings admin reset [playername]`               | Clear all unlocked blessings from a player                 |
| `/blessings admin unlockall [playername]`           | Unlock all blessings for the player's deity                |

**Player Targeting**:

- If `[playername]` is **omitted**, the command targets yourself (the admin)
- If `[playername]` is **provided**, the command targets that specific player

**Examples**:

```
/blessings admin unlock khoras_mining_speed_1 Alice
/blessings admin lock khoras_health_boost_2 Bob
/blessings admin reset Charlie
/blessings admin unlockall
```

**Important Notes**:

- **Unlock** bypasses rank requirements, deity matching, and prerequisite checks
- **Lock** removes the blessing if it was unlocked; shows friendly message if not unlocked
- **Reset** clears all player blessings (useful for testing or correcting errors)
- **UnlockAll** unlocks ALL player blessings for the target's current deity (religion blessings require founder)

### Religion Admin Commands

Admin commands for managing religion membership and fixing data issues.

| Command                                            | Description                                      |
|----------------------------------------------------|--------------------------------------------------|
| `/religion admin repair [playername]`              | Repair religion data for a player or all players |
| `/religion admin join <religionname> [playername]` | Force a player to join a religion                |
| `/religion admin leave [playername]`               | Force a player to leave their religion           |

**Examples**:

```
/religion admin repair
/religion admin repair Alice
/religion admin join "Knights of Khoras" Bob
/religion admin leave Charlie
```

**Repair Command**:

- If `[playername]` is **omitted**: Repairs data for ALL players
- If `[playername]` is **provided**: Repairs data for that specific player
- Fixes inconsistencies in religion membership data

**Join Command**:

- Bypasses all restrictions (religion visibility, bans, capacity)
- If player is already in a religion, they are automatically removed first
- **No favor penalty** is applied (admin command skips the normal religion switching penalty)
- Invitations are automatically cleared

**Leave Command**:

- **Special Founder Handling**:
    - If target is founder **with other members**: Founder role is transferred to the oldest member
    - If target is founder **and sole member**: Religion is automatically disbanded
- Otherwise: Player is simply removed from the religion

### Civilization Admin Commands

Admin commands for managing civilizations.

| Command                                                                       | Description                                                     |
|-------------------------------------------------------------------------------|-----------------------------------------------------------------|
| `/civ admin create <civname> <religion1> [religion2] [religion3] [religion4]` | Create a civilization with 1-4 religions                        |
| `/civ admin dissolve <civname>`                                               | Force-disband a civilization                                    |
| `/civ admin cleanup`                                                          | Remove orphaned civilizations (civilizations with no religions) |

**Examples**:

```
/civ admin create "Northern Alliance" "Knights of Khoras" "Hunters of Lysa"
/civ admin dissolve "Southern Coalition"
/civ admin cleanup
```

**Create Command**:

- Accepts **1-4 religion names** (use quotes if names contain spaces)
- Validates all religions exist
- **Enforces domain uniqueness**: Cannot create a civilization with duplicate domains (returns error listing conflicts)
- First religion's founder becomes the civilization founder
- Automatically adds all specified religions to the civilization

**Dissolve Command**:

- Bypasses permission checks (no need to be founder)
- Automatically cleans up pending invitations
- Notifies all member religion founders

**Cleanup Command**:

- Scans all civilizations for those with zero member religions
- Automatically disbands orphaned civilizations
- Reports the number of civilizations cleaned up

### Favor Admin Commands

Admin commands for managing player favor. *(Already documented in previous version - included for reference)*

| Command                                 | Description                                         |
|-----------------------------------------|-----------------------------------------------------|
| `/favor set <amount> [playername]`      | Set a player's current favor to an exact amount     |
| `/favor add <amount> [playername]`      | Add favor to a player                               |
| `/favor remove <amount> [playername]`   | Remove favor from a player                          |
| `/favor reset [playername]`             | Reset a player's favor to 0                         |
| `/favor max [playername]`               | Set a player's favor to maximum (10,000)            |
| `/favor settotal <amount> [playername]` | Set a player's lifetime favor earned (affects rank) |

**Examples**:

```
/favor set 500 Alice
/favor add 100 Bob
/favor remove 50 Charlie
/favor max
```

### Admin Command Best Practices

**When to Use Admin Commands**:

- **Testing**: Rapidly test blessing combinations or game mechanics
- **Bug Fixes**: Repair corrupted player data or religion memberships
- **Events**: Set up special scenarios or events for players
- **Recovery**: Restore lost progress due to bugs or server issues

**Caution**:

- Admin commands **bypass all game balance** - use sparingly to preserve gameplay integrity
- Always **back up save data** before running mass operations (like `/religion admin repair` without a player name)
- Consider **informing players** when making changes that affect them
- Use **`/favor settotal`** carefully - changing lifetime favor affects Favor Rank permanently

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

**4. Holy Sites Tab**
- **List**: View all holy sites belonging to your religion
- **Details**: Select a site to view tier, prayer multiplier, and location
- **Rituals**: View active ritual progress and step completion
- **Contributors**: See who has contributed to ongoing rituals

### Interface Tips

- Hover over blessings to see detailed tooltips
- Icons indicate blessing types and deity associations
- Color coding shows unlock status and requirements
- Use filters to find specific content quickly

---

## Advanced Tips

### Optimal Favor Farming

**Craft Domain Followers**:
1. Set up automated smelting operations for passive favor
2. Create a deep mine with multiple ore veins
3. Combine mining sessions with smithing batches
4. Focus on complex anvil crafts (10-15 favor each)
5. Build a Cathedral (Tier 3) near your workshop for efficient prayers (15 favor per hour)

**Wild Domain Followers**:
1. Find areas with high animal spawns (wolves/bears = 12-15 favor)
2. Create hunting routes through multiple biomes
3. Forage consistently while traveling (0.5 favor adds up)
4. Balance hunting with sustainable foraging
5. Build a Tier 3 holy site at your hunting lodge for prayers (15 favor per hour)

**Harvest Domain Followers**:
1. Build large automated crop farms for mass harvesting
2. Cook complex meals (stews, pies, porridge) for 5-8 favor each
3. Plant in bulk during planting season
4. Set up cooking stations near farms for efficiency
5. Build a Tier 3 holy site at your farm for prayers with food offerings (15+ favor per hour)

**Stone Domain Followers**:
1. Mass-produce storage vessels (5 favor each) and planters (4 favor)
2. Batch fire pottery in pit kilns for efficient favor
3. Build with clay bricks (2 favor per brick) for dual-purpose construction
4. Keep clay digging and pottery crafting as regular routines
5. Build a Tier 3 holy site at your pottery workshop for prayers (15 favor per hour)

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

- Form civilizations with complementary domains
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
A: Make sure the religion you're trying to join is public, or that you have a valid invitation if it's private.

**Q: My favor isn't increasing**
A: Make sure you're performing activities aligned with your domain:

- **Craft**: Mining ore, smithing at anvil, smelting metals
- **Wild**: Hunting animals, foraging plants
- **Harvest**: Harvesting crops, cooking meals, planting
- **Stone**: Crafting pottery, firing kilns, placing bricks

**Q: I can't unlock a blessing**
A: Check the requirements: Favor Rank, prerequisite blessings, and favor cost. All must be met.

**Q: The GUI won't open**
A: Make sure you're pressing `Shift+G` (not just `G`). Check for keybind conflicts with other mods.

**Q: Can I change domains without leaving my religion?**
A: No, domains are tied to religions. To switch domains, you must leave your religion and join one following a
different domain.

**Q: My altar isn't creating a holy site**
A: Make sure:

- You are a member of a religion
- You have active land claims in the area
- The altar is placed within your land claim boundaries
- Your land claims haven't expired

**Q: Prayer isn't working at my altar**
A: Check these requirements:

- The altar must be part of a holy site (use `/holysite info` while standing near it)
- You must be a member of the religion that owns the holy site
- You must not be on cooldown (1 hour between prayers)
- The holy site hasn't been deconsecrated (altar destroyed)

**Q: How do I increase my holy site tier?**
A: Complete sacred rituals to upgrade your holy site. Each domain has specific rituals for tier upgrades:

- **Tier 1 (Shrine)**: Created automatically when you place an altar in your land claims
- **Tier 2 (Temple)**: Complete the Shrine → Temple ritual by offering required items
- **Tier 3 (Cathedral)**: Complete the Temple → Cathedral ritual for maximum bonuses

Rituals have 3-5 steps that start hidden. Offer matching items at the altar to discover steps and progress. Multiple
players can contribute to the same ritual.

---

## FAQ

**Q: What happens to my favor if I switch religions?**
A: Your Favor Rank persists (based on lifetime favor earned), but you lose access to your previous domain's blessings.
You'll need to unlock blessings for your new domain.

**Q: Can religions have multiple domains?**
A: No, each religion follows a single domain. However, civilizations can unite religions of different domains.

**Q: Can I customize my deity's name?**
A: Yes! Religion founders can use `/religion setdeityname "Your Deity Name"` to give their deity a custom name.

**Q: How do I become a religion leader?**
A: Create your own religion to become its Founder, or be promoted by the current Founder to Elder rank.

**Q: Is there a maximum religion size?**
A: No hard cap, but coordination becomes harder with very large groups.

**Q: What's the best domain for beginners?**
A: It depends on your playstyle:

- **Craft**: Easiest for beginners - mining and smithing are core Vintage Story activities
- **Wild**: Good for players who enjoy hunting and wilderness survival
- **Harvest**: Perfect for players who focus on farming and cooking
- **Stone**: Great for players who enjoy pottery crafting and building with clay

---

## Community & Support

- Report bugs and issues on the [GitHub repository](https://github.com/Quantumheart/DivineAscension)
- Share strategies and builds with other players
- Coordinate with your religion and civilization for better gameplay
- Experiment with different domain paths and blessing builds

**Good luck on your divine journey, and may your deity watch over you!**

---

*Last Updated: January 21, 2026*
*Divine Ascension v1.0.0 - Holy Sites & Prayer System*
