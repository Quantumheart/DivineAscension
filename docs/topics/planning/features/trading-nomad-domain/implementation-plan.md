# Caravan Domain (Trade + Nomad) Implementation Plan

**Status**: Design / pre-implementation
**Created**: 2026-05-23
**Target branch**: `claude/trading-nomad-domain-RlR9l`

## Overview

Add a sixth playable `DeityDomain` covering **exchange and motion** — the deity (working title: **Vehari, the Caravan-Wanderer**) blesses those who trade, gift, and travel. Mechanically, two complementary verbs feed one favor pool:

- **Trade** — completing transactions with NPC traders and gifting goods between players.
- **Nomad** — discovering new chunks, landmarks, and translocators.

The blessing tree branches along the same split (Trade vs. Wayfaring) with a shared capstone, matching the existing Branching system already used by Stone (`docs/topics/planning/features/blessing-branching/implementation-plan.md`).

## Design Goals

1. **Distinct play loop** — neither Craft nor Conquest currently rewards movement or merchant activity; Caravan fills that gap.
2. **No new core systems** — reuse the favor → rank → blessing pipeline. New trackers, new JSON, new portable-altar BlockBehavior; no changes to FavorSystem dispatch.
3. **Resistant to AFK farming** — distance-only metrics are tempting but easy to grief; favor sources tie to *discrete events* (trade completion, first-time chunk visit, gift accepted).
4. **Server-authoritative** — every favor grant flows through `IFavorSystem.AwardFavorForAction`, same as existing domains.

---

## Domain Enum & Identity

### Enum value

`DivineAscension/Models/Enum/DeityDomain.cs:6` — current values are sparse (`Stone = 7`, gaps at 5 and 6). Proposed:

```csharp
public enum DeityDomain
{
    None     = 0,
    Craft    = 1,
    Wild     = 2,
    Conquest = 3,
    Harvest  = 4,
    Caravan  = 5,   // NEW
    Stone    = 7
}
```

**Open question:** are slots 5 and 6 reserved for in-flight domains? If so, append at the next free value. Either way, **never reuse a retired value** — `PlayerProgressionData` persists this enum in proto, so a collision corrupts saves.

### Naming

- **Enum / code:** `Caravan` — single word, parallel to `Craft`/`Stone`/`Wild`.
- **Deity display name:** `Vehari` (placeholder; choose alongside the existing pantheon).
- **Domain title (`DomainHelper.GetDeityTitle`):** `"Domain of the Caravan"`.
- **Color (`DomainHelper.GetDeityColor`):** dusty saffron / road-ochre — distinct from harvest wheat and stone slate. Suggest `RGBA(0.78, 0.55, 0.21, 1.0)`.
- **Glyph (`DomainGlyphRenderer`):** wagon wheel (circle + 6 spokes), or a stylized compass rose. Vector-drawn, no PNG.

---

## Favor Sources & Trackers

Three trackers, each implementing `IFavorTracker` with `DeityDomain = DeityDomain.Caravan`. All live under `Systems/Favor/`.

### 1. `TraderTransactionFavorTracker`

**Hook:** Harmony postfix on the trader-conversation completion. Vintage Story's trader is `EntityTrader` with `InventoryTrader`; the close/confirm path is the most reliable site. Two candidates:

- `InventoryTrader.PerformTrade(...)` — fires per transaction.
- `EntityTrader.OnInteract(...)` close branch — fires once per conversation, can read `InventoryTrader.TraderBuys` / `TraderSells` to diff inventories.

Prefer `PerformTrade` if it exists in 1.22 (verify via `ilspycmd "$VINTAGE_STORY/Mods/VSSurvivalMod.dll" | grep -B1 PerformTrade`). Patch class goes under `Systems/Patches/TraderPatches.cs`, exposes an event the tracker subscribes to (same pattern as `AnvilPatches` → `AnvilFavorTracker`).

