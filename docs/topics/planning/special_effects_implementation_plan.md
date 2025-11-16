# Special Effects Implementation Plan

## Executive Summary

The PantheonWars blessing system currently has **80 blessings fully defined** across 8 deities, with **stat modifiers working perfectly**. However, **21 special effects** (lifesteal, poison, critical strikes, stealth, etc.) are defined in blessing data but have **no handler implementations**. This document provides a comprehensive plan to implement these special effect handlers.

**Current Status:**
- ✅ Stat modifiers (damage, health, speed): **FULLY WORKING**
- ✅ 80 blessings defined with special effects: **COMPLETE**
- ⚠️ Special effect handlers: **NOT IMPLEMENTED** (~10-12 hours remaining)

**Impact:**
Special effects represent ~40% of blessing value. Without handlers, many blessings provide only partial benefits, significantly reducing gameplay depth and deity differentiation.

**Goal:**
Implement a robust, event-based special effect handler system that applies blessing effects to combat, utility, and gameplay mechanics.

---

## Special Effects Inventory

### Effects Defined in BlessingDefinitions.cs

Based on comprehensive codebase analysis, the following **21 special effects** are currently defined:

#### Combat - Damage Enhancement (8 effects)
1. **Lifesteal3** - Heal 3% of damage dealt (Khoras, Morthen)
2. **Lifesteal10** - Heal 10% of damage dealt (Khoras)
3. **Lifesteal15** - Heal 15% of damage dealt (Morthen)
4. **Lifesteal20** - Heal 20% of damage dealt (Khoras Avatar)
5. **CriticalChance10** - 10% chance for 2x damage (Lysa, Umbros)
6. **CriticalChance20** - 20% chance for 2x damage (Umbros)
7. **HeadshotBonus** - 50% extra damage on headshots (Lysa)
8. **ExecuteThreshold** - Instant kill enemies below 15% HP (Khoras, Morthen)

#### Combat - Damage Over Time (3 effects)
9. **PoisonDot** - Apply poison DoT on hit (Lysa, Morthen)
10. **PoisonDotStrong** - Apply strong poison DoT (Morthen)
11. **PlagueAura** - AoE poison effect around player (Morthen religion)

#### Combat - Area Effects (2 effects)
12. **AoeCleave** - Melee attacks hit multiple enemies (Khoras)
13. **DeathAura** - Damage nearby enemies on kill (Morthen)

#### Defense (1 effect)
14. **DamageReduction10** - Reduce incoming damage by 10% (Aethra, Gaia)

#### Utility - Stealth & Vision (3 effects)
15. **StealthBonus** - Harder to detect by mobs/players (Umbros)
16. **TrackingVision** - See recent enemy movement trails (Lysa)
17. **AnimalCompanion** - Spawn loyal wolf companion (Lysa)

#### Utility - Combat Enhancement (2 effects)
18. **Multishot** - Fire 3 arrows simultaneously (Lysa)
19. **WarCry** - Temporary damage boost on command (Khoras religion)

#### Religion-Specific Effects (2 effects)
20. **PackTracking** - Religion members see shared tracking vision (Lysa religion)
21. **DeathMark** - Mark enemies for bonus damage by religion members (Morthen religion)

---

## Architecture Design

### Overview

The special effect handler system will use an **event-based architecture** that hooks into Vintage Story's entity damage, behavior, and action systems.

### Core Components

```
┌─────────────────────────────────────────────────────────────┐
│                  BlessingEffectSystem                       │
│  (Existing - manages stat modifiers)                        │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ delegates to
                     ▼
┌─────────────────────────────────────────────────────────────┐
│              SpecialEffectHandlerRegistry                   │
│  - Registers all special effect handlers                    │
│  - Dispatches effects to appropriate handlers               │
│  - Manages effect activation/deactivation                   │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ contains
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                 ISpecialEffectHandler                       │
│  Interface implemented by all handlers                      │
│  - OnDamageDealt(attacker, target, damage)                  │
│  - OnDamageReceived(victim, attacker, damage)               │
│  - OnPlayerAction(player, actionType)                       │
│  - OnTick(player, deltaTime)                                │
└─────────────────────────────────────────────────────────────┘
                     │
                     │ implemented by
                     ▼
┌─────────────────────────────────────────────────────────────┐
│           Concrete Effect Handlers                          │
│  - LifestealHandler                                         │
│  - CriticalStrikeHandler                                    │
│  - PoisonDotHandler                                         │
│  - StealthHandler                                           │
│  - ... (one handler per effect type)                        │
└─────────────────────────────────────────────────────────────┘
```

### Event Integration Points

#### 1. Damage Events (Combat Effects)
Hook into `Entity.ReceiveDamage()` via event system:
- **OnDamageDealt**: Triggers lifesteal, critical strikes, poison application
- **OnDamageReceived**: Triggers damage reduction, shields, counter-effects

