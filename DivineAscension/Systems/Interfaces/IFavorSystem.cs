using System;
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
    ///     Awards favor for deity-aligned actions (extensible for future features)
    /// </summary>
    void AwardFavorForAction(IServerPlayer player, string actionType, int amount);

    /// <summary>
    ///     Awards a fractional amount of favor (supports fine-grained activities)
    /// </summary>
    void AwardFavorForAction(IServerPlayer player, string actionType, float amount);
}