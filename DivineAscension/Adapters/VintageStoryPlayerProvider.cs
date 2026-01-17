using DivineAscension.Services.Abstractions;
using Vintagestory.API.Server;

namespace DivineAscension.Adapters;

/// <summary>
/// Adapts Vintage Story's player API to the IPlayerProvider abstraction.
/// </summary>
internal sealed class VintageStoryPlayerProvider(ICoreServerAPI sapi) : IPlayerProvider
{
    public string? GetPlayerName(string playerUid)
    {
        return (sapi.World.PlayerByUid(playerUid) as IServerPlayer)?.PlayerName;
    }

    public bool IsPlayerOnline(string playerUid)
    {
        var player = sapi.World.PlayerByUid(playerUid) as IServerPlayer;
        return player?.ConnectionState == EnumClientState.Playing;
    }
}