using System.Collections.Generic;

namespace PantheonWars.GUI.UI.Adapters.ReligionMembers;

/// <summary>
/// UI-only data source for listing religion members. Intended for swapping
/// between a real provider and a dev-only fake provider without touching systems/persistence.
/// </summary>
internal interface IReligionMemberProvider
{
    IReadOnlyList<MemberVM> GetMembers(string? religionId = null);
    void ConfigureDevSeed(int count, int seed);
    void Refresh();
}
