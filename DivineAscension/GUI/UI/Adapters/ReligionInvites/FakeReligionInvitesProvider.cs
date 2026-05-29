using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DivineAscension.GUI.Models.Religion.Invites;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.UI.Adapters.ReligionInvites;

/// <summary>
///     Dev-only, UI-only invites provider that generates synthetic Letters
///     deterministically so the Letters chapter can be styled and reviewed
///     without a server.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeReligionInvitesProvider : IReligionInvitesProvider
{
    private static readonly string[] OrderNames =
    {
        "Order of the Forge",
        "Wildwood Covenant",
        "Brotherhood of the Furrow",
        "Crown of the Cairn",
        "Sons of the Sundered Banner",
        "Council of the Patient Hammer",
        "Sisters of the Green Hush",
        "Vigil of the First Harvest",
        "Stoneblood Reliquary",
        "Wardens of the Last Field"
    };

    private static readonly string[] Descriptions =
    {
        "We temper iron and will alike; join us at the great anvil.",
        "The wild remembers those who walk gently. Walk with us.",
        "", // exercises the default-quote fallback
        "Beneath the cairn our oaths are kept and our dead keep watch.",
        "A long missive that runs well past the comfortable single line so the truncation and ellipsis behaviour can be reviewed in the styled preview without a server."
    };

    // Mirrors the domains a player can actually worship (Caravan appears only
    // when its feature flag is on) — sourced from the SST (#558).
    private static readonly DeityDomain[] Domains = DeityDomains.Selectable.ToArray();

    private IReadOnlyList<InviteData> _cache = Array.Empty<InviteData>();
    private int _count = 4;
    private int _seed = 20260521;

    public IReadOnlyList<InviteData> GetInvites()
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
            _cache = Array.Empty<InviteData>();
            return;
        }

        var rnd = new Random(_seed);
        var list = new List<InviteData>(_count);
        for (var i = 0; i < _count; i++)
        {
            var inviteId = Guid.NewGuid().ToString("N");
            var name = OrderNames[i % OrderNames.Length];
            var domain = Domains[rnd.Next(Domains.Length)];
            var expiresAt = DateTime.UtcNow.AddDays(rnd.Next(1, 14));
            var description = Descriptions[i % Descriptions.Length];
            list.Add(new InviteData(inviteId, name, expiresAt, domain, description));
        }

        _cache = list;
    }
}