**Favor formula (initial):**
```
baseFavor = 2
+ floor(transactionValue / 10)   // 1 favor per 10 gear value
* rarityMultiplier(item)         // 1.0 common, 1.5 uncommon, 2.0 rare
```
Cap at ~20 favor per transaction to prevent whale-trade exploits.

### 2. `ExplorationFavorTracker`

**Hook:** Periodic scan, modelled on `RuinDiscoveryFavorTracker` (`Systems/Favor/RuinDiscoveryFavorTracker.cs:55`). Each tick (~2 s), for each online player, get the chunk coord they stand in; if not in `PlayerProgressionData.DiscoveredChunks` (new HashSet<long>), grant favor and add it.

**Favor formula:**
- Normal new chunk: `1` favor.
- New translocator/teleporter discovered (block code match): `+5` favor bonus.
- New trader spawn discovered (entity scan in radius, like ruin tracker): `+10` favor bonus, once per trader entity ID.

**Anti-AFK:** require chunk to be *traversed* — only credit on chunk transition (player was in chunk A last tick, is in B this tick), not on standing inside one.

**Persistence:** `DiscoveredChunks` adds a new ProtoMember to `PlayerProgressionData`. Bump `DataVersion = 5` and write a migration that leaves the set empty for old saves (no rewards lost — exploration starts fresh).

### 3. `GiftingFavorTracker`

**Hook:** Two options, pick one based on appetite:

- **(Preferred) Dedicated `/gift <player>` chat command.** Pops the active hotbar slot's stack, deposits into the target's inventory, fires the favor grant on receipt. Clear intent, no false positives.
- **(Alternative) Drop-and-pickup heuristic.** Patch `Entity.OnInteract` on `EntityItem`; if pickup is by a *different* player than the dropper within a window (e.g., 30 s) and the item value is non-trivial, count as gift. Fragile and game-able.

Go with the `/gift` command via `IChatCommandService` — clearer, testable, no Harmony patch. Cooldown of 60 s per (giver, recipient) pair via the existing `CooldownManager`.

**Favor formula:**
- Giver: `floor(itemValue / 5)`, capped at 10.
- Recipient: small flat `+2` (encourages accepting gifts, but not a farm vector).

---

## Blessing Tree

Two exclusive branches plus a Shared capstone. Tier numbers and `requiredFavorRank` mirror Stone's layout.

```
Domain: Caravan (Vehari)
├── Branch: Trade
│   ├── Haggler's Tongue     (Tier 1, rank 1)  — +10% trader buy prices, restock −20% time
│   ├── Silver-Tongued       (Tier 2, rank 3)  — unlocks rare-ware tier from traders
│   └── Master Merchant      (Tier 3, rank 5)  — trader sell prices +25%, gift cooldown halved
│
├── Branch: Wayfaring
│   ├── Sure-Footed          (Tier 1, rank 1)  — +10% walk speed, off-road only
│   ├── Light Step           (Tier 2, rank 3)  — −25% hunger drain while sprinting, no fall damage <4 blocks
│   └── Pathfinder           (Tier 3, rank 5)  — reveal nearby translocator/trader on map; +15% chunk-exploration favor
│
└── Shared
    ├── Caravan's Burden     (Tier 1, rank 0)  — Foundation; +1 hotbar slot effective (carry-weight stat)
    └── Avatar of the Road   (Tier 4 capstone) — place a temporary altar anywhere (see "Portable Altar")
```

JSON files to create:

- `assets/divineascension/config/blessings/caravan.json` — schema matches `blessings/stone.json` (kind, category, iconName, requiredFavorRank, requiredPrestigeRank, prerequisiteBlessings, statModifiers, specialEffects, cost, branch, exclusiveBranches).
- `assets/divineascension/config/offerings/caravan.json` — coins/gear, exotic trader-only items, foreign-region resources.
- `assets/divineascension/config/rituals/caravan.json` — tier-up rituals: Tier 1→2 requires *N* completed trades + *N* gear; Tier 2→3 requires reaching the four cardinal directions from the altar (chunk-distance check), themed as "Pilgrimage Rite".

