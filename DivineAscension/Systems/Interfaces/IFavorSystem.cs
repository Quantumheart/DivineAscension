using System;
using DivineAscension.Models.Enum;
using Vintagestory.API.Server;

namespace DivineAscension.Systems.Interfaces;

/// <summary>
///     Interface for managing divine favor rewards and penalties
/// </summary>
public interface IFavorSystem : IDisposable
{
    /// <summary>
    ///     Initializes the favor system and hooks into game events
    /// </summary>
    void Initialize();

    /// <summary>
    ///     Awards favor for a deity-aligned action against the specified source domain.
    ///     Patron multiplier (1.5x) is applied inside the system when the player's religion
    ///     has <paramref name="sourceDomain"/> as its PatronDomain; otherwise 1.0x.
    /// </summary>
    void AwardFavorForAction(IServerPlayer player, string actionType, float amount, DeityDomain sourceDomain);

    /// <summary>
    ///     Awards favor by player UID (for async/delayed events) against the specified source domain.
    /// </summary>
    void AwardFavorForAction(string playerUid, string actionType, float amount, DeityDomain sourceDomain);

    /// <summary>
    ///     Queues favor for batched processing. Use this for high-frequency events like scythe harvesting
    ///     to avoid per-block overhead. Favor is accumulated and applied on the next game tick.
    /// </summary>
    void QueueFavorForAction(IServerPlayer player, string actionType, float amount, DeityDomain deityDomain);
}