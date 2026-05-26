using System.Collections.Generic;
using DivineAscension.Network;

namespace DivineAscension.GUI.UI.Adapters.Bans;

/// <summary>
///     UI-only provider that supplies banned-player rows for the "This Order"
///     (player's own religion) Stricken-from-the-Ledger section. Lets the
///     section be styled and reviewed without a server.
/// </summary>
internal interface IBanListProvider
{
    IReadOnlyList<PlayerReligionInfoResponsePacket.BanInfo> GetBannedPlayers();
    void ConfigureDevSeed(int count, int seed);
    void Refresh();
}