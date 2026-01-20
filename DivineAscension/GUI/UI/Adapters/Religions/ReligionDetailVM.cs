using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DivineAscension.GUI.UI.Adapters.Religions;

[ExcludeFromCodeCoverage]
internal sealed record MemberDetailVM(
    string PlayerUID,
    string PlayerName,
    string FavorRank,
    int Favor
);

[ExcludeFromCodeCoverage]
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