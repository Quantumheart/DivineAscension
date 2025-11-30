using System;
using Vintagestory.API.Server;

namespace PantheonWars.Systems.Interfaces;

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
}
