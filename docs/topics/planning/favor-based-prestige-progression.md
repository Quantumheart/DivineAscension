# Favor-Based Prestige Progression Implementation Plan

> **⚠️ SUPERSEDED**: This document describes the original implementation (10:1 conversion, 15 PvP prestige).
> **Current System** (v4.6.0): **1:1 conversion**, **75 PvP prestige** (112 during war), **5x threshold scaling**
> See [Prestige System Rebalance (v4.6.0)](#v460-rebalance) for current values.

## Problem Statement

**Current State**: Religion prestige only progresses through PvP kills (75 prestige per kill), creating a PvP-heavy
progression path that excludes peaceful players.

**Goal**: Add deity-specific prestige progression through favor-earning activities, making peaceful playstyles equally viable to PvP.

## Design Requirements (User-Specified)

1. **Equal Viability**: Both PvP and peaceful activities should contribute equally to prestige
2. **Deity-Specific**: Only activities matching a deity's theme grant prestige (Khoras: crafting, Lysa: hunting, Aethra: farming, Gaia: building)
3. **Direct Conversion**: 1 favor earned = 1 prestige awarded (simple, understandable ratio)
4. **No Artificial Limits**: Trust the conversion rate for balance (no caps or diminishing returns)

## Architecture Decision: Centralized Conversion in FavorSystem

**Rationale**: FavorSystem already orchestrates all favor awarding through `AwardFavorForAction()`. Adding prestige conversion at this central point:
- Maintains single source of truth for conversion logic
- Avoids code duplication across 8 favor trackers
- Enables consistent deity filtering
- Follows existing architecture patterns

## Implementation Approach

### Phase 1: Add Prestige Manager Dependency

**File**: `PantheonWars/Systems/FavorSystem.cs`

**Changes**:
1. Add private field: `private readonly IReligionPrestigeManager _prestigeManager;`
2. Update constructor to inject `IReligionPrestigeManager prestigeManager` parameter
3. Assign in constructor: `_prestigeManager = prestigeManager;`
4. Update dependency injection registration (in ModSystem) to include this dependency

### Phase 2: Implement Deity-Activity Mapping

**File**: `PantheonWars/Systems/FavorSystem.cs`

**Add new method**:
```csharp
/// <summary>
/// Determines if an activity is deity-themed and should grant prestige
/// </summary>
private bool ShouldAwardPrestigeForActivity(DeityType deity, string actionType)
{
    string actionLower = actionType.ToLowerInvariant();

    // Exclude PvP - already handled by PvPManager
    if (actionLower.Contains("pvp") || actionLower.Contains("kill"))
        return false;

    // Exclude passive favor - not deity-themed activity
    if (actionLower.Contains("passive") || actionLower.Contains("devotion"))
        return false;

    return deity switch
    {
        DeityType.Khoras =>
            actionLower.Contains("mining") ||
            actionLower.Contains("smithing") ||
            actionLower.Contains("smelting") ||
            actionLower.Contains("anvil"),

        DeityType.Lysa =>
            actionLower.Contains("hunting") ||
            actionLower.Contains("foraging") ||
            actionLower.Contains("exploration"),

        DeityType.Aethra =>
            actionLower.Contains("harvest") ||
            actionLower.Contains("planting") ||
            actionLower.Contains("cooking"),

        DeityType.Gaia =>
            actionLower.Contains("pottery") ||
            actionLower.Contains("brick") ||
            actionLower.Contains("clay"),

        _ => false
    };
}
```

### Phase 3: Modify Integer AwardFavorForAction Method

**File**: `PantheonWars/Systems/FavorSystem.cs` (lines 200-213)

**Update method** `AwardFavorForAction(string playerUid, string actionType, int amount)`:

```csharp
public void AwardFavorForAction(string playerUid, string actionType, int amount)
{
    var religionData = _playerReligionDataManager.GetOrCreatePlayerData(playerUid);

    if (religionData.ActiveDeity == DeityType.None) return;

    // Award favor (existing logic)
    _playerReligionDataManager.AddFavor(playerUid, amount, actionType);

    // Award prestige if deity-themed activity and player is in a religion
    if (!string.IsNullOrEmpty(religionData.ReligionUID) &&
        ShouldAwardPrestigeForActivity(religionData.ActiveDeity, actionType))
    {
        int prestigeAmount = amount / 10; // 10:1 conversion
        if (prestigeAmount > 0)
        {
            try
            {
                var player = _sapi.World.PlayerByUid(playerUid);
                string playerName = player?.PlayerName ?? playerUid;
                _prestigeManager.AddPrestige(
                    religionData.ReligionUID,
                    prestigeAmount,
                    $"{actionType} by {playerName}"
                );
            }
            catch (Exception ex)
            {
                _sapi.Logger.Error($"[FavorSystem] Failed to award prestige: {ex.Message}");
                // Don't fail favor award if prestige fails
            }
        }
    }

    // Existing message code (lines 208-212)
    var player = _sapi.World.PlayerByUid(playerUid) as IServerPlayer;
    if (player != null)
    {
        AwardFavorMessage(player, actionType, amount, religionData);
    }
}
```

### Phase 4: Modify Float AwardFavorForAction Method

**File**: `PantheonWars/Systems/FavorSystem.cs` (lines 347-360)

**Update method** `AwardFavorForAction(string playerUid, string actionType, float amount)`:

```csharp
public void AwardFavorForAction(string playerUid, string actionType, float amount)
{
    var religionData = _playerReligionDataManager.GetOrCreatePlayerData(playerUid);

    if (religionData.ActiveDeity == DeityType.None) return;

    // Award favor (existing logic)
    _playerReligionDataManager.AddFractionalFavor(playerUid, amount, actionType);

    // Award prestige if deity-themed activity and player is in a religion
    if (!string.IsNullOrEmpty(religionData.ReligionUID) &&
        ShouldAwardPrestigeForActivity(religionData.ActiveDeity, actionType))
    {
        float prestigeAmount = amount / 10f; // 10:1 conversion
        if (prestigeAmount >= 1.0f) // Only award whole prestige points
        {
            try
            {
                var player = _sapi.World.PlayerByUid(playerUid);
                string playerName = player?.PlayerName ?? playerUid;
                _prestigeManager.AddPrestige(
                    religionData.ReligionUID,
                    (int)prestigeAmount,
                    $"{actionType} by {playerName}"
                );
            }
            catch (Exception ex)
            {
                _sapi.Logger.Error($"[FavorSystem] Failed to award prestige: {ex.Message}");
                // Don't fail favor award if prestige fails
            }
        }
    }

    // Existing message code (lines 355-359)
    var player = _sapi.World.PlayerByUid(playerUid) as IServerPlayer;
    if (player != null)
    {
        AwardFavorMessage(player, actionType, amount, religionData);
    }
}
```

### Phase 5: Update Dependency Injection

**File**: Find where `FavorSystem` is registered (likely `PantheonWarsModSystem.cs` or similar)

**Update registration** to include `IReligionPrestigeManager` dependency:
```csharp
// Before
serviceCollection.AddSingleton<IFavorSystem>(provider =>
    new FavorSystem(
        sapi,
        provider.GetService<IPlayerDataManager>(),
        provider.GetService<IPlayerReligionDataManager>(),
        provider.GetService<IDeityRegistry>(),
        provider.GetService<IReligionManager>()
    ));

// After
serviceCollection.AddSingleton<IFavorSystem>(provider =>
    new FavorSystem(
        sapi,
        provider.GetService<IPlayerDataManager>(),
        provider.GetService<IPlayerReligionDataManager>(),
        provider.GetService<IDeityRegistry>(),
        provider.GetService<IReligionManager>(),
        provider.GetService<IReligionPrestigeManager>() // NEW
    ));
```

## Balance Analysis

### Expected Prestige Rates

**PvP (via PvPManager.cs)**:
- Rival deity kill: 30 prestige + 2 favor (20 favor / 10) = ~32 prestige total
- Neutral deity kill: 15 prestige (no favor conversion needed)
- Allied deity kill: 7.5 prestige
- Estimated: ~120-160 prestige/hour with active PvP

**Peaceful Activities (with 10:1 conversion)**:
- **Khoras** (mining/smithing): ~100-150 prestige/hour
- **Lysa** (hunting/foraging): ~80-120 prestige/hour
- **Aethra** (farming/cooking): ~60-100 prestige/hour
- **Gaia** (pottery/building): ~70-110 prestige/hour

**Verdict**: Peaceful activities achieve 40-95% of PvP efficiency, which is balanced because:
1. Peaceful activities have lower risk
2. Peaceful activities produce valuable resources
3. Different deities naturally have different efficiency (intentional design)
4. Mixed playstyles (PvP + peaceful) are encouraged

## Edge Cases Handled

### 1. PvP Double-Award Prevention
**Issue**: PvPManager already awards prestige for kills. FavorSystem also awards favor for kills.
**Solution**: Filter checks for "pvp" and "kill" in action type, preventing conversion of PvP favor to prestige.
**Verification**: PvPManager.ProcessPvPKill() (line 123-127) awards both favor and prestige separately.

### 2. Passive Favor Exclusion
**Issue**: Passive favor generation (0.5/hour) is not deity-themed activity.
**Solution**: Filter checks for "passive" and "devotion" in action type.
**Verification**: FavorSystem.AwardPassiveFavor() (line 285) uses action type "Passive devotion".

### 3. Fractional Prestige
**Issue**: Small activities award fractional favor (e.g., 0.5 favor). Should we track fractional prestige?
**Solution**: Only award prestige when favor amount reaches 10+ (for integers) or when conversion results in ≥1.0 prestige (for floats).
**Rationale**: Simpler implementation, small loss acceptable (players still get favor).

### 4. Player Without Religion
**Issue**: Player has deity but hasn't joined a religion.
**Solution**: Check `religionData.ReligionUID != null` before awarding prestige.
**Rationale**: Can't award prestige without a religion to award it to.

### 5. Automation Penalties
**Issue**: AnvilFavorTracker applies 35% penalty for automation. Should this affect prestige?
**Solution**: Yes, automatically handled. Reduced favor → reduced prestige via conversion.
**Rationale**: Consistent penalty across both progression systems.

## Critical Files Modified

1. **`PantheonWars/Systems/FavorSystem.cs`** - Add prestige awarding logic, inject dependency, implement filtering
2. **`PantheonWarsModSystem.cs`** (or equivalent DI setup file) - Update FavorSystem registration with new dependency

## Testing Checklist

### Unit Tests
- [ ] Verify 10:1 conversion rate (10 favor → 1 prestige, 100 favor → 10 prestige)
- [ ] Verify deity filtering (Khoras mining → prestige, Khoras hunting → no prestige)
- [ ] Verify PvP exclusion (PvP favor → no prestige conversion)
- [ ] Verify passive favor exclusion (passive favor → no prestige)
- [ ] Verify player without religion → no prestige awarded
- [ ] Verify fractional accumulation (9 favor → 0 prestige, 10 favor → 1 prestige)

### Integration Tests
- [ ] Full workflow: Join religion → perform deity activity → verify favor + prestige awarded
- [ ] Multi-deity test: Different deities earning prestige simultaneously
- [ ] Religion rank progression: Verify prestige accumulation leads to rank-ups

### Manual Testing
- [ ] Create religion for each deity (Khoras, Lysa, Aethra, Gaia)
- [ ] Perform 100 favor worth of themed activities per deity
- [ ] Verify 10 prestige was awarded to each religion
- [ ] Perform 100 favor worth of non-themed activities
- [ ] Verify 0 prestige was awarded for non-themed activities
- [ ] Verify PvP still awards prestige via PvPManager (not double-awarded)
- [ ] Verify passive favor does not award prestige
- [ ] Check religion rank progression UI updates correctly

## Implementation Sequence

1. **Add dependency** - Update FavorSystem constructor and DI registration
2. **Add filtering method** - Implement `ShouldAwardPrestigeForActivity()`
3. **Update int method** - Modify `AwardFavorForAction(string, string, int)`
4. **Update float method** - Modify `AwardFavorForAction(string, string, float)`
5. **Test thoroughly** - Verify all edge cases and balance

## Risk Mitigation

**Risk**: Performance impact from additional prestige awards
**Mitigation**: Prestige operations are already happening for PvP. The filter is lightweight (string comparison). Monitor server performance.

**Risk**: Retroactive prestige expectations
**Mitigation**: Document that this is new functionality. Existing religions start from current prestige baseline.

**Risk**: Notification spam
**Mitigation**: Don't add new notifications. Favor notifications already exist. Religion rank-up notifications already exist.

## Success Criteria

✅ Peaceful players can progress religion prestige at comparable rates to PvP
✅ Only deity-themed activities grant prestige (filtering works correctly)
✅ 10:1 conversion rate is consistent and balanced
✅ No double-awarding of prestige for PvP
✅ No prestige from passive favor
✅ Code is maintainable and follows existing patterns

---

**Estimated Implementation Time**: 1-2 hours
**Lines of Code Changed**: ~100-120 lines across 2 files
**Risk Level**: Low (well-understood changes to existing system)

---

## v4.6.0 Rebalance

**Date**: 2026-01-14
**Status**: ✅ Implemented

### Changes From Original Plan

1. **Conversion Ratio**: Changed from 10:1 to **1:1** (1 favor = 1 prestige)
2. **PvP Rewards**: Increased from 15 to **75 prestige** per kill (112 during war)
3. **Alliance Bonus**: Increased from 100 to **500 prestige**
4. **Prestige Thresholds**: Scaled by 5x to maintain progression balance
   - Fledgling: 0 (unchanged)
   - Established: 500 → **2,500**
   - Renowned: 2,000 → **10,000**
   - Legendary: 5,000 → **25,000**
   - Mythic: 10,000 → **50,000**

### Rationale

The 10:1 conversion resulted in most small activities awarding **0 prestige** due to truncation (e.g., 1 favor = 0.1
prestige → 0). The 1:1 conversion ensures **all activities award whole prestige numbers**, providing immediate, tangible
feedback to players.

### Impact

- **Before**: Mining 1 copper ore → 1 favor, 0 prestige ✗
- **After**: Mining 1 copper ore → 1 favor, 1 prestige ✓

Players now see progress on **every activity**, maintaining engagement and reward feedback loops.
