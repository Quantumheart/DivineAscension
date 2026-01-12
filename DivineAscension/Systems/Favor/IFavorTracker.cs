using DivineAscension.Models.Enum;

namespace DivineAscension.Systems.Favor;

public interface IFavorTracker
{
    DeityDomain DeityDomain { get; }
    void Initialize();
}