using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DivineAscension.Network;

namespace DivineAscension.GUI.UI.Adapters.Bans;

/// <summary>
///     Dev-only, UI-only ban provider that generates synthetic banned players
///     deterministically so the Stricken-from-the-Ledger section can be styled
///     and reviewed without a server.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeBanListProvider : IBanListProvider
{
    private static readonly string[] First =
    {
        "Aldric", "Brenna", "Corwin", "Dagmar", "Edric", "Freya",
        "Gareth", "Halla", "Ivo", "Jorund", "Kestrel", "Lyra"
    };

    private static readonly string[] Last =
    {
        "the Faithless", "Oathbreaker", "Coldhand", "of the Ash",
        "Two-Tongue", "the Profane", "Blackvow", "the Exiled"
    };

    private static readonly string[] Reasons =
    {
        "Defiled the altar",
        "Stole from the treasury",
        "Broke the blood-oath",
        "Heresy against the deity",
        "Betrayed the order to rivals",
        "Repeated desecration of holy sites",
        "Spread profane teachings"
    };

    private IReadOnlyList<PlayerReligionInfoResponsePacket.BanInfo> _cache =
        Array.Empty<PlayerReligionInfoResponsePacket.BanInfo>();

    private int _count = 5;
    private int _seed = 20260526;

    public IReadOnlyList<PlayerReligionInfoResponsePacket.BanInfo> GetBannedPlayers()
    {
        if (_cache.Count == 0) Regenerate();
        return _cache;
    }

    public void ConfigureDevSeed(int count, int seed)
    {
        _count = Math.Max(0, count);
        _seed = seed;
        Regenerate();
    }

    public void Refresh()
    {
        Regenerate();
    }

    private void Regenerate()
    {
        if (_count == 0)
        {
            _cache = Array.Empty<PlayerReligionInfoResponsePacket.BanInfo>();
            return;
        }

        var rnd = new Random(_seed);
        var list = new List<PlayerReligionInfoResponsePacket.BanInfo>(_count);
        for (var i = 0; i < _count; i++)
        {
            var name = $"{First[rnd.Next(First.Length)]} {Last[rnd.Next(Last.Length)]}";
            var bannedAt = DateTime.UtcNow.AddDays(-rnd.Next(1, 120));
            var isPermanent = rnd.NextDouble() < 0.4;
            var expiresAt = isPermanent ? (DateTime?)null : bannedAt.AddDays(rnd.Next(3, 60));

            list.Add(new PlayerReligionInfoResponsePacket.BanInfo
            {
                PlayerUID = Guid.NewGuid().ToString("N"),
                PlayerName = name,
                Reason = Reasons[rnd.Next(Reasons.Length)],
                BannedAt = bannedAt.ToString("yyyy-MM-dd HH:mm"),
                ExpiresAt = expiresAt?.ToString("yyyy-MM-dd HH:mm") ?? "Never",
                IsPermanent = isPermanent,
            });
        }

        _cache = list;
    }
}