#### 2. Entity Behaviors (Continuous Effects)
Use `EntityBehavior` pattern for per-tick effects:
- **Stealth**: Modify entity detection ranges
- **Tracking Vision**: Update visual overlays
- **Regeneration**: Apply healing per tick

#### 3. Action Events (Triggered Effects)
Hook into player actions:
- **Multishot**: Modify projectile spawning
- **War Cry**: Apply temporary buffs on command

### Handler Registration

```csharp
public class SpecialEffectHandlerRegistry
{
    private Dictionary<string, ISpecialEffectHandler> _handlers;
    private ICoreServerAPI _api;

    public void Initialize(ICoreServerAPI api)
    {
        _api = api;
        _handlers = new Dictionary<string, ISpecialEffectHandler>();

        // Register all handlers
        RegisterHandler("lifesteal3", new LifestealHandler(0.03f));
        RegisterHandler("lifesteal10", new LifestealHandler(0.10f));
        RegisterHandler("critical_strike", new CriticalStrikeHandler(0.10f, 2.0f));
        RegisterHandler("poison_dot", new PoisonDotHandler(2f, 5f));
        // ... register all 21 effects
    }

    public void OnDamageDealt(IPlayer attacker, Entity target, DamageSource damageSource, ref float damage)
    {
        var playerBlessings = GetPlayerActiveEffects(attacker.PlayerUID);

        foreach (var effectId in playerBlessings)
        {
            if (_handlers.TryGetValue(effectId, out var handler))
            {
                handler.OnDamageDealt(attacker, target, damageSource, ref damage);
            }
        }
    }
}
```

### Performance Considerations

1. **Effect Caching**: Cache active effects per player, refresh only on blessing unlock/religion change
2. **Event Filtering**: Only subscribe to events if at least one player has relevant effects
3. **Handler Pooling**: Reuse handler instances across players
4. **Batch Processing**: Process multiple effects in single event pass

---

## Implementation Phases

### Phase 1: Handler Infrastructure (~2-3 hours)

**Goal**: Build the foundation for all special effect handlers

**Tasks:**
1. Create `ISpecialEffectHandler` interface (30 min)
2. Create `SpecialEffectHandlerRegistry` system (1 hour)
3. Integrate with `BlessingEffectSystem` (1 hour)
4. Add damage event hooks (30 min)
5. Test infrastructure with mock handler (30 min)

**Deliverables:**
- Core handler system operational
- Event hooks connected
- One test handler working (e.g., simple damage modifier)

---

### Phase 2: Combat Effects (~3-4 hours)

**Goal**: Implement all combat-related special effects (highest priority)

#### 2.1 Lifesteal Effects (45 min)
```csharp
public class LifestealHandler : ISpecialEffectHandler
{
    private float _lifestealPercent;

    public LifestealHandler(float percent) => _lifestealPercent = percent;

    public void OnDamageDealt(IPlayer attacker, Entity target, DamageSource source, ref float damage)
    {
        float healAmount = damage * _lifestealPercent;

        var player = attacker as IServerPlayer;
        player.Entity.ReceiveDamage(new DamageSource()
        {
            Type = EnumDamageType.Heal,
            SourceEntity = player.Entity
        }, healAmount);
    }
}
```

**Variants**: Lifesteal3 (3%), Lifesteal10 (10%), Lifesteal15 (15%), Lifesteal20 (20%)

#### 2.2 Critical Strike Effects (1 hour)
```csharp
public class CriticalStrikeHandler : ISpecialEffectHandler
{
    private float _critChance;
    private float _critMultiplier;
    private Random _random = new Random();

    public void OnDamageDealt(IPlayer attacker, Entity target, DamageSource source, ref float damage)
    {
        if (_random.NextDouble() < _critChance)
        {
            damage *= _critMultiplier;

            // Visual feedback
            ShowCriticalHitEffect(attacker, target);

            // Audio feedback
            PlayCriticalSound(attacker);
        }
    }
}
```

**Variants**: CriticalChance10 (10%, 2x), CriticalChance20 (20%, 2x), HeadshotBonus (headshot detection)

#### 2.3 Execute Threshold (45 min)
```csharp
public class ExecuteHandler : ISpecialEffectHandler
{
    private float _threshold = 0.15f; // 15% HP

    public void OnDamageDealt(IPlayer attacker, Entity target, DamageSource source, ref float damage)
    {
        float targetHpPercent = target.WatchedAttributes.GetFloat("health") / target.WatchedAttributes.GetFloat("maxhealth");

        if (targetHpPercent <= _threshold)
        {
            // Instant kill
            damage = target.WatchedAttributes.GetFloat("health") + 100;

            // Visual effect
            ShowExecuteEffect(target);
        }
    }
}
```

