# Rank System Reference

**Last Updated:** 2026-01-14
**Version:** v4.6.0 (Prestige System Rebalance)

## Overview

The PantheonWars rank system consists of two parallel progression tracks:
1. **Player Favor Ranks** - Individual progression based on total favor earned
2. **Religion Prestige Ranks** - Group progression based on religion's total prestige

## Player Favor Ranks

Player favor ranks represent an individual player's dedication to their chosen deity. Ranks are determined by **total favor earned** (lifetime), not current spendable favor.

### Rank Thresholds

| Rank | Enum Value | Display Name | Total Favor Required | Rank Number |
|------|------------|--------------|---------------------|-------------|
| Initiate | `FavorRank.Initiate` | Initiate | **0 - 499** | 0 |
| Disciple | `FavorRank.Disciple` | Disciple | **500 - 1,999** | 1 |
| Zealot | `FavorRank.Zealot` | Zealot | **2,000 - 4,999** | 2 |
| Champion | `FavorRank.Champion` | Champion | **5,000 - 9,999** | 3 |
| Avatar | `FavorRank.Avatar` | Avatar | **10,000+** | 4 |

### Favor vs Total Favor Earned

- **Favor** (Current): Spendable currency used to unlock blessings
- **Total Favor Earned**: Lifetime total that determines rank (never decreases)

**Example:**
- Player earns 600 total favor
- Player spends 100 favor on blessings
- Current favor: 500
- Total favor earned: 600
- Rank: Disciple (Rank 1) - based on total earned, not current

### Progression Display

The UI displays progress toward the next rank:
- Rank 0 (Initiate): "Initiate (250/500)" - need 500 total to reach Rank 1
- Rank 1 (Disciple): "Disciple (1200/2000)" - need 2000 total to reach Rank 2
- Rank 2 (Zealot): "Zealot (3500/5000)" - need 5000 total to reach Rank 3
- Rank 3 (Champion): "Champion (7500/10000)" - need 10000 total to reach Rank 4
- Rank 4 (Avatar): "Avatar (15000/15000)" - max rank achieved

## Religion Prestige Ranks

Religion prestige ranks represent a religion's collective strength and influence. Prestige is earned by the entire religion and affects all members.

### Rank Thresholds

| Rank        | Enum Value                 | Display Name | Total Prestige Required | Rank Number |
|-------------|----------------------------|--------------|-------------------------|-------------|
| Fledgling   | `PrestigeRank.Fledgling`   | Fledgling    | **0 - 2,499**           | 0           |
| Established | `PrestigeRank.Established` | Established  | **2,500 - 9,999**       | 1           |
| Renowned    | `PrestigeRank.Renowned`    | Renowned     | **10,000 - 24,999**     | 2           |
| Legendary   | `PrestigeRank.Legendary`   | Legendary    | **25,000 - 49,999**     | 3           |
| Mythic      | `PrestigeRank.Mythic`      | Mythic       | **50,000+**             | 4           |

### Prestige Acquisition

Prestige is earned through:

- Member contributions (1:1 favor-to-prestige conversion for domain-aligned activities)
- PvP victories (75 prestige per kill, 112 prestige during war)
- Diplomatic alliances (500 prestige bonus upon formation)
- Religion-wide achievements

## Rank Requirements in Code

### Source Files

The authoritative rank calculations are defined in:

1. **`RankRequirements.cs`** - Central system for rank threshold lookups
   ```csharp
   GetRequiredFavorForNextRank(0) => 500    // Initiate → Disciple
   GetRequiredFavorForNextRank(1) => 2000   // Disciple → Zealot
   GetRequiredFavorForNextRank(2) => 5000   // Zealot → Champion
   GetRequiredFavorForNextRank(3) => 10000  // Champion → Avatar
   ```

2. **`PlayerReligionData.UpdateFavorRank()`** - Calculates player's current rank
   ```csharp
   TotalFavorEarned >= 10000 => FavorRank.Avatar
   TotalFavorEarned >= 5000  => FavorRank.Champion
   TotalFavorEarned >= 2000  => FavorRank.Zealot
   TotalFavorEarned >= 500   => FavorRank.Disciple
   Default                   => FavorRank.Initiate
   ```

