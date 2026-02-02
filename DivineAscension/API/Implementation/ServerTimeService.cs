using System;
using DivineAscension.API.Interfaces;
using Vintagestory.API.Server;

namespace DivineAscension.API.Implementation;

/// <summary>
/// Server-side implementation of ITimeService that wraps IServerWorldAccessor.
/// Provides access to the game world's elapsed time for absolute timestamps.
/// </summary>
public class ServerTimeService : ITimeService
{
    private readonly IServerWorldAccessor _world;

    /// <summary>
    /// Initializes a new instance of ServerTimeService.
    /// </summary>
    /// <param name="world">The server world accessor providing time data</param>
    /// <exception cref="ArgumentNullException">Thrown if world is null</exception>
    public ServerTimeService(IServerWorldAccessor world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    /// <inheritdoc />
    public long ElapsedMilliseconds => _world.ElapsedMilliseconds;

    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
