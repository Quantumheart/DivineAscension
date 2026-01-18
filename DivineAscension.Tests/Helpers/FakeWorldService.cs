using System;
using System.Collections.Generic;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of IWorldService for testing.
/// Provides in-memory storage for players, blocks, and entities.
/// </summary>
public sealed class FakeWorldService : IWorldService
{
    private readonly Dictionary<string, IServerPlayer> _playersByUID = new();
    private readonly Dictionary<string, IServerPlayer> _playersByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<BlockPos, Block> _blocks = new();
    private readonly Dictionary<int, Block> _blocksById = new();
    private readonly Dictionary<BlockPos, BlockEntity> _blockEntities = new();
    private readonly Dictionary<Vec3i, IWorldChunk> _chunks = new();
    private readonly List<SoundEvent> _soundsPlayed = new();
    private readonly List<ParticleEvent> _particlesSpawned = new();
    private long _elapsedMs = 0;

    public long ElapsedMilliseconds => _elapsedMs;

    // Player access
    public IServerPlayer? GetPlayerByUID(string uid)
    {
        return _playersByUID.TryGetValue(uid, out var player) ? player : null;
    }

    public IPlayer? GetPlayerByName(string name)
    {
        return _playersByName.TryGetValue(name, out var player) ? player : null;
    }

    public IEnumerable<IServerPlayer> GetAllOnlinePlayers()
    {
        return _playersByUID.Values;
    }

    // Block access
    public Block GetBlock(BlockPos pos)
    {
        return _blocks.TryGetValue(pos, out var block) ? block : Block.FromId(0); // Return air block if not found
    }

    public Block GetBlock(int blockId)
    {
        return _blocksById.TryGetValue(blockId, out var block) ? block : Block.FromId(0);
    }

    public BlockEntity? GetBlockEntity(BlockPos pos)
    {
        return _blockEntities.TryGetValue(pos, out var entity) ? entity : null;
    }

    // Chunk access
    public bool IsChunkLoaded(Vec3i chunkPos)
    {
        return _chunks.ContainsKey(chunkPos);
    }

    public IWorldChunk? GetChunk(Vec3i chunkPos)
    {
        return _chunks.TryGetValue(chunkPos, out var chunk) ? chunk : null;
    }

    // Sound and particles
    public void PlaySoundAt(AssetLocation sound, double x, double y, double z, IPlayer? sourcePlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
    {
        _soundsPlayed.Add(new SoundEvent(sound, new Vec3d(x, y, z), sourcePlayer, randomizePitch, range, volume));
    }

    public void SpawnParticles(SimpleParticleProperties particles, Vec3d pos, IPlayer? sourcePlayer = null)
    {
        _particlesSpawned.Add(new ParticleEvent(particles, pos, sourcePlayer));
    }

    // Block accessor (simplified - returns null for now)
    public IBlockAccessor GetBlockAccessor(bool isWriteAccess, bool isRevertable)
    {
        // For testing, we can return null or a mock
        // Most tests won't need this method
        throw new NotImplementedException("GetBlockAccessor is not implemented in FakeWorldService. Mock IBlockAccessor separately if needed.");
    }

    // Test helper methods
    public void AddPlayer(IServerPlayer player)
    {
        _playersByUID[player.PlayerUID] = player;
        _playersByName[player.PlayerName] = player;
    }

    public void RemovePlayer(string uid)
    {
        if (_playersByUID.TryGetValue(uid, out var player))
        {
            _playersByUID.Remove(uid);
            _playersByName.Remove(player.PlayerName);
        }
    }

    public void SetBlock(BlockPos pos, Block block)
    {
        _blocks[pos] = block;
        if (block.BlockId > 0)
        {
            _blocksById[block.BlockId] = block;
        }
    }

    public void SetBlockEntity(BlockPos pos, BlockEntity entity)
    {
        _blockEntities[pos] = entity;
    }

    public void SetChunk(Vec3i chunkPos, IWorldChunk chunk)
    {
        _chunks[chunkPos] = chunk;
    }

    public void SetElapsedMilliseconds(long ms)
    {
        _elapsedMs = ms;
    }

    public void Clear()
    {
        _playersByUID.Clear();
        _playersByName.Clear();
        _blocks.Clear();
        _blocksById.Clear();
        _blockEntities.Clear();
        _chunks.Clear();
        _soundsPlayed.Clear();
        _particlesSpawned.Clear();
        _elapsedMs = 0;
    }

    // Test inspection helpers
    public IReadOnlyList<SoundEvent> GetSoundsPlayed() => _soundsPlayed.AsReadOnly();
    public IReadOnlyList<ParticleEvent> GetParticlesSpawned() => _particlesSpawned.AsReadOnly();

    public sealed record SoundEvent(AssetLocation Sound, Vec3d Position, IPlayer? SourcePlayer, bool RandomizePitch, float Range, float Volume);
    public sealed record ParticleEvent(SimpleParticleProperties Particles, Vec3d Position, IPlayer? SourcePlayer);
}