#### 2.4 Damage Reduction (30 min)
```csharp
public class DamageReductionHandler : ISpecialEffectHandler
{
    private float _reductionPercent;

    public void OnDamageReceived(IPlayer victim, Entity attacker, DamageSource source, ref float damage)
    {
        damage *= (1f - _reductionPercent);
    }
}
```

#### 2.5 Poison DoT Effects (1 hour)
```csharp
public class PoisonDotHandler : ISpecialEffectHandler
{
    private float _damagePerSecond;
    private float _duration;

    public void OnDamageDealt(IPlayer attacker, Entity target, DamageSource source, ref float damage)
    {
        // Apply poison buff using BuffManager pattern
        var buff = new PoisonBuff()
        {
            DamagePerSecond = _damagePerSecond,
            Duration = _duration,
            SourcePlayer = attacker.PlayerUID
        };

        ApplyBuffToEntity(target, buff);
    }
}
```

**Variants**: PoisonDot (2 DPS, 5s), PoisonDotStrong (5 DPS, 8s), PlagueAura (AoE variant)

#### 2.6 AoE Cleave (45 min)
```csharp
public class AoeCleaveHandler : ISpecialEffectHandler
{
    private float _radius = 3f;

    public void OnDamageDealt(IPlayer attacker, Entity target, DamageSource source, ref float damage)
    {
        if (source.Type != EnumDamageType.SlashingAttack &&
            source.Type != EnumDamageType.BluntAttack) return;

        var nearbyEntities = GetEntitiesInRadius(target.Pos.XYZ, _radius);

        foreach (var entity in nearbyEntities)
        {
            if (entity != target && IsHostile(attacker, entity))
            {
                entity.ReceiveDamage(new DamageSource()
                {
                    Type = source.Type,
                    Source = EnumDamageSource.Player,
                    SourceEntity = attacker.Entity
                }, damage * 0.5f); // 50% splash damage
            }
        }
    }
}
```

**Estimated Phase 2 Total**: 3-4 hours

---

### Phase 3: Utility Effects (~2-3 hours)

**Goal**: Implement non-combat special effects

#### 3.1 Stealth Bonus (1 hour)
```csharp
public class StealthHandler : ISpecialEffectHandler
{
    public void OnPlayerLogin(IServerPlayer player)
    {
        // Add stealth behavior to player entity
        var stealthBehavior = new EntityBehaviorStealth(player.Entity);
        stealthBehavior.SetDetectionModifier(0.5f); // 50% harder to detect

        player.Entity.AddBehavior(stealthBehavior);
    }

    public void OnPlayerLogout(IServerPlayer player)
    {
        player.Entity.RemoveBehavior("stealth");
    }
}

public class EntityBehaviorStealth : EntityBehavior
{
    private float _detectionModifier = 1.0f;

    public override void OnEntitySpawn()
    {
        // Modify entity's detection range for hostile mobs
        entity.WatchedAttributes.SetFloat("detectionRange",
            entity.WatchedAttributes.GetFloat("detectionRange") * _detectionModifier);
    }
}
```

#### 3.2 Tracking Vision (45 min)
```csharp
public class TrackingVisionHandler : ISpecialEffectHandler
{
    private Dictionary<string, Queue<Vec3d>> _enemyTrails = new();
    private float _trailDuration = 30f; // 30 seconds

    public void OnTick(IServerPlayer player, float deltaTime)
    {
        // Track enemy positions
        var nearbyPlayers = GetNearbyEnemyPlayers(player, 50f);

        foreach (var enemy in nearbyPlayers)
        {
            RecordPosition(enemy.PlayerUID, enemy.Entity.Pos.XYZ);
        }

        // Send trail data to client for rendering
        SendTrailDataToClient(player);
    }

    private void RecordPosition(string playerUID, Vec3d position)
    {
        if (!_enemyTrails.ContainsKey(playerUID))
            _enemyTrails[playerUID] = new Queue<Vec3d>();

        var trail = _enemyTrails[playerUID];
        trail.Enqueue(position);

        // Remove old trail points
        while (trail.Count > 0 && IsExpired(trail.Peek()))
        {
            trail.Dequeue();
        }
    }
}
```

#### 3.3 Multishot (1 hour)
```csharp
public class MultishotHandler : ISpecialEffectHandler
{
    public void OnProjectileSpawn(IPlayer player, EntityProjectile projectile)
    {
        // Spawn 2 additional arrows at slight angles
        var baseVelocity = projectile.ServerPos.Motion;

        SpawnAdditionalArrow(player, baseVelocity, angleOffset: -15f);
        SpawnAdditionalArrow(player, baseVelocity, angleOffset: 15f);
    }

    private void SpawnAdditionalArrow(IPlayer player, Vec3d baseVelocity, float angleOffset)
    {
        var rotatedVelocity = RotateVectorY(baseVelocity, angleOffset);

        var arrow = player.World.SpawnEntity(new EntityProperties()
        {
            Code = new AssetLocation("arrow-flint")
        });

        arrow.ServerPos.Motion.Set(rotatedVelocity);
        // ... configure arrow damage, owner, etc.
    }
}
```

