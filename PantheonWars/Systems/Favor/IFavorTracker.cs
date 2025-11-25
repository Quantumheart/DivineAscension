using PantheonWars.Models.Enum;

namespace PantheonWars.Systems.Favor;

public interface IFavorTracker
{
    void Initialize();
    DeityType DeityType { get; }   
}