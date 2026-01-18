using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace DivineAscension.API.Interfaces;

/// <summary>
/// Service for accessing world state (players, blocks, entities).
/// Wraps ICoreServerAPI.World for testability.
/// </summary>
public interface IWorldService
{
    /// <summary>
    /// Get the elapsed time in milliseconds since the world was loaded.
    /// </summary>
    long ElapsedMilliseconds { get; }

    /// <summary>
    /// Get the number of in-game hours per real day (for time calculations).
    /// </summary>
    float HoursPerDay { get; }

    /// <summary>
    /// Get the game calendar for accessing in-game time.
    /// </summary>
    IGameCalendar Calendar { get; }

    /// <summary>
    /// Get direct access to the block accessor for reading blocks.
    /// </summary>
    IBlockAccessor BlockAccessor { get; }

    /// <summary>
    /// Get direct access to the world accessor (for low-level operations that don't have wrappers yet).
    /// </summary>
    IServerWorldAccessor World { get; }

    /// <summary>
    /// Get a player by their unique identifier.
    /// </summary>
    /// <param name="uid">The player's unique ID.</param>
    /// <returns>The player if found, otherwise null.</returns>
    IServerPlayer? GetPlayerByUID(string uid);

    /// <summary>
    /// Get a player by their name.
    /// </summary>
    /// <param name="name">The player's name.</param>
    /// <returns>The player if found, otherwise null.</returns>
    IPlayer? GetPlayerByName(string name);

    /// <summary>
    /// Get all currently online players.
    /// </summary>
    /// <returns>An enumerable of all online players.</returns>
    IEnumerable<IPlayer> GetAllOnlinePlayers();

    /// <summary>
    /// Get a block at the specified position.
    /// </summary>
    /// <param name="pos">The block position.</param>
    /// <returns>The block at the position.</returns>
    Block GetBlock(BlockPos pos);

    /// <summary>
    /// Get a block by its ID.
    /// </summary>
    /// <param name="blockId">The block ID.</param>
    /// <returns>The block with the specified ID.</returns>
    Block GetBlock(int blockId);

    /// <summary>
    /// Get a block entity at the specified position.
    /// </summary>
    /// <param name="pos">The block entity position.</param>
    /// <returns>The block entity if found, otherwise null.</returns>
    BlockEntity? GetBlockEntity(BlockPos pos);

    /// <summary>
    /// Check if a chunk is currently loaded.
    /// </summary>
    /// <param name="chunkPos">The chunk position.</param>
    /// <returns>True if the chunk is loaded, otherwise false.</returns>
    bool IsChunkLoaded(Vec3i chunkPos);

    /// <summary>
    /// Get a chunk at the specified position.
    /// </summary>
    /// <param name="chunkPos">The chunk position.</param>
    /// <returns>The chunk if loaded, otherwise null.</returns>
    IWorldChunk? GetChunk(Vec3i chunkPos);

    /// <summary>
    /// Play a sound at a specific location in the world.
    /// </summary>
    /// <param name="sound">The sound asset location.</param>
    /// <param name="x">X coordinate.</param>
    /// <param name="y">Y coordinate.</param>
    /// <param name="z">Z coordinate.</param>
    /// <param name="sourcePlayer">Optional source player.</param>
    /// <param name="randomizePitch">Whether to randomize pitch.</param>
    /// <param name="range">Sound audible range in blocks.</param>
    /// <param name="volume">Sound volume multiplier.</param>
    void PlaySoundAt(AssetLocation sound, double x, double y, double z, IPlayer? sourcePlayer = null,
        bool randomizePitch = true, float range = 32f, float volume = 1f);

    /// <summary>
    /// Spawn particles at a specific position.
    /// </summary>
    /// <param name="particles">The particle properties.</param>
    /// <param name="pos">The position to spawn particles.</param>
    /// <param name="sourcePlayer">Optional source player.</param>
    void SpawnParticles(SimpleParticleProperties particles, Vec3d pos, IPlayer? sourcePlayer = null);

    /// <summary>
    /// Spawn an item entity in the world at the specified position.
    /// </summary>
    /// <param name="itemstack">The item stack to spawn.</param>
    /// <param name="position">The world position to spawn the item.</param>
    /// <param name="velocity">Optional initial velocity for the item entity.</param>
    void SpawnItemEntity(ItemStack itemstack, Vec3d position, Vec3d? velocity = null);

    /// <summary>
    /// Get a block accessor for reading or writing blocks.
    /// </summary>
    /// <param name="isWriteAccess">Whether write access is needed.</param>
    /// <param name="isRevertable">Whether changes should be revertable.</param>
    /// <returns>A block accessor instance.</returns>
    IBlockAccessor GetBlockAccessor(bool isWriteAccess, bool isRevertable);
}