#### 3.4 Animal Companion (30 min)
```csharp
public class AnimalCompanionHandler : ISpecialEffectHandler
{
    private Dictionary<string, EntityAgent> _companions = new();

    public void OnBlessingUnlock(IServerPlayer player)
    {
        // Spawn wolf companion
        var wolf = player.World.SpawnEntity(new EntityProperties()
        {
            Code = new AssetLocation("wolf-male")
        }) as EntityAgent;

        // Make wolf friendly to player
        wolf.WatchedAttributes.SetString("owner", player.PlayerUID);
        wolf.WatchedAttributes.SetBool("tamed", true);

        _companions[player.PlayerUID] = wolf;
    }

    public void OnBlessingRemoved(IServerPlayer player)
    {
        if (_companions.TryGetValue(player.PlayerUID, out var wolf))
        {
            wolf.Die();
            _companions.Remove(player.PlayerUID);
        }
    }
}
```

**Estimated Phase 3 Total**: 2-3 hours

---

### Phase 4: Testing & Balance (~2-3 hours)

**Goal**: Verify all effects work correctly and balance values

#### 4.1 Unit Testing (1 hour)
- Test each handler in isolation
- Verify damage calculations
- Verify buff application/removal
- Test edge cases (multiple effects stacking, effect conflicts)

#### 4.2 Integration Testing (1 hour)
- Test multiple effects on single player
- Test religion + player effect combinations
- Test PvP scenarios with different deity matchups
- Verify network synchronization

#### 4.3 Balance Adjustments (1 hour)
- Playtest all deities
- Adjust lifesteal percentages if too strong/weak
- Tune critical strike chances
- Balance poison DoT damage values
- Adjust AoE radii and damage multipliers

---

## Priority Classification

### Critical (Must-Have for v1.0 Beta)
**Estimated Time**: ~4-5 hours

1. **Lifesteal** (all variants) - Core Khoras/Morthen mechanic
2. **Damage Reduction** - Core Aethra/Gaia mechanic
3. **Critical Strikes** - Core Lysa/Umbros mechanic
4. **Execute Threshold** - High-impact Khoras/Morthen ability

**Rationale**: These effects are referenced in multiple high-tier blessings and define deity playstyles.

### High Priority (Launch Features)
**Estimated Time**: ~3-4 hours

5. **Poison DoT** - Core Morthen/Lysa mechanic
6. **Stealth Bonus** - Core Umbros mechanic
7. **AoE Cleave** - Khoras combat identity
8. **Multishot** - Lysa ranged identity

**Rationale**: These effects significantly impact gameplay but aren't in every blessing tree.

### Medium Priority (Post-Launch v1.1)
**Estimated Time**: ~2-3 hours

9. **Tracking Vision** - Lysa utility
10. **Animal Companion** - Lysa flavor
11. **War Cry** - Khoras religion buff
12. **Plague Aura** - Morthen religion mechanic

**Rationale**: Nice-to-have features that enhance but aren't essential to core gameplay.

### Low Priority (Future Enhancements)
**Estimated Time**: ~1-2 hours

13. **Death Aura** - Visual flair
14. **Headshot Bonus** - Niche mechanic
15. **Pack Tracking** - Religion coordination
16. **Death Mark** - Religion coordination

**Rationale**: These add polish and advanced coordination mechanics but can be deferred.

---

## Technical Specifications

### Combat Damage Effects

#### Lifesteal Pattern
```csharp
public class LifestealHandler : ISpecialEffectHandler
{
    private readonly float _healPercent;

    public LifestealHandler(float percent) => _healPercent = percent;

    public void OnDamageDealt(IServerPlayer attacker, Entity target, DamageSource source, ref float damage)
    {
        // Only trigger on actual damage dealt (not healing, etc.)
        if (damage <= 0) return;

        float healAmount = damage * _healPercent;

        // Apply healing to attacker
        attacker.Entity.ReceiveDamage(new DamageSource()
        {
            Type = EnumDamageType.Heal,
            Source = EnumDamageSource.Internal
        }, healAmount);

        // Visual feedback
        SpawnHealingParticles(attacker.Entity.Pos.XYZ);
    }
}
```

