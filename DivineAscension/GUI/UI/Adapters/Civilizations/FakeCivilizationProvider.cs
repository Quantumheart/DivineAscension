using System;
using System.Collections.Generic;
using DivineAscension.Models.Enum;

namespace DivineAscension.GUI.UI.Adapters.Civilizations;

/// <summary>
///     Dev-only, UI-only civilization provider that generates synthetic civilizations deterministically.
///     Uses adapter pattern via ICivilizationProvider so UIs can swap between real and fake data.
/// </summary>
internal sealed class FakeCivilizationProvider : ICivilizationProvider
{
    private static readonly string[] Prefixes =
    {
        "The", "United", "Grand", "Imperial", "Sacred", "Ancient", "Free",
        "Noble", "Eternal", "Golden", "Silver", "Divine", "Mystic", "Holy"
    };

    private static readonly string[] Types =
    {
        "Alliance", "Federation", "Empire", "Kingdom", "Republic", "Covenant",
        "Confederacy", "Union", "Assembly", "Order", "League", "Concord",
        "Coalition", "Dominion", "Realm", "Collective"
    };

    private static readonly string[] Modifiers =
    {
        "of the East", "of the West", "of the North", "of the South",
        "of Light", "of Stone", "of the Forge", "of the Wild",
        "of Dawn", "of Dusk", "of the Mountains", "of the Plains",
        "of the Seas", "of the Skies", "of Peace", "of Unity"
    };

    private static readonly string[] Icons =
    {
        "default", "congress", "byzantin-temple", "egyptian-temple", "granary",
        "huts-village", "indian-palace", "moai", "pagoda", "saint-basil-cathedral",
        "viking-church", "village", "scales", "yin-yang", "peace-dove",
        "freemasonry", "cursed-star"
    };

    private static readonly string[] Deities =
    {
        nameof(DeityDomain.Craft), nameof(DeityDomain.Wild),
        nameof(DeityDomain.Harvest), nameof(DeityDomain.Stone)
    };

    private static readonly string[] ReligionPrefixes =
    {
        "Followers of", "Temple of", "Order of", "Church of",
        "Disciples of", "Sect of", "Faithful of", "Brotherhood of"
    };

    private IReadOnlyList<CivilizationVM> _cache = Array.Empty<CivilizationVM>();
    private int _count = 25;
    private int _seed = 1337;

    public IReadOnlyList<CivilizationVM> GetCivilizations()
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
        var list = new List<CivilizationVM>(_count + 2);

        for (var i = 0; i < _count; i++)
        {
            var civId = GenerateGuid(rnd).ToString("N");
            var name = GenerateCivilizationName(rnd);
            var founderUID = GenerateGuid(rnd).ToString("N");
            var founderReligionUID = GenerateGuid(rnd).ToString("N");

            // Generate 1-4 member religions
            var memberCount = rnd.Next(1, 5);
            var memberDeities = new List<string>();
            var memberReligionNames = new List<string>();

            // Assign icon (distribute evenly)
            var icon = Icons[i % Icons.Length];

            for (var j = 0; j < memberCount; j++)
            {
                var deity = Deities[rnd.Next(Deities.Length)];
                var religionPrefix = ReligionPrefixes[rnd.Next(ReligionPrefixes.Length)];
                var religionName = $"{religionPrefix} {deity}";

                memberDeities.Add(deity);
                memberReligionNames.Add(religionName);
            }

            list.Add(new CivilizationVM(
                civId,
                name,
                founderUID,
                founderReligionUID,
                memberCount,
                memberDeities,
                memberReligionNames,
                icon
            ));
        }

        // Edge case 1: Solo civilization (1 member)
        list.Add(new CivilizationVM(
            GenerateGuid(rnd).ToString("N"),
            "The Lone Order of the Hermit",
            GenerateGuid(rnd).ToString("N"),
            GenerateGuid(rnd).ToString("N"),
            1,
            new List<string> { nameof(DeityDomain.Craft) },
            new List<string> { "Solitary Followers of Craft" },
            "default"
        ));

        // Edge case 2: Max members civilization (4 religions)
        list.Add(new CivilizationVM(
            GenerateGuid(rnd).ToString("N"),
            "The Great Unity of All Faiths",
            GenerateGuid(rnd).ToString("N"),
            GenerateGuid(rnd).ToString("N"),
            4,
            new List<string>
            {
                nameof(DeityDomain.Craft),
                nameof(DeityDomain.Wild),
                nameof(DeityDomain.Harvest),
                nameof(DeityDomain.Stone)
            },
            new List<string>
            {
                "Temple of Craft",
                "Church of Wild",
                "Order of Harvest",
                "Disciples of Stone"
            },
            "peace-dove"
        ));

        _cache = list;
    }

    private static string GenerateCivilizationName(Random rnd)
    {
        var prefix = Prefixes[rnd.Next(Prefixes.Length)];
        var type = Types[rnd.Next(Types.Length)];
        var modifier = Modifiers[rnd.Next(Modifiers.Length)];

        return $"{prefix} {type} {modifier}";
    }

    private static Guid GenerateGuid(Random rnd)
    {
        var bytes = new byte[16];
        rnd.NextBytes(bytes);
        return new Guid(bytes);
    }
}