using PantheonWars.Models.Enum;

namespace PantheonWars.Systems.Favor;

public interface IFavorTracker
{
    DeityType DeityType { get; }
    void Initialize();
}