**Stat modifiers** — three are new and need to flow through `BlessingStatApplication`:

| Stat key | Affects | Implementation |
|---|---|---|
| `traderBuyPriceMultiplier` | NPC trade prices when player buys | Read in `TraderPatches` postfix on price calc |
| `traderSellPriceMultiplier` | NPC trade prices when player sells | Same |
| `chunkDiscoveryFavorMultiplier` | Multiplies favor from exploration tracker | Read in `ExplorationFavorTracker` |

The `walkspeed`, `hungerRate`, and carry-weight equivalents already exist (Stone uses `walkspeed: 0.05`) — reuse them.

---

## Custom Content

### Portable Altar (`BlockBehaviorCaravanShrine`)

A capstone-blessing-gated altar that the player can place anywhere outdoors and remove without losing it. Mechanics:

- New block `divineascension:caravanshrine` — wagon-and-cloth visual. Single-block.
- `BlockBehaviorCaravanShrine` extends the `BlockBehaviorAltar` pattern: emits the same prayer/offering events through `AltarEventEmitter`, so prayer mechanics are reused.
- **Restrictions enforced server-side in `AltarPlacementHandler`:**
  - Requires the `caravan_avatar_road` blessing unlocked.
  - One placed shrine per player at a time (track in `PlayerProgressionData`).
  - Cannot be placed inside another religion's holy-site radius.
  - Holy-site tier capped at 1 (no rituals possible on a portable shrine).
- Block-break drops the placed block back to the player's inventory (no loss).

JSON patch: new asset file `assets/divineascension/blocktypes/caravanshrine.json` + recipe gated behind crafting the avatar blessing (or just hand it out on unlock via inventory grant — simpler).

### Items

