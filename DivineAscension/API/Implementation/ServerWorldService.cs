using System;
using System.Collections.Generic;
using System.Linq;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.API.Implementation;

/// <summary>
/// Server-side implementation of IWorldService that wraps IServerWorldAccessor.
/// Provides a thin abstraction layer over Vintage Story's world access for improved testability.
/// </summary>
internal sealed class ServerWorldService(IServerWorldAccessor worldAccessor) : IWorldService
{
    private readonly IServerWorldAccessor _worldAccessor =
        worldAccessor ?? throw new ArgumentNullException(nameof(worldAccessor));

    public IServerPlayer? GetPlayerByUID(string uid)
    {
        if (string.IsNullOrEmpty(uid)) throw new ArgumentNullException(nameof(uid));
        return _worldAccessor.PlayerByUid(uid) as IServerPlayer;
    }

    public IPlayer? GetPlayerByName(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

        // IServerWorldAccessor doesn't have PlayerByName, so we search through all players
        return _worldAccessor.AllOnlinePlayers
            .FirstOrDefault(p => p.PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<IPlayer> GetAllOnlinePlayers()
    {
        return _worldAccessor.AllOnlinePlayers;
    }

    public Block GetBlock(BlockPos pos)
    {
        if (pos == null) throw new ArgumentNullException(nameof(pos));
        return _worldAccessor.BlockAccessor.GetBlock(pos);
    }

    public Block GetBlock(int blockId)
    {
        return _worldAccessor.BlockAccessor.GetBlock(blockId);
    }

    public BlockEntity? GetBlockEntity(BlockPos pos)
    {
        if (pos == null) throw new ArgumentNullException(nameof(pos));
        return _worldAccessor.BlockAccessor.GetBlockEntity(pos);
    }

    public bool IsChunkLoaded(Vec3i chunkPos)
    {
        if (chunkPos == null) throw new ArgumentNullException(nameof(chunkPos));

        // Check if chunk exists using the BlockAccessor's chunk map
        var chunk = _worldAccessor.BlockAccessor.GetChunk(chunkPos.X, chunkPos.Y, chunkPos.Z);
        return chunk != null;
    }

    public IWorldChunk? GetChunk(Vec3i chunkPos)
    {
        if (chunkPos == null) throw new ArgumentNullException(nameof(chunkPos));
        return _worldAccessor.BlockAccessor.GetChunk(chunkPos.X, chunkPos.Y, chunkPos.Z);
    }

    public void PlaySoundAt(AssetLocation sound, double x, double y, double z, IPlayer? sourcePlayer = null,
        bool randomizePitch = true, float range = 32f, float volume = 1f)
    {
        if (sound == null) throw new ArgumentNullException(nameof(sound));
        _worldAccessor.PlaySoundAt(sound, x, y, z, sourcePlayer, randomizePitch, range, volume);
    }

    public void SpawnParticles(SimpleParticleProperties particles, Vec3d pos, IPlayer? sourcePlayer = null)
    {
        if (particles == null) throw new ArgumentNullException(nameof(particles));
        if (pos == null) throw new ArgumentNullException(nameof(pos));

        particles.MinPos = pos;
        _worldAccessor.SpawnParticles(particles, sourcePlayer);
    }

    public long ElapsedMilliseconds => _worldAccessor.ElapsedMilliseconds;

    public float HoursPerDay => _worldAccessor.Calendar.HoursPerDay;

    public IBlockAccessor GetBlockAccessor(bool isWriteAccess, bool isRevertable)
    {
        // VS API requires 4 booleans: lockCheck, revertable, strict, debug
        // We map isWriteAccess -> lockCheck (false = read-only), isRevertable -> revertable
        // strict and debug default to false for standard usage
        return _worldAccessor.GetBlockAccessor(isWriteAccess, isRevertable, false, false);
    }
}