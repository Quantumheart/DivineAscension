using System.Diagnostics.CodeAnalysis;
using DivineAscension.API.Interfaces;
using Moq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.Tests.Helpers;

/// <summary>
/// Fake implementation of IWorldService for testing.
/// Provides in-memory storage for players, blocks, and entities.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class FakeWorldService : IWorldService
{
    private readonly Dictionary<BlockPos, BlockEntity> _blockEntities = new();
    private readonly Dictionary<BlockPos, Block> _blocks = new();
    private readonly Dictionary<int, Block> _blocksById = new();
    private readonly Dictionary<Vec3i, IWorldChunk> _chunks = new();
    private readonly List<ParticleEvent> _particlesSpawned = new();
    private readonly Dictionary<string, IServerPlayer> _playersByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IServerPlayer> _playersByUID = new();
    private readonly List<SoundEvent> _soundsPlayed = new();
    private readonly List<ItemStack> _spawnedItems = new();
    private IBlockAccessor? _blockAccessor;
    private IGameCalendar? _calendar;
    private IServerWorldAccessor? _worldAccessor;
    private long _elapsedMs = 0;
    private float _hoursPerDay = 24f; // Default to 24 hours
    private readonly Dictionary<BlockPos, LandClaim[]> _landClaims = new();

    public FakeWorldService()
    {
        // Set up default world accessor with land claim API
        var mockWorld = new Mock<IServerWorldAccessor>();
        var mockLandClaimAPI = new Mock<ILandClaimAPI>();
        mockLandClaimAPI.Setup(x => x.Get(It.IsAny<BlockPos>()))
            .Returns<BlockPos>(pos => _landClaims.TryGetValue(pos, out var claims) ? claims : Array.Empty<LandClaim>());
        mockWorld.Setup(x => x.Claims).Returns(mockLandClaimAPI.Object);
        _worldAccessor = mockWorld.Object;
    }

    public long ElapsedMilliseconds => _elapsedMs;
    public float HoursPerDay => _hoursPerDay;
    public IGameCalendar Calendar => _calendar ?? throw new InvalidOperationException("Calendar not set. Call SetCalendar() first.");
    public IBlockAccessor BlockAccessor => _blockAccessor ?? throw new InvalidOperationException("BlockAccessor not set. Call SetBlockAccessor() first.");
    public IServerWorldAccessor World => _worldAccessor!;

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

    public IEnumerable<IPlayer> GetAllPlayers()
    {
        // In fake service, return same as online players (tests can control this)
        return _playersByUID.Values;
    }

    // Block access
    public Block GetBlock(BlockPos pos)
    {
        return _blocks.TryGetValue(pos, out var block)
            ? block
            : null!; // Return null if not found (tests should set up blocks)
    }

    public Block GetBlock(int blockId)
    {
        return _blocksById.TryGetValue(blockId, out var block) ? block : null!;
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
    public void PlaySoundAt(AssetLocation sound, double x, double y, double z, IPlayer? sourcePlayer = null,
        bool randomizePitch = true, float range = 32f, float volume = 1f)
    {
        _soundsPlayed.Add(new SoundEvent(sound, new Vec3d(x, y, z), sourcePlayer, randomizePitch, range, volume));
    }

    public void SpawnParticles(SimpleParticleProperties particles, Vec3d pos, IPlayer? sourcePlayer = null)
    {
        _particlesSpawned.Add(new ParticleEvent(particles, pos, sourcePlayer));
    }

    public void SpawnItemEntity(ItemStack itemstack, Vec3d position, Vec3d? velocity = null)
    {
        _spawnedItems.Add(itemstack);
    }

    // Block accessor - configurable for tests
    public IBlockAccessor GetBlockAccessor(bool isWriteAccess, bool isRevertable)
    {
        // Return the configured block accessor, or null if not set
        // Tests that need a block accessor should call SetBlockAccessor() first
        return _blockAccessor!;
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

    public void SetHoursPerDay(float hours)
    {
        _hoursPerDay = hours;
    }

    public void SetBlockAccessor(IBlockAccessor blockAccessor)
    {
        _blockAccessor = blockAccessor;
    }

    public void SetCalendar(IGameCalendar calendar)
    {
        _calendar = calendar;
    }

    public void SetWorldAccessor(IServerWorldAccessor worldAccessor)
    {
        _worldAccessor = worldAccessor;
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
        _spawnedItems.Clear();
        _landClaims.Clear();
        _elapsedMs = 0;
        _hoursPerDay = 24f;
        _blockAccessor = null;
        _calendar = null;
        // Don't null out world accessor - we need it for claims
    }

    // Test inspection helpers
    public IReadOnlyList<SoundEvent> GetSoundsPlayed() => _soundsPlayed.AsReadOnly();
    public IReadOnlyList<ParticleEvent> GetParticlesSpawned() => _particlesSpawned.AsReadOnly();
    public IReadOnlyList<ItemStack> GetSpawnedItems() => _spawnedItems.AsReadOnly();

    /// <summary>
    /// Creates a mock player with the specified UID and name.
    /// This is a helper method for tests to quickly create players.
    /// </summary>
    public IServerPlayer CreatePlayer(string uid, string name)
    {
        var mockPlayer = new Mock<IServerPlayer>();
        mockPlayer.Setup(x => x.PlayerUID).Returns(uid);
        mockPlayer.Setup(x => x.PlayerName).Returns(name);

        var mockEntity = new Mock<EntityPlayer>();
        // Create a real ItemSlot instance instead of mocking it
        var rightHandSlot = new ItemSlot(null);

        mockEntity.Setup(x => x.RightHandItemSlot).Returns(rightHandSlot);
        mockPlayer.Setup(x => x.Entity).Returns(mockEntity.Object);

        var player = mockPlayer.Object;
        AddPlayer(player);
        return player;
    }

    public void AddLandClaim(BlockPos pos, LandClaim[] claims)
    {
        _landClaims[pos] = claims;
    }

    public sealed record SoundEvent(
        AssetLocation Sound,
        Vec3d Position,
        IPlayer? SourcePlayer,
        bool RandomizePitch,
        float Range,
        float Volume);

    public sealed record ParticleEvent(SimpleParticleProperties Particles, Vec3d Position, IPlayer? SourcePlayer);
}

/// <summary>
/// Fake block for testing that allows setting the Code
/// </summary>
public class FakeBlock : Block
{
    public new AssetLocation? Code { get; set; }
}