#### Critical Strike Pattern
```csharp
public class CriticalStrikeHandler : ISpecialEffectHandler
{
    private readonly float _critChance;
    private readonly float _critMultiplier;
    private readonly Random _random = new Random();

    public void OnDamageDealt(IServerPlayer attacker, Entity target, DamageSource source, ref float damage)
    {
        // Roll for critical strike
        if (_random.NextDouble() < _critChance)
        {
            float originalDamage = damage;
            damage *= _critMultiplier;

            // Send critical hit notification
            SendCriticalHitMessage(attacker, damage - originalDamage);

            // Visual and audio feedback
            SpawnCriticalParticles(target.Pos.XYZ);
            PlaySound(attacker, "game:sounds/player/strike-critical");
        }
    }
}
```

### Damage Over Time Effects

#### Poison DoT Pattern
```csharp
public class PoisonDotHandler : ISpecialEffectHandler
{
    private readonly float _damagePerSecond;
    private readonly float _duration;

    public void OnDamageDealt(IServerPlayer attacker, Entity target, DamageSource source, ref float damage)
    {
        // Apply poison buff to target
        var poisonBuff = new PoisonBuff()
        {
            Type = "poison",
            DamagePerSecond = _damagePerSecond,
            Duration = _duration,
            TickInterval = 1.0f, // Damage every 1 second
            SourcePlayerUID = attacker.PlayerUID,
            RemainingDuration = _duration
        };

        // Use existing BuffManager pattern
        var buffBehavior = target.GetBehavior<EntityBehaviorBuffTracker>();
        if (buffBehavior != null)
        {
            buffBehavior.ApplyBuff(poisonBuff);
        }

        // Visual feedback
        SpawnPoisonParticles(target.Pos.XYZ);
    }
}

// Buff implementation
public class PoisonBuff : Buff
{
    public float DamagePerSecond { get; set; }
    private float _tickTimer = 0f;

    public override void OnTick(Entity entity, float deltaTime)
    {
        _tickTimer += deltaTime;

        if (_tickTimer >= TickInterval)
        {
            _tickTimer = 0f;

            // Apply poison damage
            entity.ReceiveDamage(new DamageSource()
            {
                Type = EnumDamageType.Poison,
                Source = EnumDamageSource.Internal
            }, DamagePerSecond);

            // Visual tick
            SpawnPoisonTickParticles(entity.Pos.XYZ);
        }
    }
}
```

### Defensive Effects

#### Damage Reduction Pattern
```csharp
public class DamageReductionHandler : ISpecialEffectHandler
{
    private readonly float _reductionPercent;

    public DamageReductionHandler(float percent) => _reductionPercent = percent;

    public void OnDamageReceived(IServerPlayer victim, Entity attacker, DamageSource source, ref float damage)
    {
        // Don't reduce healing or true damage
        if (source.Type == EnumDamageType.Heal) return;

        float reducedAmount = damage * _reductionPercent;
        damage -= reducedAmount;

        // Visual feedback for damage blocked
        if (reducedAmount > 0)
        {
            SpawnShieldParticles(victim.Entity.Pos.XYZ);
            SendDamageBlockedMessage(victim, reducedAmount);
        }
    }
}
```

### Utility Effects

#### Stealth Pattern
```csharp
public class StealthHandler : ISpecialEffectHandler
{
    private readonly float _detectionModifier = 0.5f; // 50% harder to detect

    public void OnBlessingActivate(IServerPlayer player)
    {
        // Add stealth behavior if not present
        var stealthBehavior = player.Entity.GetBehavior<EntityBehaviorStealth>();

        if (stealthBehavior == null)
        {
            stealthBehavior = new EntityBehaviorStealth(player.Entity);
            player.Entity.AddBehavior(stealthBehavior);
        }

        stealthBehavior.SetDetectionModifier(_detectionModifier);
    }

    public void OnBlessingDeactivate(IServerPlayer player)
    {
        player.Entity.RemoveBehavior<EntityBehaviorStealth>();
    }
}

public class EntityBehaviorStealth : EntityBehavior
{
    private float _detectionModifier = 1.0f;

    public EntityBehaviorStealth(Entity entity) : base(entity) { }

    public override string PropertyName() => "stealth";

    public void SetDetectionModifier(float modifier)
    {
        _detectionModifier = modifier;
        UpdateEntityAttributes();
    }

    private void UpdateEntityAttributes()
    {
        // Modify how far away mobs can detect this entity
        entity.WatchedAttributes.SetFloat("detectionRangeModifier", _detectionModifier);
    }
}
```

---

## Testing Strategy

### Unit Testing Approach