- **Caravan Pack** (`divineascension:caravanpack`) — wearable backpack-style item, +N inventory rows when equipped. Reuses VS's existing wearable-with-slots pattern. Granted on unlocking `Caravan's Burden`.
- **Waystone Token** (`divineascension:waystone`) — placed on the ground, marks a personal waypoint visible on map and counts the player's chunk as "claimed for Wayfaring favor". One per player. Low priority; can defer to v2.

### `/gift` Command

```
/gift <playername>
```
Server-side handler reads the giver's active hotbar slot, validates target is online and within ~10 blocks, transfers the stack, grants favor on success. Uses `IChatCommandService` (Tier-3 wrapper) so it's mockable in tests.

---

## Integration-Point Checklist

The exploration agent identified 9 sites to touch when adding a domain. Annotated with the Caravan-specific work:

| # | File | Change |
|---|---|---|
| 1 | `Models/Enum/DeityDomain.cs:6` | Add `Caravan = 5` (or next free slot). |
| 2 | `GUI/UI/Renderers/DomainGlyphRenderer.cs:35` | Add wagon-wheel/compass glyph case. |
| 3 | `GUI/UI/Utilities/DomainHelper.cs:64` | Add Caravan color (RGBA saffron). |
| 4 | `GUI/UI/Utilities/DomainHelper.cs:96` | Add `"Domain of the Caravan"` title. |
| 5 | `GUI/UI/Utilities/DomainHelper.cs:16` | Append `"Caravan"` to `DomainNames`. |
| 6 | `Systems/FavorSystem.cs:310` | Add prestige-keyword case: `"trade\|gift\|discovery\|exploration"`. |
| 7 | `Systems/Loaders/BlessingLoader.cs:19` | Append `"caravan"` to `DomainFiles`. |
| 8 | `assets/divineascension/config/{blessings,offerings,rituals}/caravan.json` | Create 3 new files. |
| 9 | `Systems/DivineAscensionSystemInitializer.cs` | Construct + register `TraderTransactionFavorTracker`, `ExplorationFavorTracker`, `GiftingFavorTracker`. Mind init order — they depend on `FavorSystem`, `PlayerProgressionDataManager`, `CooldownManager`. |

Additional for portable altar:
- New `BlockBehaviorCaravanShrine` registered in `DivineAscensionModSystem.Start()` via `api.RegisterBlockBehaviorClass`.
- New block JSON in `assets/divineascension/blocktypes/`.
- `PlayerProgressionData` gains `ulong? PlacedCaravanShrineBlockId` and `HashSet<long> DiscoveredChunks`. Bump `DataVersion = 5`.

Localization keys — add Caravan entries to every `lang/*.json` already touching domain names (search `domain.craft`, `deity.title.craft` etc. for the full set).

---

## Phased Rollout

Recommend three PRs to keep diffs reviewable. Each phase is independently shippable; players can use the domain after Phase 1.

### Phase 1 — Minimum viable domain *(target: ~1 week)*
- Enum value, UI hooks, JSON files (blessings/offerings/rituals).
- `TraderTransactionFavorTracker` only (the central verb).
- All 9 integration points.
- Tests: `TraderTransactionFavorTrackerTests`, JSON load smoke tests, `DomainHelperTests` updates.
- **Done when:** a player can pick Caravan as patron, trade with an NPC, accrue favor, and unlock Tier-1 blessings.

### Phase 2 — Exploration + gifting *(target: ~3-4 days)*
- `ExplorationFavorTracker` + `PlayerProgressionData.DiscoveredChunks` (data migration to v5).
- `GiftingFavorTracker` + `/gift` command.
- Wayfaring blessing branch enabled.
- Tests cover migration path and AFK-resistance (player stationary in chunk grants zero).

### Phase 3 — Portable altar + capstone *(target: ~1 week)*
- `BlockBehaviorCaravanShrine` + new block + caravan-pack item.
- Capstone blessing `Avatar of the Road` wired to grant the shrine block on unlock.
- Edge cases: shrine in someone's holy-site radius, two placements, save/load survives a placed shrine.

---

## Open Questions

1. **Enum slot.** Confirm `5` and `6` aren't reserved. Grep `DeityDomain\.` for any string-based usages that would break with a new value (`OfferingLoader` keys by enum, should be safe).
2. **Trader patch surface.** Need to verify the exact 1.22.x method to patch in `VSSurvivalMod.dll` (`PerformTrade` vs. `EntityTrader.OnInteract`). Decide before Phase 1.
3. **Civilization milestone interaction.** `united_front` rewards "all 4 domains represented" in a civ. With Caravan, do we make it 5? Or add a Caravan-specific civ milestone (e.g., "Trade Hub" — 50 trades civ-wide)? Suggest the latter to avoid retro-breaking existing civs.
4. **Gift command vs. trade UI.** A native trade UI between players would be nicer than `/gift` but is a much larger surface. Defer to v2.
5. **Distance vs. discovery.** Spec uses chunk-discovery, not distance. If discovery rewards turn out too sparse in playtesting, add a *small* per-1km distance bonus with an anti-AFK guard (must change biome or altitude).

---

## Risks

- **Harmony patch fragility.** Trader internals are the most likely thing to shift between VS releases. Mitigation: keep `TraderPatches` thin, raise an event, do all logic in the tracker.
- **Save migration.** Bumping `DataVersion` is well-trodden, but `HashSet<long>` of discovered chunks could grow unbounded on long-lived saves. Estimate: ~16 B per chunk × 100k chunks for a heavily-explored player = ~1.6 MB. Acceptable. Add a soft cap (e.g., 1M chunks) and stop awarding past it.
- **Balance.** Trade favor in particular is easy to overtune. Phase 1 should ship with conservative numbers and a config knob (`caravan.tradeFavorMultiplier` in `GameBalanceConfig`).
- **Branch protection.** This work lives on `claude/trading-nomad-domain-RlR9l` per session instructions. Don't push to `main`.
