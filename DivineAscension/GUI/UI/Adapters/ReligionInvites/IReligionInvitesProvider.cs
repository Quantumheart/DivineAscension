using System.Collections.Generic;
using DivineAscension.GUI.Models.Religion.Invites;

namespace DivineAscension.GUI.UI.Adapters.ReligionInvites;

/// <summary>
///     UI-only data source for pending religion invitations ("Letters").
///     Lets the Letters chapter swap between real network data and a dev-only
///     fake provider without touching systems/persistence.
/// </summary>
internal interface IReligionInvitesProvider
{
    IReadOnlyList<InviteData> GetInvites();
    void ConfigureDevSeed(int count, int seed);
    void Refresh();
}
