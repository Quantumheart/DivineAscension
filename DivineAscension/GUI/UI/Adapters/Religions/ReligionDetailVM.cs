using System.Collections.Generic;

namespace DivineAscension.GUI.UI.Adapters.Religions;

internal sealed record MemberDetailVM(
    string PlayerUID,
    string PlayerName,
    string FavorRank,
    int Favor
);

internal sealed record ReligionDetailVM(
    string ReligionUID,
    string ReligionName,
    string Deity,
    string DeityName,
    string Description,
    int Prestige,
    string PrestigeRank,
    bool IsPublic,
    string FounderUID,
    string FounderName,
    IReadOnlyList<MemberDetailVM> Members
);