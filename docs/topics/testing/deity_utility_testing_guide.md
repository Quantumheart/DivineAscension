# Deity Utility System - Comprehensive Testing Guide

**Version:** 1.0
**Last Updated:** 2025-12-02
**Related Documentation:** [Deity Utility Migration Plan](../planning/deity_utility_migration_plan.md)

---

## Table of Contents

1. [Introduction](#introduction)
2. [Testing Infrastructure](#testing-infrastructure)
3. [Phase 1: Khoras Testing](#phase-1-khoras-testing)
4. [Phase 2: Lysa Testing](#phase-2-lysa-testing)
5. [Phase 3: Aethra Testing](#phase-3-aethra-testing)
6. [Phase 4: Gaia Testing](#phase-4-gaia-testing)
7. [Cross-Cutting Testing](#cross-cutting-testing)
8. [Test Implementation Examples](#test-implementation-examples)
9. [Success Criteria](#success-criteria)

---

## Introduction

### Purpose and Scope

This document provides comprehensive testing specifications for the **Deity Utility System Migration**, which transitions PantheonWars from an 8-deity combat-focused system to a 4-deity utility-focused system. This guide serves as the single source of truth for all testing activities related to the migration.

**Target Audience:**
- QA engineers performing manual testing
- Developers implementing unit and integration tests
- Server administrators validating deployments

**Related Documentation:**
- **Implementation Details:** [Deity Utility Migration Plan](../planning/deity_utility_migration_plan.md)
- **Deity Design:**
  - [Khoras Forge Blessings](../reference/khoras_forge_blessings.md)
  - [Lysa Hunt Blessings](../reference/lysa_hunt_blessings.md)
  - [Aethra Agriculture Blessings](../reference/aethra_agriculture_blessings.md)
  - [Gaia Pottery Blessings](../reference/gaia_pottery_blessings.md)

### Testing Philosophy

The deity utility migration follows a **phased, one-deity-at-a-time approach** to minimize risk and allow thorough testing between phases. Each phase requires:

1. **Unit Tests:** Verify individual components work correctly in isolation
2. **Integration Tests:** Verify components work together correctly
3. **Manual Tests:** Verify in-game behavior matches specifications
4. **Performance Tests:** Verify system scales without degradation

**Key Principles:**
- ✅ Test early and often - don't wait until phase completion
- ✅ Automate repetitive tests - manual testing for user experience only
- ✅ Test edge cases and error conditions
- ✅ Verify isolation - one deity's favor shouldn't affect another
- ✅ Performance matters - event handlers must not cause lag

### Coverage Targets

Based on existing PantheonWars testing standards:

| Component Type | Target Coverage |
|----------------|-----------------|
| Favor Trackers | 85%+ |
| Blessing Definitions | 90%+ |
| Special Effect Handlers | 85%+ |
| Patches (Harmony) | 75%+ |
| Integration Workflows | 80%+ |

**Coverage Tools:**
```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coveragereport
```

---

## Testing Infrastructure

### Framework Overview

**Test Framework:** xUnit.net v3.1.0
**Mocking Framework:** Moq v4.20.72
**Target Framework:** .NET 8.0

**Key Dependencies:**
```xml
<PackageReference Include="xunit.v3" Version="3.1.0"/>
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.5"/>
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0"/>
<PackageReference Include="Moq" Version="4.20.72"/>
<PackageReference Include="coverlet.collector" Version="6.0.0"/>
```

### TestFixtures Pattern

All tests in PantheonWars use the **TestFixtures** pattern for consistent mock creation and test data setup. The `TestFixtures` class provides factory methods for:

**Mock API Builders:**
- `CreateMockCoreAPI()` - Basic ICoreAPI with logger
- `CreateMockServerAPI()` - Full server API with world and events
- `CreateMockClientAPI()` - Client-side API

**Mock System Interfaces:**
- `CreateMockDeityRegistry()` - Pre-configured with deities
- `CreateMockPlayerDataManager()`
- `CreateMockPlayerReligionDataManager()`
- `CreateMockReligionManager()`
- `CreateMockBlessingRegistry()`
- `CreateMockFavorSystem()`

**Test Data Builders:**
- `CreateMockServerPlayer(uid, name)` - Mock server player
- `CreateTestDeity(type, name, domain)` - Fully configured Deity
- `CreateTestBlessing(...)` - Blessing with stat modifiers
- `CreateTestPlayerReligionData(...)` - Player religion data

**Example Usage:**
```csharp
public class MyFavorTrackerTests
{
    [Fact]
    public void OnBlockBroken_WhenOreBlock_AwardsFavor()
    {
        // Arrange - Use TestFixtures for consistent setup
        var mockAPI = TestFixtures.CreateMockServerAPI();
        var mockPlayerDataManager = TestFixtures.CreateMockPlayerDataManager();
        var tracker = new MiningFavorTracker(mockAPI.Object, mockPlayerDataManager.Object);

        // ... rest of test
    }
}
```

### Test Organization Conventions

**Naming Convention:**
```
MethodName_Scenario_ExpectedBehavior()
```

Examples:
- `AddFavor_ShouldIncreaseFavor_ForPlayer()`
- `OnBlockBroken_WhenOreBlock_AwardsFavor()`
- `GetBlessing_WhenPrerequisitesMet_ReturnsBlessing()`

**File Organization:**
```
PantheonWars.Tests/
├── Systems/
│   ├── Favor/
│   │   ├── MiningFavorTrackerTests.cs
│   │   ├── SmeltingFavorTrackerTests.cs
│   │   └── ...
│   ├── BlessingEffects/
│   │   ├── KhorasEffectHandlerTests.cs
│   │   └── ...
│   └── BlessingDefinitionsTests.cs
├── Models/
│   ├── BlessingTests.cs
│   └── ...
└── TestFixtures.cs
```

**Code Organization:**
```csharp
[ExcludeFromCodeCoverage]
public class TrackerTests
{
    #region Test Setup
    private readonly Mock<ICoreServerAPI> _mockAPI;

    public TrackerTests()
    {
        _mockAPI = TestFixtures.CreateMockServerAPI();
    }
    #endregion

    #region Feature A Tests
    [Fact]
    public void FeatureA_Scenario1_Outcome() { }

    [Fact]
    public void FeatureA_Scenario2_Outcome() { }
    #endregion

    #region Feature B Tests
    [Fact]
    public void FeatureB_Scenario1_Outcome() { }
    #endregion
}
```

### Running Tests

**Run All Tests:**
```bash
dotnet test
```

**Run Specific Test Class:**
```bash
dotnet test --filter "FullyQualifiedName~MiningFavorTrackerTests"
```

**Run with Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

**Watch Mode (auto-run on changes):**
```bash
dotnet watch test
```

---

## Phase 1: Khoras Testing

**Deity:** Khoras - God of Forge & Craft
**Focus:** Tool durability, ore efficiency, mining speed, smithing
**Implementation Status:** ~95% Complete (as of 2025-12-02)
**Reference:** [Khoras Forge Blessings](../reference/khoras_forge_blessings.md)

### Unit Tests

#### 1. KhorasBlessingTests.cs

**Purpose:** Verify all 10 Khoras blessings (6 player + 4 religion) apply correct stat modifiers.

**Test Class Location:** `PantheonWars.Tests/Systems/BlessingDefinitionsTests.cs` (or new `KhorasBlessingTests.cs`)

**Tests Needed:**

```csharp
[Fact]
public void KhorasT1_CraftsmansTouch_AppliesToolDurabilityAndOreYield()
{
    // Arrange
    var blessings = BlessingDefinitions.GetKhorasBlessings();
    var t1 = blessings.First(b => b.Id == BlessingIds.Khoras_T1_CraftsmansTouch);

    // Assert
    Assert.Contains(t1.StatModifiers, m => m.StatCode == VintageStoryStats.ToolDurability && m.Value == 0.10f);
    Assert.Contains(t1.StatModifiers, m => m.StatCode == VintageStoryStats.OreYield && m.Value == 0.10f);
}

[Fact]
public void KhorasT2A_MasterworkTools_StacksWithT1()
{
    // Verify additive stacking: T1 (10%) + T2A (15%) = 25% total
    // Test that stat modifiers combine correctly
}

[Fact]
public void KhorasT2B_ForgebornEndurance_AppliesMeleeDamageAndHealth()
{
    // Verify +10% melee damage, +10% max health
}

[Fact]
public void KhorasT3A_LegendarySmith_RequiresT2A_Prerequisite()
{
    // Verify prerequisite enforcement
    var t3a = blessings.First(b => b.Id == BlessingIds.Khoras_T3A_LegendarySmith);
    Assert.Contains(BlessingIds.Khoras_T2A_MasterworkTools, t3a.PrerequisiteIds);
}

[Fact]
public void KhorasT4_AvatarOfTheForge_RequiresBothT3Paths()
{
    // Verify capstone requires both T3A and T3B
    var t4 = blessings.First(b => b.Id == BlessingIds.Khoras_T4_AvatarOfTheForge);
    Assert.Contains(BlessingIds.Khoras_T3A_LegendarySmith, t4.PrerequisiteIds);
    Assert.Contains(BlessingIds.Khoras_T3B_Unyielding, t4.PrerequisiteIds);
}

[Fact]
public void KhorasReligion_AllTiers_ApplyToAllMembers()
{
    // Verify religion blessings marked as IsReligionBlessing = true
}
```

**Total Tests:** ~15-20 covering all blessing tiers, prerequisites, stat modifiers, and stacking behavior.

#### 2. MiningFavorTrackerTests.cs

**Purpose:** Verify mining ore awards correct favor amounts.

**Test Class Location:** `PantheonWars.Tests/Systems/Favor/MiningFavorTrackerTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void OnBlockBroken_WhenCopperOre_Awards2Favor()
{
    // Arrange
    var mockAPI = TestFixtures.CreateMockServerAPI();
    var mockPlayerDataManager = TestFixtures.CreateMockPlayerDataManager();
    var mockFavorSystem = TestFixtures.CreateMockFavorSystem();
    var tracker = new MiningFavorTracker(mockAPI.Object, mockPlayerDataManager.Object, mockFavorSystem.Object);

    var player = TestFixtures.CreateMockServerPlayer("player-1", "TestPlayer");
    var playerData = TestFixtures.CreateTestPlayerReligionData("player-1", DeityType.Khoras);

    // Setup player following Khoras
    mockPlayerDataManager.Setup(m => m.GetPlayerReligionData("player-1"))
        .Returns(playerData);

    // Act - Simulate mining copper ore
    // (Implementation will vary based on actual tracker API)

    // Assert
    mockFavorSystem.Verify(m => m.AwardFavorForAction(
        It.Is<IServerPlayer>(p => p.PlayerUID == "player-1"),
        "mining ore",
        2), Times.Once);
}

[Theory]
[InlineData("ore-poor-copper")]
[InlineData("ore-medium-tin")]
[InlineData("ore-rich-iron")]
[InlineData("ore-poor-silver")]
[InlineData("ore-medium-gold")]
[InlineData("ore-meteorite")]
public void OnBlockBroken_AllOreTypes_Award2Favor(string oreBlockCode)
{
    // Test all ore variants award 2 favor
}

[Fact]
public void OnBlockBroken_WhenStone_AwardsNoFavor()
{
    // Non-ore blocks should award 0 favor
}

[Fact]
public void OnBlockBroken_WhenPlayerNotFollowingKhoras_AwardsNoFavor()
{
    // Player following Lysa should get 0 favor from mining
}
```

**Total Tests:** ~10-12 covering all ore types, non-ore blocks, and deity affiliation.

#### 3. SmeltingFavorTrackerTests.cs

**Purpose:** Verify smelting ingots awards favor.

**Test Class Location:** `PantheonWars.Tests/Systems/Favor/SmeltingFavorTrackerTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void OnSmeltingComplete_CopperIngot_AwardsFavor()
{
    // Verify smelting completion awards favor
}

[Fact]
public void OnSmeltingComplete_DifferentIngotTypes_AwardsVariableFavor()
{
    // Iron might award more than copper, etc.
}

[Fact]
public void OnSmeltingComplete_NonKhorasFollower_AwardsNoFavor()
{
    // Deity isolation test
}
```

**Total Tests:** ~8-10 tests

#### 4. AnvilFavorTrackerTests.cs

**Purpose:** Verify anvil crafting awards 5-15 favor based on item complexity.

**Test Class Location:** `PantheonWars.Tests/Systems/Favor/AnvilFavorTrackerTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void OnAnvilCraft_SimpleItem_Awards5Favor()
{
    // Test crafting nails, simple items
}

[Fact]
public void OnAnvilCraft_ComplexItem_Awards15Favor()
{
    // Test crafting complex tools, weapons
}

[Fact]
public void OnAnvilCraft_NonKhorasFollower_AwardsNoFavor()
{
    // Deity isolation
}
```

**Total Tests:** ~8-10 tests

#### 5. KhorasEffectHandlerTests.cs

**Purpose:** Verify special effects like passive tool repair work correctly.

**Test Class Location:** `PantheonWars.Tests/Systems/BlessingEffects/Handlers/KhorasEffectHandlerTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void PassiveToolRepair_After5Minutes_Repairs1Durability()
{
    // Mock time progression, verify tool durability increases
}

[Fact]
public void PassiveToolRepair_OnlyRepairsEquippedTools()
{
    // Verify only equipped/hotbar tools are repaired
}

[Fact]
public void MaterialSaving_10PercentChance_TriggersCorrectly()
{
    // If implemented - verify material saving effect
    // May need statistical testing (100 crafts, expect ~10 savings)
}
```

**Total Tests:** ~5-8 tests

### Integration Tests

**Purpose:** Verify full workflows work end-to-end.

**Test Class Location:** `PantheonWars.Tests/Integration/KhorasIntegrationTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void FullProgression_MineToChampion_WorksCorrectly()
{
    // Arrange - Create player following Khoras
    // Act - Award favor through mining (0 → 500 → 2000 → 5000)
    // Assert - Verify blessings unlock at correct thresholds
    // Assert - Verify stat modifiers apply correctly
}

[Fact]
public void ReligionBlessings_AllMembers_ReceiveBonuses()
{
    // Create religion with 3 members
    // Award prestige to religion
    // Verify all 3 members receive religion blessing bonuses
}

[Fact]
public void PlayerAndReligionBonuses_Stack_Additively()
{
    // Player has T1 blessing (+10% tool durability)
    // Religion has R1 blessing (+10% tool durability)
    // Verify player has +20% total (additive stacking)
}
```

**Total Tests:** ~5-7 integration tests

### Manual Testing Procedures

**Purpose:** Verify in-game behavior matches specifications.

#### Manual Test 1: Mine Ore for Favor

**Preconditions:**
- Fresh world or test world with ore veins
- Player following Khoras deity (`/deity set Khoras`)
- Player has a pickaxe
- Known location of copper, tin, and iron ore

**Steps:**
1. Open chat and run `/favor check`
   - **Expected:** Chat displays current favor amount (e.g., "Divine Favor: 0 / 500")
2. Mine 1 copper ore block (any quality: poor, medium, rich)
   - **Expected:** Chat message appears: "You gained 2 favor with Khoras for mining ore"
3. Run `/favor check` again
   - **Expected:** Favor increased by exactly 2 points
4. Mine 10 more copper ore blocks
   - **Expected:** +2 favor per block (total +20 favor)
   - **Expected:** Chat message appears for each block mined
5. Mine 5 tin ore blocks
   - **Expected:** +2 favor per block (same as copper)
6. Mine 5 iron ore blocks
   - **Expected:** +2 favor per block (same as other ores)
7. Mine 10 stone blocks (non-ore)
   - **Expected:** No favor award, no chat message
8. Mine 10 dirt blocks (non-ore)
   - **Expected:** No favor award, no chat message

**Verification:**
- Each ore block awards exactly 2 favor
- All ore types work: copper, tin, iron, silver, gold, meteorite
- Non-ore blocks award 0 favor
- Chat messages appear consistently

**Troubleshooting:**
- If no favor awarded: Verify player is following Khoras with `/deity check`
- If wrong amount: Check MiningFavorTracker.cs implementation
- If no chat message: Check FavorSystem.AwardFavorForAction() logging

#### Manual Test 2: Anvil Crafting for Favor

**Preconditions:**
- Player following Khoras
- Access to anvil
- Metal ingots available

**Steps:**
1. Check current favor: `/favor check`
2. Craft 1 simple item at anvil (e.g., nails)
   - **Expected:** Chat message: "You gained 5 favor with Khoras for crafting at anvil"
3. Craft 1 complex item (e.g., sword)
   - **Expected:** Chat message: "You gained 10-15 favor with Khoras for crafting at anvil" (varies by complexity)
4. Verify favor increases match chat messages

**Verification:**
- Simple items: 5 favor
- Medium items: 8-10 favor
- Complex items: 12-15 favor

#### Manual Test 3: Blessing Progression

**Preconditions:**
- Player following Khoras
- Administrative privileges to award favor (`/favor set admin`)

**Steps:**
1. Award 500 favor: `/favor set <playername> 500`
2. Open blessing UI
   - **Expected:** Tier 1 blessing (Craftsman's Touch) available to unlock
3. Unlock Tier 1 blessing
   - **Expected:** +10% tool durability, +10% ore yield stat bonuses apply
4. Award 2000 favor: `/favor set <playername> 2000`
   - **Expected:** Tier 2A and 2B both available
5. Unlock Tier 2A (Masterwork Tools)
   - **Expected:** Tool durability now +25% (10% + 15% additive)
6. Verify cannot unlock Tier 3A without Tier 2A
7. Award 5000 favor: `/favor set <playername> 5000`
8. Unlock Tier 3A and Tier 3B
9. Unlock Tier 4 (Avatar of the Forge)
   - **Expected:** Requires both Tier 3A and Tier 3B
   - **Expected:** Passive tool repair starts (1 durability per 5 minutes)

**Verification:**
- Blessings unlock at correct favor thresholds
- Prerequisites enforce correctly
- Stat modifiers stack additively
- Capstone requires both Tier 3 paths

#### Manual Test 4: Passive Tool Repair

**Preconditions:**
- Player has unlocked Khoras Tier 4 (Avatar of the Forge)
- Player has a damaged tool in inventory or hotbar

**Steps:**
1. Damage a pickaxe to 50% durability
2. Place pickaxe in hotbar or equip it
3. Wait 5 minutes (real-time or accelerated server time)
   - **Expected:** Pickaxe durability increases by 1 point
4. Wait another 5 minutes
   - **Expected:** Pickaxe durability increases by 1 more point
5. Move pickaxe to storage chest
6. Wait 5 minutes
   - **Expected:** Pickaxe does NOT repair (only repairs equipped/hotbar tools)

**Verification:**
- Tool repairs 1 durability every 5 minutes
- Only equipped or hotbar tools repair
- Multiple tools can repair simultaneously

### Performance Testing

**Purpose:** Verify mining event handler doesn't cause server lag.

#### Performance Test 1: Mining Event Overhead

**What to Measure:**
- Server tick time before and after MiningFavorTracker registration
- Memory usage over 1000 block breaks
- Event handler execution time per block broken

**Methodology:**
1. Start server with MiningFavorTracker disabled
2. Measure baseline server tick time over 1 minute
3. Mine 1000 blocks (mix of ore and non-ore)
4. Record average tick time
5. Enable MiningFavorTracker
6. Mine 1000 blocks again
7. Compare tick times and memory usage

**Success Criteria:**
- Tick time increase < 5% (minimal overhead)
- No memory leaks (memory returns to baseline after test)
- Linear scaling with player count (test with 1, 10, 50 players)

**Tools:**
- .NET Performance Profiler
- Server tick time monitoring
- Memory profiler (dotMemory or similar)

#### Performance Test 2: Concurrent Player Mining

**What to Measure:**
- Server performance with multiple players mining simultaneously
- Favor award latency per player

**Methodology:**
1. Create 10 test players, all following Khoras
2. Have all 10 players mine ore simultaneously in different locations
3. Measure server tick time and favor award latency
4. Repeat with 50 players

**Success Criteria:**
- No exponential performance degradation
- Favor awards complete within 1 second for all players
- Server remains responsive

---

## Phase 2: Lysa Testing

**Deity:** Lysa - Goddess of Hunt & Wild
**Focus:** Hunting, foraging, movement, wilderness survival
**Implementation Status:** ~85% Complete (as of 2025-12-02)
**Reference:** [Lysa Hunt Blessings](../reference/lysa_hunt_blessings.md)

### Unit Tests

#### 1. LysaBlessingTests.cs

**Purpose:** Verify all 10 Lysa blessings (6 player + 4 religion) apply correct stat modifiers.

**Test Class Location:** `PantheonWars.Tests/Systems/BlessingDefinitionsTests.cs` (or new `LysaBlessingTests.cs`)

**Tests Needed:**

```csharp
[Fact]
public void LysaT1_HuntersInstinct_AppliesAnimalAndForageDrops()
{
    var blessings = BlessingDefinitions.GetLysaBlessings();
    var t1 = blessings.First(b => b.Id == BlessingIds.Lysa_T1_HuntersInstinct);

    Assert.Contains(t1.StatModifiers, m => m.StatCode == VintageStoryStats.AnimalDrops && m.Value == 0.15f);
    Assert.Contains(t1.StatModifiers, m => m.StatCode == VintageStoryStats.ForageDrops && m.Value == 0.15f);
    Assert.Contains(t1.StatModifiers, m => m.StatCode == VintageStoryStats.MovementSpeed && m.Value == 0.05f);
}

[Fact]
public void LysaT2A_MasterForager_StacksWithT1()
{
    // Verify forage drops: T1 (15%) + T2A (20%) = 35% total
}

[Fact]
public void LysaT2B_ApexPredator_AppliesAnimalHarvestingBonus()
{
    // Verify +20% animal drops, +10% animal harvesting speed
}

[Fact]
public void LysaT4_AvatarOfTheWild_ReducesAnimalSeekingRange()
{
    // Verify +20% ranged distance, -20% animal seeking range
}
```

**Total Tests:** ~15-20 covering all blessing tiers

#### 2. HuntingFavorTrackerTests.cs

**Purpose:** Verify killing animals awards favor based on animal type.

**Test Class Location:** `PantheonWars.Tests/Systems/Favor/HuntingFavorTrackerTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void OnAnimalKilled_Wolf_Awards12Favor()
{
    // Setup player following Lysa
    // Simulate wolf kill
    // Verify 12 favor awarded
}

[Theory]
[InlineData("wolf", 12)]
[InlineData("deer", 8)]
[InlineData("rabbit", 3)]
[InlineData("bear", 15)]
[InlineData("boar", 10)]
public void OnAnimalKilled_DifferentTypes_AwardsCorrectFavor(string animalType, int expectedFavor)
{
    // Test favor value table
}

[Fact]
public void OnAnimalKilled_NonLysaFollower_AwardsNoFavor()
{
    // Deity isolation test
}

[Fact]
public void OnAnimalKilled_PassiveCreatureDeath_AwardsNoFavor()
{
    // Animal dies from fall damage or drowning (not player kill)
    // Should award 0 favor
}
```

**Total Tests:** ~10-12 tests

#### 3. ForagingFavorTrackerTests.cs

**Purpose:** Verify foraging plants awards 0.5 favor per harvest.

**Test Class Location:** `PantheonWars.Tests/Systems/Favor/ForagingFavorTrackerTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void OnPlantHarvested_Berry_Awards0Point5Favor()
{
    // Test berry bush harvesting
}

[Theory]
[InlineData("berrybush-blackberry")]
[InlineData("berrybush-blueberry")]
[InlineData("mushroom-fieldmushroom")]
[InlineData("mushroom-flyagaric")]
public void OnPlantHarvested_VariousPlants_Awards0Point5Favor(string plantCode)
{
    // Test various forageable plants
}

[Fact]
public void OnPlantHarvested_NonLysaFollower_AwardsNoFavor()
{
    // Deity isolation
}
```

**Total Tests:** ~8-10 tests

#### 4. ExplorationFavorTrackerTests.cs

**Purpose:** Verify exploring new chunks awards 2 favor per chunk discovered.

**Test Class Location:** `PantheonWars.Tests/Systems/Favor/ExplorationFavorTrackerTests.cs`

**Status:** ⏸️ **Not Yet Implemented** - Pending chunk discovery system

**Tests Needed:**

```csharp
[Fact]
public void OnChunkDiscovered_NewChunk_Awards2Favor()
{
    // Track visited chunks per player
    // Award favor on first visit only
}

[Fact]
public void OnChunkDiscovered_AlreadyVisitedChunk_AwardsNoFavor()
{
    // Verify no favor for revisiting chunks
}

[Fact]
public void OnChunkDiscovered_NonLysaFollower_AwardsNoFavor()
{
    // Deity isolation
}
```

**Total Tests:** ~6-8 tests

#### 5. LysaEffectHandlerTests.cs

**Purpose:** Verify food spoilage reduction and temperature resistance effects.

**Test Class Location:** `PantheonWars.Tests/Systems/BlessingEffects/Handlers/LysaEffectHandlerTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void FoodSpoilageReduction_T2A_Reduces15Percent()
{
    // Verify food spoils 15% slower with T2A blessing
}

[Fact]
public void FoodSpoilageReduction_T3A_Reduces40Percent()
{
    // Verify food spoils 40% slower total (15% + 25%)
}

[Fact]
public void TemperatureResistance_T4_Adds5DegreeResistance()
{
    // Verify +5°C hot/cold resistance
}
```

**Total Tests:** ~5-7 tests

### Integration Tests

**Test Class Location:** `PantheonWars.Tests/Integration/LysaIntegrationTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void FullProgression_HuntToChampion_WorksCorrectly()
{
    // Hunt animals, forage, explore
    // Verify blessings unlock at correct thresholds
}

[Fact]
public void HuntAndForage_Together_BothAwardFavor()
{
    // Verify multiple favor sources work simultaneously
}
```

**Total Tests:** ~5-7 integration tests

### Manual Testing Procedures

#### Manual Test 1: Hunt Animals for Favor

**Preconditions:**
- Player following Lysa deity
- Access to hunting weapons (bow, spear)
- World with various animals

**Steps:**
1. Check current favor: `/favor check`
2. Kill 1 rabbit
   - **Expected:** Chat message: "You gained 3 favor with Lysa for hunting"
3. Kill 1 deer
   - **Expected:** Chat message: "You gained 8 favor with Lysa for hunting"
4. Kill 1 wolf
   - **Expected:** Chat message: "You gained 12 favor with Lysa for hunting"
5. Kill 1 bear
   - **Expected:** Chat message: "You gained 15 favor with Lysa for hunting"
6. Verify `/favor check` shows correct total

**Expected Favor Values:**
| Animal | Favor Award |
|--------|-------------|
| Rabbit | 3 |
| Chicken | 2 |
| Deer | 8 |
| Boar | 10 |
| Wolf | 12 |
| Bear | 15 |

**Verification:**
- Each animal type awards correct favor amount
- Chat messages appear for each kill
- Player must deal killing blow (passive deaths award no favor)

#### Manual Test 2: Forage Plants for Favor

**Preconditions:**
- Player following Lysa
- World with berry bushes, mushrooms

**Steps:**
1. Check current favor: `/favor check`
2. Harvest 10 berry bushes (any type)
   - **Expected:** +5 favor total (0.5 per bush)
   - **Expected:** Chat messages may batch or appear per harvest
3. Harvest 10 mushrooms
   - **Expected:** +5 favor total (0.5 per mushroom)
4. Verify favor increased by 10 total

**Verification:**
- Each forageable plant awards 0.5 favor
- Works for: berries, mushrooms, flowers, wild crops

#### Manual Test 3: Lysa Blessing Effects

**Preconditions:**
- Player following Lysa
- Admin privileges to set favor

**Steps:**
1. Set favor to 500: `/favor set <player> 500`
2. Unlock T1 (Hunter's Instinct)
3. Kill an animal and harvest it
   - **Expected:** +15% more drops (verify drop counts)
4. Harvest berry bushes
   - **Expected:** +15% more berries
5. Check movement speed
   - **Expected:** Noticeably faster (+5% movement speed)
6. Progress to T2A, verify forage drops increase to +35%
7. Progress to T3A, test food spoilage reduction
   - Place food in inventory, wait in-game time
   - Verify food spoils 40% slower
8. Progress to T4, test animal seeking range reduction
   - Walk near animals
   - **Expected:** Animals notice player from 20% shorter distance

**Verification:**
- All stat bonuses apply correctly
- Food spoilage reduction is noticeable
- Movement speed increase is perceptible

### Performance Testing

#### Performance Test: Entity Event Overhead

**What to Measure:**
- Server tick time with animal kill event handlers
- Memory usage with many entities

**Methodology:**
1. Spawn 100 animals in world
2. Measure baseline server performance
3. Have 10 players hunt simultaneously
4. Monitor event handler execution time

**Success Criteria:**
- No lag spikes during animal kills
- Memory stable (no leaks from entity tracking)
- Event handler completes in <10ms

---

## Phase 3: Aethra Testing

**Deity:** Aethra - Goddess of Light & Agriculture
**Focus:** Crop yields, cooking, food satiety, heat resistance
**Implementation Status:** ~95% Complete (as of 2025-12-02)
**Reference:** [Aethra Agriculture Blessings](../reference/aethra_agriculture_blessings.md)

### Unit Tests

#### 1. AethraBlessingTests.cs

**Purpose:** Verify all 10 Aethra blessings apply correct stat modifiers.

**Test Class Location:** `PantheonWars.Tests/Systems/BlessingDefinitionsTests.cs` (or new `AethraBlessingTests.cs`)

**Tests Needed:**

```csharp
[Fact]
public void AethraT1_SunsBlessing_AppliesCropYieldAndSatiety()
{
    var blessings = BlessingDefinitions.GetAethraBlessings();
    var t1 = blessings.First(b => b.Id == BlessingIds.Aethra_T1_SunsBlessing);

    Assert.Contains(t1.StatModifiers, m => m.StatCode == VintageStoryStats.CropYield && m.Value == 0.15f);
    Assert.Contains(t1.StatModifiers, m => m.StatCode == VintageStoryStats.Satiety && m.Value == 0.10f);
}

[Fact]
public void AethraT2A_BountifulHarvest_AddsSeedDropChance()
{
    // Verify +20% crop yield, +20% seed drop chance
}

[Fact]
public void AethraT2B_BakersTouch_AppliesCookingYieldAndFoodSpoilage()
{
    // Verify +30% cooking yield, -20% food spoilage
}

[Fact]
public void AethraT3A_MasterFarmer_Stacks60PercentCropYield()
{
    // Verify cumulative: T1 (15%) + T2A (20%) + T3A (25%) = 60% total
}
```

**Total Tests:** ~15-20 covering all blessing tiers

#### 2. AethraFavorTrackerTests.cs

**Purpose:** Verify crop harvesting, planting, and cooking award favor.

**Test Class Location:** `PantheonWars.Tests/Systems/Favor/AethraFavorTrackerTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void OnCropHarvested_Wheat_Awards1Favor()
{
    // Test crop harvesting
}

[Theory]
[InlineData("crop-wheat")]
[InlineData("crop-flax")]
[InlineData("crop-carrot")]
[InlineData("crop-onion")]
public void OnCropHarvested_VariousCrops_Awards1Favor(string cropCode)
{
    // Test various crop types
}

[Fact]
public void OnCropPlanted_Wheat_Awards0Point5Favor()
{
    // Test planting crops
}

[Fact]
public void OnCooking_SimpleMeal_Awards3Favor()
{
    // Test cooking simple meal (bread)
}

[Fact]
public void OnCooking_ComplexMeal_Awards8Favor()
{
    // Test cooking complex meal (stew)
}

[Fact]
public void OnCooking_NonAethraFollower_AwardsNoFavor()
{
    // Deity isolation
}
```

**Total Tests:** ~12-15 tests

#### 3. CookingPatchesTests.cs

**Purpose:** Verify firepit and crock cooking detection with owner attribution.

**Test Class Location:** `PantheonWars.Tests/Systems/Patches/CookingPatchesTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void FirepitOwnerTracking_PlayerLightsFire_TracksOwner()
{
    // Verify ConditionalWeakTable tracks firepit owner
}

[Fact]
public void FirepitCooking_OwnerAttributed_AwardsFavor()
{
    // Player lights fire, cooking completes, player gets favor
}

[Fact]
public void CrockSealing_OwnerAttributed_AwardsFavor()
{
    // Player seals crock, meal completes, player gets favor
}

[Fact]
public void AbandonedCooking_NoOwner_AwardsNoFavor()
{
    // Firepit/crock with no player attribution awards no favor
}
```

**Total Tests:** ~8-10 tests

#### 4. AethraEffectHandlerTests.cs

**Purpose:** Verify blessed meal buffs, malnutrition prevention, rare crop discovery.

**Test Class Location:** `PantheonWars.Tests/Systems/BlessingEffects/Handlers/AethraEffectHandlerTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void BlessedMealBuff_OnEating_AppliesTemporaryBonus()
{
    // Player eats meal, gets temporary buff
}

[Fact]
public void MalnutritionPrevention_AlwaysHealthy_Works()
{
    // Player never becomes malnourished with blessing
}

[Fact]
public void RareCropDiscovery_OnHarvest_ChanceForRareVariant()
{
    // Statistical test: 100 harvests, expect rare crop discovery
}
```

**Total Tests:** ~6-8 tests

### Integration Tests

**Test Class Location:** `PantheonWars.Tests/Integration/AethraIntegrationTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void FullProgression_FarmToChampion_WorksCorrectly()
{
    // Plant → harvest → cook → eat progression
}

[Fact]
public void CookingAndFarming_Together_BothAwardFavor()
{
    // Verify multiple favor sources work
}

[Fact]
public void BlessedMeal_Lifecycle_WorksEndToEnd()
{
    // Cook meal → eat → buff applies → buff expires
}
```

**Total Tests:** ~5-7 integration tests

### Manual Testing Procedures

#### Manual Test 1: Crop Farming for Favor

**Preconditions:**
- Player following Aethra
- Access to farmland
- Seeds available (wheat, flax, vegetables)

**Steps:**
1. Check current favor: `/favor check`
2. Plant 10 wheat seeds
   - **Expected:** +5 favor total (0.5 per seed)
   - **Expected:** Chat message may batch: "You gained 5 favor with Aethra for planting crops"
3. Wait for crops to mature
4. Harvest 10 wheat crops
   - **Expected:** +10 favor total (1 per crop)
   - **Expected:** Chat message: "You gained 10 favor with Aethra for harvesting crops"
5. Repeat with flax, carrots, onions
6. Verify all crop types award same favor amounts

**Verification:**
- Planting: 0.5 favor per seed
- Harvesting: 1 favor per crop
- Works for all crop types (wheat, flax, vegetables, etc.)

#### Manual Test 2: Cooking for Favor

**Preconditions:**
- Player following Aethra
- Access to firepit or crock pot
- Ingredients available

**Steps:**
1. Check current favor: `/favor check`
2. Cook 1 simple meal (bread) in firepit
   - Light firepit
   - Place dough in firepit
   - Wait for cooking to complete
   - **Expected:** Chat message: "You gained 3 favor with Aethra for cooking a meal"
3. Cook 1 complex meal (vegetable stew) in crock pot
   - Fill crock with ingredients
   - Seal crock
   - Wait for cooking to complete
   - **Expected:** Chat message: "You gained 6-8 favor with Aethra for cooking a meal"
4. Cook 1 gourmet meal (multiple ingredients, long cook time)
   - **Expected:** +8 favor
5. Verify `/favor check` shows correct total

**Expected Favor Values:**
| Meal Complexity | Favor Award |
|-----------------|-------------|
| Simple (bread, porridge) | 3 |
| Medium (stews, soups) | 5-6 |
| Complex (multi-ingredient meals) | 7-8 |

**Verification:**
- Player must light fire or seal crock (ownership attribution)
- Abandoned cooking awards no favor
- Cooking yield bonuses apply with blessings

#### Manual Test 3: Blessed Meal Buffs

**Preconditions:**
- Player following Aethra
- T2B blessing unlocked (Baker's Touch) or higher

**Steps:**
1. Cook a meal in firepit or crock
2. Eat the cooked meal
   - **Expected:** Chat message: "You feel Aethra's blessing in this meal"
   - **Expected:** Temporary buff icon appears
3. Check character stats
   - **Expected:** Temporary bonus to satiety or other stats
4. Wait for buff duration to expire
   - **Expected:** Buff icon disappears, bonus removed

**Verification:**
- Blessed meal buffs appear on food consumption
- Buff duration matches specification
- Buff bonuses apply correctly

### Performance Testing

#### Performance Test: Crop Harvesting Event Overhead

**What to Measure:**
- Server tick time during mass crop harvesting
- Memory usage with many farmland blocks

**Methodology:**
1. Create large farm (100x100 blocks)
2. Plant all crops
3. Harvest all crops simultaneously
4. Monitor server performance

**Success Criteria:**
- No lag during harvest
- Event handler completes quickly (<5ms per crop)

---

## Phase 4: Gaia Testing

**Deity:** Gaia - Goddess of Pottery & Clay
**Focus:** Pottery crafting, clay gathering, defensive fortification (armor), kiln efficiency
**Implementation Status:** ~95% Complete (as of 2025-12-02)
**Reference:** [Gaia Pottery Blessings](../reference/gaia_pottery_blessings.md)

### Unit Tests

#### 1. GaiaBlessingTests.cs

**Purpose:** Verify all 10 Gaia blessings apply correct stat modifiers.

**Test Class Location:** `PantheonWars.Tests/Systems/BlessingDefinitionsTests.cs` (or new `GaiaBlessingTests.cs`)

**Tests Needed:**

```csharp
[Fact]
public void GaiaT1_ClayShaper_AppliesClayYieldAndHealth()
{
    var blessings = BlessingDefinitions.GetGaiaBlessings();
    var t1 = blessings.First(b => b.Id == BlessingIds.Gaia_T1_ClayShaper);

    Assert.Contains(t1.StatModifiers, m => m.StatCode == VintageStoryStats.ClayYield && m.Value == 0.20f);
    Assert.Contains(t1.StatModifiers, m => m.StatCode == VintageStoryStats.MaxHealth && m.Value == 0.10f);
}

[Fact]
public void GaiaT2A_MasterPotter_AppliesBatchCompletionChance()
{
    // Verify +10% batch completion chance (duplicate pottery items)
}

[Fact]
public void GaiaT2B_EarthenBuilder_AppliesArmorEffectiveness()
{
    // Verify +15% armor effectiveness, +15% stone yield
}

[Fact]
public void GaiaT3A_KilnMaster_Stacks60PercentBatchCompletion()
{
    // Verify T2A (10%) + T3A (15%) = 25% batch completion chance
}
```

**Total Tests:** ~15-20 covering all blessing tiers

#### 2. GaiaFavorTrackerTests.cs

**Purpose:** Verify pottery crafting, kiln firing, and brick placement award favor.

**Test Class Location:** `PantheonWars.Tests/Systems/Favor/GaiaFavorTrackerTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void OnPotteryCraft_Vessel_Awards5Favor()
{
    // Test crafting pottery vessel (highest value)
}

[Theory]
[InlineData("vessel", 5)]
[InlineData("planter", 4)]
[InlineData("pot", 3)]
[InlineData("mold", 2)]
[InlineData("brick", 1)]
public void OnPotteryCraft_DifferentItems_AwardsCorrectFavor(string itemType, int expectedFavor)
{
    // Test item-specific favor values
}

[Fact]
public void OnKilnFiring_Awards3To8Favor()
{
    // Test kiln firing completion
}

[Fact]
public void OnBrickPlacement_Awards2Favor()
{
    // Test placing clay bricks
}

[Fact]
public void OnBrickPlacement_NonGaiaFollower_AwardsNoFavor()
{
    // Deity isolation
}
```

**Total Tests:** ~12-15 tests

#### 3. PitKilnPatchesTests.cs

**Purpose:** Verify kiln firing detection.

**Test Class Location:** `PantheonWars.Tests/Systems/Patches/PitKilnPatchesTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void OnPitKilnFired_TracksItems_AwardsFavor()
{
    // Verify kiln firing completion detection
}

[Fact]
public void OnPitKilnFired_ItemTracking_Accurate()
{
    // Verify item count tracking correct
}
```

**Total Tests:** ~5-7 tests

#### 4. ClayFormingPatchesTests.cs

**Purpose:** Verify pottery forming and knapping detection.

**Test Class Location:** `PantheonWars.Tests/Systems/Patches/ClayFormingPatchesTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void OnClayForming_Pottery_AwardsFavor()
{
    // Test clay forming detection
}

[Fact]
public void OnKnapping_Flint_AwardsFavor()
{
    // Test knapping detection (if applicable to Gaia)
}
```

**Total Tests:** ~6-8 tests

#### 5. GaiaEffectHandlerTests.cs

**Purpose:** Verify pottery batch completion bonus (duplicate items).

**Test Class Location:** `PantheonWars.Tests/Systems/BlessingEffects/Handlers/GaiaEffectHandlerTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void PotteryBatchCompletion_25PercentChance_DuplicatesItem()
{
    // Statistical test: 100 pottery crafts, expect ~25 duplicates
    // Mock random to test probability distribution
}

[Fact]
public void PotteryBatchCompletion_DuplicateAddedToInventory()
{
    // Verify duplicate item added to player inventory
}

[Fact]
public void ArmorEffectiveness_35Percent_Applies()
{
    // Verify armor effectiveness stat modifier
}
```

**Total Tests:** ~6-8 tests

### Integration Tests

**Test Class Location:** `PantheonWars.Tests/Integration/GaiaIntegrationTests.cs`

**Tests Needed:**

```csharp
[Fact]
public void FullProgression_PotteryToChampion_WorksCorrectly()
{
    // Form clay → fire kiln → place bricks → progression
}

[Fact]
public void BatchCompletionBonus_Workflow_WorksEndToEnd()
{
    // Craft pottery → batch completion triggers → duplicate item created
}
```

**Total Tests:** ~5-7 integration tests

### Manual Testing Procedures

#### Manual Test 1: Pottery Crafting for Favor

**Preconditions:**
- Player following Gaia
- Access to clay forming bench
- Clay available

**Steps:**
1. Check current favor: `/favor check`
2. Form 1 pottery vessel on clay forming bench
   - **Expected:** Chat message: "You gained 5 favor with Gaia for crafting pottery"
3. Form 1 planter
   - **Expected:** +4 favor
4. Form 1 pottery pot
   - **Expected:** +3 favor
5. Form 1 mold
   - **Expected:** +2 favor
6. Form 1 brick
   - **Expected:** +1 favor
7. Verify `/favor check` shows +15 favor total

**Expected Favor Values:**
| Pottery Item | Favor Award |
|--------------|-------------|
| Vessel | 5 |
| Planter | 4 |
| Pottery Pot | 3 |
| Mold/Crucible | 2 |
| Brick | 1 |

**Verification:**
- Each pottery type awards correct favor amount
- Chat messages appear for each craft

#### Manual Test 2: Kiln Firing for Favor

**Preconditions:**
- Player following Gaia
- Access to pit kiln
- Unfired pottery items

**Steps:**
1. Check current favor: `/favor check`
2. Build pit kiln structure
3. Place unfired pottery in kiln
4. Light kiln
5. Wait for firing to complete
   - **Expected:** Chat message: "You gained 3-8 favor with Gaia for firing pottery"
   - Favor amount varies by quantity and item type
6. Verify favor increased correctly

**Verification:**
- Kiln firing awards 3-8 favor based on items
- More items = more favor
- Higher value items (vessels) = more favor

#### Manual Test 3: Clay Brick Placement for Favor

**Preconditions:**
- Player following Gaia
- Clay bricks available (fired in kiln)

**Steps:**
1. Check current favor: `/favor check`
2. Place 1 clay brick as building block
   - **Expected:** Chat message: "You gained 2 favor with Gaia for placing clay brick"
3. Place 10 more clay bricks
   - **Expected:** +20 favor total
4. Verify `/favor check` shows +22 favor total (2 per brick * 11 bricks)

**Verification:**
- Each brick placement awards 2 favor
- Total favor from brick: 3 (1 for crafting + 2 for placing)

#### Manual Test 4: Pottery Batch Completion Bonus

**Preconditions:**
- Player following Gaia
- T2A blessing unlocked (Master Potter) or higher
- 25%+ batch completion chance

**Steps:**
1. Craft 20 pottery vessels on clay forming bench
   - **Expected:** Some crafts produce 2 vessels instead of 1
   - **Expected:** Chat message: "Gaia's blessing duplicated your pottery!"
2. Count total vessels created
   - **Expected:** ~25 vessels (20 crafted + ~5 duplicates at 25% rate)
3. Check inventory for duplicate items

**Verification:**
- Duplicate items appear in inventory
- Probability matches stat modifier (25% with T2A, up to 60% with T3A)
- Works for all pottery types

#### Manual Test 5: Armor Effectiveness

**Preconditions:**
- Player following Gaia
- T2B blessing unlocked (Earthen Builder) or higher
- Armor equipped

**Steps:**
1. Equip armor (any type)
2. Check armor stats
   - **Expected:** Armor effectiveness increased by 15% (T2B) or 35% (T3B)
3. Take damage from enemy
   - **Expected:** Damage reduced more than without blessing
4. Verify armor durability loss reduced

**Verification:**
- Armor effectiveness stat applies
- Damage reduction noticeable

### Performance Testing

#### Performance Test: Pottery Crafting Event Overhead

**What to Measure:**
- Server tick time during mass pottery crafting
- Memory usage with batch completion bonus

**Methodology:**
1. Craft 100 pottery items rapidly
2. Monitor server performance
3. Check for memory leaks with ConditionalWeakTable

**Success Criteria:**
- No lag during crafting
- Memory stable (no leaks)

---

## Cross-Cutting Testing

**Purpose:** Test functionality that spans all deities or the entire system.

### Balance Testing

#### Balance Test 1: Progression Pacing

**Purpose:** Verify players can reach Champion rank in reasonable time.

**Methodology:**
1. Create 4 test players, one for each deity
2. Play normally for 10 hours (simulated or real)
3. Track favor progression for each deity
4. Measure time to reach each rank:
   - Follower (0 favor) → Initiate (500 favor)
   - Initiate (500) → Adherent (2000)
   - Adherent (2000) → Champion (5000)

**Success Criteria:**
- Follower → Initiate: ~2-4 hours of active play
- Initiate → Adherent: ~6-10 hours
- Adherent → Champion: ~15-25 hours
- All deities feel roughly equivalent in progression speed

**Questions to Answer:**
- Is any deity significantly faster/slower to progress?
- Are favor rates tuned correctly for casual vs. hardcore players?
- Is passive favor (0.5/hour) meaningful or negligible?

#### Balance Test 2: Favor Source Distribution

**Purpose:** Verify activity-based favor is primary source, not passive.

**Methodology:**
1. Track favor earnings over 10 hours for each deity
2. Break down favor by source:
   - Passive (0.5/hour)
   - Primary activity (mining, hunting, farming, pottery)
   - Secondary activity
   - PvP (if applicable)
3. Calculate percentage from each source

**Success Criteria:**
- Primary activity: 60-80% of total favor
- Secondary activities: 15-25%
- Passive: 5-15%
- PvP: 0-10% (varies by server)

**Red Flags:**
- Passive > 30% (players idling for favor)
- Single activity > 90% (exploit/farm)

#### Balance Test 3: Blessing Power Level

**Purpose:** Verify blessings are meaningful but not overpowered.

**Methodology:**
1. Test each blessing tier's impact on gameplay
2. Measure effectiveness:
   - Tool durability increase (how noticeable?)
   - Crop yield increase (how many extra crops?)
   - Damage bonuses (how much stronger in combat?)
3. Compare blessed vs. non-blessed players

**Success Criteria:**
- Blessings provide 15-30% improvement in core activities
- Champion players feel noticeably stronger than Followers
- Blessings don't break game balance (no invincibility, infinite resources)

**Questions:**
- Do players feel rewarded for unlocking blessings?
- Are any blessings "must-have" vs. "useless"?
- Do dual-path choices feel meaningful?

#### Balance Test 4: Cross-Deity Balance

**Purpose:** Verify all deities are equally attractive and viable.

**Methodology:**
1. Survey player deity choices (if multiplayer)
2. Compare progression rates across deities
3. Measure "deity switching" rate (players changing deities)

**Success Criteria:**
- No deity has >40% or <15% player population
- All deities feel viable for different playstyles
- Players stick with chosen deity (low switching rate)

**Red Flags:**
- One deity dominates (>50% players)
- One deity abandoned (<5% players)
- High switching rate (players min-maxing)

### Religion Progression Testing

#### Religion Test 1: Prestige Accumulation

**Purpose:** Verify religion prestige system works correctly for groups.

**Methodology:**
1. Create religion with 5 members
2. All members follow same deity
3. Track prestige accumulation as members earn favor
4. Verify prestige thresholds unlock religion blessings

**Success Criteria:**
- Prestige accumulates correctly from all members
- Religion blessings unlock at correct thresholds (500, 2000, 5000 prestige)
- All members receive religion blessing bonuses

#### Religion Test 2: Multi-Deity Religions

**Purpose:** Verify religions with mixed deity followers work correctly.

**Methodology:**
1. Create religion with 4 members, each following different deity
2. Track prestige accumulation
3. Verify each member gets blessings for their deity only

**Success Criteria:**
- Prestige still accumulates across deities
- Members receive correct deity-specific religion blessings
- No cross-contamination (Khoras follower doesn't get Lysa religion blessings)

### Stat Modifier Stacking Tests

#### Stacking Test 1: Player + Religion Bonuses

**Purpose:** Verify player and religion bonuses stack additively.

**Methodology:**
1. Create player following Khoras
2. Player unlocks T1 (Craftsman's Touch): +10% tool durability
3. Join religion with R1 (Shared Workshop): +10% tool durability
4. Verify player has +20% total tool durability

**Success Criteria:**
- Player blessing + Religion blessing = additive stacking
- Works for all stat types
- No multiplicative stacking (would be overpowered)

#### Stacking Test 2: Multiple Stat Sources

**Purpose:** Verify stat modifiers from multiple blessings combine correctly.

**Methodology:**
1. Player unlocks T1, T2A, T3A (all with tool durability bonuses)
2. Calculate expected total: 10% + 15% + 20% = 45%
3. Verify actual stat modifier matches expected

**Success Criteria:**
- All stat modifiers sum correctly
- No rounding errors
- Stat modifiers apply to game mechanics correctly

### Performance Benchmarking

#### Performance Benchmark 1: Event Handler Overhead

**Purpose:** Measure total performance impact of all favor trackers.

**Methodology:**
1. Start server with all 4 deities disabled
2. Measure baseline server tick time over 10 minutes
3. Enable all favor trackers
4. Measure server tick time again
5. Calculate overhead percentage

**Success Criteria:**
- Total overhead < 5% of baseline tick time
- No individual tracker > 2% overhead
- Overhead scales linearly with player count

**Tools:**
- .NET Performance Profiler
- Server tick time logs
- Custom timing instrumentation

#### Performance Benchmark 2: Concurrent Players

**Purpose:** Verify system scales with many simultaneous players.

**Methodology:**
1. Simulate 10, 50, 100 players performing deity activities
2. Measure server performance at each tier
3. Monitor:
   - Server tick time
   - Memory usage
   - CPU usage
   - Favor award latency

**Success Criteria:**
- Linear scaling (100 players ≈ 10x load of 10 players)
- No exponential degradation
- Server remains responsive at 100 players

#### Performance Benchmark 3: Memory Leak Detection

**Purpose:** Verify no memory leaks in long-running servers.

**Methodology:**
1. Start server with all favor trackers enabled
2. Run for 24 hours (or simulate extended playtime)
3. Monitor memory usage over time
4. Check for gradual memory increase (leak indicator)

**Success Criteria:**
- Memory usage stable after initial startup
- No gradual memory increase over time
- Memory freed correctly when players disconnect

**Tools:**
- dotMemory or similar memory profiler
- Server memory monitoring

---

## Test Implementation Examples

This section provides minimal representative examples following existing PantheonWars.Tests patterns.

### Example 1: Unit Test for Favor Tracker

**Pattern:** Test individual favor tracker components in isolation.

```csharp
using PantheonWars.Systems.Favor;
using PantheonWars.Tests.Helpers;
using Xunit;
using Moq;

namespace PantheonWars.Tests.Systems.Favor
{
    [ExcludeFromCodeCoverage]
    public class MiningFavorTrackerTests
    {
        #region Test Setup
        private readonly Mock<ICoreServerAPI> _mockAPI;
        private readonly Mock<IPlayerDataManager> _mockPlayerDataManager;
        private readonly Mock<IFavorSystem> _mockFavorSystem;
        private readonly MiningFavorTracker _tracker;

        public MiningFavorTrackerTests()
        {
            _mockAPI = TestFixtures.CreateMockServerAPI();
            _mockPlayerDataManager = TestFixtures.CreateMockPlayerDataManager();
            _mockFavorSystem = TestFixtures.CreateMockFavorSystem();

            _tracker = new MiningFavorTracker(
                _mockAPI.Object,
                _mockPlayerDataManager.Object,
                _mockFavorSystem.Object
            );
        }
        #endregion

        #region Ore Mining Tests
        [Fact]
        public void OnBlockBroken_WhenCopperOre_Awards2Favor()
        {
            // Arrange
            var player = TestFixtures.CreateMockServerPlayer("player-1", "TestPlayer");
            var playerData = TestFixtures.CreateTestPlayerReligionData("player-1", DeityType.Khoras);

            _mockPlayerDataManager.Setup(m => m.GetPlayerReligionData("player-1"))
                .Returns(playerData);

            // Mock ore block
            var mockBlock = new Mock<Block>();
            mockBlock.Setup(b => b.Code).Returns(new AssetLocation("game", "ore-poor-copper"));

            // Act
            _tracker.OnBlockBroken(player.Object, mockBlock.Object);

            // Assert
            _mockFavorSystem.Verify(m => m.AwardFavorForAction(
                It.Is<IServerPlayer>(p => p.PlayerUID == "player-1"),
                "mining ore",
                2), Times.Once);
        }

        [Fact]
        public void OnBlockBroken_WhenStone_AwardsNoFavor()
        {
            // Arrange
            var player = TestFixtures.CreateMockServerPlayer("player-1", "TestPlayer");
            var mockBlock = new Mock<Block>();
            mockBlock.Setup(b => b.Code).Returns(new AssetLocation("game", "stone-granite"));

            // Act
            _tracker.OnBlockBroken(player.Object, mockBlock.Object);

            // Assert
            _mockFavorSystem.Verify(m => m.AwardFavorForAction(
                It.IsAny<IServerPlayer>(),
                It.IsAny<string>(),
                It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void OnBlockBroken_WhenPlayerNotFollowingKhoras_AwardsNoFavor()
        {
            // Arrange
            var player = TestFixtures.CreateMockServerPlayer("player-1", "TestPlayer");
            var playerData = TestFixtures.CreateTestPlayerReligionData("player-1", DeityType.Lysa);

            _mockPlayerDataManager.Setup(m => m.GetPlayerReligionData("player-1"))
                .Returns(playerData);

            var mockBlock = new Mock<Block>();
            mockBlock.Setup(b => b.Code).Returns(new AssetLocation("game", "ore-poor-copper"));

            // Act
            _tracker.OnBlockBroken(player.Object, mockBlock.Object);

            // Assert - No favor awarded
            _mockFavorSystem.Verify(m => m.AwardFavorForAction(
                It.IsAny<IServerPlayer>(),
                It.IsAny<string>(),
                It.IsAny<int>()), Times.Never);
        }
        #endregion
    }
}
```

### Example 2: Integration Test for Blessing Progression

**Pattern:** Test multiple components working together.

```csharp
using PantheonWars.Systems;
using PantheonWars.Tests.Helpers;
using Xunit;

namespace PantheonWars.Tests.Integration
{
    [ExcludeFromCodeCoverage]
    public class KhorasIntegrationTests
    {
        #region Test Setup
        private readonly Mock<ICoreServerAPI> _mockAPI;
        private readonly PlayerDataManager _playerDataManager;
        private readonly FavorSystem _favorSystem;
        private readonly BlessingEffectSystem _blessingSystem;

        public KhorasIntegrationTests()
        {
            _mockAPI = TestFixtures.CreateMockServerAPI();

            // Use real implementations with mocked dependencies
            _playerDataManager = new PlayerDataManager(_mockAPI.Object);
            _favorSystem = new FavorSystem(_mockAPI.Object, _playerDataManager, ...);
            _blessingSystem = new BlessingEffectSystem(_mockAPI.Object, ...);
        }
        #endregion

        #region Full Progression Tests
        [Fact]
        public void FullProgression_MineOreToChampion_UnlocksBlessingsCorrectly()
        {
            // Arrange
            var player = TestFixtures.CreateMockServerPlayer("player-1", "TestPlayer");
            _playerDataManager.SetPlayerDeity("player-1", DeityType.Khoras);

            // Act - Award favor to reach Initiate (500)
            _favorSystem.AwardFavorForAction(player.Object, "mining ore", 500);

            // Assert - T1 blessing available
            var availableBlessings = _blessingSystem.GetAvailableBlessings("player-1");
            Assert.Contains(availableBlessings, b => b.Id == BlessingIds.Khoras_T1_CraftsmansTouch);

            // Act - Unlock T1
            _blessingSystem.UnlockBlessing("player-1", BlessingIds.Khoras_T1_CraftsmansTouch);

            // Assert - Stat modifiers applied
            var stats = _blessingSystem.GetPlayerStats("player-1");
            Assert.Equal(0.10f, stats.GetModifier(VintageStoryStats.ToolDurability));
            Assert.Equal(0.10f, stats.GetModifier(VintageStoryStats.OreYield));

            // Act - Award favor to reach Adherent (2000)
            _favorSystem.AwardFavorForAction(player.Object, "mining ore", 1500);

            // Assert - T2 blessings available
            availableBlessings = _blessingSystem.GetAvailableBlessings("player-1");
            Assert.Contains(availableBlessings, b => b.Id == BlessingIds.Khoras_T2A_MasterworkTools);
            Assert.Contains(availableBlessings, b => b.Id == BlessingIds.Khoras_T2B_ForgebornEndurance);

            // Act - Unlock T2A
            _blessingSystem.UnlockBlessing("player-1", BlessingIds.Khoras_T2A_MasterworkTools);

            // Assert - Stat modifiers stack additively
            stats = _blessingSystem.GetPlayerStats("player-1");
            Assert.Equal(0.25f, stats.GetModifier(VintageStoryStats.ToolDurability)); // 10% + 15%

            // Continue to Champion rank...
        }

        [Fact]
        public void PlayerAndReligionBonuses_Stack_Additively()
        {
            // Arrange
            var player = TestFixtures.CreateMockServerPlayer("player-1", "TestPlayer");
            var religion = TestFixtures.CreateTestReligion("TestReligion", DeityType.Khoras);

            // Player unlocks T1: +10% tool durability
            _blessingSystem.UnlockBlessing("player-1", BlessingIds.Khoras_T1_CraftsmansTouch);

            // Religion unlocks R1: +10% tool durability
            _blessingSystem.UnlockReligionBlessing(religion.Id, BlessingIds.Khoras_R1_SharedWorkshop);

            // Act - Join religion
            _playerDataManager.SetPlayerReligion("player-1", religion.Id);

            // Assert - Total bonus = 20% (additive)
            var stats = _blessingSystem.GetPlayerStats("player-1");
            Assert.Equal(0.20f, stats.GetModifier(VintageStoryStats.ToolDurability));
        }
        #endregion
    }
}
```

### Example 3: Mock Setup with TestFixtures

**Pattern:** Use TestFixtures for consistent test data creation.

```csharp
// Example from actual test class showing TestFixtures usage
public class BlessingEffectSystemTests
{
    private readonly Mock<ICoreServerAPI> _mockAPI;
    private readonly Mock<IPlayerDataManager> _mockPlayerDataManager;
    private readonly Mock<IBlessingRegistry> _mockBlessingRegistry;
    private readonly BlessingEffectSystem _system;

    public BlessingEffectSystemTests()
    {
        // Use TestFixtures for all mock creation
        _mockAPI = TestFixtures.CreateMockServerAPI();
        _mockPlayerDataManager = TestFixtures.CreateMockPlayerDataManager();
        _mockBlessingRegistry = TestFixtures.CreateMockBlessingRegistry();

        // Create real system under test
        _system = new BlessingEffectSystem(
            _mockAPI.Object,
            _mockPlayerDataManager.Object,
            _mockBlessingRegistry.Object
        );
    }

    [Fact]
    public void UnlockBlessing_AppliesStatModifiers()
    {
        // Arrange - Use TestFixtures to create test blessing
        var blessing = TestFixtures.CreateTestBlessing(
            id: "test-blessing",
            deityType: DeityType.Khoras,
            statModifiers: new List<StatModifier>
            {
                new StatModifier(VintageStoryStats.ToolDurability, 0.10f)
            }
        );

        _mockBlessingRegistry.Setup(r => r.GetBlessing("test-blessing"))
            .Returns(blessing);

        // Act
        _system.UnlockBlessing("player-1", "test-blessing");

        // Assert
        var stats = _system.GetPlayerStats("player-1");
        Assert.Equal(0.10f, stats.GetModifier(VintageStoryStats.ToolDurability));
    }
}
```

---

## Success Criteria

### Functional Requirements

**All features implemented and tested:**
- ✅ 4 deities fully functional (Khoras, Lysa, Aethra, Gaia)
- ✅ 40 blessings total (10 per deity) all working correctly
- ✅ Activity-based favor earning for all deities
- ✅ All special effects working (passive tool repair, batch completion, etc.)
- ✅ Dual-path progression intact (Tier 2A/2B choices)
- ✅ Player + Religion bonuses stack correctly

**All critical workflows tested:**
- ✅ Favor earning → Blessing unlock → Stat bonuses apply
- ✅ Religion creation → Prestige accumulation → Group bonuses
- ✅ Deity switching (if applicable)
- ✅ Multi-deity religions

### Technical Requirements

**Code quality:**
- ✅ 0 compilation errors
- ✅ Build succeeds with 0 errors
- ✅ 90%+ unit test coverage on new code (favor trackers, effect handlers)
- ✅ All integration tests passing
- ✅ Code follows existing PantheonWars patterns

**Performance:**
- ✅ No performance degradation (< 5% overhead)
- ✅ No memory leaks in long-running servers
- ✅ Event handlers complete in <10ms
- ✅ Scales linearly with player count

**Coverage targets met:**
| Component | Target | Status |
|-----------|--------|--------|
| Favor Trackers | 85%+ | ⏸️ Pending |
| Blessing Definitions | 90%+ | ⏸️ Pending |
| Special Effect Handlers | 85%+ | ⏸️ Pending |
| Patches (Harmony) | 75%+ | ⏸️ Pending |
| Integration Workflows | 80%+ | ⏸️ Pending |

### Documentation Requirements

**Player-facing documentation:**
- ✅ Deity reference docs complete (khoras_forge_blessings.md, etc.)
- ⏸️ Player guide: "Getting Started with Deities"
- ⏸️ Favor earning guide with rates and activities

**Developer documentation:**
- ✅ Migration plan complete
- ✅ Testing guide complete (this document)
- ⏸️ Code comments on complex systems
- ⏸️ Changelog updated with migration notes

**Admin documentation:**
- ⏸️ Migration guide for server admins
- ⏸️ Balance adjustment guide
- ⏸️ Troubleshooting guide

### User Experience Requirements

**Balance:**
- ⏸️ All deities feel equally viable and attractive
- ⏸️ Progression pacing feels appropriate (Follower → Champion in 15-25 hours)
- ⏸️ Blessings are meaningful but not overpowered
- ⏸️ Favor sources are diverse and engaging

**Polish:**
- ✅ Chat messages for all favor awards
- ⏸️ Clear feedback for blessing unlocks
- ⏸️ UI displays all information clearly
- ⏸️ No confusing interactions or edge cases

---

## Appendix: Test Execution Checklist

Use this checklist to track testing progress:

### Phase 1: Khoras
- [ ] Unit tests implemented (5 test classes, ~60 tests)
- [ ] Integration tests implemented (~7 tests)
- [ ] Manual testing complete (4 test procedures)
- [ ] Performance testing complete (2 benchmarks)
- [ ] Coverage target met (85%+)

### Phase 2: Lysa
- [ ] Unit tests implemented (5 test classes, ~50 tests)
- [ ] Integration tests implemented (~7 tests)
- [ ] Manual testing complete (3 test procedures)
- [ ] Performance testing complete (1 benchmark)
- [ ] Coverage target met (85%+)

### Phase 3: Aethra
- [ ] Unit tests implemented (4 test classes, ~50 tests)
- [ ] Integration tests implemented (~7 tests)
- [ ] Manual testing complete (3 test procedures)
- [ ] Performance testing complete (1 benchmark)
- [ ] Coverage target met (85%+)

### Phase 4: Gaia
- [ ] Unit tests implemented (5 test classes, ~55 tests)
- [ ] Integration tests implemented (~7 tests)
- [ ] Manual testing complete (5 test procedures)
- [ ] Performance testing complete (1 benchmark)
- [ ] Coverage target met (85%+)

### Cross-Cutting
- [ ] Balance testing complete (4 tests)
- [ ] Religion progression testing complete (2 tests)
- [ ] Stat modifier stacking tests complete (2 tests)
- [ ] Performance benchmarking complete (3 benchmarks)

### Final Validation
- [ ] All unit tests passing
- [ ] All integration tests passing
- [ ] All manual tests verified in-game
- [ ] Performance benchmarks meet criteria
- [ ] Coverage targets met across all components
- [ ] Documentation complete

---

**End of Testing Guide**

For implementation details, see [Deity Utility Migration Plan](../planning/deity_utility_migration_plan.md).
