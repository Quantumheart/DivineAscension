using System;
using System.Collections.Generic;
using PantheonWars.Models.Enum;

namespace PantheonWars.GUI.UI.Adapters.Religions;

/// <summary>
///     Dev-only, UI-only religion provider that generates synthetic religions deterministically.
///     Uses adapter pattern via IReligionProvider so UIs can swap between real and fake data.
/// </summary>
internal sealed class FakeReligionProvider : IReligionProvider
{
    private static readonly string[] First =
    {
        "Ari", "Kai", "Niko", "Mira", "Sora", "Lena", "Theo", "Rin", "Eli", "Nova",
        "Ira", "Juno", "Orin", "Zara", "Kade", "Vera", "Tess", "Vale", "Rhea", "Cai"
    };

    private static readonly string[] Last =
    {
        "Stone", "Reed", "Ash", "Vale", "Rook", "Quill", "Frost", "Wilde", "Grove", "Bluff",
        "Kestrel", "March", "Dawn", "Flint", "Hollow", "Strand", "Rowan", "Lark", "Moss", "Ever"
    };

    private static readonly string[] Deities =
        { nameof(DeityType.Khoras), nameof(DeityType.Lysa), nameof(DeityType.Aethra), nameof(DeityType.Gaia) };

    private IReadOnlyList<ReligionVM> _cache = Array.Empty<ReligionVM>();
    private int _count = 250;
    private int _seed = 1337;

    public IReadOnlyList<ReligionVM> GetReligions()
    {
        if (_cache.Count != _count) Regenerate();
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
        var rnd = new Random(_seed);
        var list = new List<ReligionVM>(_count + 1);
        for (var i = 0; i < _count; i++)
        {
            var uid = GenerateGuid(rnd).ToString("N");
            var name = $"{First[rnd.Next(First.Length)]} {Last[rnd.Next(Last.Length)]}";
            var deity = Deities[rnd.Next(Deities.Length)];
            var memberCount = rnd.Next(0, 500);
            var prestige = rnd.Next(0, 20000);
            var prestigeRank = GetPrestigeRank(prestige);
            var isPublic = rnd.NextDouble() < 0.7; // majority public for browse
            var founderUid = GenerateGuid(rnd).ToString("N");
            var description = GenerateDescription(rnd, name, deity, memberCount);
            list.Add(new ReligionVM(uid, name, deity, memberCount, prestige, prestigeRank, isPublic, founderUid, description));
        }

        // A couple of edge cases
        list.Add(new ReligionVM(
            GenerateGuid(rnd).ToString("N"),
            new string('X', 36), // very long name
            "Zephra",
            0, // no members
            0, // no prestige
            "Unranked",
            true,
            GenerateGuid(rnd).ToString("N"),
            "A newly formed sect still seeking its first followers."));

        _cache = list;
    }

    private static string GetPrestigeRank(int prestige)
    {
        return prestige switch
        {
            < 250 => "Unranked",
            < 1000 => "Bronze",
            < 3000 => "Silver",
            < 7000 => "Gold",
            < 12000 => "Platinum",
            < 18000 => "Diamond",
            _ => "Mythic"
        };
    }

    private static string GenerateDescription(Random rnd, string name, string deity, int members)
    {
        var phrases = new[]
        {
            "ancient rites",
            "forgotten songs",
            "sunlit groves",
            "hidden caverns",
            "storm-wrought altars",
            "whispered omens",
            "celestial patterns",
            "river-born blessings",
            "ember-lit vigils",
            "moon-touched paths"
        };

        var p1 = phrases[rnd.Next(phrases.Length)];
        var p2 = phrases[rnd.Next(phrases.Length)];
        while (p2 == p1) p2 = phrases[rnd.Next(phrases.Length)];

        return $"{name} venerates {deity} through {p1} and {p2}. Currently {members} faithful walk its way.";
    }

    private static Guid GenerateGuid(Random rnd)
    {
        var bytes = new byte[16];
        rnd.NextBytes(bytes);
        return new Guid(bytes);
    }
}