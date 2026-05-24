using System;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Lectern;

/// <summary>
/// Service locator bridging <see cref="DivineAscension.Blocks.BlockBehaviorLectern"/>
/// (no DI support) with lectern interaction handlers.
/// </summary>
public class LecternEventEmitter
{
    /// <summary>Raised when a player right-clicks a lectern variant block.</summary>
    public event Action<IServerPlayer, BlockSelection>? OnLecternUsed;

    public virtual void RaiseLecternUsed(IServerPlayer player, BlockSelection blockSelection)
    {
        OnLecternUsed?.Invoke(player, blockSelection);
    }

    public void ClearSubscribers()
    {
        OnLecternUsed = null;
    }
}
