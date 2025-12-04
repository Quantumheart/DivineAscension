using System;
using System.Collections.Generic;

namespace PantheonWars.GUI.UI.Adapters.ReligionMembers;

/// <summary>
/// Dev-only, UI-only member provider that generates synthetic members deterministically.
/// </summary>
internal sealed class FakeReligionMemberProvider : IReligionMemberProvider
{
    private int _count = 250;
    private int _seed = 1337;
    private IReadOnlyList<MemberVM> _cache = Array.Empty<MemberVM>();

    private static readonly string[] First =
    {
        "Ari","Kai","Niko","Mira","Sora","Lena","Theo","Rin","Eli","Nova",
        "Ira","Juno","Orin","Zara","Kade","Vera","Tess","Vale","Rhea","Cai"
    };

    private static readonly string[] Last =
    {
        "Stone","Reed","Ash","Vale","Rook","Quill","Frost","Wilde","Grove","Bluff",
        "Kestrel","March","Dawn","Flint","Hollow","Strand","Rowan","Lark","Moss","Ever"
    };

    private static readonly string[] Deities = { "Zephra", "Ignarus", "Noctis", "Solara", "Aquantis", "Terras", "Aether", "Umbra" };

    public IReadOnlyList<MemberVM> GetMembers(string? religionId = null)
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
        var list = new List<MemberVM>(_count + 1);
        for (int i = 0; i < _count; i++)
        {
            var uid = Guid.NewGuid().ToString("N");
            var name = $"{First[rnd.Next(First.Length)]} {Last[rnd.Next(Last.Length)]}";
            var deity = Deities[rnd.Next(Deities.Length)];
            var favor = Math.Round(rnd.NextDouble() * 200 - 50, 1); // -50..150
            var joined = DateTime.UtcNow.AddDays(-rnd.Next(0, 365));
            var online = rnd.NextDouble() < 0.35;
            list.Add(new MemberVM(uid, name, deity, favor, joined, online, true));
        }

        // A couple of edge cases
        list.Add(new MemberVM(Guid.NewGuid().ToString("N"), new string('X', 36), "Zephra", -25, DateTime.UtcNow, false, true));

        _cache = list;
    }
}
