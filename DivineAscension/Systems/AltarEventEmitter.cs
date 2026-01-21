using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems;

/// <summary>
/// Service locator for altar block events.
/// Bridges BlockBehaviorAltar (no DI support) with altar handlers (full DI).
/// </summary>
public class AltarEventEmitter
{
    /// <summary>
    /// Raised when a player uses (right-clicks) an altar block.
    /// </summary>
    public event Action<IPlayer, BlockSelection>? OnAltarUsed;

    /// <summary>
    /// Raised when a player breaks an altar block.
    /// </summary>
    public event Action<IServerPlayer, BlockPos>? OnAltarBroken;

    /// <summary>
    /// Raised when a player places an altar block (during DoPlaceBlock, before SetBlock).
    /// </summary>
    public event Action<IServerPlayer, int, BlockSelection, ItemStack>? OnAltarPlaced;

    /// <summary>
    /// Raises the OnAltarUsed event.
    /// </summary>
    public virtual void RaiseAltarUsed(IPlayer player, BlockSelection blockSelection)
    {
        OnAltarUsed?.Invoke(player, blockSelection);
    }

    /// <summary>
    /// Raises the OnAltarBroken event.
    /// </summary>
    public virtual void RaiseAltarBroken(IServerPlayer player, BlockPos pos)
    {
        OnAltarBroken?.Invoke(player, pos);
    }

    /// <summary>
    /// Raises the OnAltarPlaced event.
    /// </summary>
    public virtual void RaiseAltarPlaced(IServerPlayer player, int oldBlockId, BlockSelection blockSelection, ItemStack withItemStack)
    {
        OnAltarPlaced?.Invoke(player, oldBlockId, blockSelection, withItemStack);
    }

    /// <summary>
    /// Clears all event subscribers. Called during mod disposal.
    /// </summary>
    public void ClearSubscribers()
    {
        OnAltarUsed = null;
        OnAltarBroken = null;
        OnAltarPlaced = null;
    }
}
