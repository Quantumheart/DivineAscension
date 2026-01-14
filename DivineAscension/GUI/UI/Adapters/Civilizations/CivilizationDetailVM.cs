using System;
using System.Collections.Generic;

namespace DivineAscension.GUI.UI.Adapters.Civilizations;

internal sealed record MemberReligionDetailVM(
    string ReligionId,
    string ReligionName,
    string Domain,
    string FounderUID,
    string FounderName,
    int MemberCount,
    string DeityName
);

internal sealed record CivilizationDetailVM(
    string CivId,
    string Name,
    string FounderUID,
    string FounderName,
    string FounderReligionUID,
    string FounderReligionName,
    IReadOnlyList<MemberReligionDetailVM> MemberReligions,
    DateTime CreatedDate,
    string Icon,
    string Description
);