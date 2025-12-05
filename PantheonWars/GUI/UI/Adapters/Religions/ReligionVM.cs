using System;

namespace PantheonWars.GUI.UI.Adapters.Religions;

internal sealed record ReligionVM(
    string religionUID,
    string religionName,
    string deity,
    int memberCount,
    int prestige,
    string prestigeRank,
    bool isPublic,
    string founderUID,
    string description
);