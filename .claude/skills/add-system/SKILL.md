---
name: add-system
description: Add a new server-side manager/system/network-handler to the Divine Ascension mod and wire it into the strictly-ordered initializer. Use when introducing a new Manager, Registry, Loader, Handler, or System that must be constructed at startup. Encodes the critical init-order and disposal/back-wiring rules.
---

# Adding a server-side system

Server systems are constructed in **one place, in a strict order** that must not
be reordered: `DivineAscension/Systems/DivineAscensionSystemInitializer.cs`
(`InitializeServerSystems`). Dependencies are noted in comments at each step.
Getting the order wrong yields null-deref crashes at startup or subtly broken
cross-system events.

Read CLAUDE.md "Initialization order (critical)" before editing — it summarizes
the dependency chains. Confirm them against the file; don't trust memory.

## Design the system first

- One responsibility: state + lookups in a dedicated class (`Managers/Registries`).
- Constructor DI only. Take the API wrapper interfaces (`IEventService`,
  `IWorldService`, `IPersistenceService`, `INetworkService`, `ITimeService`,
  `IPlayerMessengerService`, …) and any manager deps — **never** the raw VS API,
  so the system stays testable. Guard each with `?? throw new ArgumentNullException`.
- Inject clocks/RNG rather than reading them directly (determinism in tests).
- Periodic work: register via `IEventService.RegisterGameTickListener` /
  `RegisterCallback`, not your own threads.
- Cross-system comms: raise/subscribe events (e.g. `ReligionManager.OnReligionDeleted`)
  rather than calling sideways into another manager mid-construction.
- If the system holds unmanaged/event subscriptions, implement `Dispose()`.

## Wire into the initializer (order matters)

1. **Find the right insertion point.** Place construction *after* every
   dependency is already constructed and *before* every consumer. Honor the
   documented chains, e.g.:
   - `LocalizationService` is initialized before any localized message use.
   - `ReligionManager` before `ActivityLogManager` / `CivilizationManager` / `HolySiteManager`.
   - `ReligionPrestigeManager` before `FavorSystem`.
   - Loaders (`OfferingLoader`, `RitualLoader`, `MilestoneDefinitionLoader`) before consumers.
   - Loaders → `BuffManager` → `PlayerProgressionService` → `AltarPrayerHandler`.
   - `CivilizationMilestoneManager` → `CivilizationBonusSystem`.

2. **Construct it** with the already-built services/managers, then call its own
   init if it has one (`xManager.Initialize();`), matching the surrounding style.

3. **Circular deps:** if A needs B and B needs A, construct both, then resolve
   the back-edge with a setter after construction — as the bonus system is wired
   back into `FavorSystem`, `HolySiteManager`, `PvPManager` via `Set*()` calls,
   and `BlessingEffectSystem` registers with the prestige manager after construction.
   Do **not** reorder to "fix" a cycle.

4. **Expose it on `InitializationResult`** (bottom of the same file): add a
   `public MyManager MyManager { get; init; } = null!;` property and set it in the
   `return new InitializationResult { … MyManager = myManager, … }` block.

5. **Keep the final step last:** membership validation/repair
   (`religionManager.ValidateAllMemberships()`) must remain the final action.

## Hold + dispose in the ModSystem

In `DivineAscensionModSystem.StartServerSide` the result fields are copied to
private fields (`_xManager = result.XManager;`). If your system needs disposal or
event unsubscription:
- add a private field and assign it from `result`,
- dispose it in `Dispose()` alongside the others (`_xManager?.Dispose();`).

## If the system is a network handler

Construct it in the initializer, call `handler.RegisterHandlers()`, expose it on
`InitializationResult`, assign `_xNetworkHandler` in `StartServerSide`, and
`Dispose()` it. See the add-network-packet skill for the packet side.

## Static service-locator systems (block behaviors)

BlockBehavior/Patch classes can't take DI. They emit through a static emitter
(`AltarEventEmitter`, etc.) initialized in the initializer, and a fully-DI'd
handler subscribes. Patch classes also need `ClearSubscribers()` (called at
"Step 1" of the initializer to reset between loads) and, if they resolve tags,
an `Initialize(api)` call there.

## Finish
- `dotnet build DivineAscension.sln -c Debug` — a wrong order usually surfaces as a null reference at startup, not a compile error, so also smoke-check the init path.
- Add a test under `DivineAscension.Tests/<Area>/` using the Fake/Spy doubles; do not mock raw VS API.
- `dotnet test --filter FullyQualifiedName~<Pattern>`.

## Checklist
- [ ] Constructor-DI only, wrapper interfaces (no raw VS API), null-guarded.
- [ ] Inserted after all deps, before all consumers; documented chains respected.
- [ ] Circular deps resolved with post-construction `Set*()`, not reordering.
- [ ] Added to `InitializationResult` (property + return block).
- [ ] Field + disposal in `ModSystem` if it owns resources.
- [ ] Validation/repair remains the final initializer step.
- [ ] Build clean + test added with Fakes/Spies.
