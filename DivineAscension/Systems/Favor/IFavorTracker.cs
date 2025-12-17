using DivineAscension.Models.Enum;

namespace DivineAscension.Systems.Favor;

public interface IFavorTracker
{
    DeityType DeityType { get; }
    void Initialize();
}