```csharp
[TestFixture]
public class SpecialEffectHandlerTests
{
    [Test]
    public void LifestealHandler_Should_HealAttacker()
    {
        // Arrange
        var handler = new LifestealHandler(0.10f); // 10% lifesteal
        var mockAttacker = CreateMockPlayer("attacker-1");
        var mockTarget = CreateMockEntity();
        float damage = 100f;

        float initialHealth = mockAttacker.Entity.WatchedAttributes.GetFloat("health");

        // Act
        handler.OnDamageDealt(mockAttacker, mockTarget, null, ref damage);

        // Assert
        float expectedHeal = 10f; // 10% of 100
        float finalHealth = mockAttacker.Entity.WatchedAttributes.GetFloat("health");
        Assert.AreEqual(initialHealth + expectedHeal, finalHealth);
    }

    [Test]
    public void CriticalStrikeHandler_Should_IncreaseDamage_WhenCritical()
    {
        // Arrange
        var handler = new CriticalStrikeHandler(1.0f, 2.0f); // 100% crit, 2x mult
        float baseDamage = 50f;
        float damage = baseDamage;

        // Act
        handler.OnDamageDealt(mockAttacker, mockTarget, null, ref damage);

        // Assert
        Assert.AreEqual(baseDamage * 2.0f, damage);
    }

    [Test]
    public void PoisonDotHandler_Should_ApplyBuff()
    {
        // Arrange
        var handler = new PoisonDotHandler(2f, 5f);
        var mockTarget = CreateMockEntityWithBuffBehavior();

        // Act
        handler.OnDamageDealt(mockAttacker, mockTarget, null, ref damage);

        // Assert
        var buffBehavior = mockTarget.GetBehavior<EntityBehaviorBuffTracker>();
        Assert.IsTrue(buffBehavior.HasBuff("poison"));
    }
}
```

### Integration Testing

**Test Scenarios:**

1. **Multiple Effects Stacking**
   - Player with Lifesteal + Critical Strike
   - Verify critical hits also heal (lifesteal applies to crit damage)
   - Check damage calculation order

2. **Effect Conflicts**
   - DamageReduction + Shield effects
   - Verify effects don't over-reduce damage (min 0)

3. **Religion + Player Effects**
   - Player blessing: +10% damage
   - Religion blessing: War Cry (+15% damage)
   - Verify both apply (multiplicative or additive based on design)

4. **Effect Removal**
   - Player leaves religion with religion effects
   - Verify effects are properly removed
   - Check no lingering buffs

5. **PvP Cross-Deity Testing**
   - Khoras (lifesteal) vs Aethra (damage reduction)
   - Umbros (stealth) vs Lysa (tracking vision)
   - Verify effects interact correctly

### In-Game Testing Checklist

- [ ] Lifesteal: Deal damage, verify health increases
- [ ] Critical Strike: Deal damage, verify occasional crits with visual/audio
- [ ] Execute: Hit low-HP enemy, verify instant kill
- [ ] Damage Reduction: Take damage, verify reduced amount
- [ ] Poison DoT: Hit enemy, verify DoT ticks appear
- [ ] AoE Cleave: Attack with enemies nearby, verify splash damage
- [ ] Stealth: Approach mobs, verify detection range reduced
- [ ] Tracking: Have enemy move nearby, verify trails appear
- [ ] Multishot: Fire arrow, verify 3 arrows spawn
- [ ] Animal Companion: Unlock blessing, verify wolf spawns

---

## Integration Points

### Vintage Story API Hooks

#### 1. Damage System
```csharp
// Hook into entity damage
entity.OnHurt += OnEntityHurt;

private void OnEntityHurt(DamageSource source, float damage)
{
    if (source.SourceEntity is EntityPlayer attacker)
    {
        var attackerPlayer = GetPlayerByEntity(attacker);

        // Trigger OnDamageDealt handlers
        _effectRegistry.OnDamageDealt(attackerPlayer, entity, source, ref damage);
    }

    if (entity is EntityPlayer victim)
    {
        var victimPlayer = GetPlayerByEntity(victim);

        // Trigger OnDamageReceived handlers
        _effectRegistry.OnDamageReceived(victimPlayer, source.SourceEntity, source, ref damage);
    }
}
```

#### 2. Projectile System
```csharp
// Hook into projectile spawning for multishot
api.Event.OnEntitySpawn += OnEntitySpawn;

private void OnEntitySpawn(Entity entity)
{
    if (entity is EntityProjectile projectile &&
        projectile.FiredBy is EntityPlayer shooter)
    {
        var player = GetPlayerByEntity(shooter);

        // Check for multishot effect
        if (HasEffect(player, "multishot"))
        {
            _multishotHandler.OnProjectileSpawn(player, projectile);
        }
    }
}
```

