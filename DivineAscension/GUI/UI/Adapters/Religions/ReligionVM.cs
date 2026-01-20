using System.Diagnostics.CodeAnalysis;

namespace DivineAscension.GUI.UI.Adapters.Religions;

[ExcludeFromCodeCoverage]
internal sealed record ReligionVM(
    string religionUID,
    string religionName,
    string deity,
    string deityName,
    int memberCount,
    int prestige,
    string prestigeRank,
    bool isPublic,
    string founderUID,
    string description
);