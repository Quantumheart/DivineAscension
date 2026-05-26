# Civilization Edicts System - Implementation Plan

## Overview

Add **edicts** (decrees): founder-activated civilization policies that boost one
mechanic at the cost of another. Edicts give the currently-inert
`CivilizationData.Ethos` field real weight, and introduce the mod's first
*negative* civilization modifiers — every existing modifier today is a positive
milestone reward.

The feature builds on existing patterns (milestone bonuses, JSON-driven loaders,
founder-only civilization governance, the `"divineascension"` network channel,
the ImGui civilization tab) and integrates with the favor, prestige, holy-site,
PvP, and diplomacy systems.

## Scope

- **Civilization-level only**, founder-only activation (governance model).
- **Private** — only the owning civilization sees its active edicts; members
  read-only, founder controls.
- **Ethos is immutable** — set once at founding, never changed. Enforced
  behaviorally (no change-ethos action); the property keeps a setter for
  protobuf deserialization only.
- **Rank-gated slots** — a civ runs 1 edict at low rank, scaling to 3 at Eternal.
- **Tiered edicts** — stronger edicts gate behind `MinRank`.
- **Cross-ethos access** — a universal pool every civ can use, plus a thin pool
  per ethos (mirrors how blessings are open).
- **Real-time switch lockout** — activating an edict commits the civ for a
  real-time duration (`DateTime.UtcNow`), persisted on the civ. Not in-game time
  (skippable via sleep/`/time`) and not `CooldownManager` (in-memory, lost on
  restart).

## Key design decisions (resolved)

| Question | Decision | Rationale |
|----------|----------|-----------|
| Switch lockout time base | Real-world time | Game time is skippable, defeating the anti-pre-war-buff purpose; matches `CivilizationData.CreatedDate` (DateTime) |
| Ethos mutability | Immutable at founding | Prevents pool-hopping to cherry-pick edicts |
| Diplomacy-lock semantics | Breaks existing conflicting treaties | Closes the deactivate-sign-reactivate dodge |
| Runaway prevention | Duplicate-lever rule | Each edict declares a `PrimaryLever`; two active edicts can't share one. Cleaner than numeric caps, enforces loadout diversity |
| Negative effects | New `CivilizationNegatives` record | Keeps the milestone invariant (bonuses always positive); a resolver computes net |

## Modifier model

`CivilizationBonuses` (existing) carries only positive milestone rewards.
Edicts contribute signed effects. A new `CivilizationModifierResolver`
combines milestone bonuses + active edict effects into a **net** value per
lever, applied once at grant time.

### Stacking math

Multipliers (Favor / Prestige / Conquest) accumulate in delta-space around 1.0:

```
net = 1.0 + Σ(milestone deltas) + Σ(edict deltas, signed)
```

Clamps:

- Favor / Prestige: floor **0.5**, cap **3.0**
- Conquest: floor **0.0** (a pacifist edict may fully suppress), cap **3.0**
- Holy-site slots (int): final cap floored at **1**; a negative-slot edict only
  blocks new sites (`HolySiteManager.CanCreateHolySite` is `current < max`, no
  forced removal — verified)
- Death favor-loss multiplier: cap **1.0**

Domain-weighted favor is a second multiplicative layer, orthogonal to the global
favor multiplier so the global economy stays balanced while distribution tilts:

```
domainMult(d) = netGlobalFavorMult × (1.0 + Σ edict domain deltas for d)
```

Per-domain floor ~**0.25** (a taxed domain still trickles).

### Required apply-site change

All four apply sites currently guard `> 1.0f`, silently swallowing sub-1.0
values (`FavorSystem.cs:383,424`, `PvPManager.cs:202,225`). These must be
rewritten to apply the resolver's net multiplier unconditionally (clamped).
This is a prerequisite for any negative edict to function.

## Edict pools (v1, 13 total)

Each entry is a signed effect map. Downsides favor **structural constraints**
(caps, locks, vulnerabilities) over mirror-penalties, so no playstyle dodges them.

**Universal (3, all Tier-I, all civs):**

| Edict | Boost | Cost |
|-------|-------|------|
| Zealous Tithe | +Favor | −Prestige |
| Monument Cult | +Prestige | −Favor |
| Grand Reliquary | +1 Holy-site slot | −Favor |

**Ethos pools (2 each):** Martial (Conquest), Ascetic (favor / pacifism),
Mystic (Prestige / holy-site), Sovereign (capital-domain weighting),
Mercantile (broad favor / roster lock). Domain-weighted favor and
diplomacy/roster locks are the identity levers (Slices 7–8).

## Vertical slices

1. **Net-Modifier Engine (enabler)** — `CivilizationNegatives`,
   `CivilizationModifierResolver`, rewrite the four `> 1.0f` apply-site guards,
   stacking math + clamps. No edicts yet; net behavior matches today but sub-1.0
   becomes possible. Tests.
2. **Edict definitions + loader + fake provider (enabler)** — `EdictDefinition`,
   `EdictEffect`, `EdictEffectType`, `PrimaryLever`, `MinRank`, `Tier`;
   `EdictLoader` (JSON, mirrors `MilestoneDefinitionLoader`); `IEdictProvider` +
   `FakeEdictProvider` for UI styling. Author the 13 edicts as data.
3. **Decrees UI shell + founding ethos tooltip** — private "Decrees" sub-page
   under the Civilization tab (founder controls, members read-only) backed by the
   fake provider; founding-flow tooltip previewing each ethos's pool.
4. **Activation core + persistence + networking (one edict end-to-end)** —
   `ActiveEdictIds` + per-slot set-dates on `CivilizationData`, DataVersion 4→5
   + migration; `EdictManager` (founder-only set/clear, single slot, real-time
   lockout); set/clear/query packets; wire one universal edict through the
   resolver to the real UI. Enforce ethos immutability.
5. **Multi-slot + rank tiers + duplicate-lever rule** — slot count from
   `GetCivilizationRank` (1→3), `MinRank` tier gating, duplicate-lever rejection.
6. **Universal pool complete (3 edicts)** — Zealous Tithe, Monument Cult,
   Grand Reliquary.
7. **Ethos pools + domain-weighted favor lever (10 edicts)** — new
   domain-weighted favor hook in `FavorSystem`; 2 edicts per ethos across tiers.
8. **Constraint-cost levers (diplomacy/roster locks)** — `DiplomacyManager` hook
   to break conflicting treaties on activation; roster lock; death-vulnerability.
   Highest blast radius, shipped last.

## Persistence

`CivilizationData` gains `ActiveEdictIds` (set) and per-slot activation
timestamps. Bump `DataVersion` 4 → 5 with a migration defaulting existing civs
to no active edicts.

## Initialization order

`EdictLoader` loads before `EdictManager` (mirrors
`MilestoneDefinitionLoader` → consumers). `CivilizationModifierResolver`
constructs after `CivilizationMilestoneManager` (its bonus source) and is wired
into `FavorSystem`, `HolySiteManager`, `PvPManager` via `Set*()` calls like the
existing `CivilizationBonusSystem`.

## Out of scope (v1)

- "Rent a civ-wide blessing while active" lever (most complex / exploit-prone) —
  deferred to a later tier.
- War-state-conditional edicts beyond the diplomacy locks in Slice 8.