#### 3. Entity Behaviors
```csharp
// Add custom behaviors to player entities
api.Event.PlayerJoin += (player) =>
{
    var effects = GetPlayerActiveEffects(player.PlayerUID);

    if (effects.Contains("stealth_bonus"))
    {
        var stealthBehavior = new EntityBehaviorStealth(player.Entity);
        player.Entity.AddBehavior(stealthBehavior);
    }

    if (effects.Contains("tracking_vision"))
    {
        var trackingBehavior = new EntityBehaviorTracking(player.Entity);
        player.Entity.AddBehavior(trackingBehavior);
    }
};
```

#### 4. Buff System Integration
```csharp
// Reuse existing BuffManager pattern
public void ApplyPoisonBuff(Entity target, float dps, float duration)
{
    var buffBehavior = target.GetBehavior<EntityBehaviorBuffTracker>();

    if (buffBehavior == null)
    {
        buffBehavior = new EntityBehaviorBuffTracker(target);
        target.AddBehavior(buffBehavior);
    }

    var poisonBuff = new PoisonBuff()
    {
        DamagePerSecond = dps,
        Duration = duration
    };

    buffBehavior.ApplyBuff(poisonBuff);
}
```

### Integration with BlessingEffectSystem

```csharp
// BlessingEffectSystem.cs updates

public class BlessingEffectSystem
{
    private SpecialEffectHandlerRegistry _specialEffectRegistry;

    public void Initialize(ICoreServerAPI api)
    {
        // Existing initialization...

        // Initialize special effect handlers
        _specialEffectRegistry = new SpecialEffectHandlerRegistry(api);
        _specialEffectRegistry.Initialize();

        // Hook into damage events
        RegisterDamageHooks();
    }

    private void RegisterDamageHooks()
    {
        _api.Event.OnEntityDeath += (entity, damageSource) =>
        {
            if (damageSource?.SourceEntity is EntityPlayer attacker)
            {
                var player = GetPlayerByEntity(attacker);
                _specialEffectRegistry.OnEntityKill(player, entity);
            }
        };
    }

    public void RefreshPlayerBlessings(string playerUID)
    {
        // Existing stat modifier refresh...

        // Refresh special effects
        _specialEffectRegistry.RefreshPlayerEffects(playerUID);
    }
}
```

---

## Timeline & Effort Estimate

### Detailed Hour Breakdown

#### Phase 1: Infrastructure (2-3 hours)
- [ ] 0.5h - Create `ISpecialEffectHandler` interface
- [ ] 1.0h - Create `SpecialEffectHandlerRegistry` class
- [ ] 1.0h - Integrate with `BlessingEffectSystem`
- [ ] 0.5h - Add damage event hooks
- [ ] 0.5h - Test with mock handler

**Phase 1 Subtotal**: 2.5-3.5 hours

#### Phase 2: Combat Effects (3-4 hours)
- [ ] 0.75h - Lifesteal handler (4 variants)
- [ ] 1.0h - Critical strike handler (3 variants)
- [ ] 0.75h - Execute threshold handler
- [ ] 0.5h - Damage reduction handler
- [ ] 1.0h - Poison DoT handler (3 variants)
- [ ] 0.75h - AoE cleave handler

**Phase 2 Subtotal**: 4.75 hours → **Estimate: 3-4 hours** (with efficiency gains)

#### Phase 3: Utility Effects (2-3 hours)
- [ ] 1.0h - Stealth handler + entity behavior
- [ ] 0.75h - Tracking vision handler
- [ ] 1.0h - Multishot handler
- [ ] 0.5h - Animal companion handler

**Phase 3 Subtotal**: 3.25 hours → **Estimate: 2-3 hours**

#### Phase 4: Testing & Balance (2-3 hours)
- [ ] 1.0h - Unit testing (all handlers)
- [ ] 1.0h - Integration testing (combinations, PvP)
- [ ] 1.0h - Balance adjustments and playtesting

**Phase 4 Subtotal**: 3 hours

### Total Estimated Time: **10-13 hours**

**Breakdown by Priority:**
- Critical effects only: **4-5 hours**
- Critical + High Priority: **7-9 hours**
- All effects (full implementation): **10-13 hours**

### Recommended Development Schedule

**Week 1** (Critical Effects - 4-5 hours)
- Day 1-2: Infrastructure + Lifesteal + Damage Reduction (3h)
- Day 3: Critical Strikes + Execute (2h)
- Day 4: Testing critical effects (1h)

**Week 2** (High Priority - 3-4 hours)
- Day 1: Poison DoT + AoE Cleave (2h)
- Day 2: Stealth + Multishot (2h)
- Day 3: Integration testing (1h)

**Week 3** (Polish - 2-3 hours)
- Day 1: Remaining utility effects (1.5h)
- Day 2: Balance testing (1h)
- Day 3: Final polish and documentation (0.5h)

---

## Success Criteria

### Technical Completion
- [ ] All 21 special effects have handler implementations
- [ ] All handlers pass unit tests
- [ ] Integration testing passes (no conflicts, proper stacking)
- [ ] Performance acceptable (no lag with 10+ players using effects)
- [ ] Network sync working (clients see effect visuals)

