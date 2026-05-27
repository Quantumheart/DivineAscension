---
name: add-block-behavior
description: Add a BlockBehavior (or CollectibleBehavior) to a vanilla Vintage Story block in the Divine Ascension mod, using the static service-locator → DI-handler pattern. Use when hooking mod logic to break/place/use of a vanilla block via JSON patch. Encodes the no-DI-in-behavior workaround, registration, and JSON-patch gotchas.
---

# Adding a block behavior

BlockBehavior constructors **cannot take DI** — VS instantiates them. So a
behavior does almost nothing itself: it forwards the event through a static hook
to a fully-DI'd handler that subscribes at startup. Behaviors are attached to
vanilla blocks via JSON patches, not code.

Implemented examples to copy: `BlockBehaviorAltar`, `BlockBehaviorLectern`,
`BlockBehaviorStone`, `BlockBehaviorOre` (in `DivineAscension/Blocks/`), plus the
collectible `CollectibleBehaviorChiselTracking`.

Two bridging styles exist — pick by how many event kinds you need:

- **Static event on the behavior** (simple, e.g. `BlockBehaviorOre`): a
  `public static event Action<...>?` raised directly from the override, plus a
  `static ClearSubscribers()` and an `internal static Trigger…` for tests.
- **Emitter instance via static setter** (richer, e.g. `BlockBehaviorAltar` +
  `AltarEventEmitter`): the behavior holds `private static Emitter? _emitter;` set
  by `SetEventEmitter(...)` from the initializer; the emitter exposes
  `Raise…`/`On…` events and `ClearSubscribers()`. Use this when there are several
  event kinds (used/placed/broken) or you want a mockable `virtual Raise…`.

## 1. Write the behavior — `DivineAscension/Blocks/BlockBehaviorFoo.cs`

```csharp
public class BlockBehaviorFoo(Block block) : BlockBehavior(block)
{
    public static event Action<IWorldAccessor, BlockPos, IPlayer?, Block, EnumHandling>? OnFooBroken;

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer,
        float dropQuantityMultiplier, ref EnumHandling handling)
        => OnFooBroken?.Invoke(world, pos, byPlayer, block, handling);

    public static void ClearSubscribers() => OnFooBroken = null;
    internal static void TriggerFooBroken(/* … */) => OnFooBroken?.Invoke(/* … */);
}
```

Rules:
- The class **must be `public`** or VS won't load it.
- Keep behavior logic minimal — just forward. Domain logic lives in the handler.
- For **placement**, override `DoPlaceBlock` (has player context), **not**
  `OnBlockPlaced` (no context, also fires for worldgen). For richer payloads use
  the emitter style and `RaiseFooPlaced(player, oldBlockId, blockSel, stack)`.
- Pass `block` along so subscribers can read block type/grade.

## 2. Register the class — `DivineAscensionModSystem.cs` (`Start`, ~line 109)

```csharp
api.RegisterBlockBehaviorClass("DivineAscensionFoo", typeof(BlockBehaviorFoo));
// collectibles use api.RegisterCollectibleBehaviorClass(...)
```
The string name here must match the `name` used in the JSON patch (step 4).

## 3. Wire the handler + reset — `DivineAscensionSystemInitializer.cs`

- **Reset subscribers** at "Step 1" alongside the other `ClearSubscribers()`
  calls (prevents duplicate subscriptions across world reloads). If the behavior
  resolves tags or needs the API, also add an `Initialize(api)` there.
- **Emitter style:** construct the emitter, hand it to the behavior, and pass it
  to the handler:
  ```csharp
  var fooEmitter = new FooEventEmitter();
  BlockBehaviorFoo.SetEventEmitter(fooEmitter);
  // … later: new FooHandler(logger, …, fooEmitter); then expose both on InitializationResult
  ```
- The **handler** is a normal DI class (see add-system skill): subscribe in its
  ctor/an init method, do the authoritative work (server-side perms!), and
  `Dispose()` to unsubscribe. Add handler (and emitter) to `InitializationResult`,
  assign fields in `ModSystem.StartServerSide`, dispose in `ModSystem.Dispose`.

## 4. Attach via JSON patch — `assets/divineascension/patches/foo.json`

```json
[
  {
    "op": "addmerge",
    "path": "/behaviors/-",
    "value": { "name": "DivineAscensionFoo" },
    "file": "game:blocktypes/stone/ore-graded.json",
    "side": "Server"
  }
]
```
Patch tips:
- `"op": "addmerge"` (1.19.4+); `/behaviors/-` appends to the array.
- `"side"` is **capitalised** (`"Server"` / `"Client"`). Most gameplay hooks are `"Server"`.
- Prefix vanilla files with `game:`.
- If the target item/block has no flat `behaviors` array, patch a
  `behaviorsByType` path instead.
- Add one patch object per target block file.

## Finish
- `dotnet build DivineAscension.sln -c Debug`.
- Test the handler under `DivineAscension.Tests/` using the `Trigger…` helper or
  a mocked emitter + Fake/Spy services — don't drive the raw VS API.
- `dotnet test --filter FullyQualifiedName~Foo`.

## Checklist
- [ ] `public` behavior class, logic forwarded (not implemented) in the override.
- [ ] Placement via `DoPlaceBlock`, not `OnBlockPlaced`.
- [ ] `RegisterBlockBehaviorClass("DivineAscensionFoo", …)` name matches JSON `name`.
- [ ] `ClearSubscribers()` (and `Initialize(api)` if needed) called at initializer Step 1.
- [ ] DI handler subscribes + disposes; emitter/handler on `InitializationResult` and in `ModSystem` fields.
- [ ] JSON patch with `addmerge`, capitalised `side`, `game:` prefix.
- [ ] Build clean + handler test.
