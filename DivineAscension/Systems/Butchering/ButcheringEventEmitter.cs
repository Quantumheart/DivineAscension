using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Butchering;

/// <summary>
///     Service locator for Butchering mod workstation events.
///     Bridges the conditional Harmony patches (no DI support) with the ButcheringFavorTracker (full DI).
///     Mirrors the ToolsmithEventEmitter pattern.
/// </summary>
public class ButcheringEventEmitter
{
    /// <summary>
    ///     Raised when a player skins a hung carcass on a Butchering skinning hook.
    ///     Parameters: player, workstation position, the ItemButcherable stack being processed,
    ///     and the butcheringWorkLoad tier ("small"/"medium"/"large").
    /// </summary>
    public event Action<IServerPlayer, BlockPos, ItemStack, string>? OnAnimalSkinned;

    /// <summary>
    ///     Raised when a player butchers a bled-out carcass on a Butchering butcher table.
    ///     Parameters: player, workstation position, the ItemButcherable stack being processed,
    ///     and the butcheringWorkLoad tier ("small"/"medium"/"large").
    /// </summary>
    public event Action<IServerPlayer, BlockPos, ItemStack, string>? OnAnimalButchered;

    public virtual void RaiseAnimalSkinned(IServerPlayer player, BlockPos pos, ItemStack butcherableStack,
        string workload)
    {
        OnAnimalSkinned?.Invoke(player, pos, butcherableStack, workload);
    }

    public virtual void RaiseAnimalButchered(IServerPlayer player, BlockPos pos, ItemStack butcherableStack,
        string workload)
    {
        OnAnimalButchered?.Invoke(player, pos, butcherableStack, workload);
    }

    /// <summary>
    ///     Clears all event subscribers. Called during mod disposal.
    /// </summary>
    public void ClearSubscribers()
    {
        OnAnimalSkinned = null;
        OnAnimalButchered = null;
    }
}