### Gameplay Validation
- [ ] Each deity has distinct playstyle feel
- [ ] No single effect dominates (balanced)
- [ ] Effects provide noticeable gameplay impact
- [ ] Visual and audio feedback present for major effects
- [ ] PvP encounters showcase deity differences

### Code Quality
- [ ] All handlers follow `ISpecialEffectHandler` interface
- [ ] Proper error handling (null checks, edge cases)
- [ ] Code documented with XML comments
- [ ] No code duplication (shared logic in base classes)
- [ ] Performance optimized (caching, event filtering)

### Player Experience
- [ ] Effects feel impactful but not overpowered
- [ ] Visual feedback is clear but not overwhelming
- [ ] Tooltip descriptions match actual behavior
- [ ] No confusing interactions between effects
- [ ] Effects contribute to strategic depth

---

## Risk Mitigation

### Technical Risks

**Risk**: Vintage Story damage system hooks may not provide enough control
- **Mitigation**: Research VS API thoroughly before implementation; use entity behaviors as alternative if hooks insufficient

**Risk**: Performance issues with many players using effects simultaneously
- **Mitigation**: Implement caching and event filtering early; profile performance during Phase 4

**Risk**: Effect stacking creates exploits (e.g., 100% damage reduction)
- **Mitigation**: Implement hard caps on stacking effects; test edge cases thoroughly

### Scope Risks

**Risk**: 13 hours estimate may be optimistic
- **Mitigation**: Use priority classification; ship Critical effects first, defer Low Priority to v1.1

**Risk**: Effect balance requires extensive playtesting
- **Mitigation**: Start conservative (low values); iterate based on feedback; community playtesting

### Integration Risks

**Risk**: Effects may break existing systems (stat modifiers, buffs)
- **Mitigation**: Thorough integration testing; maintain separation of concerns; use feature flags

---

## Next Steps

### Immediate Actions (Before Implementation)

1. **Review this plan** with development team
2. **Approve priority classification** (Critical vs Low Priority)
3. **Set target date** for Critical effects (v1.0 beta)
4. **Create feature branch**: `feature/special-effect-handlers`
5. **Set up testing environment** with test blessings

### Implementation Kickoff

1. **Week 1 Focus**: Infrastructure + Critical effects
2. **Daily standups**: Track progress, identify blockers
3. **Mid-week check-in**: Review handler implementations
4. **End-of-week demo**: Show working critical effects

### Post-Implementation

1. **Community playtesting**: Gather feedback on balance
2. **Iteration**: Adjust values based on data
3. **Documentation**: Update blessing descriptions with actual mechanics
4. **v1.1 planning**: Schedule remaining Low Priority effects

---

## Appendix: Effect-to-Blessing Mapping

### Khoras (War) Blessings
| Blessing | Special Effect |
|----------|----------------|
| Bloodlust | lifesteal3 |
| Berserker Rage | lifesteal10 |
| Avatar of War | lifesteal20, aoe_cleave, execute_threshold |
| War Banner (religion) | war_cry |

### Lysa (Hunt) Blessings
| Blessing | Special Effect |
|----------|----------------|
| Lethal Precision | critical_chance10 |
| Deadly Ambush | critical_chance10, headshot_bonus |
| Avatar of Hunt | multishot, critical_chance20, poison_dot |
| Pack Tactics (religion) | pack_tracking |

### Morthen (Death) Blessings
| Blessing | Special Effect |
|----------|----------------|
| Death's Touch | lifesteal3 |
| Pandemonium | lifesteal15, poison_dot_strong |
| Avatar of Death | execute_threshold, death_aura |
| Plague Legion (religion) | plague_aura, death_mark |

### Aethra (Light) Blessings
| Blessing | Special Effect |
|----------|----------------|
| Blessed Shield | damage_reduction10 |
| Avatar of Light | healing_aura |

### Umbros (Shadows) Blessings
| Blessing | Special Effect |
|----------|----------------|
| Shadow Blend | stealth_bonus |
| Assassinate | critical_chance10 |
| Vanish | stealth_bonus_strong |
| Avatar of Shadows | critical_chance20, execute_threshold |

### Gaia (Earth) Blessings
| Blessing | Special Effect |
|----------|----------------|
| Mountain Guard | damage_reduction10 |
| Lifebloom | regeneration |

### Tharos (Storms) & Vex (Madness)
*Note: These deities have fewer special effects, focusing more on stat modifiers*

---

**Document Version**: 1.0
**Created**: 2025-11-15
**Status**: Planning Complete - Ready for Implementation
**Estimated Total Effort**: 10-13 hours (Critical path: 4-5 hours)
