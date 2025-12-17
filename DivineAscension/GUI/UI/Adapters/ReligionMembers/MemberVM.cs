using System;

namespace DivineAscension.GUI.UI.Adapters.ReligionMembers;

internal sealed record MemberVM(
    string PlayerUid,
    string DisplayName,
    string DeityCode,
    double Favor,
    DateTime JoinedAtUtc,
    bool IsOnline,
    bool IsSynthetic
);