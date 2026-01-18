using DivineAscension.API.Interfaces;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.BlessingEffects;

/// <summary>
///     Interface for special effect handlers that implement complex blessing behaviors
/// </summary>
public interface ISpecialEffectHandler
{
    /// <summary>
    ///     Unique identifier for this effect (e.g., "passive_tool_repair_1per5min")
    /// </summary>
    string EffectId { get; }

    /// <summary>
    ///     Initializes the effect handler (called once at system startup)
    /// </summary>
    void Initialize(ILogger logger, IEventService eventService, IWorldService worldService);

    /// <summary>
    ///     Activates the effect for a player (called when blessing is unlocked)
    /// </summary>
    void ActivateForPlayer(IServerPlayer player);

    /// <summary>
    ///     Deactivates the effect for a player (called when blessing is removed)
    /// </summary>
    void DeactivateForPlayer(IServerPlayer player);

    /// <summary>
    ///     Called periodically to update effect state (optional, for time-based effects)
    /// </summary>
    void OnTick(float deltaTime);
}