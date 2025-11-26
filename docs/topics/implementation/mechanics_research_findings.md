# Technical Research Findings

## 1. Anvil Smithing Detection & Material Saving

### Objective
Implement a system to detect when a player finishes smithing an item on an anvil and provide a chance to refund the raw material (ingot).

### Technical Challenge
The base game `BlockEntityAnvil` does not expose a direct C# event for "Smithing Finished". We cannot modify the base game code directly to emit this event without replacing the block entity, which reduces compatibility.

### Solution: Custom BlockEntityBehavior
We can attach a custom `BlockEntityBehavior` to all Anvil blocks. This behavior acts as a "sidecar", monitoring the state of the anvil's inventory.

#### Implementation Strategy

1.  **Behavior Class**: Create `BehaviorAnvilMaterialSaving` inheriting from `BlockEntityBehavior`.
2.  **State Monitoring**:
    *   The behavior registers a `GameTickListener` (running approximately every 200ms).
    *   It monitors **Slot 0** (the work item slot) of the Anvil's inventory (cast `BlockEntity` to `IBlockEntityContainer`).
    *   It maintains a boolean state `wasWorking`.I8
3.  **Detection Logic**:
    *   **Is Working**: An item is considered "being worked on" if the itemstack attributes contain the `voxels` key.
    *   **Finished**: If `wasWorking` is `true`, and the current item in Slot 0 *no longer* has the `voxels` attribute (but the slot is not empty), smithing is considered complete.
4.  **Reward Logic**:
    *   Upon completion detection, trigger the probability check (derived from Khoras blessings).
    *   If successful, spawn the refunded material (e.g., Iron Ingot) using `api.World.SpawnItemEntity` at the anvil's position.

#### Code Snippet (Concept)
```csharp
private void OnTick(float dt)
{
    if (Blockentity is not IBlockEntityContainer container) return;
    var slot = container.Inventory[0];
    
    // "voxels" attribute indicates an unfinished work item
    bool isWorking = !slot.Empty && slot.Itemstack.Attributes.HasAttribute("voxels");

    // Transition from Working -> Not Working (but item exists) implies completion
    if (wasWorking && !isWorking && !slot.Empty)
    {
        // Smithing Complete: Trigger Reward Logic
    }

    wasWorking = isWorking;
}
```

#### Registration
*   **Class Registration**: In `ModSystem.Start`, call `api.RegisterBlockEntityBehaviorClass("AnvilMaterialSaving", typeof(BehaviorAnvilMaterialSaving))`.
*   **Block Injection**: In `ModSystem.AssetsFinalize`:
    1.  Iterate through `api.World.Blocks`.
    2.  Identify Anvils (check `EntityClass` == "Anvil" or code path starts with `anvil-`).
    3.  Add the behavior to the block's `BlockEntityBehaviors` list.

---

## 2. Player Entity Kills (Hunting)

### Objective
Execute specific code whenever a player kills an animal entity to award favor.

### Solution: Global Event Bus
The Vintage Story API provides a global `OnEntityDeath` event on the server side.

#### Implementation Details

1.  **Event Hook**: Subscribe to `sapi.Event.OnEntityDeath` in the `Initialize` method of a tracker system.
2.  **Filtering Logic**:
    *   **Killer Check**: Inspect `damageSource.GetCauseEntity()`. Verify it is an `EntityPlayer`.
    *   **Victim Check**: Inspect the dying `entity`.
3.  **Animal Identification**:
    *   The API does not have a strict "IAnimal" interface for all passive mobs.
    *   **Heuristic**:
        *   Must be `EntityAgent`.
        *   Must NOT be `EntityPlayer`.
        *   Filter by code path allowlist (e.g., `wolf`, `sheep`, `pig`) or deny-list (e.g., `drifter`, `locust`).
    *   *Current Status*: Fully implemented in `HuntingFavorTracker.cs`.

---

## 3. Next Steps
*   Implement `BehaviorAnvilMaterialSaving.cs`.
*   Integrate the probability check from `SpecialEffectRegistry`.
*   Register the behavior in `PantheonWarsSystem.cs`.