3. **`FavorCommands.GetCurrentFavorRank()`** - Command system rank calculation (matches above)

### Blessing Requirements

Blessings use numeric rank values (0-4) for their `RequiredFavorRank` and `RequiredPrestigeRank` fields:

```csharp
RequiredFavorRank = 0  // Initiate tier
RequiredFavorRank = 1  // Devoted/Disciple tier
RequiredFavorRank = 2  // Zealot tier
RequiredFavorRank = 3  // Champion tier
RequiredFavorRank = 4  // Exalted/Avatar tier
```

## Rank Progression Examples

### Example 1: Solo Player
- Join religion, choose Khoras as deity
- Earn 250 favor from kills → Rank 0 (Initiate 250/500)
- Earn 250 more favor → Rank 1 (Disciple 500/2000)
- Can now unlock Disciple-tier blessings
- Spend 100 favor on blessing → Still Rank 1 (total earned: 500)

### Example 2: Religion Growth

- New religion starts → Prestige Rank 0 (Fledgling 0/2500)
- Members contribute → Prestige Rank 0 (Fledgling 1200/2500)
- More contributions → Prestige Rank 1 (Established 2500/10000)
- Can now unlock Established-tier religion blessings

## UI Display Conventions

### Header Display Format
```
Player Progress:  [Progress Bar] Disciple (1200/2000)
Religion Progress: [Progress Bar] Fledgling (1200/2500)
```

### Progress Bar Colors
- **Favor Progress**: Orange/Gold gradient
- **Prestige Progress**: Purple/Blue gradient

### Rank-Up Notifications
When a player ranks up, they receive:
1. Chat notification: "You have reached Disciple rank!"
2. UI update: Progress bar updates immediately
3. Blessing refresh: New blessings become available

## Common Issues & Troubleshooting

### Rank Not Updating
- **Symptom**: Player earns favor but rank stays at Initiate
- **Cause**: `PlayerReligionData.UpdateFavorRank()` was using incorrect thresholds
- **Fix**: Updated in commit `[COMMIT_HASH]` (2025-01-11)

### Blessings Not Unlocking
- **Symptom**: Player reaches required favor but blessings stay locked
- **Cause**: Rank calculation mismatch prevented rank-up
- **Fix**: Same as above - rank thresholds corrected

### Progress Bar Shows 0
- **Symptom**: Progress bar shows "Initiate (0/500)" despite having favor
- **Cause**: `TotalFavorEarned` not being sent in network packet
- **Fix**: Added `TotalFavorEarned` to `PlayerReligionDataPacket`

## Technical Implementation Notes

### Rank Update Flow

```
Server Side:
1. Player earns favor → AddFavor() or AddFractionalFavor()
2. PlayerReligionData.UpdateFavorRank() recalculates rank
3. OnPlayerDataChanged event fires
4. SendPlayerDataToClient() sends PlayerReligionDataPacket

Client Side:
1. Receive PlayerReligionDataPacket
2. Parse FavorRank enum name to numeric value
3. Update BlessingDialogManager with new rank
4. Refresh blessing states (unlock/lock based on new rank)
5. Update UI progress bars
```

### Real-Time Updates

The system uses event-driven updates to ensure UI stays synchronized:
- Favor changes (manual or passive) trigger immediate network updates
- Client receives packets and updates UI without requiring dialog refresh
- Progress bars and blessing availability update in real-time

## Related Documentation

- [Blessing System Reference](./blessing_reference.md)
- [Favor System Reference](./favor_reference.md)
- [Religion System Reference](./religion_reference.md)
- [UI Implementation Plan](../ui-design/BLESSING_UI_IMPLEMENTATION_PLAN.md)

## Version History

### v2.0 (2026-01-14)

- Updated for prestige system rebalancing (5x threshold scaling)
- New prestige thresholds: 2,500/10,000/25,000/50,000
- Added 1:1 favor-to-prestige conversion documentation
- Updated PvP rewards: 75 prestige (112 during war)
- Updated alliance bonus: 500 prestige

### v1.1 (2025-01-11)
- Corrected documentation to match planning documents
- Authoritative thresholds: 500/2000/5000/10000 total favor
- Added troubleshooting section for common rank issues

### v1.0 (2025-01-11)
- Initial documentation
