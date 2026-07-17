using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Toolsmith;

/// <summary>
///     Service locator for Toolsmith block/collectible behavior events.
///     Bridges Toolsmith behaviors (no DI support) with the ToolsmithFavorTracker (full DI).
///     Mirrors the AltarEventEmitter pattern.
/// </summary>
public class ToolsmithEventEmitter
{
    /// <summary>
    ///     Raised when a player assembles a tinkered tool at the workbench.
    ///     Parameters: player, workbench position, crafted tool stack.
    /// </summary>
    public event Action<IServerPlayer, BlockPos, ItemStack>? OnToolAssembled;

    /// <summary>
    ///     Raised when a player sharpens a tool at the grindstone or with a whetstone.
    ///     Parameters: player, position (grindstone pos or player pos for whetstone), tool stack.
    /// </summary>
    public event Action<IServerPlayer, BlockPos, ItemStack>? OnToolSharpened;

    /// <summary>
    ///     Raised when a player disassembles a tinkered tool at the workbench vise or grindstone.
    ///     Parameters: player, position, tool stack that was disassembled.
    /// </summary>
    public event Action<IServerPlayer, BlockPos, ItemStack>? OnToolDisassembled;

    /// <summary>
    ///     Raised when a player initiates reforging of a tool head at the workbench.
    ///     Parameters: player, workbench position, tool head stack being reforged.
    /// </summary>
    public event Action<IServerPlayer, BlockPos, ItemStack>? OnToolReforged;

    public virtual void RaiseToolAssembled(IServerPlayer player, BlockPos pos, ItemStack toolStack)
    {
        OnToolAssembled?.Invoke(player, pos, toolStack);
    }

    public virtual void RaiseToolSharpened(IServerPlayer player, BlockPos pos, ItemStack toolStack)
    {
        OnToolSharpened?.Invoke(player, pos, toolStack);
    }

    public virtual void RaiseToolDisassembled(IServerPlayer player, BlockPos pos, ItemStack toolStack)
    {
        OnToolDisassembled?.Invoke(player, pos, toolStack);
    }

    public virtual void RaiseToolReforged(IServerPlayer player, BlockPos pos, ItemStack toolHeadStack)
    {
        OnToolReforged?.Invoke(player, pos, toolHeadStack);
    }

    /// <summary>
    ///     Clears all event subscribers. Called during mod disposal.
    /// </summary>
    public void ClearSubscribers()
    {
        OnToolAssembled = null;
        OnToolSharpened = null;
        OnToolDisassembled = null;
        OnToolReforged = null;
    }
}