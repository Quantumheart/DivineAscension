using System;
using System.Diagnostics.CodeAnalysis;

namespace DivineAscension.GUI.UI.Adapters.ReligionMembers;

[ExcludeFromCodeCoverage]
internal sealed record MemberVM(
    string PlayerUid,
    string DisplayName,
    string DeityCode,
    double Favor,
    DateTime JoinedAtUtc,
    bool IsOnline,
    bool IsSynthetic
);